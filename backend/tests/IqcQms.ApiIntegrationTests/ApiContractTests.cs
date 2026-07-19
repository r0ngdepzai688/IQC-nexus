using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IqcQms.Domain.Entities.DataHub;
using IqcQms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IqcQms.ApiIntegrationTests;

[Trait("Category", "Contract")]
public sealed class ApiContractTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task ValidLoginAcceptsFrontendRequestAndReturnsTokenAndUser()
    {
        using var response = await factory.Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "SYN-0001",
            password = factory.SeedPassword,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var document = await ReadJsonAsync(response);
        AssertRequiredString(document.RootElement, "token", "login response");
        var user = AssertRequiredObject(document.RootElement, "user", "login response");
        foreach (var property in new[] { "username", "fullName", "position", "scope", "systemRole", "accountStatus", "organization", "part", "email", "roleProfile", "avatar" })
        {
            Assert.True(user.TryGetProperty(property, out _), $"Login user is missing frontend-consumed property '{property}'.");
        }
    }

    [Fact]
    public async Task MissingLoginFieldsReturnDocumentedBadRequestEnvelope()
    {
        using var response = await factory.Client.PostAsJsonAsync("/api/auth/login", new { username = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        AssertRequiredString(document.RootElement, "message", "missing-login-fields response");
    }

    [Fact]
    public async Task AnonymousDataHubHistoryReturnsBearerChallenge()
    {
        using var response = await factory.Client.GetAsync("/api/DataHub/history");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.Single().Scheme);
    }

    [Fact]
    public async Task AuthenticatedEmptyHistoryIsAValidJsonArray()
    {
        using var request = await AuthorizedRequestAsync(HttpMethod.Get, "/api/DataHub/history?module=NoContractRows");
        using var response = await factory.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var document = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.Empty(document.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task HistoryItemContainsFrontendConsumedStableFields()
    {
        const string module = "ContractHistory";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var source = new DataSource { SourceName = "Contract fixture", Module = module };
            context.DataSources.Add(source);
            await context.SaveChangesAsync();
            context.ImportBatches.Add(new ImportBatch
            {
                BatchId = "CONTRACT-BATCH-001",
                DataSourceId = source.Id,
                SourceName = source.SourceName,
                Module = module,
                UploadedBy = "SYN-0001",
                OriginalFileName = "contract.xlsx",
                FileHash = "contract-hash",
                Status = "Validated",
                TotalRows = 3,
                ValidRows = 2,
                ErrorRows = 1,
                ReviewRequiredRows = 0,
                DurationMs = 10,
            });
            await context.SaveChangesAsync();
        }

        using var request = await AuthorizedRequestAsync(HttpMethod.Get, $"/api/DataHub/history?module={module}");
        using var response = await factory.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        var item = Assert.Single(document.RootElement.EnumerateArray());

        foreach (var property in new[] { "batchId", "module", "uploadedBy", "uploadedAt", "status", "totalRows", "validRows", "errorRows", "reviewRequiredRows", "durationMs" })
        {
            Assert.True(item.TryGetProperty(property, out _), $"History item is missing frontend-consumed property '{property}'.");
        }
        Assert.Equal(JsonValueKind.String, item.GetProperty("batchId").ValueKind);
        foreach (var property in new[] { "totalRows", "validRows", "errorRows", "reviewRequiredRows", "durationMs" })
        {
            Assert.Equal(JsonValueKind.Number, item.GetProperty(property).ValueKind);
        }
    }

    [Fact]
    public async Task OpenApiDocumentParsesAndDeclaresCriticalOperationsAndSchemas()
    {
        using var response = await factory.Client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        AssertRequiredString(document.RootElement, "openapi", "OpenAPI document");
        var info = AssertRequiredObject(document.RootElement, "info", "OpenAPI document");
        AssertRequiredString(info, "title", "OpenAPI info");
        AssertRequiredString(info, "version", "OpenAPI info");
        _ = GetOpenApiOperation(document.RootElement, "/api/health", "get");

        const string loginEndpoint = "POST /api/Auth/login";
        var login = GetOpenApiOperation(document.RootElement, "/api/Auth/login", "post");
        var loginRequestBody = AssertRequiredObject(login, "requestBody", $"{loginEndpoint} operation");
        var loginRequestSchema = GetApplicationJsonSchema(loginRequestBody, loginEndpoint, "request body");
        AssertLocalSchemaReference(loginRequestSchema, "#/components/schemas/LoginRequest", loginEndpoint, "request body");
        var loginResponses = AssertRequiredObject(login, "responses", $"{loginEndpoint} operation");
        var loginSuccess = GetOpenApiResponse(loginResponses, "200", loginEndpoint);
        var loginSuccessSchema = GetApplicationJsonSchema(loginSuccess, loginEndpoint, "200 response");
        AssertLocalSchemaReference(loginSuccessSchema, "#/components/schemas/LoginResponse", loginEndpoint, "200 response");
        _ = GetOpenApiResponse(loginResponses, "400", loginEndpoint);
        _ = GetOpenApiResponse(loginResponses, "401", loginEndpoint);

        const string historyEndpoint = "GET /api/DataHub/history";
        var history = GetOpenApiOperation(document.RootElement, "/api/DataHub/history", "get");
        var historyResponses = AssertRequiredObject(history, "responses", $"{historyEndpoint} operation");
        var historySuccess = GetOpenApiResponse(historyResponses, "200", historyEndpoint);
        var historySchema = GetApplicationJsonSchema(historySuccess, historyEndpoint, "200 response");
        var historyType = historySchema.TryGetProperty("type", out var historyTypeProperty)
            ? historyTypeProperty.GetString()
            : null;
        Assert.True(
            string.Equals("array", historyType, StringComparison.Ordinal),
            $"{historyEndpoint} 200 response application/json schema expected type 'array', but found '{historyType ?? "<missing>"}'.");
        var historyItems = AssertRequiredObject(historySchema, "items", $"{historyEndpoint} 200 response application/json schema");
        AssertLocalSchemaReference(historyItems, "#/components/schemas/ImportBatch", historyEndpoint, "200 response array items");
        _ = GetOpenApiResponse(historyResponses, "401", historyEndpoint);

        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.TryGetProperty("LoginRequest", out _), "OpenAPI components are missing LoginRequest.");
        Assert.True(schemas.TryGetProperty("LoginResponse", out _), "OpenAPI components are missing LoginResponse.");
        Assert.True(schemas.TryGetProperty("ImportBatch", out _), "OpenAPI components are missing ImportBatch.");
    }

    [Fact]
    public async Task OpenApiDeclaresBearerAuthenticationOnlyOnProtectedHistory()
    {
        using var response = await factory.Client.GetAsync("/swagger/v1/swagger.json");
        using var document = await ReadJsonAsync(response);
        var root = document.RootElement;
        var bearer = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());

        var paths = root.GetProperty("paths");
        var history = paths.GetProperty("/api/DataHub/history").GetProperty("get");
        var requirement = Assert.Single(history.GetProperty("security").EnumerateArray());
        Assert.True(requirement.TryGetProperty("Bearer", out _), "Protected history operation is missing its Bearer requirement.");
        Assert.False(paths.GetProperty("/api/Auth/login").GetProperty("post").TryGetProperty("security", out _), "Anonymous login must not require Bearer authentication.");
    }

    private async Task<HttpRequestMessage> AuthorizedRequestAsync(HttpMethod method, string path)
    {
        using var loginResponse = await factory.Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "SYN-0001",
            password = factory.SeedPassword,
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var login = await ReadJsonAsync(loginResponse);
        var token = login.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(body);
    }

    private static JsonElement GetOpenApiOperation(JsonElement root, string path, string method)
    {
        var paths = AssertRequiredObject(root, "paths", "OpenAPI document");
        Assert.True(paths.TryGetProperty(path, out var pathItem), $"OpenAPI is missing endpoint path '{path}'.");
        Assert.True(pathItem.TryGetProperty(method, out var operation), $"OpenAPI endpoint '{path}' is missing method '{method.ToUpperInvariant()}'.");
        return operation;
    }

    private static JsonElement GetOpenApiResponse(JsonElement responses, string statusCode, string endpoint)
    {
        Assert.True(
            responses.TryGetProperty(statusCode, out var response),
            $"{endpoint} OpenAPI operation is missing response '{statusCode}'.");
        return response;
    }

    private static JsonElement GetApplicationJsonSchema(JsonElement contentOwner, string endpoint, string location)
    {
        var content = AssertRequiredObject(contentOwner, "content", $"{endpoint} {location}");
        Assert.True(
            content.TryGetProperty("application/json", out var mediaType),
            $"{endpoint} {location} is missing application/json content.");
        return AssertRequiredObject(mediaType, "schema", $"{endpoint} {location} application/json content");
    }

    private static void AssertLocalSchemaReference(JsonElement schema, string expectedReference, string endpoint, string location)
    {
        var actualReference = schema.TryGetProperty("$ref", out var reference) && reference.ValueKind == JsonValueKind.String
            ? reference.GetString()
            : null;
        Assert.True(
            string.Equals(expectedReference, actualReference, StringComparison.Ordinal),
            $"{endpoint} {location} expected schema reference '{expectedReference}', but found '{actualReference ?? "<missing>"}'.");
    }

    private static void AssertRequiredString(JsonElement parent, string propertyName, string contract)
    {
        Assert.True(parent.TryGetProperty(propertyName, out var property), $"{contract} is missing required property '{propertyName}'.");
        Assert.Equal(JsonValueKind.String, property.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(property.GetString()), $"{contract} property '{propertyName}' must be non-empty.");
    }

    private static JsonElement AssertRequiredObject(JsonElement parent, string propertyName, string contract)
    {
        Assert.True(parent.TryGetProperty(propertyName, out var property), $"{contract} is missing required property '{propertyName}'.");
        Assert.Equal(JsonValueKind.Object, property.ValueKind);
        return property;
    }
}
