using System.IO;
using System.Threading.Tasks;
using IqcQms.Domain.Entities.DataHub;

namespace IqcQms.Application.Interfaces.DataHub
{
    public interface IDataHubIngestionService
    {
        /// <summary>
        /// Phase 1: Uploads the file, creates the batch, and parses it into Staging without writing to Core tables.
        /// </summary>
        Task<ImportBatch> ProcessUploadAsync(Stream fileStream, string fileName, string uploadedBy, string module = "NewModels", IReadOnlyCollection<HeaderMappingDto>? mappings = null);

        /// <summary>
        /// Phase 2: Commits the validated staging records into Core tables.
        /// </summary>
        Task<ImportBatch> CommitBatchAsync(string batchId, string committedBy);
        
        /// <summary>
        /// Resolves a row requiring business review.
        /// </summary>
        Task<bool> ResolveReviewItemAsync(int reviewItemId, string action, string resolvedBy, string? note = null);
        Task<ImportReviewSummaryDto?> GetReviewSummaryAsync(string batchId);
        Task<ImportBatch> ResolveExistingSkuAsync(string batchId, string resolution, string resolvedBy);
        Task<bool> ResolveWarningRowAsync(string batchId, int rowNumber, string resolution, string resolvedBy);
        
        Task<ImportBatch?> GetBatchPreviewAsync(string batchId);
        
        Task<List<ImportBatch>> GetHistoryAsync(string module = "NewModels");
        Task<List<StagingMasterPlan>> GetStagingRecordsAsync(string batchId);

        Task<List<ManualFileDto>> GetManualUploadFilesAsync();

        Task<ImportBatch> ProcessManualUploadAsync(string fileName, string uploadedBy, string module = "NewModels");
    }

    public sealed class ImportReviewSummaryDto
    {
        public string BatchId { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public int ValidRows { get; init; }
        public int WarningRows { get; init; }
        public int ErrorRows { get; init; }
        public int ExistingSkuConflicts { get; init; }
        public int SkippedRows { get; init; }
        public List<ImportReviewRowDto> Rows { get; init; } = [];
    }

    public sealed class ImportReviewRowDto
    {
        public int RowNumber { get; init; }
        public string Sku { get; init; } = string.Empty;
        public string Field { get; init; } = string.Empty;
        public string CurrentValue { get; init; } = string.Empty;
        public string Severity { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

    public class ManualFileDto
    {
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime LastModifiedUtc { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public string DuplicateStatus { get; set; } = "None"; // None, DuplicateContent, ExactDuplicate
    }
}
