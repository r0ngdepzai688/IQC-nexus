using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IqcQms.Application.DataPlatform;

public sealed class ImportJobStateRecord
{
    public required ImportJob Job { get; init; }
    public required NormalizedWorkbook Workbook { get; init; }
    public MappingProfile? MappingProfile { get; set; }
    public MappingResult? MappingResult { get; set; }
    public ValidationProfile? ValidationProfile { get; set; }
    public ValidationResult? ValidationResult { get; set; }
    public ImportPreviewDetail? PreviewDetail { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public interface IImportJobStore
{
    Task SaveAsync(ImportJobStateRecord record, CancellationToken cancellationToken = default);
    Task<ImportJobStateRecord?> GetAsync(string jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportJobStateRecord>> ListAsync(string? ownerUserId = null, CancellationToken cancellationToken = default);
}
