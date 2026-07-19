using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IqcQms.Api.Controllers;
using IqcQms.Domain.Entities.Auth;
using IqcQms.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace IqcQms.ApiAuthChecks;

public sealed class AuthenticationTests
{
    private const string Issuer = "IqcQms.Api.AuthTests";
    private const string Audience = "IqcQms.AuthTests";

    [Fact]
    public async Task AnonymousProtectedRequestReturnsUnauthorized()
    {
        await using var host = await AuthHost.StartAsync(NewSecret());
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("/protected")).StatusCode);
    }

    [Fact]
    public async Task ValidJwtReachesProtectedEndpoint()
    {
        var secret = NewSecret();
        await using var host = await AuthHost.StartAsync(secret);
        var response = await host.GetProtectedAsync(CreateToken(secret, "User", DateTime.UtcNow.AddMinutes(5)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TokenWithInvalidSignatureReturnsUnauthorized()
    {
        await using var host = await AuthHost.StartAsync(NewSecret());
        var response = await host.GetProtectedAsync(CreateToken(NewSecret(), "User", DateTime.UtcNow.AddMinutes(5)));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredTokenReturnsUnauthorized()
    {
        var secret = NewSecret();
        await using var host = await AuthHost.StartAsync(secret);
        var response = await host.GetProtectedAsync(CreateToken(secret, "User", DateTime.UtcNow.AddMinutes(-10)));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginTokenUsesPersistedSystemRole()
    {
        var secret = NewSecret();
        var password = $"auth-test-{Guid.NewGuid():N}";
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        context.Users.Add(new User
        {
            Username = "SYN-AUTH-USER",
            FullName = "Synthetic Auth User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            SystemRole = "User",
            IsActive = true,
            AccountStatus = "Active"
        });
        await context.SaveChangesAsync();

        var configuration = Configuration(secret);
        var controller = new AuthController(context, configuration);
        var action = await controller.Login(new LoginRequest { Username = "SYN-AUTH-USER", Password = password });
        var response = Assert.IsType<OkObjectResult>(action);
        var login = Assert.IsType<LoginResponse>(response.Value);

        await using var host = await AuthHost.StartAsync(secret);
        var protectedResponse = await host.GetProtectedAsync(login.Token);
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
        var body = JsonDocument.Parse(await protectedResponse.Content.ReadAsStringAsync());
        Assert.Equal("User", body.RootElement.GetProperty("role").GetString());
    }

    [Fact]
    public async Task LoginTokenDoesNotIntroduceAdministratorRole()
    {
        var secret = NewSecret();
        var password = $"auth-test-{Guid.NewGuid():N}";
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        context.Users.Add(new User
        {
            Username = "SYN-NON-ADMIN",
            FullName = "Synthetic Non Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            SystemRole = "User",
            IsActive = true,
            AccountStatus = "Active"
        });
        await context.SaveChangesAsync();

        var controller = new AuthController(context, Configuration(secret));
        var action = await controller.Login(new LoginRequest { Username = "SYN-NON-ADMIN", Password = password });
        var login = Assert.IsType<LoginResponse>(Assert.IsType<OkObjectResult>(action).Value);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(login.Token);

        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "User");
        Assert.DoesNotContain(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Administrator");
    }

    private static IConfiguration Configuration(string secret) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Secret"] = secret,
            ["JwtSettings:Issuer"] = Issuer,
            ["JwtSettings:Audience"] = Audience,
            ["JwtSettings:ExpiryMinutes"] = "5"
        })
        .Build();

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options;
        var context = new AppDbContext(options);
        context.Database.OpenConnection();
        return context;
    }

    private static string NewSecret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    private static string CreateToken(string secret, string role, DateTime expires)
    {
        var now = DateTime.UtcNow.AddMinutes(-20);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim(ClaimTypes.Role, role)],
            notBefore: now,
            expires: expires,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class AuthHost : IAsyncDisposable
    {
        private readonly WebApplication _app;
        public HttpClient Client { get; }

        private AuthHost(WebApplication app)
        {
            _app = app;
            Client = app.GetTestClient();
        }

        public static async Task<AuthHost> StartAsync(string secret)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => options.TokenValidationParameters = ValidationParameters(secret));

            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapGet("/protected", [Authorize] (ClaimsPrincipal user) => Results.Ok(new
            {
                role = user.FindFirst(ClaimTypes.Role)?.Value
            }));
            await app.StartAsync();
            return new AuthHost(app);
        }

        public async Task<HttpResponseMessage> GetProtectedAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await Client.SendAsync(request);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.DisposeAsync();
        }

        private static TokenValidationParameters ValidationParameters(string secret) => new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    }
}
