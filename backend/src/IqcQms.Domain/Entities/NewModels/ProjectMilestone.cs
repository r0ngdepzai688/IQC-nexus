using System;

namespace IqcQms.Domain.Entities.NewModels
{
    public class ProjectMilestone
    {
        public int Id { get; set; }
        public int MasterPlanId { get; set; }
        public string MilestoneType { get; set; } = string.Empty; 
        public DateTime? TargetDate { get; set; }
        public string SourceBatchId { get; set; } = string.Empty;
        
        public string ProjectName { get; set; } = string.Empty;
        public string BaseModel { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty; 
        public string Status { get; set; } = string.Empty; 
        public string OwnerId { get; set; } = string.Empty; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}