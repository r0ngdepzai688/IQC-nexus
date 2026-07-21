using System.Net;
using System.Text.Json;
using IqcQms.Application;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Data.Seeders;
using IqcQms.Infrastructure.Migrations;
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
        Assert.Contains("missing an approved Cat mapping", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("20260719212323_AddAdaptiveMasterPlanUpsert", await context.Database.GetAppliedMigrationsAsync());
        Assert.DoesNotContain("Cat", await MasterPlanColumnsAsync(context));
        Assert.Equal(1, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM MasterPlans").SingleAsync());
        Assert.Equal("LEGACY-BASIC", await ScalarAsync(context, "SELECT Basic FROM MasterPlans WHERE Id = 1;"));
    }

    [Fact]
    public async Task AdaptiveUpsertMigrationSucceedsForEmptyDatabaseAndIsIdempotent()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync();
        await migrator.MigrateAsync();

        var applied = await context.Database.GetAppliedMigrationsAsync();
        Assert.Equal(1, applied.Count(value => value == "20260719212323_AddAdaptiveMasterPlanUpsert"));
        Assert.Contains("Cat", await MasterPlanColumnsAsync(context));
        Assert.True(await HasUniqueBusinessKeyIndexAsync(context));
    }

    [Fact]
    public async Task AdaptiveUpsertMigrationBackfillsApprovedLegacyRowAndPreservesLegacyData()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20260713154829_UpdateMasterPlanAndMilestones");
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO "MasterPlans"
                ("Id", "ProjectName", "Basic", "Area", "Grade", "Sku", "QtyLpr", "QtyLsr", "HwPic", "Status", "ActionStatus", "CreatedAt", "UpdatedAt", "ImportedStatus", "LastImportBatchId", "Remark")
            VALUES
                (1, 'Galaxy A56_1783959984', 'SM-A566B', 'DCM', 'A', 'SM-A566B', 7, 9, 'SYN-PIC', 'Open', 'Review', '2026-01-01', '2026-01-02', '', 'IMP-MP-20260713-008', 'legacy remark')
            """);

        var legacyBefore = await ReadLegacyProjectionAsync(context);
        Assert.True(MasterPlanBusinessKey.TryCreate("SM-A566B", "LPR", out var expectedBasicKey, out var expectedCatKey, out _));

        await migrator.MigrateAsync();
        await migrator.MigrateAsync();

        var row = await context.MasterPlans.AsNoTracking().SingleAsync(value => value.Id == 1);
        Assert.Equal("LPR", row.Cat);
        Assert.Equal(expectedBasicKey, row.BasicKey);
        Assert.Equal(expectedCatKey, row.CatKey);
        Assert.Equal(legacyBefore, await ReadLegacyProjectionAsync(context));
        Assert.True(await HasUniqueBusinessKeyIndexAsync(context));
        Assert.Equal("ok", await ScalarAsync(context, "PRAGMA integrity_check;"));
        Assert.Equal(1, await context.MasterPlans.CountAsync());
        Assert.Equal(1, (await context.Database.GetAppliedMigrationsAsync()).Count(value => value == "20260719212323_AddAdaptiveMasterPlanUpsert"));
    }

    [Fact]
    public async Task AdaptiveUpsertCollisionGuardFailsBeforeUniqueIndexCreation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20260713154829_UpdateMasterPlanAndMilestones");
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "MasterPlans" ADD "BasicKey" TEXT NOT NULL DEFAULT '';
            ALTER TABLE "MasterPlans" ADD "Cat" TEXT NOT NULL DEFAULT '';
            ALTER TABLE "MasterPlans" ADD "CatKey" TEXT NOT NULL DEFAULT '';
            INSERT INTO "MasterPlans"
                ("Id", "ProjectName", "Basic", "Area", "Grade", "Sku", "QtyLpr", "QtyLsr", "HwPic", "Status", "ActionStatus", "CreatedAt", "UpdatedAt", "ImportedStatus", "LastImportBatchId", "Remark")
            VALUES
                (1, 'One', 'Model One', '', 'A', 'SYN-1', 0, 0, '', '', '', '2026-01-01', '2026-01-01', '', '', ''),
                (2, 'Two', 'MODEL ONE', '', 'A', 'SYN-2', 0, 0, '', '', '', '2026-01-01', '2026-01-01', '', '', '');
            """);
        MasterPlanBusinessKey.TryCreate("Model One", "LPR", out var basicKey, out var catKey, out _);
        var sql = LegacyMasterPlanMigrationSql.Build(
            new(1, "Model One", "LPR", basicKey, catKey),
            new(2, "MODEL ONE", "LPR", basicKey, catKey));

        var error = await Assert.ThrowsAnyAsync<Exception>(() => context.Database.ExecuteSqlRawAsync(sql));

        Assert.Contains("Duplicate normalized MasterPlan BasicKey and CatKey", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.False(await HasUniqueBusinessKeyIndexAsync(context));
    }

    private static AppDbContext CreateContext(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);

    private static async Task<string[]> MasterPlanColumnsAsync(AppDbContext context)
    {
        var columns = new List<string>();
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(\"MasterPlans\");";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            columns.Add(reader.GetString(1));
        return columns.ToArray();
    }

    private static async Task<bool> HasUniqueBusinessKeyIndexAsync(AppDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_index_list('MasterPlans') WHERE name = 'IX_MasterPlans_BasicKey_CatKey' AND \"unique\" = 1;";
        return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
    }

    private static async Task<string> ReadLegacyProjectionAsync(AppDbContext context) =>
        await ScalarAsync(context, """
            SELECT printf('%d|%s|%s|%s|%s|%s|%d|%d|%s|%s|%s|%s|%s|%s|%s|%s',
                "Id", "ProjectName", "Basic", "Area", "Grade", "Sku", "QtyLpr", "QtyLsr",
                "HwPic", "Status", "ActionStatus", "CreatedAt", "UpdatedAt", "ImportedStatus",
                "LastImportBatchId", "Remark")
            FROM "MasterPlans" WHERE "Id" = 1;
            """);

    private static async Task<string> ScalarAsync(AppDbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(await command.ExecuteScalarAsync())!;
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
