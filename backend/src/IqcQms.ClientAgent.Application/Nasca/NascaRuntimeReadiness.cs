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
    STOP_INCOMPLETE_EVIDENCE,
    STOP_UI_AUTOMATION_ONLY,
    STOP_DIRECT_EXCEL_COM_REQUIRED,
    STOP_LICENSING_RESTRICTION,
    STOP_CONFLICTING_EVIDENCE,
    GO_CLI,
    GO_WATCHED_FOLDER
}

public class ReadinessCheckItem
{
    public string Name { get; set; } = string.Empty;
    public ReadinessStatus Status { get; set; } = ReadinessStatus.Unknown;
    public string Details { get; set; } = string.Empty;
}

public class NascaRuntimeReadinessResult
{
    public NascaRuntimeDecision Decision { get; set; } = NascaRuntimeDecision.STOP_INCOMPLETE_EVIDENCE;
    public bool IsGo => Decision == NascaRuntimeDecision.GO_CLI || Decision == NascaRuntimeDecision.GO_WATCHED_FOLDER;
    public List<ReadinessCheckItem> ReadinessMatrix { get; set; } = new();
    public string SanitizedSummary { get; set; } = string.Empty;
}

public class NascaReadinessEvaluator
{
    public NascaRuntimeReadinessResult Evaluate(NascaEvidenceManifest manifest, NascaInstallationMetadata? binaryMetadata = null)
    {
        var matrix = new List<ReadinessCheckItem>();

        // 1. Product Identity
        var hasProductName = manifest.Items.Any(i => i.ApprovedForUse && !string.IsNullOrWhiteSpace(i.ProductName) && i.SourceClassification == SourceClassification.VendorDocumentation);
        matrix.Add(new ReadinessCheckItem
        {
            Name = "ProductIdentityKnown",
            Status = hasProductName ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = hasProductName ? "Vendor documented product name verified." : "Product name missing or not vendor documented."
        });

        // 2. Executable Identity
        var hasExeIdentity = binaryMetadata != null && binaryMetadata.FileExists && !string.IsNullOrWhiteSpace(binaryMetadata.ProductName);
        matrix.Add(new ReadinessCheckItem
        {
            Name = "ExecutableIdentityKnown",
            Status = hasExeIdentity ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = hasExeIdentity ? "Binary metadata verified via inspector." : "Executable metadata missing or unverified."
        });

        // 3. Publisher Policy
        var hasPublisher = binaryMetadata != null && !string.IsNullOrWhiteSpace(binaryMetadata.Publisher);
        matrix.Add(new ReadinessCheckItem
        {
            Name = "PublisherPolicyKnown",
            Status = hasPublisher ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = hasPublisher ? "Publisher identity verified." : "Publisher policy unverified."
        });

        // 4. Version Policy
        var hasVersion = manifest.Items.Any(i => i.ApprovedForUse && !string.IsNullOrWhiteSpace(i.ProductVersion));
        matrix.Add(new ReadinessCheckItem
        {
            Name = "VersionPolicyKnown",
            Status = hasVersion ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = hasVersion ? "Version policy documented." : "Version policy unverified."
        });

        // 5. Architecture
        var hasArch = binaryMetadata != null && !string.IsNullOrWhiteSpace(binaryMetadata.Architecture);
        matrix.Add(new ReadinessCheckItem
        {
            Name = "ArchitectureKnown",
            Status = hasArch ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = hasArch ? "Architecture verified." : "Architecture unverified."
        });

        // 6. Interface Documented
        var hasInterface = manifest.Items.Any(i => i.ApprovedForUse && (i.EvidenceType == EvidenceType.VendorDocumentation || i.EvidenceType == EvidenceType.ApprovedCommandHelpOutput));
        matrix.Add(new ReadinessCheckItem
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

        matrix.Add(new ReadinessCheckItem
        {
            Name = "NoUiAutomationRequired",
            Status = !hasUiOnly ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = !hasUiOnly ? "No UI automation required." : "UI automation required - REJECTED."
        });

        matrix.Add(new ReadinessCheckItem
        {
            Name = "ExcelDependencyAcceptable",
            Status = !hasDirectCom ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = !hasDirectCom ? "No direct Excel COM required." : "Direct Excel COM required - REJECTED."
        });

        matrix.Add(new ReadinessCheckItem
        {
            Name = "LicensingPermitsAutomation",
            Status = hasLicensing ? ReadinessStatus.Pass : ReadinessStatus.Fail,
            Details = hasLicensing ? "Automation explicitly licensed." : "Licensing permission unverified or unknown."
        });

        // Determine Decision
        NascaRuntimeDecision decision;
        if (hasConflict)
        {
            decision = NascaRuntimeDecision.STOP_CONFLICTING_EVIDENCE;
        }
        else if (hasUiOnly)
        {
            decision = NascaRuntimeDecision.STOP_UI_AUTOMATION_ONLY;
        }
        else if (hasDirectCom)
        {
            decision = NascaRuntimeDecision.STOP_DIRECT_EXCEL_COM_REQUIRED;
        }
        else if (!hasProductName || !hasInterface || !hasExeIdentity)
        {
            decision = NascaRuntimeDecision.STOP_INCOMPLETE_EVIDENCE;
        }
        else if (!hasLicensing)
        {
            decision = NascaRuntimeDecision.STOP_LICENSING_RESTRICTION;
        }
        else if (manifest.Items.Any(i => i.SanitizedNotes.Contains("WATCHED_FOLDER_DOCUMENTED", StringComparison.OrdinalIgnoreCase)))
        {
            decision = NascaRuntimeDecision.GO_WATCHED_FOLDER;
        }
        else
        {
            decision = NascaRuntimeDecision.GO_CLI;
        }

        return new NascaRuntimeReadinessResult
        {
            Decision = decision,
            ReadinessMatrix = matrix,
            SanitizedSummary = $"Runtime decision: {decision}. Matched {matrix.Count(m => m.Status == ReadinessStatus.Pass)} / {matrix.Count} readiness criteria."
        };
    }
}
