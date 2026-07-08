using System;
using System.Collections.Generic;

namespace IqcQms.Domain.Entities.Tasks
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string TaskNumber { get; set; } = string.Empty; // e.g., TSK-2026-0001
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty; // Rich Text HTML or Markdown
        public string TaskType { get; set; } = "Assigned"; // Business, Assigned, Personal
        
        // Polymorphic Linkage
        public string SourceModule { get; set; } = string.Empty; // e.g., New Models, Inspection, Compliance
        public string RelatedEntityId { get; set; } = string.Empty; // e.g., ProjectId, IssueId
        public string RelatedEntityName { get; set; } = string.Empty; // e.g., "PV Preparation for Model X"
        
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal";
        public string Severity { get; set; } = "Low";
        public string Project { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        
        // Users (mapped by Username or EmployeeId, assuming string for now like CreatedBy)
        public string CreatorId { get; set; } = string.Empty;
        public string AssigneeId { get; set; } = string.Empty;
        public string ApproverId { get; set; } = string.Empty;
        public string Followers { get; set; } = string.Empty; // comma-separated or separate table
        
        // Dates
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? EstimatedHours { get; set; }
        public decimal? ActualHours { get; set; }
        public DateTime? CompletionDate { get; set; }
        
        // Status
        public string Status { get; set; } = "Draft"; // Draft, Assigned, Accepted, In Progress, Waiting External, Waiting HQ, Waiting Vendor, Completed, Rejected, Closed
        public string ApprovalStatus { get; set; } = "None"; // Waiting, Approved, Rejected
        
        public int Progress { get; set; } = 0; // 0-100%
        
        // Subtasks (Self-referencing)
        public int? ParentTaskId { get; set; }
        public TaskItem? ParentTask { get; set; }
        public ICollection<TaskItem> Subtasks { get; set; } = new List<TaskItem>();
        
        // Dependencies
        public ICollection<TaskDependency> DependentOn { get; set; } = new List<TaskDependency>();
        public ICollection<TaskDependency> RequiredBy { get; set; } = new List<TaskDependency>();
        
        // Navigation properties
        public ICollection<TaskChecklist> Checklists { get; set; } = new List<TaskChecklist>();
        public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
        public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
        
        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
