using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace IqcQms.Domain.Entities.DataHub
{
    [Table("BusinessReviewQueue")]
    public class BusinessReviewQueue
    {
        public int Id { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public int StagingId { get; set; }
        public string ConflictType { get; set; } = string.Empty; 
        public string ConflictMessage { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string ResolvedBy { get; set; } = string.Empty;
        public DateTime? ResolvedAt { get; set; }
        public string ResolutionAction { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}