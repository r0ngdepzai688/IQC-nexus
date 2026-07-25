namespace IqcQms.ClientAgent.Application.Nasca;

public enum NascaJobOutcome
{
    Success,
    NotConfigured,
    InvalidInput,
    Timeout,
    Cancelled,
    RetryableFailure,
    PermanentFailure,
    OutputMissing,
    InvalidOutput,
    SimulatedCrash,
    DuplicateCorrelation,
    ProcessError,
    ValidationFailed
}

public class NascaJobRequest
{
    public string JobId { get; set; } = Guid.NewGuid().ToString("N");
    public string InputWorkbookPath { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
}

public class NascaJobResult
{
    public NascaJobOutcome Outcome { get; set; } = NascaJobOutcome.NotConfigured;
    public int? ExitCode { get; set; }
    public string SanitizedReasonCode { get; set; } = "NASCA_NOT_CONFIGURED";
    public List<string> OutputFiles { get; set; } = new();
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
}

public interface INascaJobRunner
{
    Task<NascaJobResult> RunJobAsync(NascaJobRequest request, CancellationToken cancellationToken = default);
}
