namespace IqcQms.Domain.Entities.Standards
{
    public class DynamicForm
    {
        public int Id { get; set; }
        public int InspectionStandardId { get; set; }
        public string FormConfigJson { get; set; } = "{}"; // Stores the drag-drop UI layout
    }
}
