namespace IqcQms.Application.DataPlatform;

public enum ImportJobState
{
    Created,
    Inspecting,
    ReadyForMapping,
    Validating,
    ReadyForReview,
    Committing,
    Completed,
    Failed,
    Cancelled
}

public static class ImportJobTransitionGuard
{
    private static readonly IReadOnlyDictionary<ImportJobState, IReadOnlySet<ImportJobState>> Allowed =
        new Dictionary<ImportJobState, IReadOnlySet<ImportJobState>>
        {
            [ImportJobState.Created] = Set(ImportJobState.Inspecting, ImportJobState.Cancelled),
            [ImportJobState.Inspecting] = Set(ImportJobState.ReadyForMapping, ImportJobState.Failed, ImportJobState.Cancelled),
            [ImportJobState.ReadyForMapping] = Set(ImportJobState.Validating, ImportJobState.Cancelled),
            [ImportJobState.Validating] = Set(ImportJobState.ReadyForReview, ImportJobState.ReadyForMapping, ImportJobState.Failed, ImportJobState.Cancelled),
            [ImportJobState.ReadyForReview] = Set(ImportJobState.Committing, ImportJobState.Validating, ImportJobState.Cancelled),
            [ImportJobState.Committing] = Set(ImportJobState.Completed, ImportJobState.ReadyForReview, ImportJobState.Failed),
            [ImportJobState.Completed] = Set(),
            [ImportJobState.Failed] = Set(),
            [ImportJobState.Cancelled] = Set()
        };

    public static bool CanTransition(ImportJobState current, ImportJobState next) =>
        Allowed[current].Contains(next);

    public static void EnsureCanTransition(ImportJobState current, ImportJobState next)
    {
        if (!CanTransition(current, next))
            throw new ImportPlatformException(ImportErrorCodes.InvalidTransition, "The import job transition is not allowed.");
    }

    private static IReadOnlySet<ImportJobState> Set(params ImportJobState[] states) =>
        new HashSet<ImportJobState>(states);
}
