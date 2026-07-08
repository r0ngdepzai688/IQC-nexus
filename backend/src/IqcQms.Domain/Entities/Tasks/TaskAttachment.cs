using System;

namespace IqcQms.Domain.Entities.Tasks
{
    public class TaskAttachment
    {
        public int Id { get; set; }
        public int TaskItemId { get; set; }
        public TaskItem? TaskItem { get; set; }
        
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // Image, PDF, Office, Video
        public long FileSize { get; set; } = 0;
        public string UploaderId { get; set; } = string.Empty;
        
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
