using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IqcQms.Infrastructure.DataPlatform;

public sealed class ImportCommitBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImportCommitBackgroundWorker> _logger;
    private readonly string _workerId = $"worker-{Guid.NewGuid():N}[{Environment.MachineName}]";
    private readonly SemaphoreSlim _concurrencySemaphore = new(3, 3);

    public ImportCommitBackgroundWorker(IServiceProvider serviceProvider, ILogger<ImportCommitBackgroundWorker> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ImportCommitBackgroundWorker starting. Worker ID: '{WorkerId}'", _workerId);

        var lastStaleRecovery = DateTimeOffset.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IImportWorkQueue>();
                var commitEngine = scope.ServiceProvider.GetRequiredService<IImportCommitEngine>();

                // Periodically recover stale leases
                if (DateTimeOffset.UtcNow - lastStaleRecovery > TimeSpan.FromSeconds(30))
                {
                    await queue.RecoverStaleLeasesAsync(TimeSpan.FromMinutes(2), stoppingToken);
                    lastStaleRecovery = DateTimeOffset.UtcNow;
                }

                var leases = await queue.AcquireLeasesAsync(_workerId, batchSize: 3, leaseDuration: TimeSpan.FromMinutes(2), stoppingToken);

                if (leases.Count == 0)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                var tasks = leases.Select(item => ProcessWorkItemAsync(item, stoppingToken)).ToList();
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in ImportCommitBackgroundWorker loop.");
                await Task.Delay(2000, stoppingToken);
            }
        }

        _logger.LogInformation("ImportCommitBackgroundWorker shutting down cleanly.");
    }

    private async Task ProcessWorkItemAsync(Domain.Entities.DataHub.PersistentImportWorkItem item, CancellationToken stoppingToken)
    {
        await _concurrencySemaphore.WaitAsync(stoppingToken);
        var sw = Stopwatch.StartNew();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queue = scope.ServiceProvider.GetRequiredService<IImportWorkQueue>();
            var commitEngine = scope.ServiceProvider.GetRequiredService<IImportCommitEngine>();

            var commitRequest = new ImportCommitRequest(item.JobId, item.IdempotencyKey, item.ExpectedVersion);

            _logger.LogInformation("Processing work item '{WorkItemId}' for job '{JobId}' (Attempt {Attempt}).", item.WorkItemId, item.JobId, item.AttemptCount + 1);

            var result = await commitEngine.ExecuteCommitAsync(commitRequest, item.ActorUserId, isAdmin: true, stoppingToken);

            sw.Stop();
            ImportMetrics.CommitDurationMs.Record(sw.ElapsedMilliseconds);
            ImportMetrics.ImportedRecordCount.Record(result.InsertedCount);
            ImportMetrics.CommitSuccess.Add(1);

            await queue.CompleteWorkItemAsync(item.WorkItemId, _workerId, stoppingToken);
        }
        catch (ImportPlatformException ex)
        {
            sw.Stop();
            ImportMetrics.CommitFailure.Add(1);
            _logger.LogWarning("Work item '{WorkItemId}' failed with code '{Code}': {Message}", item.WorkItemId, ex.Code, ex.Message);

            using var scope = _serviceProvider.CreateScope();
            var queue = scope.ServiceProvider.GetRequiredService<IImportWorkQueue>();
            await queue.FailWorkItemAsync(item.WorkItemId, _workerId, ex.Code, ex.Message, cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            sw.Stop();
            ImportMetrics.CommitFailure.Add(1);
            _logger.LogError(ex, "Unexpected error processing work item '{WorkItemId}'.", item.WorkItemId);

            using var scope = _serviceProvider.CreateScope();
            var queue = scope.ServiceProvider.GetRequiredService<IImportWorkQueue>();
            await queue.FailWorkItemAsync(item.WorkItemId, _workerId, "WORKER_ERROR", ex.Message, cancellationToken: stoppingToken);
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }
}
