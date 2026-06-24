using System.Collections.Generic;

namespace IqcQms.Domain.Entities.Standards
{
    public class InspectionStandard
    {
        public int Id { get; set; }
        public int PartId { get; set; }
        public Part? Part { get; set; }
        public string VersionNumber { get; set; } = "1.0";
        public bool IsActive { get; set; } = true;
        public string DocumentUrl { get; set; } = string.Empty;
        
        public ICollection<InspectionItem> Items { get; set; } = new List<InspectionItem>();
    }
}
