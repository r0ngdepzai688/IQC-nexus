using System;

namespace IqcQms.Domain.Entities.System
{
    public class AuditLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string AdminId { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string AffectedUserId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }
}
