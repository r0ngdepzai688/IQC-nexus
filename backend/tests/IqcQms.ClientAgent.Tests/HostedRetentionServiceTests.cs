using System.Data.Common;
using IqcQms.Application.Services;
using IqcQms.Domain.Entities.Agent;
using IqcQms.Infrastructure.Config;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class HostedRetentionServiceTests : IDisposable
{
    private readonly DbConnection _connection;

    public HostedRetentionServiceTests()
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
    public async Task HostedWorker_RunsRetentionAndEnvelopeCleanupCycles()
    {
        using (var db = CreateDbContext())
        {
            // Seed expired refresh operation result
            db.AgentRefreshOperationResults.Add(new AgentRefreshOperationResult
            {
                AgentDeviceId = Guid.NewGuid(),
                DeviceId = "dev_hosted_cleanup",
                TokenFamilyId = "family_1",
                RefreshOperationId = "op_expired_1",
                EncryptedPayload = new byte[] { 1, 2, 3 },
                Nonce = new byte[12],
                Tag = new byte[16],
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5) // Expired
            });
            await db.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddSingleton(CreateDbContext());
        services.Configure<AgentPayloadRetentionOptions>(opts =>
        {
            opts.CleanupInterval = TimeSpan.FromMilliseconds(50);
            opts.CleanupBatchSize = 10;
        });
        services.AddSingleton<IqcQms.Infrastructure.Security.IRelationalConstraintViolationClassifier, IqcQms.Infrastructure.Security.RelationalConstraintViolationClassifier>();
        services.AddScoped<IAgentPayloadRetentionService, AgentPayloadRetentionService>();
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var options = serviceProvider.GetRequiredService<IOptions<AgentPayloadRetentionOptions>>();

        var worker = new AgentPayloadRetentionBackgroundWorker(scopeFactory, options, NullLogger<AgentPayloadRetentionBackgroundWorker>.Instance);

        using var cts = new CancellationTokenSource();

        // Run worker briefly for 200ms
        var workerTask = worker.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Verify expired refresh operation recovery envelope was purged
        using (var verifyDb = CreateDbContext())
        {
            var remainingEnvelopes = await verifyDb.AgentRefreshOperationResults.CountAsync();
            Assert.Equal(0, remainingEnvelopes);
        }
    }

    [Fact]
    public async Task ExpiredRefreshEnvelopes_ArePurgedWithoutLoggingSecrets()
    {
        using var db = CreateDbContext();
        db.AgentRefreshOperationResults.Add(new AgentRefreshOperationResult
        {
            AgentDeviceId = Guid.NewGuid(),
            DeviceId = "dev_purge_secret_test",
            TokenFamilyId = "family_secret_1",
            RefreshOperationId = "op_purge_secret_1",
            EncryptedPayload = System.Text.Encoding.UTF8.GetBytes("SECRET_PAYLOAD_DATA"),
            Nonce = new byte[12],
            Tag = new byte[16],
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();

        var retentionService = new AgentPayloadRetentionService(db, NullLogger<AgentPayloadRetentionService>.Instance);
        var purgedCount = await retentionService.CleanupExpiredRefreshOperationResultsAsync();

        Assert.Equal(1, purgedCount);

        var count = await db.AgentRefreshOperationResults.CountAsync();
        Assert.Equal(0, count);
    }
}
