namespace IqcQms.Domain.Entities.Tasks
{
    public class TaskChecklist
    {
        public int Id { get; set; }
        public int TaskItemId { get; set; }
        public TaskItem? TaskItem { get; set; }
        
        public string Content { get; set; } = string.Empty;
        public bool IsCompleted { get; set; } = false;
        public int Order { get; set; } = 0;
    }
}
