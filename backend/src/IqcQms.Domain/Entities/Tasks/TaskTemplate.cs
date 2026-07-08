using System;

namespace IqcQms.Domain.Entities.Tasks
{
    public class TaskTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DefinitionJson { get; set; } = string.Empty; // Defines the pre-configured child tasks
        
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
