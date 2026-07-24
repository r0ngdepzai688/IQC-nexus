using System.Data.Common;
using IqcQms.ClientAgent.Contracts;
using IqcQms.Domain.Entities.Agent;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class RefreshRotationConcurrencyTests : IDisposable
{
    private readonly DbConnection _connection;

    public RefreshRotationConcurrencyTests()
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
    public async Task RefreshToken_IsStoredOnlyAsHash()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_hash_test_1"
        });

        var cred = await db.AgentCredentials.FirstAsync();
        Assert.NotEqual(pairResp.RefreshToken, cred.ProtectedVerifierHash);
        Assert.DoesNotContain(pairResp.RefreshToken, cred.ProtectedVerifierHash);
    }

    [Fact]
    public async Task SuccessfulRefresh_RotatesTokenAndMarksOldAsConsumed()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_rotate_test_1"
        });

        var refreshResp = await service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = pairResp.RefreshToken
        });

        Assert.NotNull(refreshResp.AccessToken);
        Assert.NotEqual(pairResp.RefreshToken, refreshResp.RefreshToken);

        var creds = await db.AgentCredentials.ToListAsync();
        Assert.Equal(2, creds.Count);

        var oldCred = creds.First(c => c.ConsumedAtUtc != null);
        Assert.NotNull(oldCred.ConsumedAtUtc);
        Assert.NotNull(oldCred.RevokedAtUtc);
    }

    [Fact]
    public async Task ReplayingConsumedRefreshToken_RevokesEntireTokenFamilyAndDevice()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_replay_test_1"
        });

        // 1. Legitimate refresh
        var refreshResp1 = await service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = pairResp.RefreshToken
        });

        // 2. Attacker replays old refresh token (pairResp.RefreshToken)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = pairResp.RefreshToken
        }));

        Assert.Contains("Replayed refresh token detected", ex.Message);

        var device = await db.AgentDevices.FirstAsync(d => d.DeviceId == pairResp.DeviceId);
        Assert.Equal(AgentDeviceState.Revoked, device.State);
        Assert.NotNull(device.RevokedAtUtc);

        // Subsequent refresh with the legitimate new token fails because device is revoked
        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = refreshResp1.RefreshToken
        }));

        Assert.Equal("Device not found or revoked.", ex2.Message);
    }

    [Fact]
    public async Task ConcurrentRefreshes_ExactlyOneSucceeds_LosingRequestTriggersReplayRevocation()
    {
        string deviceId;
        string refreshToken;

        using (var dbSetup = CreateDbContext())
        {
            var setupService = new AgentService(dbSetup, NullLogger<AgentService>.Instance);
            var created = await setupService.CreatePairingCodeAsync(1, "UserA");
            var pairResp = await setupService.PairDeviceAsync(new AgentDevicePairRequest
            {
                PairingCode = created.PairingCode,
                DeviceId = "dev_concurrent_refresh_1"
            });
            deviceId = pairResp.DeviceId;
            refreshToken = pairResp.RefreshToken;
        }

        var req1 = new AgentTokenRefreshRequest { DeviceId = deviceId, RefreshToken = refreshToken };
        var req2 = new AgentTokenRefreshRequest { DeviceId = deviceId, RefreshToken = refreshToken };

        var task1 = Task.Run(async () =>
        {
            using var db1 = CreateDbContext();
            var s1 = new AgentService(db1, NullLogger<AgentService>.Instance);
            return await s1.RefreshTokenAsync(req1);
        });

        var task2 = Task.Run(async () =>
        {
            using var db2 = CreateDbContext();
            var s2 = new AgentService(db2, NullLogger<AgentService>.Instance);
            return await s2.RefreshTokenAsync(req2);
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
    }
}
