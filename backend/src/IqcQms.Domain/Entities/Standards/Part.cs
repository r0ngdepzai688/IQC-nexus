namespace IqcQms.Domain.Entities.Standards
{
    public class Part
    {
        public int Id { get; set; }
        public string PartCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
    }
}
