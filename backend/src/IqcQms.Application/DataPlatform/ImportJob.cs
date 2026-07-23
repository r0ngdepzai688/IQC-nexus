namespace IqcQms.Application.DataPlatform;

public sealed class ImportJob
{
    private readonly Dictionary<string, ImportCommitResult> _commitReceipts =
        new(StringComparer.Ordinal);

    public ImportJob(string jobId, string ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("A job identifier is required.", nameof(jobId));
        if (string.IsNullOrWhiteSpace(ownerUserId))
            throw new ArgumentException("An owner identifier is required.", nameof(ownerUserId));

        JobId = jobId;
        OwnerUserId = ownerUserId;
    }

    public string JobId { get; }
    public string OwnerUserId { get; }
    public ImportJobState State { get; private set; } = ImportJobState.Created;
    public string? PreviewVersion { get; private set; }

    public void TransitionTo(ImportJobState nextState, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ImportJobTransitionGuard.CanTransition(State, nextState))
            ThrowInvalidTransition();
        State = nextState;
        if (nextState != ImportJobState.ReadyForReview)
            PreviewVersion = null;
    }

    public void MarkPreviewReady(string previewVersion)
    {
        if (State != ImportJobState.Validating)
            ThrowInvalidTransition();
        if (string.IsNullOrWhiteSpace(previewVersion))
            throw new ImportPlatformException(
                ImportErrorCodes.PreviewRequired,
                "A preview version is required before commit.");

        PreviewVersion = previewVersion;
        State = ImportJobState.ReadyForReview;
    }

    public void BeginCommit(
        string previewVersion,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (State != ImportJobState.ReadyForReview ||
            string.IsNullOrWhiteSpace(PreviewVersion) ||
            !string.Equals(PreviewVersion, previewVersion, StringComparison.Ordinal))
        {
            throw new ImportPlatformException(
                ImportErrorCodes.PreviewRequired,
                "A current preview is required before commit.");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ImportPlatformException(
                ImportErrorCodes.CommitConflict,
                "A commit idempotency key is required.");

        if (!_commitReceipts.ContainsKey(idempotencyKey))
            TransitionTo(ImportJobState.Committing, cancellationToken);
    }

    public ImportCommitResult CompleteCommit(
        string idempotencyKey,
        int inserted,
        int updated,
        int skipped)
    {
        if (_commitReceipts.TryGetValue(idempotencyKey, out var receipt))
            return receipt with { Replayed = true };
        if (State != ImportJobState.Committing)
            ThrowInvalidTransition();

        var result = new ImportCommitResult(JobId, false, inserted, updated, skipped);
        _commitReceipts.Add(idempotencyKey, result);
        State = ImportJobState.Completed;
        return result;
    }

    public void Cancel()
    {
        if (!ImportJobTransitionGuard.CanTransition(State, ImportJobState.Cancelled))
            ThrowInvalidTransition();
        State = ImportJobState.Cancelled;
        PreviewVersion = null;
    }

    private static void ThrowInvalidTransition() =>
        throw new ImportPlatformException(
            ImportErrorCodes.InvalidTransition,
            "The import job transition is not allowed.");
}
