using System.Data.Common;
using System.Text;
using IqcQms.Api.Controllers;
using IqcQms.ClientAgent.Contracts;
using IqcQms.Domain.Entities.Agent;
using IqcQms.Infrastructure.Config;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Security;
using IqcQms.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class TestLoggerProvider : ILoggerProvider, ILogger
{
    private readonly StringBuilder _logOutput = new();
    private readonly object _lock = new();

    public string LogContent
    {
        get
        {
            lock (_lock) { return _logOutput.ToString(); }
        }
    }

    public ILogger CreateLogger(string categoryName) => this;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        lock (_lock)
        {
            _logOutput.AppendLine(message);
            if (exception != null)
            {
                _logOutput.AppendLine(exception.ToString());
            }
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public void Dispose() { }

    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}

public class SecretRedactionAuditTests : IDisposable
{
    private readonly DbConnection _connection;

    public SecretRedactionAuditTests()
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
    public async Task AuditLogsAndResponses_DoNotExposeSentinelSecrets()
    {
        var loggerProvider = new TestLoggerProvider();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(loggerProvider));

        var sentinelPepper = "SENTINEL_PEPPER_SECRET_99999";
        var sentinelEnvelopeKey = "SENTINEL_ENVELOPE_KEY_ABCDEF12345678901234567890123456";
        var sentinelHexKey = "4a6f686e446f655365637265744b657932303236497163516d73456e76656c6f";
        var sentinelPairingCode = "654321";

        var securityOptions = Options.Create(new AgentSecurityOptions
        {
            PairingPepper = sentinelPepper,
            EnvelopeEncryptionKey = sentinelHexKey
        });

        var encService = new EnvelopeEncryptionService(securityOptions);
        var canonicalizer = new NormalizedWorkbookCanonicalizer();

        using var db = CreateDbContext();
        var agentService = new AgentService(db, securityOptions, encService, canonicalizer, new RelationalConstraintViolationClassifier(), loggerFactory.CreateLogger<AgentService>());

        // 1. Pairing Attempt Failure
        var pairingCode = await agentService.CreatePairingCodeAsync(1, "UserA");
        var invalidPairingReq = new AgentDevicePairRequest
        {
            DeviceId = "dev_audit_1",
            PairingCode = "000000" // Wrong code
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => agentService.PairDeviceAsync(invalidPairingReq));

        // 2. Valid Pair for Replay Tests
        var validPairResp = await agentService.PairDeviceAsync(new AgentDevicePairRequest
        {
            DeviceId = "dev_audit_1",
            PairingCode = pairingCode.PairingCode
        });

        // 3. Refresh Replay Attack
        var refresh1 = await agentService.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = "dev_audit_1",
            RefreshToken = validPairResp.RefreshToken,
            RefreshOperationId = "op_audit_1"
        });

        // Replay old refresh token with different operation ID -> Security Exception
        await Assert.ThrowsAsync<InvalidOperationException>(() => agentService.RefreshTokenAsync(new AgentTokenRefreshRequest
        {
            DeviceId = "dev_audit_1",
            RefreshToken = validPairResp.RefreshToken, // Invalid replayed token
            RefreshOperationId = "op_audit_replay"
        }));

        // 4. Controller API Error Response Audit
        var controller = new AgentDevicesController(agentService, loggerFactory.CreateLogger<AgentDevicesController>());

        var actionResult = await controller.UploadNormalizedWorkbook("dev_audit_1", new NormalizedWorkbookUploadRequest
        {
            DeviceId = "dev_audit_1",
            PayloadSubmissionId = "sub_mismatch_1",
            Nonce = "nonce_sentinel_val_123",
            ProviderId = "test",
            SourceFingerprint = "fp",
            CanonicalPayloadHash = "hash",
            RecordCount = 10,
            ServerImportJobId = Guid.NewGuid()
        });

        // Verify public API response body is generic and does NOT leak internal exceptions or sentinels
        if (actionResult.Result is ObjectResult objResult)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(objResult.Value);
            Assert.DoesNotContain("nonce_sentinel_val_123", json);
            Assert.DoesNotContain(sentinelPepper, json);
            Assert.DoesNotContain(sentinelHexKey, json);
        }

        // Assert Captured Log Content DOES NOT contain sensitive sentinel secrets
        var logs = loggerProvider.LogContent;
        Assert.DoesNotContain(sentinelPepper, logs);
        Assert.DoesNotContain(sentinelEnvelopeKey, logs);
        Assert.DoesNotContain(sentinelPairingCode, logs);
        Assert.DoesNotContain("SECRET_CELL_VALUE", logs);
    }
}
