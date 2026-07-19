using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace IqcQms.ApiIntegrationTests;

[Trait("Category", "Integration")]
public sealed class AuthenticationDataHubTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task ValidSyntheticLoginReturnsJwtThatAuthorizesDataHubHistory()
    {
        var token = await LoginAsync("SYN-0001", factory.SeedPassword);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/DataHub/history");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await factory.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, body.RootElement.ValueKind);
        Assert.Empty(body.RootElement.EnumerateArray());
    }

    [Fact]
    [Trait("Category", "Contract")]
    public async Task InvalidSyntheticPasswordReturnsUnauthorized()
    {
        using var response = await factory.Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "SYN-0001",
            password = "definitely-not-the-generated-test-password",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.TryGetProperty("message", out var message));
        Assert.False(string.IsNullOrWhiteSpace(message.GetString()));
    }

    [Fact]
    public async Task AnonymousDataHubHistoryRequestReturnsUnauthorized()
    {
        using var response = await factory.Client.GetAsync("/api/DataHub/history");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> LoginAsync(string username, string password)
    {
        using var response = await factory.Client.PostAsJsonAsync("/api/auth/login", new { username, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = body.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }
}
