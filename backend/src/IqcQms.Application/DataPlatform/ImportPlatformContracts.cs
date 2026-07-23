namespace IqcQms.Application.DataPlatform;

[Flags]
public enum DataSourceProviderCapabilities
{
    None = 0,
    LocalFile = 1,
    MultipleWorksheets = 2,
    WorksheetVisibility = 4,
    MergedRanges = 8,
    NumberFormats = 16,
    Formulas = 32,
    AgentSubmission = 64
}

public sealed record DataSourceDescriptor(
    DataSourceProviderKind Kind,
    string DisplayName,
    string? FileName,
    string? ContentType,
    long? ContentLength);

public sealed record DataSourceProviderContext(
    DataSourceDescriptor Source,
    Stream Payload,
    ImportPlatformLimits Limits);

public sealed record ImportPlatformLimits(
    long MaximumPayloadBytes = 50L * 1024 * 1024,
    int MaximumWorksheets = 32,
    int MaximumRowsPerWorksheet = 100_000,
    int MaximumColumnsPerWorksheet = 1_024,
    long MaximumCells = 2_000_000)
{
    public static ImportPlatformLimits Default { get; } = new();
}

public interface IDataSourceProvider
{
    DataSourceProviderKind Kind { get; }
    DataSourceProviderCapabilities Capabilities { get; }
    bool CanHandle(DataSourceDescriptor source);
    Task<NormalizedWorkbook> NormalizeAsync(
        DataSourceProviderContext context,
        CancellationToken cancellationToken = default);
}

public interface IDataSourceProviderRegistry
{
    IDataSourceProvider GetRequired(DataSourceDescriptor source);
    bool TryGet(DataSourceDescriptor source, out IDataSourceProvider? provider);
}

public interface IWorkbookNormalizer
{
    Task<NormalizedWorkbook> NormalizeAsync(
        DataSourceProviderContext context,
        CancellationToken cancellationToken = default);
}

public sealed record ImportJobReference(string JobId, string OwnerUserId, ImportJobState State);
public sealed record ImportFieldMapping(int SourceColumnNumber, string DestinationField, string? Transform = null);
public sealed record ImportValidationSummary(int Valid, int Warnings, int Errors, int Skipped);
public sealed record ImportPreviewReference(string JobId, string PreviewVersion, ImportValidationSummary Summary);
public sealed record ImportCommitResult(string JobId, bool Replayed, int Inserted, int Updated, int Skipped);
public sealed record ImportAuditEvent(string JobId, string EventType, string ActorUserId, DateTimeOffset OccurredAt);

public interface IImportJobService
{
    Task<ImportJobReference> CreateAsync(
        NormalizedWorkbook workbook,
        string ownerUserId,
        CancellationToken cancellationToken = default);
}

public interface IImportMappingService
{
    Task ConfirmAsync(
        string jobId,
        string actorUserId,
        IReadOnlyCollection<ImportFieldMapping> mappings,
        CancellationToken cancellationToken = default);
}

public interface IImportValidationService
{
    Task<ImportValidationSummary> ValidateAsync(
        string jobId,
        string actorUserId,
        CancellationToken cancellationToken = default);
}

public interface IImportPreviewService
{
    Task<ImportPreviewReference> CreateAsync(
        string jobId,
        string actorUserId,
        CancellationToken cancellationToken = default);
}

public interface IImportCommitService
{
    Task<ImportCommitResult> CommitAsync(
        string jobId,
        string previewVersion,
        string idempotencyKey,
        string actorUserId,
        CancellationToken cancellationToken = default);
}

public interface IImportAuditService
{
    Task RecordAsync(ImportAuditEvent auditEvent, CancellationToken cancellationToken = default);
}

public sealed class ImportPlatformException : Exception
{
    public ImportPlatformException(string code, string safeMessage, Exception? innerException = null)
        : base(safeMessage, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}

public static class ImportErrorCodes
{
    public const string ProviderNotFound = "IMPORT_PROVIDER_NOT_FOUND";
    public const string FileTypeUnsupported = "IMPORT_FILE_TYPE_UNSUPPORTED";
    public const string FileTooLarge = "IMPORT_FILE_TOO_LARGE";
    public const string WorkbookEmpty = "IMPORT_WORKBOOK_EMPTY";
    public const string WorksheetLimitExceeded = "IMPORT_WORKSHEET_LIMIT_EXCEEDED";
    public const string RowLimitExceeded = "IMPORT_ROW_LIMIT_EXCEEDED";
    public const string ColumnLimitExceeded = "IMPORT_COLUMN_LIMIT_EXCEEDED";
    public const string CellLimitExceeded = "IMPORT_CELL_LIMIT_EXCEEDED";
    public const string NormalizationFailed = "IMPORT_NORMALIZATION_FAILED";
    public const string MappingInvalid = "IMPORT_MAPPING_INVALID";
    public const string ValidationFailed = "IMPORT_VALIDATION_FAILED";
    public const string PreviewRequired = "IMPORT_PREVIEW_REQUIRED";
    public const string CommitConflict = "IMPORT_COMMIT_CONFLICT";
    public const string CommitReplayed = "IMPORT_COMMIT_REPLAYED";
    public const string SessionForbidden = "IMPORT_SESSION_FORBIDDEN";
    public const string Cancelled = "IMPORT_CANCELLED";
    public const string ProtocolUnsupported = "IMPORT_PROTOCOL_UNSUPPORTED";
    public const string InternalError = "IMPORT_INTERNAL_ERROR";
}
