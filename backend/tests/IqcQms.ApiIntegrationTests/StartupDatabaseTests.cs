using System.Net;
using System.Text.Json;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Data.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ApiIntegrationTests;

[Trait("Category", "Integration")]
public sealed class StartupDatabaseTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    [Trait("Category", "Contract")]
    public async Task FreshHostReturnsSerializedHealthResponse()
    {
        using var response = await factory.Client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task FreshDatabaseHasEveryRealMigrationApplied()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var defined = context.Database.GetMigrations().ToArray();
        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();

        Assert.NotEmpty(defined);
        Assert.Equal(defined, applied);
    }

    [Fact]
    public async Task AdaptiveUpsertMigrationBlocksLegacyRowsWithoutApprovedCatClassification()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20260713154829_UpdateMasterPlanAndMilestones");
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO "MasterPlans"
                ("ProjectName", "Basic", "Area", "Grade", "Sku", "QtyLpr", "QtyLsr", "HwPic", "Status", "ActionStatus", "CreatedAt", "UpdatedAt", "ImportedStatus", "LastImportBatchId", "Remark")
            VALUES
                ('Synthetic legacy', 'LEGACY-BASIC', '', 'B', 'SYN-LEGACY', 0, 0, '', '', '', '2026-01-01', '2026-01-01', '', '', '')
            """);

        var error = await Assert.ThrowsAnyAsync<Exception>(() => migrator.MigrateAsync());

        Assert.Contains("CHECK constraint failed", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("20260719212323_AddAdaptiveMasterPlanUpsert", await context.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task StartupSeedsUniqueCanonicalSyntheticUsers()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = await context.Users.AsNoTracking().ToArrayAsync();

        Assert.NotEmpty(users);
        Assert.All(users, value => Assert.StartsWith("SYN-", value.Username, StringComparison.Ordinal));
        Assert.Equal(users.Length, users.Select(value => value.Username).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(users, value => value.Username == "SYN-0001" && value.IsActive && value.SystemRole == "User");
    }

    [Fact]
    public async Task RepeatedInitializationDoesNotDuplicateSyntheticUsers()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var before = await context.Users.CountAsync();

        await UserSeeder.ValidateMigrateAndSyncAsync(
            context,
            factory.FixturePath,
            factory.SeedPassword,
            NullLogger.Instance,
            () => context.Database.MigrateAsync());

        context.ChangeTracker.Clear();
        Assert.Equal(before, await context.Users.CountAsync());
        Assert.Equal(before, await context.Users.Select(value => value.Username).Distinct().CountAsync());
    }
}
