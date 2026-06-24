using Microsoft.EntityFrameworkCore;
using IqcQms.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Load custom config.json
builder.Configuration.AddJsonFile("config.json", optional: false, reloadOnChange: true);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.MapControllers();

// Basic health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy", Message = "IQC QMS API is running on SQLite!" }))
    .WithName("GetHealth")
    .WithOpenApi();

app.Run();
