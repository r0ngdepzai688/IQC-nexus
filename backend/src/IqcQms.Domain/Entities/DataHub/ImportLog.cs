using System;

namespace IqcQms.Domain.Entities.DataHub
{
    public class ImportLog
    {
        public int Id { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public string Level { get; set; } = "Info";
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}