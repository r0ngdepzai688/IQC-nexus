using System;

namespace IqcQms.Domain.Entities.NewModels
{
    public class MasterPlan
    {
        public int Id { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Basic { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Cat { get; set; } = string.Empty;
        public string BasicKey { get; set; } = string.Empty;
        public string CatKey { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int QtyLpr { get; set; }
        public int QtyLsr { get; set; }
        public DateTime? PvrTargetDate { get; set; }
        public DateTime? PraTargetDate { get; set; }
        public DateTime? SraTargetDate { get; set; }
        public DateTime? MainLprLqvDate { get; set; }
        public DateTime? MainLsrDate { get; set; }
        public string HwPic { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; 
        public string ImportedStatus { get; set; } = string.Empty;
        public string ActionStatus { get; set; } = string.Empty; 
        public string Remark { get; set; } = string.Empty;
        public string LastImportBatchId { get; set; } = string.Empty;
        public int? LinkedProjectId { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public long Version { get; set; } = 1;
    }
}
