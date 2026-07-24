using System.Data.Common;
using IqcQms.Application.Services;
using IqcQms.ClientAgent.Contracts;
using IqcQms.Domain.Entities.Agent;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class PairingSecurityTests : IDisposable
{
    private readonly DbConnection _connection;

    public PairingSecurityTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void GenerateSixDigitPairingCode_PreservesSixDigits()
    {
        for (int i = 0; i < 100; i++)
        {
            var code = AgentService.GenerateSixDigitPairingCode();
            Assert.Equal(6, code.Length);
            Assert.True(int.TryParse(code, out var val));
            Assert.InRange(val, 0, 999999);
        }
    }

    [Fact]
    public async Task CreatePairingCode_DoesNotStorePlaintext()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var resp = await service.CreatePairingCodeAsync(1, "TestUser");

        Assert.NotNull(resp.PairingCode);
        Assert.Equal(6, resp.PairingCode.Length);

        var savedReq = await db.AgentPairingRequests.FirstAsync();
        Assert.NotEqual(resp.PairingCode, savedReq.HashedCode);
        Assert.DoesNotContain(resp.PairingCode, savedReq.HashedCode);
    }

    [Fact]
    public async Task WrongCodeAttempt_IncrementsFailedAttemptCount_AndLocksAfterMax()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");

        var pairReq = new AgentDevicePairRequest
        {
            PairingCode = "000000" == created.PairingCode ? "999999" : "000000",
            DeviceId = "dev_wrong_attempt_1",
            DisplayName = "Device 1"
        };

        // 5 Failed attempts
        for (int i = 1; i <= 5; i++)
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PairDeviceAsync(pairReq));
            Assert.Equal("Pairing failed or code is no longer valid.", ex.Message);
        }

        var reqState = await db.AgentPairingRequests.FirstAsync();
        Assert.Equal(5, reqState.FailedAttemptCount);
        Assert.Equal(AgentPairingRequestState.Locked, reqState.State);
        Assert.NotNull(reqState.LockedAtUtc);
    }

    [Fact]
    public async Task ConsumedPairingCode_CannotBeReused()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");

        var pairReq = new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_reuse_1",
            DisplayName = "Device 1"
        };

        var firstResult = await service.PairDeviceAsync(pairReq);
        Assert.NotNull(firstResult.AccessToken);

        // Second attempt with same code fails
        var pairReq2 = new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_reuse_2",
            DisplayName = "Device 2"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PairDeviceAsync(pairReq2));
        Assert.Equal("Pairing failed or code is no longer valid.", ex.Message);
    }

    [Fact]
    public async Task ConcurrentValidPairingRequests_ExactlyOneSucceeds()
    {
        using (var dbSetup = CreateDbContext())
        {
            var setupService = new AgentService(dbSetup, NullLogger<AgentService>.Instance);
            var created = await setupService.CreatePairingCodeAsync(1, "UserA");

            var req1 = new AgentDevicePairRequest { PairingCode = created.PairingCode, DeviceId = "dev_concurrent_1" };
            var req2 = new AgentDevicePairRequest { PairingCode = created.PairingCode, DeviceId = "dev_concurrent_2" };

            var task1 = Task.Run(async () =>
            {
                using var db1 = CreateDbContext();
                var s1 = new AgentService(db1, NullLogger<AgentService>.Instance);
                return await s1.PairDeviceAsync(req1);
            });

            var task2 = Task.Run(async () =>
            {
                using var db2 = CreateDbContext();
                var s2 = new AgentService(db2, NullLogger<AgentService>.Instance);
                return await s2.PairDeviceAsync(req2);
            });

            try
            {
                await Task.WhenAll(task1, task2);
            }
            catch { }

            int successCount = 0;
            int failCount = 0;

            foreach (var t in new[] { task1, task2 })
            {
                if (t.IsCompletedSuccessfully) successCount++;
                else if (t.IsFaulted) failCount++;
            }

            Assert.Equal(1, successCount);
            Assert.Equal(1, failCount);

            var devicesCount = await dbSetup.AgentDevices.CountAsync();
            Assert.Equal(1, devicesCount);
        }
    }
}
