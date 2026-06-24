namespace IqcQms.Domain.Entities.Standards
{
    public class InspectionItem
    {
        public int Id { get; set; }
        public int InspectionStandardId { get; set; }
        public InspectionStandard? InspectionStandard { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public double TolerancePlus { get; set; }
        public double ToleranceMinus { get; set; }
        public int? ToolRequiredId { get; set; }
    }
}
