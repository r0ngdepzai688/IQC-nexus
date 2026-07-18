using System;

namespace IqcQms.Domain.Entities.DataHub
{
    public class ValidationError
    {
        public int Id { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public int StagingId { get; set; }
        public int RawRowNumber { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string ErrorType { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string RawValue { get; set; } = string.Empty;
        public string SuggestedValue { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}