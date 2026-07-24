using IqcQms.ClientAgent.Application.Config;
using IqcQms.ClientAgent.Infrastructure.Queue;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class LocalAgentQueueTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AgentOptions _options;

    public LocalAgentQueueTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"agent_queue_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _options = new AgentOptions
        {
            LocalStorePath = _tempDir,
            AllowedInputRoots = new List<string> { _tempDir }
        };
        _options.Validate(true);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    [Fact]
    public async Task EnqueueAndLease_WorksAtomically()
    {
        var queue = new SqliteLocalAgentQueue(_tempDir, _options, NullLogger<SqliteLocalAgentQueue>.Instance);
        await queue.InitializeAsync();

        var payloadPath = Path.Combine(_tempDir, "sample.synthetic.json");
        await File.WriteAllTextAsync(payloadPath, "{}");

        var serverJobId = Guid.NewGuid();
        var item = await queue.EnqueueJobAsync(serverJobId, "SyntheticNormalization", payloadPath);
        Assert.NotNull(item);
        Assert.Equal("Pending", item.State);

        var leased = await queue.AcquireNextLeaseAsync("worker-1", TimeSpan.FromMinutes(1));
        Assert.NotNull(leased);
        Assert.Equal(item.LocalJobId, leased.LocalJobId);
        Assert.Equal("Leased", leased.State);
        Assert.Equal("worker-1", leased.LeaseOwner);

        await queue.CompleteJobAsync(leased.LocalJobId);

        var pendingCount = await queue.GetPendingCountAsync();
        Assert.Equal(0, pendingCount);
    }

    [Fact]
    public async Task Enqueue_RejectsPathOutsideAllowedRoots()
    {
        var queue = new SqliteLocalAgentQueue(_tempDir, _options, NullLogger<SqliteLocalAgentQueue>.Instance);
        await queue.InitializeAsync();

        var forbiddenPath = @"C:\Windows\System32\drivers\etc\hosts";
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            queue.EnqueueJobAsync(Guid.NewGuid(), "SyntheticNormalization", forbiddenPath));
    }

    [Fact]
    public async Task StaleLeaseRecovery_ResetsExpiredLeasesToPending()
    {
        var queue = new SqliteLocalAgentQueue(_tempDir, _options, NullLogger<SqliteLocalAgentQueue>.Instance);
        await queue.InitializeAsync();

        var payloadPath = Path.Combine(_tempDir, "sample.synthetic.json");
        await File.WriteAllTextAsync(payloadPath, "{}");

        var item = await queue.EnqueueJobAsync(Guid.NewGuid(), "SyntheticNormalization", payloadPath);
        var leased = await queue.AcquireNextLeaseAsync("worker-stale", TimeSpan.FromMilliseconds(1));

        await Task.Delay(50); // Wait for lease to expire

        await queue.RecoverStaleLeasesAsync();

        var count = await queue.GetPendingCountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task FailJob_TransitionsToPoisonedAfterMaxAttempts()
    {
        var queue = new SqliteLocalAgentQueue(_tempDir, _options, NullLogger<SqliteLocalAgentQueue>.Instance);
        await queue.InitializeAsync();

        var payloadPath = Path.Combine(_tempDir, "sample.synthetic.json");
        await File.WriteAllTextAsync(payloadPath, "{}");

        var item = await queue.EnqueueJobAsync(Guid.NewGuid(), "SyntheticNormalization", payloadPath);

        for (int i = 0; i < 5; i++)
        {
            var leased = await queue.AcquireNextLeaseAsync("worker-1", TimeSpan.FromMinutes(1));
            Assert.NotNull(leased);
            await queue.FailJobAsync(leased.LocalJobId, "ERR_FAIL", TimeSpan.Zero);
        }

        var count = await queue.GetPendingCountAsync();
        Assert.Equal(0, count); // Poisoned jobs are not counted as active pending
    }
}
