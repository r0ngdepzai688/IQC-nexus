using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;
using IqcQms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IqcQms.Infrastructure.DataPlatform;

public sealed class ImportOutboxBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImportOutboxBackgroundWorker> _logger;

    public ImportOutboxBackgroundWorker(IServiceProvider serviceProvider, ILogger<ImportOutboxBackgroundWorker> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ImportOutboxBackgroundWorker starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var pendingMessages = await db.PersistentImportOutboxMessages
                    .Where(m => !m.IsDispatched)
                    .OrderBy(m => m.OccurredAtUtc)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                if (pendingMessages.Count > 0)
                {
                    var now = DateTimeOffset.UtcNow;
                    foreach (var msg in pendingMessages)
                    {
                        msg.IsDispatched = true;
                        msg.DispatchedAtUtc = now;
                        db.PersistentImportOutboxMessages.Update(msg);
                        ImportMetrics.OutboxDispatch.Add(1);
                    }

                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Dispatched {Count} outbox messages.", pendingMessages.Count);
                }
                else
                {
                    await Task.Delay(2000, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages.");
                ImportMetrics.OutboxFailure.Add(1);
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("ImportOutboxBackgroundWorker shutting down cleanly.");
    }
}
