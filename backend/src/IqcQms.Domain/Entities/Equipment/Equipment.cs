using System;

namespace IqcQms.Domain.Entities.Equipment
{
    public class Equipment
    {
        public int Id { get; set; }
        public string EquipmentCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // Active, Maintenance, Broken
        public DateTime? NextCalibrationDate { get; set; }
    }
}
