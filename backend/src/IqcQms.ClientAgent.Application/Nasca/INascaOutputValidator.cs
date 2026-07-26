namespace IqcQms.ClientAgent.Application.Nasca;

public enum NascaOutputValidationOutcome
{
    Valid = 0,
    Missing,
    Empty,
    Unstable,
    OutsideApprovedRoot,
    TraversalDetected,
    ReparsePointDetected,
    UnexpectedDirectory,
    MaximumDepthExceeded,
    TooManyFiles,
    SingleFileSizeExceeded,
    TotalSizeExceeded,
    CorrelationMismatch,
    DuplicateOutput,
    Cancelled,
    ValidationTimedOut,
    AccessDenied,
    QuarantineRequired,
    UnknownFailure
}

public class NascaValidatedFileDescriptor
{
    public string RelativePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public int FileIndex { get; set; }
    public DateTime ValidatedUtc { get; set; } = DateTime.UtcNow;

    public void ValidateInvariants()
    {
        if (string.IsNullOrWhiteSpace(RelativePath))
            throw new InvalidOperationException("NascaValidatedFileDescriptor: RelativePath is required.");

        if (FileSizeBytes < 0)
            throw new InvalidOperationException("NascaValidatedFileDescriptor: FileSizeBytes cannot be negative.");

        if (string.IsNullOrWhiteSpace(Sha256Hash))
            throw new InvalidOperationException("NascaValidatedFileDescriptor: Sha256Hash is required.");

        if (FileIndex < 0)
            throw new InvalidOperationException("NascaValidatedFileDescriptor: FileIndex cannot be negative.");
    }
}

public class NascaOutputValidationResult
{
    public NascaOutputValidationOutcome Outcome { get; set; } = NascaOutputValidationOutcome.UnknownFailure;
    public string SanitizedReasonCode { get; set; } = "UNINITIALIZED";
    public bool IsValid => Outcome == NascaOutputValidationOutcome.Valid;
    public bool IsRetryable => Outcome == NascaOutputValidationOutcome.Unstable || Outcome == NascaOutputValidationOutcome.ValidationTimedOut;
    public bool RequiresQuarantine =>
        Outcome == NascaOutputValidationOutcome.OutsideApprovedRoot ||
        Outcome == NascaOutputValidationOutcome.TraversalDetected ||
        Outcome == NascaOutputValidationOutcome.ReparsePointDetected ||
        Outcome == NascaOutputValidationOutcome.CorrelationMismatch ||
        Outcome == NascaOutputValidationOutcome.QuarantineRequired;

    public int ValidatedFileCount { get; set; }
    public long ValidatedTotalSizeBytes { get; set; }
    public IReadOnlyList<NascaValidatedFileDescriptor> Descriptors { get; set; } = Array.Empty<NascaValidatedFileDescriptor>();
    public DateTime ValidationStartedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ValidationCompletedUtc { get; set; } = DateTime.UtcNow;

    public void ValidateInvariants()
    {
        if (string.IsNullOrWhiteSpace(SanitizedReasonCode))
            throw new InvalidOperationException("NascaOutputValidationResult: SanitizedReasonCode is required.");

        if (ValidatedFileCount < 0)
            throw new InvalidOperationException("NascaOutputValidationResult: ValidatedFileCount cannot be negative.");

        if (ValidatedTotalSizeBytes < 0)
            throw new InvalidOperationException("NascaOutputValidationResult: ValidatedTotalSizeBytes cannot be negative.");

        if (ValidationCompletedUtc < ValidationStartedUtc)
            throw new InvalidOperationException("NascaOutputValidationResult: ValidationCompletedUtc cannot be earlier than ValidationStartedUtc.");

        if (IsValid && RequiresQuarantine)
            throw new InvalidOperationException("NascaOutputValidationResult: Valid outcome cannot require quarantine.");

        if (IsValid && IsRetryable)
            throw new InvalidOperationException("NascaOutputValidationResult: Valid outcome cannot be retryable.");

        if (!IsValid && Outcome == NascaOutputValidationOutcome.Valid)
            throw new InvalidOperationException("NascaOutputValidationResult: Invalid IsValid/Outcome state.");

        foreach (var descriptor in Descriptors)
        {
            descriptor.ValidateInvariants();
        }
    }
}

public class NascaOutputValidationOptions
{
    public int MaximumFileCount { get; set; } = 100;
    public int MaximumDirectoryCount { get; set; } = 0; // Default: reject nested directories
    public long MaximumSingleFileSizeBytes { get; set; } = 100 * 1024 * 1024; // 100 MB
    public long MaximumTotalOutputSizeBytes { get; set; } = 500 * 1024 * 1024; // 500 MB
    public int MaximumDirectoryDepth { get; set; } = 0;
    public TimeSpan StabilityWindow { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan StabilityPollingInterval { get; set; } = TimeSpan.FromMilliseconds(200);
    public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(10);

    // Milestone 3A.6.1 compatibility method retained for backward compatibility.
    // Existing validation behavior is preserved to avoid runtime/test regressions.
    public void Validate()
    {
        if (MaximumFileCount <= 0 || MaximumFileCount > 1000)
            throw new InvalidOperationException("NascaOutputValidationOptions: MaximumFileCount must be positive and bounded between 1 and 1000.");

        if (MaximumDirectoryCount < 0 || MaximumDirectoryCount > 50)
            throw new InvalidOperationException("NascaOutputValidationOptions: MaximumDirectoryCount must be non-negative and bounded between 0 and 50.");

        if (MaximumSingleFileSizeBytes <= 0 || MaximumSingleFileSizeBytes > 1024 * 1024 * 1024L)
            throw new InvalidOperationException("NascaOutputValidationOptions: MaximumSingleFileSizeBytes must be positive and <= 1 GB.");

        if (MaximumTotalOutputSizeBytes <= 0 || MaximumTotalOutputSizeBytes > 5 * 1024 * 1024 * 1024L)
            throw new InvalidOperationException("NascaOutputValidationOptions: MaximumTotalOutputSizeBytes must be positive and <= 5 GB.");

        if (MaximumTotalOutputSizeBytes < MaximumSingleFileSizeBytes)
            throw new InvalidOperationException("NascaOutputValidationOptions: MaximumTotalOutputSizeBytes cannot be smaller than MaximumSingleFileSizeBytes.");

        if (MaximumDirectoryDepth < 0 || MaximumDirectoryDepth > 5)
            throw new InvalidOperationException("NascaOutputValidationOptions: MaximumDirectoryDepth must be bounded between 0 and 5.");

        if (StabilityPollingInterval <= TimeSpan.Zero)
            throw new InvalidOperationException("NascaOutputValidationOptions: StabilityPollingInterval must be positive.");

        if (ValidationTimeout <= TimeSpan.Zero)
            throw new InvalidOperationException("NascaOutputValidationOptions: ValidationTimeout must be positive.");

        if (StabilityPollingInterval >= ValidationTimeout)
            throw new InvalidOperationException("NascaOutputValidationOptions: StabilityPollingInterval must be strictly smaller than ValidationTimeout.");

        if (StabilityWindow >= ValidationTimeout)
            throw new InvalidOperationException("NascaOutputValidationOptions: StabilityWindow must be strictly smaller than ValidationTimeout.");
    }
}

public class NascaOutputValidationRequest
{
    public string CorrelationId { get; set; } = string.Empty;
    public string ExecutionId { get; set; } = string.Empty;
    public int AttemptNumber { get; set; } = 1;
    public string WorkDirectoryId { get; set; } = string.Empty;
    public string OutputRoot { get; set; } = string.Empty;
    public string AgentWorkspaceId { get; set; } = string.Empty;
    public NascaOutputValidationOptions Options { get; set; } = new();

    public void ValidateInvariants()
    {
        if (string.IsNullOrWhiteSpace(CorrelationId))
            throw new InvalidOperationException("NascaOutputValidationRequest: CorrelationId is required.");

        if (string.IsNullOrWhiteSpace(ExecutionId))
            throw new InvalidOperationException("NascaOutputValidationRequest: ExecutionId is required.");

        if (AttemptNumber < 1)
            throw new InvalidOperationException("NascaOutputValidationRequest: AttemptNumber must be >= 1.");

        if (string.IsNullOrWhiteSpace(WorkDirectoryId))
            throw new InvalidOperationException("NascaOutputValidationRequest: WorkDirectoryId is required.");

        if (string.IsNullOrWhiteSpace(OutputRoot))
            throw new InvalidOperationException("NascaOutputValidationRequest: OutputRoot is required.");

        if (string.IsNullOrWhiteSpace(AgentWorkspaceId))
            throw new InvalidOperationException("NascaOutputValidationRequest: AgentWorkspaceId is required.");

        if (Options is null)
            throw new InvalidOperationException("NascaOutputValidationRequest: Options is required.");
    }
}

public interface INascaOutputValidator
{
    Task<NascaOutputValidationResult> ValidateOutputAsync(NascaOutputValidationRequest request, CancellationToken cancellationToken = default);
}
