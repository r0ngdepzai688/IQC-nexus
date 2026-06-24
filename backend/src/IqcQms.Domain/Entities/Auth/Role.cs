namespace IqcQms.Domain.Entities.Auth
{
    public class Role
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string Permissions { get; set; } = string.Empty;
        
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
