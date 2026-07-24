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
            RefreshToken = pairResp.RefreshToken,
            RefreshOperationId = "op_rotate_1"
        });

        Assert.False(refreshResp.IsDuplicateRetry);
        Assert.NotNull(refreshResp.AccessToken);
        Assert.NotEqual(pairResp.RefreshToken, refreshResp.RefreshToken);

        var creds = await db.AgentCredentials.ToListAsync();
        Assert.Equal(2, creds.Count);

        var oldCred = creds.First(c => c.ConsumedAtUtc != null);
        Assert.NotNull(oldCred.ConsumedAtUtc);
        Assert.NotNull(oldCred.RevokedAtUtc);
    }

    [Fact]
    public async Task ConcurrentSameOperationRequests_DoNotRevokeDevice()
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
                DeviceId = "dev_same_op_concurrent_1"
            });
            deviceId = pairResp.DeviceId;
            refreshToken = pairResp.RefreshToken;
        }

        var sameOpId = "op_same_concurrent_100";
        var req1 = new AgentTokenRefreshRequest { DeviceId = deviceId, RefreshToken = refreshToken, RefreshOperationId = sameOpId };
        var req2 = new AgentTokenRefreshRequest { DeviceId = deviceId, RefreshToken = refreshToken, RefreshOperationId = sameOpId };

        using var db1 = CreateDbContext();
        using var db2 = CreateDbContext();
        var s1 = new AgentService(db1, NullLogger<AgentService>.Instance);
        var s2 = new AgentService(db2, NullLogger<AgentService>.Instance);

        var resp1 = await s1.RefreshTokenAsync(req1);
        var resp2 = await s2.RefreshTokenAsync(req2);

        Assert.False(resp1.IsDuplicateRetry);
        Assert.True(resp2.IsDuplicateRetry);

        using var dbCheck = CreateDbContext();
        var device = await dbCheck.AgentDevices.FirstAsync(d => d.DeviceId == deviceId);
        Assert.Equal(AgentDeviceState.Active, device.State);
    }

    [Fact]
    public async Task SameOperationRetry_IsDeterministic()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_retry_op_1"
        });

        var opId = "op_retry_100";
        var resp1 = await service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = pairResp.RefreshToken,
            RefreshOperationId = opId
        });

        Assert.False(resp1.IsDuplicateRetry);

        // Same operation ID retried after lost HTTP response
        var resp2 = await service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = pairResp.RefreshToken,
            RefreshOperationId = opId
        });

        Assert.True(resp2.IsDuplicateRetry);

        var device = await db.AgentDevices.FirstAsync(d => d.DeviceId == pairResp.DeviceId);
        Assert.Equal(AgentDeviceState.Active, device.State);
    }

    [Fact]
    public async Task DifferentOperationReuse_RevokesTokenFamilyAndDevice()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_diff_op_1"
        });

        // 1. Legitimate rotation with op_legit_1
        var refreshResp1 = await service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = pairResp.RefreshToken,
            RefreshOperationId = "op_legit_1"
        });

        Assert.False(refreshResp1.IsDuplicateRetry);

        // 2. Attacker attempts replay with a DIFFERENT operation ID (op_attacker_2)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = pairResp.RefreshToken,
            RefreshOperationId = "op_attacker_2"
        }));

        Assert.Contains("Replayed refresh token detected", ex.Message);

        var device = await db.AgentDevices.FirstAsync(d => d.DeviceId == pairResp.DeviceId);
        Assert.Equal(AgentDeviceState.Revoked, device.State);
        Assert.NotNull(device.RevokedAtUtc);
    }
}
