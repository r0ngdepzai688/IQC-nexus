namespace IqcQms.Domain.Entities.DataHub;

public sealed class HeaderMappingProfile
{
    public int Id { get; set; }
    public string NormalizedHeaderPath { get; set; } = string.Empty;
    public string CanonicalField { get; set; } = string.Empty;
    public string DetectedDataType { get; set; } = string.Empty;
    public string WorkbookFingerprint { get; set; } = string.Empty;
    public int ConfirmationCount { get; set; }
    public int RejectionCount { get; set; }
    public double Confidence { get; set; }
    public bool IsApproved { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}
