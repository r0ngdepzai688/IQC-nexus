using System;

namespace IqcQms.Domain.Entities.NewModels
{
    public class MasterPlanUpload
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        public string UploadedBy { get; set; } = string.Empty;
        public int TotalRecordsParsed { get; set; }
        public string DeltaSummary { get; set; } = string.Empty; // AI Note about changes
    }
}
