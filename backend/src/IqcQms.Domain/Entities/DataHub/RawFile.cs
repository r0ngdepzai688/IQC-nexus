using System;

namespace IqcQms.Domain.Entities.DataHub
{
    public class RawFile
    {
        public int Id { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ArchivedPath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileHash { get; set; } = string.Empty;
        public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
    }
}