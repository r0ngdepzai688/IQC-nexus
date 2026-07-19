using IqcQms.Application.Interfaces.DataHub;
using IqcQms.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace IqcQms.ApiIntegrationTests;

public sealed class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _runtimeDirectory = Path.Combine(
        Path.GetTempPath(),
        "iqc-nexus-integration",
        Guid.NewGuid().ToString("N"));
    private bool _cleanedUp;

    public HttpClient Client { get; private set; } = null!;
    public string SeedPassword { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private string JwtSecret { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public string FixturePath => Path.Combine(AppContext.BaseDirectory, "fixtures", "personnel.synthetic.json");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_runtimeDirectory);
        var settings = new Dictionary<string, string?>
        {
            ["DatabaseConfig:ConnectionString"] = $"Data Source={Path.Combine(_runtimeDirectory, "integration.db")};Pooling=False",
            ["DataHub:BasePath"] = Path.Combine(_runtimeDirectory, "data-hub"),
            ["IQC_SYNTHETIC_USER_SEED_PASSWORD"] = SeedPassword,
            ["JwtSettings:Secret"] = JwtSecret,
            ["JwtSettings:Issuer"] = "IqcQms.Api.IntegrationTests",
            ["JwtSettings:Audience"] = "IqcQms.IntegrationTests",
            ["JwtSettings:ExpiryMinutes"] = "5",
            ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning",
        };

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(settings));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(settings["DatabaseConfig:ConnectionString"]));
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = settings["JwtSettings:Issuer"],
                    ValidAudience = settings["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)),
                });
            services.PostConfigure<DataHubPathConfig>(options =>
            {
                options.BasePath = settings["DataHub:BasePath"]!;
                options.NewModelsMasterPlanBasePath = Path.Combine(options.BasePath, "NewModels", "MasterPlan");
                options.NewModelsMasterPlanManualUploadPath = Path.Combine(options.NewModelsMasterPlanBasePath, "ManualUpload");
                options.NewModelsMasterPlanRawPath = Path.Combine(options.NewModelsMasterPlanBasePath, "Raw");
                options.NewModelsMasterPlanProcessedPath = Path.Combine(options.NewModelsMasterPlanBasePath, "Processed");
                options.NewModelsMasterPlanRejectedPath = Path.Combine(options.NewModelsMasterPlanBasePath, "Rejected");
                options.NewModelsMasterPlanReportsPath = Path.Combine(options.NewModelsMasterPlanBasePath, "Reports");
                options.NewModelsMasterPlanTempPath = Path.Combine(options.NewModelsMasterPlanBasePath, "Temp");
            });
        });
    }

    Task IAsyncLifetime.InitializeAsync()
    {
        Client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        return Task.CompletedTask;
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        if (Client is not null)
        {
            Client.Dispose();
        }
        Dispose();
        CleanupRuntimeDirectory();
        return Task.CompletedTask;
    }

    private void CleanupRuntimeDirectory()
    {
        if (_cleanedUp)
        {
            return;
        }

        _cleanedUp = true;
        if (Directory.Exists(_runtimeDirectory))
        {
            Directory.Delete(_runtimeDirectory, recursive: true);
        }
    }
}
