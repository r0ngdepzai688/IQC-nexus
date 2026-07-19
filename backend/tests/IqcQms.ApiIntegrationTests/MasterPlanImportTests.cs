using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IqcQms.Domain.Entities.NewModels;
using IqcQms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IqcQms.ApiIntegrationTests;

[Trait("Category", "Integration")]
public sealed class MasterPlanImportTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private static readonly string[] Headers = ["Project Name", "SKU", "PVR Target", "Area", "Grade", "HW PIC"];

    [Theory]
    [InlineData("inspect")]
    [InlineData("upload")]
    [InlineData("review")]
    [InlineData("commit")]
    public async Task AnonymousCriticalImportRequestsReturnUnauthorized(string operation)
    {
        using var request = operation switch
        {
            "inspect" => WorkbookRequest("/api/DataHub/inspect-headers", MasterPlanWorkbook.Create(Headers, Row(UniqueSku()))),
            "upload" => WorkbookRequest("/api/DataHub/upload", MasterPlanWorkbook.Create(Headers, Row(UniqueSku()))),
            "review" => new HttpRequestMessage(HttpMethod.Get, "/api/DataHub/review/unknown-batch"),
            _ => new HttpRequestMessage(HttpMethod.Post, "/api/DataHub/commit/unknown-batch"),
        };

        using var response = await factory.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedHeaderInspectionReturnsFrontendContract()
    {
        using var request = WorkbookRequest("/api/DataHub/inspect-headers", MasterPlanWorkbook.Create(Headers, Row(UniqueSku())));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync());

        using var response = await factory.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal(JsonValueKind.Number, body.RootElement.GetProperty("headerRow").ValueKind);
        Assert.Equal(JsonValueKind.Array, body.RootElement.GetProperty("columns").ValueKind);
        Assert.Contains("ProjectName", body.RootElement.GetProperty("requiredFields").EnumerateArray().Select(value => value.GetString()));
        Assert.Contains("SKU", body.RootElement.GetProperty("requiredFields").EnumerateArray().Select(value => value.GetString()));
        Assert.Contains("PVRTarget", body.RootElement.GetProperty("requiredFields").EnumerateArray().Select(value => value.GetString()));
    }

    [Fact]
    public async Task InvalidWorkbookReturnsProblemDetailsAndMissingColumnsReturnFailedBatch()
    {
        var token = await LoginAsync();
        using (var invalidRequest = WorkbookRequest("/api/DataHub/upload", "not-an-excel-workbook"u8.ToArray()))
        {
            invalidRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var invalidResponse = await factory.Client.SendAsync(invalidRequest);
            Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
            using var problem = await JsonDocument.ParseAsync(await invalidResponse.Content.ReadAsStreamAsync());
            Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
            Assert.Equal(JsonValueKind.String, problem.RootElement.GetProperty("detail").ValueKind);
        }

        using var missingRequest = WorkbookRequest("/api/DataHub/upload", MasterPlanWorkbook.Create(["Project Name"], ["Missing SKU"]));
        missingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var missingResponse = await factory.Client.SendAsync(missingRequest);
        Assert.Equal(HttpStatusCode.OK, missingResponse.StatusCode);
        using var batch = await JsonDocument.ParseAsync(await missingResponse.Content.ReadAsStreamAsync());
        Assert.Equal("Failed - Missing Columns", batch.RootElement.GetProperty("status").GetString());
        Assert.Equal(0, batch.RootElement.GetProperty("totalRows").GetInt32());
    }

    [Fact]
    public async Task DuplicateFileReturnsConflictProblemDetails()
    {
        var token = await LoginAsync();
        var workbook = MasterPlanWorkbook.Create(Headers, Row(UniqueSku()));
        using (var first = WorkbookRequest("/api/DataHub/upload", workbook))
        {
            first.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var firstResponse = await factory.Client.SendAsync(first);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        }

        using var duplicate = WorkbookRequest("/api/DataHub/upload", workbook);
        duplicate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await factory.Client.SendAsync(duplicate);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var problem = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal(409, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Contains("Duplicate file", problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ReviewShapeIsUsableAndBlockingRowsRejectCommit()
    {
        var token = await LoginAsync();
        var batchId = await UploadAsync(token, MasterPlanWorkbook.Create(Headers, Row(UniqueSku())));

        using var reviewRequest = Authorized(HttpMethod.Get, $"/api/DataHub/review/{batchId}", token);
        using var reviewResponse = await factory.Client.SendAsync(reviewRequest);
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
        using var review = await JsonDocument.ParseAsync(await reviewResponse.Content.ReadAsStreamAsync());
        foreach (var property in new[] { "batchId", "fileName", "validRows", "warningRows", "errorRows", "existingSkuConflicts", "skippedRows", "rows" })
            Assert.True(review.RootElement.TryGetProperty(property, out _), $"Review response is missing '{property}'.");
        var row = Assert.Single(review.RootElement.GetProperty("rows").EnumerateArray());
        foreach (var property in new[] { "rowNumber", "sku", "field", "currentValue", "severity", "message", "status", "conflictType", "reviewItemId", "supportedActions" })
            Assert.True(row.TryGetProperty(property, out _), $"Review row is missing '{property}'.");

        using var commitRequest = Authorized(HttpMethod.Post, $"/api/DataHub/commit/{batchId}", token);
        using var commitResponse = await factory.Client.SendAsync(commitRequest);
        Assert.Equal(HttpStatusCode.BadRequest, commitResponse.StatusCode);
        Assert.Contains("blocking", await commitResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExplicitWarningResolutionCommitsAndPublishesNewMasterPlan()
    {
        var token = await LoginAsync();
        var sku = UniqueSku();
        var batchId = await UploadAsync(token, MasterPlanWorkbook.Create(Headers, Row(sku)));
        await ResolveWarningsAsync(batchId, token);

        using var commitRequest = Authorized(HttpMethod.Post, $"/api/DataHub/commit/{batchId}", token);
        using var commitResponse = await factory.Client.SendAsync(commitRequest);
        Assert.Equal(HttpStatusCode.OK, commitResponse.StatusCode);
        using var committed = await JsonDocument.ParseAsync(await commitResponse.Content.ReadAsStreamAsync());
        Assert.Equal(1, committed.RootElement.GetProperty("createdRecords").GetInt32());
        Assert.Equal(0, committed.RootElement.GetProperty("updatedRecords").GetInt32());

        using var recordsRequest = Authorized(HttpMethod.Get, "/api/masterplan/records", token);
        using var recordsResponse = await factory.Client.SendAsync(recordsRequest);
        Assert.Equal(HttpStatusCode.OK, recordsResponse.StatusCode);
        using var records = await JsonDocument.ParseAsync(await recordsResponse.Content.ReadAsStreamAsync());
        Assert.Contains(records.RootElement.EnumerateArray(), item => item.GetProperty("sku").GetString() == sku);
    }

    [Fact]
    public async Task ExistingSkuSkipNeverOverwritesCoreRecordAndCommitsOnlyNewRow()
    {
        var token = await LoginAsync();
        var existingSku = UniqueSku();
        var newSku = UniqueSku();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.MasterPlans.Add(new MasterPlan { ProjectName = "Protected original", Sku = existingSku, ActionStatus = "Ready" });
            await context.SaveChangesAsync();
        }
        var workbook = MasterPlanWorkbook.Create(Headers, Row(existingSku, "Attempted overwrite"), Row(newSku));
        var batchId = await UploadAsync(token, workbook);

        using (var reviewRequest = Authorized(HttpMethod.Get, $"/api/DataHub/review/{batchId}", token))
        using (var reviewResponse = await factory.Client.SendAsync(reviewRequest))
        {
            Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
            using var review = await JsonDocument.ParseAsync(await reviewResponse.Content.ReadAsStreamAsync());
            var conflict = Assert.Single(review.RootElement.GetProperty("rows").EnumerateArray(), row =>
                row.GetProperty("sku").GetString() == existingSku);
            Assert.Equal("ExistingSku", conflict.GetProperty("conflictType").GetString());
        }

        using (var skipRequest = Authorized(HttpMethod.Post, $"/api/DataHub/resolve-existing/{batchId}", token, new { resolution = "Skip" }))
        using (var skipResponse = await factory.Client.SendAsync(skipRequest))
            Assert.Equal(HttpStatusCode.OK, skipResponse.StatusCode);
        await ResolveWarningsAsync(batchId, token);
        using (var commitRequest = Authorized(HttpMethod.Post, $"/api/DataHub/commit/{batchId}", token))
        using (var commitResponse = await factory.Client.SendAsync(commitRequest))
            Assert.Equal(HttpStatusCode.OK, commitResponse.StatusCode);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Protected original", (await verification.MasterPlans.AsNoTracking().SingleAsync(value => value.Sku == existingSku)).ProjectName);
        Assert.True(await verification.MasterPlans.AsNoTracking().AnyAsync(value => value.Sku == newSku));
    }

    [Fact]
    public async Task ConcurrentExistingSkuRejectsCommitWithoutOverwritingCoreRecord()
    {
        var token = await LoginAsync();
        var sku = UniqueSku();
        var batchId = await UploadAsync(token, MasterPlanWorkbook.Create(Headers, Row(sku, "Attempted replacement")));
        await ResolveWarningsAsync(batchId, token);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.MasterPlans.Add(new MasterPlan { ProjectName = "Concurrent protected original", Sku = sku, ActionStatus = "Ready" });
            await context.SaveChangesAsync();
        }

        using var commitRequest = Authorized(HttpMethod.Post, $"/api/DataHub/commit/{batchId}", token);
        using var commitResponse = await factory.Client.SendAsync(commitRequest);
        Assert.Equal(HttpStatusCode.BadRequest, commitResponse.StatusCode);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Concurrent protected original", (await verification.MasterPlans.AsNoTracking().SingleAsync(value => value.Sku == sku)).ProjectName);
    }

    private async Task ResolveWarningsAsync(string batchId, string token)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var reviewRequest = Authorized(HttpMethod.Get, $"/api/DataHub/review/{batchId}", token);
            using var reviewResponse = await factory.Client.SendAsync(reviewRequest);
            using var review = await JsonDocument.ParseAsync(await reviewResponse.Content.ReadAsStreamAsync());
            var warning = review.RootElement.GetProperty("rows").EnumerateArray().FirstOrDefault(row =>
                row.GetProperty("severity").GetString() == "Warning" &&
                row.GetProperty("conflictType").GetString() != "ExistingSku");
            if (warning.ValueKind == JsonValueKind.Undefined) return;
            var reviewItemId = warning.GetProperty("reviewItemId");
            using var request = reviewItemId.ValueKind == JsonValueKind.Number
                ? Authorized(HttpMethod.Post, $"/api/DataHub/resolve-review/{reviewItemId.GetInt32()}", token, new { action = "Override" })
                : Authorized(HttpMethod.Post, $"/api/DataHub/resolve-warning/{batchId}/{warning.GetProperty("rowNumber").GetInt32()}", token, new { resolution = "Accept" });
            using var response = await factory.Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        throw new InvalidOperationException("Review resolution did not converge within 20 actions.");
    }

    private async Task<string> UploadAsync(string token, byte[] workbook)
    {
        using var request = WorkbookRequest("/api/DataHub/upload", workbook);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await factory.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return body.RootElement.GetProperty("batchId").GetString()!;
    }

    private async Task<string> LoginAsync()
    {
        using var response = await factory.Client.PostAsJsonAsync("/api/auth/login", new { username = "SYN-0001", password = factory.SeedPassword });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return body.RootElement.GetProperty("token").GetString()!;
    }

    private static HttpRequestMessage WorkbookRequest(string path, byte[] workbook)
    {
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(workbook), "file", $"contract-{Guid.NewGuid():N}.xlsx");
        return new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string path, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }

    private static string[] Row(string sku, string projectName = "Synthetic import") => [projectName, sku, "2026-08-01", "Synthetic Area", "A", "Synthetic PIC"];
    private static string UniqueSku() => $"SYN-MP-{Guid.NewGuid():N}";
}
