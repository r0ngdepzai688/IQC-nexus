using Microsoft.EntityFrameworkCore;
using IqcQms.Infrastructure.Data;
using IqcQms.Api.Hubs;

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

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR(); // Add SignalR

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
        context.Database.Migrate();

        var logger = services.GetRequiredService<ILogger<Program>>();
        var env = services.GetRequiredService<IWebHostEnvironment>();
        
        // Find User_DB.xlsx in the root of the project (3 levels up from Api/bin/Debug/net8.0 usually, or explicitly provided)
        // A safer way is to navigate up to find the file or provide its path in configuration.
        // Let's assume it's at the solution root level.
        var solutionRoot = Directory.GetParent(env.ContentRootPath)?.Parent?.Parent?.FullName;
        var excelPath = solutionRoot != null ? Path.Combine(solutionRoot, "User_DB.xlsx") : "User_DB.xlsx";
        
        if (!File.Exists(excelPath))
        {
            // fallback for when running from solution root directly
            excelPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "User_DB.xlsx");
            if (!File.Exists(excelPath))
            {
                 excelPath = Path.Combine(Directory.GetCurrentDirectory(), "User_DB.xlsx"); // if running directly from Portal root
            }
        }
        
        await IqcQms.Infrastructure.Data.Seeders.UserSeeder.SyncUsersFromExcelAsync(context, excelPath, logger);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database migration or seeding.");
    }
}

app.Run();
