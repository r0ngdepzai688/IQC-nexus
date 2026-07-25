namespace IqcQms.ClientAgent.Application.Nasca;

public enum EvidenceType
{
    Unknown,
    VendorDocumentation,
    VendorSignedBinaryMetadata,
    OperatorConfirmed,
    InstallationManifest,
    ApprovedCommandHelpOutput
}

public enum SourceClassification
{
    Unknown,
    VendorDocumentation,
    VendorSignedBinaryMetadata,
    OperatorConfirmed,
    InstallationManifest,
    ApprovedCommandHelpOutput
}

public enum EvidenceVerificationStatus
{
    Missing,
    OperatorConfirmed,
    VendorDocumented,
    Verified,
    Conflicting
}

public class NascaEvidenceItem
{
    public string EvidenceId { get; set; } = Guid.NewGuid().ToString("N");
    public EvidenceType EvidenceType { get; set; } = EvidenceType.Unknown;
    public SourceClassification SourceClassification { get; set; } = SourceClassification.Unknown;
    public EvidenceVerificationStatus VerificationStatus { get; set; } = EvidenceVerificationStatus.Missing;
    public string? ProductName { get; set; }
    public string? VendorName { get; set; }
    public string? ProductVersion { get; set; }
    public DateTime? EvidenceDate { get; set; }
    public string? SuppliedBy { get; set; }
    public bool ApprovedForUse { get; set; }
    public string ConfidentialityClassification { get; set; } = "INTERNAL_ENGINEERING";
    public string? ContentHash { get; set; }
    public string SanitizedNotes { get; set; } = string.Empty;
}

public class NascaEvidenceManifest
{
    public string ManifestId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<NascaEvidenceItem> Items { get; set; } = new();
}
