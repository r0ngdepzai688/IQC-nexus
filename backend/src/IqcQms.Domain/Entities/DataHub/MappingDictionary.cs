using System;

namespace IqcQms.Domain.Entities.DataHub
{
    public class MappingDictionary
    {
        public int Id { get; set; }
        public string DictionaryType { get; set; } = string.Empty; 
        public string RawValue { get; set; } = string.Empty;
        public string MappedValue { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}