using System;

namespace IqcQms.Domain.Entities.Tasks
{
    public class TaskComment
    {
        public int Id { get; set; }
        public int TaskItemId { get; set; }
        public TaskItem? TaskItem { get; set; }
        
        public string AuthorId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty; // Including rejection reason
        public string CommentType { get; set; } = "User"; // User, System (Status Change, Approval)
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
