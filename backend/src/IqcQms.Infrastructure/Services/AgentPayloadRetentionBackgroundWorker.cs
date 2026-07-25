using IqcQms.Application.Services;
using IqcQms.Infrastructure.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IqcQms.Infrastructure.Services;

public class AgentPayloadRetentionBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AgentPayloadRetentionOptions _options;
    private readonly ILogger<AgentPayloadRetentionBackgroundWorker> _logger;

    public AgentPayloadRetentionBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<AgentPayloadRetentionOptions> options,
        ILogger<AgentPayloadRetentionBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _options.Validate();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AgentPayloadRetentionBackgroundWorker started with interval {Interval}", _options.CleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var retentionService = scope.ServiceProvider.GetRequiredService<IAgentPayloadRetentionService>();

                var submissionsCleaned = await retentionService.CleanupExpiredSubmissionsAsync(stoppingToken);
                var tombstonesCleaned = await retentionService.CleanupExpiredTombstonesAsync(stoppingToken);
                var refreshEnvelopesCleaned = await retentionService.CleanupExpiredRefreshOperationResultsAsync(stoppingToken);

                if (submissionsCleaned > 0 || tombstonesCleaned > 0 || refreshEnvelopesCleaned > 0)
                {
                    _logger.LogInformation("Retention worker completed cycle: Submissions={Submissions}, Tombstones={Tombstones}, RefreshEnvelopes={Envelopes}",
                        submissionsCleaned, tombstonesCleaned, refreshEnvelopesCleaned);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred during retention background cleanup cycle.");
            }

            try
            {
                await Task.Delay(_options.CleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
