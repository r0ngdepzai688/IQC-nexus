namespace IqcQms.Domain.Entities.Tasks
{
    public class TaskDependency
    {
        public int Id { get; set; }
        
        // The task that depends on the prerequisite
        public int TaskId { get; set; }
        public TaskItem? Task { get; set; }
        
        // The prerequisite task that must be completed first
        public int PrerequisiteTaskId { get; set; }
        public TaskItem? PrerequisiteTask { get; set; }
    }
}
