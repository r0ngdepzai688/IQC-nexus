using System;

namespace IqcQms.Domain.Entities.DataHub
{
    public class ImportBatch
    {
        public int Id { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public int DataSourceId { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string ImportMethod { get; set; } = "Manual Upload";
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileHash { get; set; } = string.Empty;
        public string Status { get; set; } = "Uploaded";
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int ErrorRows { get; set; }
        public int ReviewRequiredRows { get; set; }
        public int SkippedRows { get; set; }
        public int CreatedRecords { get; set; }
        public int UpdatedRecords { get; set; }
        public long DurationMs { get; set; }
    }
}