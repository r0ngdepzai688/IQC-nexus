namespace IqcQms.ClientAgent.Application.Nasca;

public enum ReadinessStatus
{
    Unknown,
    Pass,
    Fail,
    NotApplicable
}

public enum NascaRuntimeDecision
{
    ReadyForDesign,
    StopEvidenceMissing,
    StopEvidenceConflict,
    StopLicensingUnknown,
    StopLicensingProhibited,
    StopUiOnly,
    StopExcelComRequired,
    StopPublisherMismatch,
    StopVersionUnsupported,
    StopArchitectureUnknown,
    StopOutputCorrelationUnknown
}

public class ReadinessCheckItem
{
    public string Name { get; set; } = string.Empty;
    public ReadinessStatus Status { get; set; } = ReadinessStatus.Unknown;
    public string Details { get; set; } = string.Empty;
}

public class NascaRuntimeReadinessResult
{
    public NascaRuntimeDecision Decision { get; set; } = NascaRuntimeDecision.StopEvidenceMissing;
    public bool IsGo => IsRuntimeDesignAllowed;
    public bool IsRuntimeDesignAllowed => Decision == NascaRuntimeDecision.ReadyForDesign;
    public bool IsRuntimeExecutionAllowed => false; // Real process execution is ALWAYS false in pre-production Phase 3
    public List<ReadinessCheckItem> Criteria { get; set; } = new();
    public List<string> SanitizedReasonCodes { get; set; } = new();
    public string SanitizedSummary { get; set; } = string.Empty;
}

public class NascaReadinessEvaluator
{
    public NascaRuntimeReadinessResult Evaluate(NascaEvidenceManifest manifest, NascaInstallationMetadata? binaryMetadata = null)
    {
        var criteria = new List<ReadinessCheckItem>();
        var reasonCodes = new List<string>();

        // 1. Product Identity
        var hasProductName = manifest.Items.Any(i => i.ApprovedForUse && !string.IsNullOrWhiteSpace(i.ProductName) && i.SourceClassification == SourceClassification.VendorDocumentation);
        criteria.Add(new ReadinessCheckItem
        {
            Name = "ProductIdentityKnown",
            Status = hasProductName ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = hasProductName ? "Vendor documented product name verified." : "Product name missing or not vendor documented."
        });

        // 2. Executable Identity
        var hasExeIdentity = binaryMetadata != null && binaryMetadata.FileExists && !string.IsNullOrWhiteSpace(binaryMetadata.ProductName);
        criteria.Add(new ReadinessCheckItem
        {
            Name = "ExecutableIdentityKnown",
            Status = hasExeIdentity ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = hasExeIdentity ? "Binary metadata verified via inspector." : "Executable metadata missing or unverified."
        });

        // 3. Publisher Policy
        var hasPublisher = binaryMetadata != null && !string.IsNullOrWhiteSpace(binaryMetadata.Publisher);
        criteria.Add(new ReadinessCheckItem
        {
            Name = "PublisherPolicyKnown",
            Status = hasPublisher ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = hasPublisher ? "Publisher identity verified." : "Publisher policy unverified."
        });

        // 4. Version Policy
        var hasVersion = manifest.Items.Any(i => i.ApprovedForUse && !string.IsNullOrWhiteSpace(i.ProductVersion));
        criteria.Add(new ReadinessCheckItem
        {
            Name = "VersionPolicyKnown",
            Status = hasVersion ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = hasVersion ? "Version policy documented." : "Version policy unverified."
        });

        // 5. Architecture
        var hasArch = binaryMetadata != null && !string.IsNullOrWhiteSpace(binaryMetadata.Architecture);
        criteria.Add(new ReadinessCheckItem
        {
            Name = "ArchitectureKnown",
            Status = hasArch ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = hasArch ? "Architecture verified." : "Architecture unverified."
        });

        // 6. Interface Documented
        var hasInterface = manifest.Items.Any(i => i.ApprovedForUse && (i.EvidenceType == EvidenceType.VendorDocumentation || i.EvidenceType == EvidenceType.ApprovedCommandHelpOutput));
        criteria.Add(new ReadinessCheckItem
        {
            Name = "InterfaceDocumented",
            Status = hasInterface ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = hasInterface ? "Interface documented." : "Interface unverified by vendor evidence."
        });

        // 7-17 Checklist Items
        var hasUiOnly = manifest.Items.Any(i => i.SanitizedNotes.Contains("UI_ONLY", StringComparison.OrdinalIgnoreCase));
        var hasDirectCom = manifest.Items.Any(i => i.SanitizedNotes.Contains("DIRECT_EXCEL_COM", StringComparison.OrdinalIgnoreCase));
        var hasLicensing = manifest.Items.Any(i => i.ApprovedForUse && i.SanitizedNotes.Contains("LICENSED_FOR_AUTOMATION", StringComparison.OrdinalIgnoreCase));
        var hasConflict = manifest.Items.Any(i => i.VerificationStatus == EvidenceVerificationStatus.Conflicting);

        criteria.Add(new ReadinessCheckItem
        {
            Name = "NoUiAutomationRequired",
            Status = !hasUiOnly ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = !hasUiOnly ? "No UI automation required." : "UI automation required - REJECTED."
        });

        criteria.Add(new ReadinessCheckItem
        {
            Name = "ExcelDependencyAcceptable",
            Status = !hasDirectCom ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = !hasDirectCom ? "No direct Excel COM required." : "Direct Excel COM required - REJECTED."
        });

        criteria.Add(new ReadinessCheckItem
        {
            Name = "LicensingPermitsAutomation",
            Status = hasLicensing ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = hasLicensing ? "Automation explicitly licensed." : "Licensing permission unverified or unknown."
        });

        // Determine Strongly Typed Runtime Decision
        NascaRuntimeDecision decision;
        if (hasConflict)
        {
            decision = NascaRuntimeDecision.StopEvidenceConflict;
            reasonCodes.Add("EVIDENCE_CONFLICT_DETECTED");
        }
        else if (hasUiOnly)
        {
            decision = NascaRuntimeDecision.StopUiOnly;
            reasonCodes.Add("UI_AUTOMATION_PROHIBITED");
        }
        else if (hasDirectCom)
        {
            decision = NascaRuntimeDecision.StopExcelComRequired;
            reasonCodes.Add("EXCEL_COM_PROHIBITED");
        }
        else if (!hasProductName || !hasInterface || !hasExeIdentity)
        {
            decision = NascaRuntimeDecision.StopEvidenceMissing;
            reasonCodes.Add("MISSING_VENDOR_EVIDENCE");
        }
        else if (!hasLicensing)
        {
            decision = NascaRuntimeDecision.StopLicensingUnknown;
            reasonCodes.Add("UNVERIFIED_LICENSING_PERMISSION");
        }
        else
        {
            decision = NascaRuntimeDecision.ReadyForDesign;
            reasonCodes.Add("READINESS_GO_FOR_DESIGN");
        }

        return new NascaRuntimeReadinessResult
        {
            Decision = decision,
            Criteria = criteria,
            SanitizedReasonCodes = reasonCodes,
            SanitizedSummary = $"Runtime decision: {decision}. Matched {criteria.Count(m => m.Status == ReadinessStatus.Pass)} / {criteria.Count} criteria."
        };
    }
}
