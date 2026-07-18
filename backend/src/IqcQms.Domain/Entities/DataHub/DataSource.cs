using System;

namespace IqcQms.Domain.Entities.DataHub
{
    public class DataSource
    {
        public int Id { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}