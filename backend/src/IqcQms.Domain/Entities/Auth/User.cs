using System;

namespace IqcQms.Domain.Entities.Auth
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty; // Used for Mã nhân viên
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string ClName { get; set; } = string.Empty;
        public string KnoxId { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string SystemRole { get; set; } = "User";
        public string DashboardProfile { get; set; } = "Auto";
        
        public string AccountStatus { get; set; } = "Active";
        public DateTime? LastLogin { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public int? RoleId { get; set; }
        public Role? Role { get; set; }
    }
}
