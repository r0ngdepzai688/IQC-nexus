using Microsoft.EntityFrameworkCore;
using IqcQms.Infrastructure.Data;
using IqcQms.Api.Hubs;
using IqcQms.Application.Interfaces.DataHub;
using IqcQms.Infrastructure.Services.DataHub;

var builder = WebApplication.CreateBuilder(args);

// Load custom config.json
builder.Configuration.AddJsonFile("config.json", optional: false, reloadOnChange: true);

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

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR(); // Add SignalR

// Register Application Services
builder.Services.AddScoped<IqcQms.Application.Interfaces.NewModels.IMasterPlanService, IqcQms.Infrastructure.Services.NewModels.MasterPlanService>();
builder.Services.AddScoped<IMasterPlanContractParser, MasterPlanContractParser>();
builder.Services.AddScoped<IDataHubIngestionService, DataHubIngestionService>();

// JWT Authentication setup
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey ?? ""))
    };
});
builder.Services.AddAuthorization();

// Configure Database Connection (SQLite for local dev)
var dbConfig = builder.Configuration.GetSection("DatabaseConfig");
var connectionString = dbConfig["ConnectionString"];

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
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
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "fixtures", "personnel.synthetic.json");
        var seedPassword = env.IsDevelopment() || env.IsEnvironment("Testing")
            ? Environment.GetEnvironmentVariable("IQC_SYNTHETIC_USER_SEED_PASSWORD")
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
