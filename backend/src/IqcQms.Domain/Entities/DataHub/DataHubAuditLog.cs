using System;

namespace IqcQms.Domain.Entities.DataHub
{
    public class DataHubAuditLog
    {
        public int Id { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string RecordKey { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public string Reason { get; set; } = string.Empty;
    }
}