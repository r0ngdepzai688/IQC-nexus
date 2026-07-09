using System;

namespace IqcQms.Domain.Entities.NewModels
{
    public class ProjectWorkspace
    {
        public int Id { get; set; }
        public int SourceRecordId { get; set; } // Nguồn từ dòng Excel nào (FK to MasterPlanRecord)
        
        public string ProjectName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        
        public string OwnerId { get; set; } = string.Empty; // PIC được map từ PicIqc
        
        public DateTime ActivatedDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Preparation"; // Preparation, InProgress, Completed
        
        // Tiến độ (0-100)
        public int CompletionPercentage { get; set; } = 0;
    }
}
