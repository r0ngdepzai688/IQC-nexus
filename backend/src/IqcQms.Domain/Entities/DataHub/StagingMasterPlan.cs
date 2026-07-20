using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace IqcQms.Domain.Entities.DataHub
{
    [Table("Staging_MasterPlan")]
    public class StagingMasterPlan
    {
        public int Id { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public int RawRowNumber { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Basic { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Cat { get; set; } = string.Empty;
        public string BasicKey { get; set; } = string.Empty;
        public string CatKey { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int? QtyLpr { get; set; }
        public int? QtyLsr { get; set; }
        public DateTime? PvrTargetDate { get; set; }
        public DateTime? PraTargetDate { get; set; }
        public DateTime? SraTargetDate { get; set; }
        public DateTime? MainLprLqvDate { get; set; }
        public DateTime? MainLsrDate { get; set; }
        public string HwPic { get; set; } = string.Empty;
        public string RawStatus { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        
        public string RowStatus { get; set; } = string.Empty; 
        public string ValidationMessage { get; set; } = string.Empty;
        public string CleaningMessage { get; set; } = string.Empty;
        public string CoreValidationMessage { get; set; } = string.Empty;
        public int? TargetMasterPlanId { get; set; }
        public long? TargetVersion { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
