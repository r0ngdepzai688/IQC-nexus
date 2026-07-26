using IqcQms.ClientAgent.Contracts;

namespace IqcQms.ClientAgent.Application.Providers;

public class ClientProviderCapabilities
{
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderVersion { get; set; } = "1.0.0";
    public bool IsSynthetic { get; set; } = true;
    public bool SupportsNasca { get; set; } = false;
    public bool SupportsExcelCom { get; set; } = false;
    public List<string> SupportedExtensions { get; set; } = new() { ".synthetic.json", ".csv" };
}

public class ClientNormalizationRequest
{
    public Guid ServerImportJobId { get; set; }
    public string InputPathOrReference { get; set; } = string.Empty;
}

public class ClientNormalizationResult
{
    public bool IsSuccess { get; set; }
    public NormalizedWorkbook? NormalizedWorkbook { get; set; }
    public int RecordCount { get; set; }
    public string SourceFingerprint { get; set; } = string.Empty;
    public string DiagnosticsSummary { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
}

public interface IClientDataProvider
{
    ClientProviderCapabilities Capabilities { get; }
    Task<ClientNormalizationResult> NormalizeAsync(ClientNormalizationRequest request, CancellationToken cancellationToken = default);
}

public interface IClientDataProviderRegistry
{
    void RegisterProvider(IClientDataProvider provider);
    IClientDataProvider? GetProvider(string providerId);
    IReadOnlyList<IClientDataProvider> GetAvailableProviders();
}
