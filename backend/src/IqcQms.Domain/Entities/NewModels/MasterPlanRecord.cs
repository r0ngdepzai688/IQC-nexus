using System;

namespace IqcQms.Domain.Entities.NewModels
{
    public class MasterPlanRecord
    {
        public int Id { get; set; }
        public int UploadId { get; set; } // FK to MasterPlanUpload
        
        // Parsed from Excel
        public string ProjectName { get; set; } = string.Empty;
        public string Basic { get; set; } = string.Empty;
        public string AreaRegion { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // LPR or LSR
        
        public int QtyLpr { get; set; }
        public int QtyLsr { get; set; }
        
        public DateTime? PvrTargetDate { get; set; }
        public DateTime? PraTargetDate { get; set; }
        public DateTime? SraTargetDate { get; set; }
        public string HwPic { get; set; } = string.Empty; // HW 검증 (IQC)
        
        // Calculated state for Candidate Pool
        public string ActionStatus { get; set; } = "Future"; // Urgent, Ready, Future
        public bool IsActivated { get; set; } = false; // Moved to active project?
    }
}
