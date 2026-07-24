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
    public async Task LostResponseRecovery_ReturnsExactCommittedCredentials()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_lost_resp_1"
        });

        var opId = $"op_lost_resp_{Guid.NewGuid():N}";

        // 1. Initial rotation request succeeds on server, but HTTP response is lost before client receives it
        var firstResp = await service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = pairResp.RefreshToken,
            RefreshOperationId = opId
        });

        Assert.False(firstResp.IsDuplicateRetry);
        Assert.NotEmpty(firstResp.AccessToken);
        Assert.NotEmpty(firstResp.RefreshToken);

        // 2. Client retries using the same old token and SAME RefreshOperationId
        var recoveredResp = await service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = pairResp.RefreshToken,
            RefreshOperationId = opId
        });

        Assert.True(recoveredResp.IsDuplicateRetry);
        Assert.Equal(firstResp.AccessToken, recoveredResp.AccessToken);
        Assert.Equal(firstResp.RefreshToken, recoveredResp.RefreshToken);
        Assert.Equal(firstResp.AccessExpiresAtUtc, recoveredResp.AccessExpiresAtUtc);

        // 3. The recovered refresh token can now perform the NEXT valid rotation!
        var nextOpId = $"op_next_rot_{Guid.NewGuid():N}";
        var nextResp = await service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = recoveredResp.RefreshToken,
            RefreshOperationId = nextOpId
        });

        Assert.False(nextResp.IsDuplicateRetry);
        Assert.NotEmpty(nextResp.RefreshToken);
        Assert.NotEqual(recoveredResp.RefreshToken, nextResp.RefreshToken);
    }

    [Fact]
    public async Task EncryptedEnvelope_ContainsNoSearchablePlaintextToken()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_env_security_1"
        });

        var opId = $"op_sec_{Guid.NewGuid():N}";
        var refreshResp = await service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = pairResp.RefreshToken,
            RefreshOperationId = opId
        });

        var envelope = await db.AgentRefreshOperationResults.FirstAsync();
        Assert.NotEmpty(envelope.EncryptedPayload);
        Assert.NotEmpty(envelope.Nonce);
        Assert.NotEmpty(envelope.Tag);

        // Verify plaintext refresh token does not exist in DB columns
        var rawPayloadString = System.Text.Encoding.UTF8.GetString(envelope.EncryptedPayload);
        Assert.DoesNotContain(refreshResp.RefreshToken, rawPayloadString);
        Assert.DoesNotContain(refreshResp.AccessToken, rawPayloadString);
    }

    [Fact]
    public async Task MissingOrMalformedOperationId_IsRejectedBeforeRotation()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_bad_opid_1"
        });

        // Empty operation ID
        await Assert.ThrowsAsync<ArgumentException>(() => service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = pairResp.RefreshToken,
            RefreshOperationId = ""
        }));

        // Too short operation ID
        await Assert.ThrowsAsync<ArgumentException>(() => service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = pairResp.RefreshToken,
            RefreshOperationId = "short"
        }));

        // Token was NOT consumed by failed validation
        var cred = await db.AgentCredentials.FirstAsync();
        Assert.Null(cred.ConsumedAtUtc);
    }

    [Fact]
    public async Task DifferentOperationId_TriggersConfirmedReplayRevocation()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_replay_diff_op_1"
        });

        var op1 = $"op_valid_{Guid.NewGuid():N}";
        var refreshResp1 = await service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = pairResp.RefreshToken,
            RefreshOperationId = op1
        });

        // Attacker replays consumed token with DIFFERENT operation ID
        var opAttacker = $"op_attacker_{Guid.NewGuid():N}";
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = pairResp.DeviceId,
            RefreshToken = pairResp.RefreshToken,
            RefreshOperationId = opAttacker
        }));

        Assert.Contains("Replayed refresh token detected", ex.Message);

        var device = await db.AgentDevices.FirstAsync(d => d.DeviceId == pairResp.DeviceId);
        Assert.Equal(AgentDeviceState.Revoked, device.State);
    }
}
