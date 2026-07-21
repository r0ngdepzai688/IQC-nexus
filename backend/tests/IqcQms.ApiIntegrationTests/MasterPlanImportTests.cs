using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IqcQms.Application;
using IqcQms.Domain.Entities.NewModels;
using IqcQms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IqcQms.ApiIntegrationTests;

[Trait("Category", "Integration")]
public sealed class MasterPlanImportTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private static readonly string[] Headers = ["Project Name", "Basic", "Grade", "Cat", "PVR Target", "Area", "HW PIC"];

    [Theory]
    [InlineData("inspect")]
    [InlineData("upload")]
    [InlineData("review")]
    [InlineData("commit")]
    public async Task AnonymousCriticalImportRequestsReturnUnauthorized(string operation)
    {
        using var request = operation switch
        {
            "inspect" => WorkbookRequest("/api/DataHub/inspect-headers", MasterPlanWorkbook.Create(Headers, Row(UniqueBasic()))),
            "upload" => WorkbookRequest("/api/DataHub/upload", MasterPlanWorkbook.Create(Headers, Row(UniqueBasic()))),
            "review" => new HttpRequestMessage(HttpMethod.Get, "/api/DataHub/review/unknown-batch"),
            _ => new HttpRequestMessage(HttpMethod.Post, "/api/DataHub/commit/unknown-batch"),
        };

        using var response = await factory.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedHeaderInspectionReturnsFrontendContract()
    {
        using var request = WorkbookRequest("/api/DataHub/inspect-headers", MasterPlanWorkbook.Create(Headers, Row(UniqueBasic())));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync());

        using var response = await factory.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal(JsonValueKind.Number, body.RootElement.GetProperty("headerRow").ValueKind);
        Assert.Equal(JsonValueKind.Array, body.RootElement.GetProperty("columns").ValueKind);
        Assert.Contains("ProjectName", body.RootElement.GetProperty("requiredFields").EnumerateArray().Select(value => value.GetString()));
        Assert.Contains("Basic", body.RootElement.GetProperty("requiredFields").EnumerateArray().Select(value => value.GetString()));
        Assert.Contains("Grade", body.RootElement.GetProperty("requiredFields").EnumerateArray().Select(value => value.GetString()));
        Assert.Contains("Cat", body.RootElement.GetProperty("requiredFields").EnumerateArray().Select(value => value.GetString()));
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

        using var missingRequest = WorkbookRequest("/api/DataHub/upload", MasterPlanWorkbook.Create(["Project Name"], ["Missing fields"]));
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
        var workbook = MasterPlanWorkbook.Create(Headers, Row(UniqueBasic()));
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
        var batchId = await UploadAsync(token, MasterPlanWorkbook.Create(Headers, Row(UniqueBasic())));

        using var reviewRequest = Authorized(HttpMethod.Get, $"/api/DataHub/review/{batchId}", token);
        using var reviewResponse = await factory.Client.SendAsync(reviewRequest);
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
        using var review = await JsonDocument.ParseAsync(await reviewResponse.Content.ReadAsStreamAsync());
        foreach (var property in new[] { "batchId", "fileName", "validRows", "warningRows", "errorRows", "existingBusinessKeyConflicts", "readyToUpdateRows", "noChangeRows", "skippedRows", "rows" })
            Assert.True(review.RootElement.TryGetProperty(property, out _), $"Review response is missing '{property}'.");
        var row = Assert.Single(review.RootElement.GetProperty("rows").EnumerateArray());
        foreach (var property in new[] { "rowNumber", "basic", "cat", "field", "oldValue", "newValue", "severity", "message", "status", "conflictType", "reviewItemId", "supportedActions" })
            Assert.True(row.TryGetProperty(property, out _), $"Review row is missing '{property}'.");

        using var commitRequest = Authorized(HttpMethod.Post, $"/api/DataHub/commit/{batchId}", token);
        using var commitResponse = await factory.Client.SendAsync(commitRequest);
        Assert.Equal(HttpStatusCode.BadRequest, commitResponse.StatusCode);
        Assert.Contains("blocking", await commitResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BlankBasicAndNormalizedDuplicateBusinessKeysAreValidationErrors()
    {
        var token = await LoginAsync();
        var blankBatch = await UploadAsync(token, MasterPlanWorkbook.Create(Headers, Row(string.Empty)));
        using (var preview = Authorized(HttpMethod.Get, $"/api/DataHub/preview/{blankBatch}", token))
        using (var response = await factory.Client.SendAsync(preview))
        {
            using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            Assert.Equal(1, body.RootElement.GetProperty("errorRows").GetInt32());
        }

        var basic = UniqueBasic();
        var duplicateBatch = await UploadAsync(token, MasterPlanWorkbook.Create(Headers, Row($"  {basic}  "), Row(basic.ToLowerInvariant())));
        using var duplicatePreview = Authorized(HttpMethod.Get, $"/api/DataHub/preview/{duplicateBatch}", token);
        using var duplicateResponse = await factory.Client.SendAsync(duplicatePreview);
        using var duplicateBody = await JsonDocument.ParseAsync(await duplicateResponse.Content.ReadAsStreamAsync());
        Assert.Equal(2, duplicateBody.RootElement.GetProperty("errorRows").GetInt32());
    }

    [Fact]
    public async Task PartiallyPopulatedBusinessRowIsStagedAndReportsEveryMissingRequiredField()
    {
        var token = await LoginAsync();
        var batchId = await UploadAsync(token, MasterPlanWorkbook.Create(Headers, ["", UniqueBasic(), "", "", "", "Synthetic Area", ""]));

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var batch = await context.ImportBatches.AsNoTracking().SingleAsync(value => value.BatchId == batchId);
        var errors = await context.ValidationErrors.AsNoTracking()
            .Where(value => value.BatchId == batchId)
            .Select(value => value.FieldName)
            .ToListAsync();

        Assert.Equal(1, batch.TotalRows);
        Assert.Equal(1, batch.ErrorRows);
        Assert.Contains("ProjectName", errors);
        Assert.Contains("Grade", errors);
        Assert.Contains("Cat", errors);
    }

    [Fact]
    public async Task SameBasicWithDifferentCatIsAllowed()
    {
        var token = await LoginAsync();
        var basic = UniqueBasic();
        var second = Row(basic);
        second[3] = "LQV";
        var batchId = await UploadAsync(token, MasterPlanWorkbook.Create(Headers, Row(basic), second));
        await ResolveWarningsAsync(batchId, token);
        using var commitRequest = Authorized(HttpMethod.Post, $"/api/DataHub/commit/{batchId}", token);
        using var commitResponse = await factory.Client.SendAsync(commitRequest);
        Assert.Equal(HttpStatusCode.OK, commitResponse.StatusCode);
        using var committed = await JsonDocument.ParseAsync(await commitResponse.Content.ReadAsStreamAsync());
        Assert.Equal(2, committed.RootElement.GetProperty("createdRecords").GetInt32());
    }

    [Fact]
    public async Task PersistedKeysUseTheExactRuntimeCanonicalNormalizer()
    {
        var token = await LoginAsync();
        var rawBasic = "\t\uFF2D\uFF4F\uFF44\uFF45\uFF4C\u00A0  One\t";
        var row = Row(rawBasic);
        row[3] = "\u00A0lpr\t";
        var batchId = await UploadAsync(token, MasterPlanWorkbook.Create(Headers, row));
        await ResolveWarningsAsync(batchId, token);
        using var commit = Authorized(HttpMethod.Post, $"/api/DataHub/commit/{batchId}", token);
        using var response = await factory.Client.SendAsync(commit);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await context.MasterPlans.AsNoTracking().SingleAsync(value => value.LastImportBatchId == batchId);
        Assert.Equal(MasterPlanBusinessKey.NormalizeBasic(rawBasic), persisted.BasicKey);
        Assert.Equal(MasterPlanBusinessKey.NormalizeCat(row[3]), persisted.CatKey);
    }

    [Fact]
    public async Task ExplicitWarningResolutionCommitsAndPublishesNewMasterPlan()
    {
        var token = await LoginAsync();
        var basic = UniqueBasic();
        var batchId = await UploadAsync(token, MasterPlanWorkbook.Create(Headers, Row(basic)));
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
        Assert.Contains(records.RootElement.EnumerateArray(), item => item.GetProperty("basic").GetString() == basic);
    }

    [Fact]
    public async Task ExistingBusinessKeySkipNeverOverwritesCoreRecordAndCommitsOnlyNewRow()
    {
        var token = await LoginAsync();
        var existingBasic = UniqueBasic();
        var newBasic = UniqueBasic();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.MasterPlans.Add(new MasterPlan { ProjectName = "Protected original", Basic = existingBasic, BasicKey = existingBasic.ToUpperInvariant(), Grade = "B", Cat = "LPR", CatKey = "LPR", ActionStatus = "Ready" });
            await context.SaveChangesAsync();
        }
        var workbook = MasterPlanWorkbook.Create(Headers, Row(existingBasic, "Attempted overwrite"), Row(newBasic));
        var batchId = await UploadAsync(token, workbook);

        using (var reviewRequest = Authorized(HttpMethod.Get, $"/api/DataHub/review/{batchId}", token))
        using (var reviewResponse = await factory.Client.SendAsync(reviewRequest))
        {
            Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
            using var review = await JsonDocument.ParseAsync(await reviewResponse.Content.ReadAsStreamAsync());
            var conflict = review.RootElement.GetProperty("rows").EnumerateArray().First(row =>
                row.GetProperty("basic").GetString() == existingBasic && row.GetProperty("conflictType").GetString() == "ExistingBusinessKey");
            Assert.Equal("ExistingBusinessKey", conflict.GetProperty("conflictType").GetString());
        }

        using (var skipRequest = Authorized(HttpMethod.Post, $"/api/DataHub/resolve-existing-business-key/{batchId}", token, new { resolution = "Skip" }))
        using (var skipResponse = await factory.Client.SendAsync(skipRequest))
            Assert.Equal(HttpStatusCode.OK, skipResponse.StatusCode);
        await ResolveWarningsAsync(batchId, token);
        using (var commitRequest = Authorized(HttpMethod.Post, $"/api/DataHub/commit/{batchId}", token))
        using (var commitResponse = await factory.Client.SendAsync(commitRequest))
            Assert.Equal(HttpStatusCode.OK, commitResponse.StatusCode);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Protected original", (await verification.MasterPlans.AsNoTracking().SingleAsync(value => value.BasicKey == existingBasic.ToUpperInvariant())).ProjectName);
        Assert.True(await verification.MasterPlans.AsNoTracking().AnyAsync(value => value.BasicKey == newBasic.ToUpperInvariant()));
    }

    [Fact]
    public async Task ExistingBusinessKeyUpdateRequiresReviewAndAuditsChangedField()
    {
        var token = await LoginAsync();
        var basic = UniqueBasic();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.MasterPlans.Add(new MasterPlan { ProjectName = "Original", Basic = basic, BasicKey = basic.ToUpperInvariant(), Grade = "B", Cat = "LPR", CatKey = "LPR", Area = "Synthetic Area", HwPic = "Synthetic PIC", PvrTargetDate = new DateTime(2026, 8, 1), ActionStatus = "Ready" });
            await context.SaveChangesAsync();
        }
        var batchId = await UploadAsync(token, MasterPlanWorkbook.Create(Headers, Row(basic, "Reviewed update")));
        using (var reviewRequest = Authorized(HttpMethod.Get, $"/api/DataHub/review/{batchId}", token))
        using (var reviewResponse = await factory.Client.SendAsync(reviewRequest))
        {
            using var review = await JsonDocument.ParseAsync(await reviewResponse.Content.ReadAsStreamAsync());
            Assert.Contains(review.RootElement.GetProperty("rows").EnumerateArray(), value => value.GetProperty("conflictType").GetString() == "ExistingBusinessKey" && value.GetProperty("field").GetString() == "ProjectName");
        }
        using (var updateRequest = Authorized(HttpMethod.Post, $"/api/DataHub/resolve-existing-business-key/{batchId}", token, new { resolution = "Update" }))
        using (var updateResponse = await factory.Client.SendAsync(updateRequest)) Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        await ResolveWarningsAsync(batchId, token);
        using (var commitRequest = Authorized(HttpMethod.Post, $"/api/DataHub/commit/{batchId}", token))
        using (var commitResponse = await factory.Client.SendAsync(commitRequest)) Assert.Equal(HttpStatusCode.OK, commitResponse.StatusCode);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Reviewed update", (await verification.MasterPlans.AsNoTracking().SingleAsync(value => value.BasicKey == basic.ToUpperInvariant())).ProjectName);
        Assert.True(await verification.DataHubAuditLogs.AnyAsync(value => value.BatchId == batchId && value.FieldName == "ProjectName" && value.Reason == "Master Plan Import"));
    }

    [Fact]
    public async Task ExistingBusinessKeyWithNoMutableChangesSkipsWithoutUpdate()
    {
        var token = await LoginAsync();
        var basic = UniqueBasic();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.MasterPlans.Add(new MasterPlan { ProjectName = "Synthetic import", Basic = basic, BasicKey = basic.ToUpperInvariant(), Grade = "B", Cat = "LPR", CatKey = "LPR", Area = "Synthetic Area", HwPic = "Synthetic PIC", PvrTargetDate = new DateTime(2026, 8, 1), ActionStatus = "Ready" });
            await context.SaveChangesAsync();
        }
        var batchId = await UploadAsync(token, MasterPlanWorkbook.Create(Headers, Row(basic)));
        await ResolveWarningsAsync(batchId, token);
        using var reviewRequest = Authorized(HttpMethod.Get, $"/api/DataHub/review/{batchId}", token);
        using var reviewResponse = await factory.Client.SendAsync(reviewRequest);
        using var review = await JsonDocument.ParseAsync(await reviewResponse.Content.ReadAsStreamAsync());
        Assert.Equal(1, review.RootElement.GetProperty("noChangeRows").GetInt32());
        using var commitRequest = Authorized(HttpMethod.Post, $"/api/DataHub/commit/{batchId}", token);
        using var commitResponse = await factory.Client.SendAsync(commitRequest);
        Assert.Equal(HttpStatusCode.OK, commitResponse.StatusCode);
        using var committed = await JsonDocument.ParseAsync(await commitResponse.Content.ReadAsStreamAsync());
        Assert.Equal(0, committed.RootElement.GetProperty("updatedRecords").GetInt32());
        Assert.Equal(1, committed.RootElement.GetProperty("noChangeRecords").GetInt32());
    }

    [Fact]
    public async Task ConcurrentExistingBusinessKeyRejectsCommitWithoutOverwritingCoreRecord()
    {
        var token = await LoginAsync();
        var basic = UniqueBasic();
        var batchId = await UploadAsync(token, MasterPlanWorkbook.Create(Headers, Row(basic, "Attempted replacement")));
        await ResolveWarningsAsync(batchId, token);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.MasterPlans.Add(new MasterPlan { ProjectName = "Concurrent protected original", Basic = basic, BasicKey = basic.ToUpperInvariant(), Grade = "B", Cat = "LPR", CatKey = "LPR", ActionStatus = "Ready" });
            await context.SaveChangesAsync();
        }

        using var commitRequest = Authorized(HttpMethod.Post, $"/api/DataHub/commit/{batchId}", token);
        using var commitResponse = await factory.Client.SendAsync(commitRequest);
        Assert.Equal(HttpStatusCode.BadRequest, commitResponse.StatusCode);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Concurrent protected original", (await verification.MasterPlans.AsNoTracking().SingleAsync(value => value.BasicKey == basic.ToUpperInvariant())).ProjectName);
    }

    [Fact]
    public async Task ReviewedUpdateConcurrencyConflictRollsBackMixedInsertAndUpdate()
    {
        var token = await LoginAsync();
        var existingBasic = UniqueBasic();
        var newBasic = UniqueBasic();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.MasterPlans.Add(new MasterPlan { ProjectName = "Original", Basic = existingBasic, BasicKey = existingBasic.ToUpperInvariant(), Grade = "B", Cat = "LPR", CatKey = "LPR", Area = "Synthetic Area", HwPic = "Synthetic PIC", PvrTargetDate = new DateTime(2026, 8, 1), ActionStatus = "Ready" });
            await context.SaveChangesAsync();
        }
        var batchId = await UploadAsync(token, MasterPlanWorkbook.Create(Headers, Row(newBasic), Row(existingBasic, "Reviewed update")));
        using (var update = Authorized(HttpMethod.Post, $"/api/DataHub/resolve-existing-business-key/{batchId}", token, new { resolution = "Update" }))
        using (var updateResponse = await factory.Client.SendAsync(update)) Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        await ResolveWarningsAsync(batchId, token);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existing = await context.MasterPlans.SingleAsync(value => value.BasicKey == existingBasic.ToUpperInvariant());
            existing.ProjectName = "Concurrent edit";
            existing.Version++;
            await context.SaveChangesAsync();
        }
        using var commit = Authorized(HttpMethod.Post, $"/api/DataHub/commit/{batchId}", token);
        using var response = await factory.Client.SendAsync(commit);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await verification.MasterPlans.AnyAsync(value => value.BasicKey == newBasic.ToUpperInvariant()));
        Assert.Equal("Concurrent edit", (await verification.MasterPlans.SingleAsync(value => value.BasicKey == existingBasic.ToUpperInvariant())).ProjectName);
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
                row.GetProperty("conflictType").GetString() != "ExistingBusinessKey");
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

    private static string[] Row(string basic, string projectName = "Synthetic import") => [projectName, basic, "B", "LPR", "2026-08-01", "Synthetic Area", "Synthetic PIC"];
    private static string UniqueBasic() => $"SYN-MP-{Guid.NewGuid():N}";
}
