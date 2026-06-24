namespace IqcQms.Domain.Entities.HR
{
    public class Employee
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty; // Bậc thợ
    }
}
