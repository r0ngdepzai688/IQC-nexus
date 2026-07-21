using Microsoft.EntityFrameworkCore;
using IqcQms.Infrastructure.Data;
using IqcQms.Api.Hubs;
using IqcQms.Application.Interfaces.DataHub;
using IqcQms.Infrastructure.Services.DataHub;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using IqcQms.Api.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Load custom config.json
builder.Configuration.AddJsonFile("config.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // Next.js frontend
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Required for SignalR
        });
});

// Register Configuration
builder.Services.Configure<IqcQms.Application.Interfaces.DataHub.DataHubPathConfig>(
    builder.Configuration.GetSection("DataHub"));
builder.Services.PostConfigure<IqcQms.Application.Interfaces.DataHub.DataHubPathConfig>(options =>
{
    var basePath = string.IsNullOrWhiteSpace(options.BasePath)
        ? Path.Combine(builder.Environment.ContentRootPath, "DataHub")
        : Path.GetFullPath(options.BasePath);
    var masterPlanPath = string.IsNullOrWhiteSpace(options.NewModelsMasterPlanBasePath)
        ? Path.Combine(basePath, "NewModels", "MasterPlan")
        : Path.GetFullPath(options.NewModelsMasterPlanBasePath);

    options.BasePath = basePath;
    options.NewModelsMasterPlanBasePath = masterPlanPath;
    options.NewModelsMasterPlanManualUploadPath = ResolvePath(options.NewModelsMasterPlanManualUploadPath, "ManualUpload");
    options.NewModelsMasterPlanRawPath = ResolvePath(options.NewModelsMasterPlanRawPath, "Raw");
    options.NewModelsMasterPlanProcessedPath = ResolvePath(options.NewModelsMasterPlanProcessedPath, "Processed");
    options.NewModelsMasterPlanRejectedPath = ResolvePath(options.NewModelsMasterPlanRejectedPath, "Rejected");
    options.NewModelsMasterPlanReportsPath = ResolvePath(options.NewModelsMasterPlanReportsPath, "Reports");
    options.NewModelsMasterPlanTempPath = ResolvePath(options.NewModelsMasterPlanTempPath, "Temp");

    string ResolvePath(string configured, string folder) => string.IsNullOrWhiteSpace(configured)
        ? Path.Combine(masterPlanPath, folder)
        : Path.GetFullPath(configured);
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT bearer token returned by POST /api/auth/login."
    });
    options.OperationFilter<AuthorizeOperationFilter>();
});
builder.Services.AddSignalR(); // Add SignalR

// Register Application Services
builder.Services.AddScoped<IqcQms.Application.Interfaces.NewModels.IMasterPlanService, IqcQms.Infrastructure.Services.NewModels.MasterPlanService>();
builder.Services.AddScoped<IMasterPlanContractParser, MasterPlanContractParser>();
builder.Services.AddScoped<IDataHubIngestionService, DataHubIngestionService>();

// JWT Authentication setup
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];
if (string.IsNullOrWhiteSpace(secretKey))
{
    if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
        throw new InvalidOperationException("JwtSettings:Secret must be supplied through environment variables or user-secrets outside Development/Testing.");
    secretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    builder.Configuration["JwtSettings:Secret"] = secretKey;
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    if (builder.Environment.IsDevelopment())
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                const string developmentToken = "mock-jwt-token-test";
                if (context.Request.Headers.Authorization == $"Bearer {developmentToken}")
                {
                    var claims = new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, "local-development-user"),
                        new Claim(JwtRegisteredClaimNames.UniqueName, "local-development-user"),
                        new Claim("FullName", "Local Development User"),
                        new Claim(ClaimTypes.Role, "User"),
                    };
                    var credentials = new SigningCredentials(
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                        SecurityAlgorithms.HmacSha256);
                    var token = new JwtSecurityToken(
                        issuer: jwtSettings["Issuer"],
                        audience: jwtSettings["Audience"],
                        claims: claims,
                        expires: DateTime.UtcNow.AddMinutes(15),
                        signingCredentials: credentials);
                    context.Token = new JwtSecurityTokenHandler().WriteToken(token);
                }

                return Task.CompletedTask;
            }
        };
    }
});
builder.Services.AddAuthorization();

// Configure Database Connection (SQLite for local dev)
var dbConfig = builder.Configuration.GetSection("DatabaseConfig");
var connectionString = dbConfig["ConnectionString"];

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend"); // Apply CORS

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chathub"); // Map SignalR Hub

// Basic health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy", Message = "IQC QMS API is running on SQLite!" }))
    .WithName("GetHealth")
    .WithOpenApi();

// Run DB Migrations and Seeders
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        var env = services.GetRequiredService<IWebHostEnvironment>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "fixtures", "personnel.synthetic.json");
        var seedPassword = env.IsDevelopment() || env.IsEnvironment("Testing")
            ? configuration["IQC_SYNTHETIC_USER_SEED_PASSWORD"]
            : null;

        await IqcQms.Infrastructure.Data.Seeders.UserSeeder.ValidateMigrateAndSyncAsync(
            context,
            fixturePath,
            seedPassword,
            logger,
            () => context.Database.MigrateAsync());
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "Synthetic personnel validation, database migration, or seeding failed. Startup is aborted.");
        throw;
    }
}

app.Run();

public partial class Program { }
