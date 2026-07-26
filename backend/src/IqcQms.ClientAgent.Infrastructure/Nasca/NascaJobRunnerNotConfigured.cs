using IqcQms.ClientAgent.Application.Nasca;
using Microsoft.Extensions.Logging;

namespace IqcQms.ClientAgent.Infrastructure.Nasca;

public class NascaJobRunnerNotConfigured : INascaJobRunner
{
    private readonly ILogger<NascaJobRunnerNotConfigured> _logger;

    public NascaJobRunnerNotConfigured(ILogger<NascaJobRunnerNotConfigured> logger)
    {
        _logger = logger;
    }

    public Task<NascaJobResult> RunJobAsync(NascaJobRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("NASCA integration runner is not configured. Job {JobId} (Correlation: {CorrelationId}) rejected safely.",
            request.JobId, request.CorrelationId);

        var result = new NascaJobResult
        {
            Outcome = NascaJobOutcome.NotConfigured,
            ExitCode = null,
            SanitizedReasonCode = "NASCA_NOT_CONFIGURED",
            OutputFiles = new List<string>(),
            StartedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        };

        return Task.FromResult(result);
    }
}
