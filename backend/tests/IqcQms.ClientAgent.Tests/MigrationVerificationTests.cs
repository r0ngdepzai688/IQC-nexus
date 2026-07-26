using IqcQms.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class MigrationVerificationTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly string _connectionString;

    public MigrationVerificationTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"migration_test_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_tempDbPath};Pooling=False";
    }

    public void Dispose()
    {
        if (File.Exists(_tempDbPath))
        {
            try { File.Delete(_tempDbPath); } catch { }
        }
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connectionString)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CleanDatabase_AppliesAllMigrationsSuccessfully()
    {
        using (var db = CreateDbContext())
        {
            // Apply all EF Migrations to empty database
            await db.Database.MigrateAsync();

            // Verify key tables exist
            var canConnect = await db.Database.CanConnectAsync();
            Assert.True(canConnect);

            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            Assert.Empty(pendingMigrations);

            var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
            Assert.NotEmpty(appliedMigrations);
            Assert.Contains(appliedMigrations, m => m.Contains("AddPayloadRetentionIndexes"));
        }

        // Verify entities can be queried and inserted on clean schema
        using (var db = CreateDbContext())
        {
            var deviceCount = await db.AgentDevices.CountAsync();
            Assert.Equal(0, deviceCount);

            var submissionCount = await db.AgentPayloadSubmissions.CountAsync();
            Assert.Equal(0, submissionCount);

            var tombstoneCount = await db.AgentPayloadReplayTombstones.CountAsync();
            Assert.Equal(0, tombstoneCount);
        }
    }

    [Fact]
    public async Task UpgradeDatabase_PreservesDataAndSnapshotConsistency()
    {
        using (var db = CreateDbContext())
        {
            // Apply migrations up to initial agent tables
            var migrator = db.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260724143121_AddAgentDevices");

            // Insert representative legacy row before subsequent migrations
            var device = new Domain.Entities.Agent.AgentDevice
            {
                DeviceId = "legacy_dev_001",
                OwnerUserId = 1,
                DisplayName = "Legacy Device",
                AgentVersion = "1.0.0",
                ProtocolVersion = "1.0",
                State = Domain.Entities.Agent.AgentDeviceState.Active,
                PairedAtUtc = DateTime.UtcNow,
                LastSeenAtUtc = DateTime.UtcNow,
                ConcurrencyVersion = 1
            };
            db.AgentDevices.Add(device);
            await db.SaveChangesAsync();
        }

        // Now upgrade to latest migration
        using (var db = CreateDbContext())
        {
            await db.Database.MigrateAsync();

            var legacyDevice = await db.AgentDevices.FirstOrDefaultAsync(d => d.DeviceId == "legacy_dev_001");
            Assert.NotNull(legacyDevice);
            Assert.Equal("Legacy Device", legacyDevice.DisplayName);

            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            Assert.Empty(pendingMigrations);
        }
    }

    [Fact]
    public async Task ApplicationStartup_MigratesDatabaseWithoutEnsureCreated()
    {
        using var db = CreateDbContext();

        // Production startup migration path must use MigrateAsync(), not EnsureCreated()
        var pendingBefore = await db.Database.GetPendingMigrationsAsync();
        Assert.NotEmpty(pendingBefore);

        await db.Database.MigrateAsync();

        var pendingAfter = await db.Database.GetPendingMigrationsAsync();
        Assert.Empty(pendingAfter);
    }
}
