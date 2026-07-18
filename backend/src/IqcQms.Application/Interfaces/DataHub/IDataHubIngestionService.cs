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
        Task<ImportBatch> ProcessUploadAsync(Stream fileStream, string fileName, string uploadedBy, string module = "NewModels");

        /// <summary>
        /// Phase 2: Commits the validated staging records into Core tables.
        /// </summary>
        Task<ImportBatch> CommitBatchAsync(string batchId, string committedBy);
        
        /// <summary>
        /// Resolves a row requiring business review.
        /// </summary>
        Task<bool> ResolveReviewItemAsync(int reviewItemId, string action, string resolvedBy, string? note = null);
        
        Task<ImportBatch?> GetBatchPreviewAsync(string batchId);
        
        Task<List<ImportBatch>> GetHistoryAsync(string module = "NewModels");
        Task<List<StagingMasterPlan>> GetStagingRecordsAsync(string batchId);

        Task<List<ManualFileDto>> GetManualUploadFilesAsync();

        Task<ImportBatch> ProcessManualUploadAsync(string fileName, string uploadedBy, string module = "NewModels");
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
