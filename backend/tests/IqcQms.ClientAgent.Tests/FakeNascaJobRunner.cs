using IqcQms.ClientAgent.Application.Nasca;

namespace IqcQms.ClientAgent.Tests;

public enum FakeNascaScenario
{
    Success,
    Timeout,
    Cancelled,
    RetryableFailure,
    PermanentFailure,
    OutputMissing,
    InvalidOutput,
    SimulatedCrash,
    DuplicateCorrelation
}

public class FakeNascaJobRunner : INascaJobRunner
{
    private readonly HashSet<string> _seenCorrelations = new();
    private readonly object _lock = new();

    public FakeNascaScenario Scenario { get; set; } = FakeNascaScenario.Success;
    public int InvocationCount { get; private set; }
    public string? LastCorrelationId { get; private set; }
    public IReadOnlyCollection<string> SeenCorrelations
    {
        get
        {
            lock (_lock) { return _seenCorrelations.ToList(); }
        }
    }

    public Task<NascaJobResult> RunJobAsync(NascaJobRequest request, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            InvocationCount++;
            LastCorrelationId = request.CorrelationId;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(new NascaJobResult
            {
                Outcome = NascaJobOutcome.Cancelled,
                ExitCode = null,
                SanitizedReasonCode = "SIMULATED_JOB_CANCELLED",
                StartedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow
            });
        }

        lock (_lock)
        {
            if (_seenCorrelations.Contains(request.CorrelationId))
            {
                return Task.FromResult(new NascaJobResult
                {
                    Outcome = NascaJobOutcome.DuplicateCorrelation,
                    ExitCode = null,
                    SanitizedReasonCode = "DUPLICATE_CORRELATION_DETECTED",
                    StartedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow
                });
            }
            _seenCorrelations.Add(request.CorrelationId);
        }

        switch (Scenario)
        {
            case FakeNascaScenario.Success:
                return Task.FromResult(new NascaJobResult
                {
                    Outcome = NascaJobOutcome.Success,
                    ExitCode = 0,
                    SanitizedReasonCode = "SIMULATED_SUCCESS",
                    OutputFiles = new List<string> { "output.json" },
                    StartedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow
                });

            case FakeNascaScenario.Timeout:
                return Task.FromResult(new NascaJobResult
                {
                    Outcome = NascaJobOutcome.Timeout,
                    ExitCode = null,
                    SanitizedReasonCode = "SIMULATED_TIMEOUT",
                    StartedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow
                });

            case FakeNascaScenario.Cancelled:
                return Task.FromResult(new NascaJobResult
                {
                    Outcome = NascaJobOutcome.Cancelled,
                    ExitCode = null,
                    SanitizedReasonCode = "SIMULATED_CANCELLED",
                    StartedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow
                });

            case FakeNascaScenario.RetryableFailure:
                return Task.FromResult(new NascaJobResult
                {
                    Outcome = NascaJobOutcome.RetryableFailure,
                    ExitCode = 101,
                    SanitizedReasonCode = "SIMULATED_RETRYABLE_FAILURE",
                    StartedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow
                });

            case FakeNascaScenario.PermanentFailure:
                return Task.FromResult(new NascaJobResult
                {
                    Outcome = NascaJobOutcome.PermanentFailure,
                    ExitCode = 201,
                    SanitizedReasonCode = "SIMULATED_PERMANENT_FAILURE",
                    StartedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow
                });

            case FakeNascaScenario.OutputMissing:
                return Task.FromResult(new NascaJobResult
                {
                    Outcome = NascaJobOutcome.OutputMissing,
                    ExitCode = 0,
                    SanitizedReasonCode = "SIMULATED_OUTPUT_MISSING",
                    StartedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow
                });

            case FakeNascaScenario.InvalidOutput:
                return Task.FromResult(new NascaJobResult
                {
                    Outcome = NascaJobOutcome.InvalidOutput,
                    ExitCode = 0,
                    SanitizedReasonCode = "SIMULATED_INVALID_OUTPUT",
                    StartedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow
                });

            case FakeNascaScenario.SimulatedCrash:
                return Task.FromResult(new NascaJobResult
                {
                    Outcome = NascaJobOutcome.ProcessError,
                    ExitCode = -1073741819, // 0xC0000005 ACCESS_VIOLATION
                    SanitizedReasonCode = "SIMULATED_CRASH",
                    StartedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow
                });

            case FakeNascaScenario.DuplicateCorrelation:
                return Task.FromResult(new NascaJobResult
                {
                    Outcome = NascaJobOutcome.DuplicateCorrelation,
                    ExitCode = null,
                    SanitizedReasonCode = "DUPLICATE_CORRELATION_DETECTED",
                    StartedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow
                });

            default:
                return Task.FromResult(new NascaJobResult
                {
                    Outcome = NascaJobOutcome.NotConfigured,
                    ExitCode = null,
                    SanitizedReasonCode = "UNHANDLED_SCENARIO",
                    StartedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow
                });
        }
    }
}
