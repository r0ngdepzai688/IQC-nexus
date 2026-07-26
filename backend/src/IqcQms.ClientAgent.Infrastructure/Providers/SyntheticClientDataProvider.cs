using System.Security.Cryptography;
using System.Text;
using IqcQms.ClientAgent.Application.Providers;
using IqcQms.ClientAgent.Contracts;
using Microsoft.Extensions.Logging;

namespace IqcQms.ClientAgent.Infrastructure.Providers;

public class SyntheticClientDataProvider : IClientDataProvider
{
    private readonly ILogger<SyntheticClientDataProvider> _logger;

    public SyntheticClientDataProvider(ILogger<SyntheticClientDataProvider> logger)
    {
        _logger = logger;
    }

    public ClientProviderCapabilities Capabilities => new()
    {
        ProviderId = "SyntheticProvider",
        ProviderVersion = "1.0.0",
        IsSynthetic = true,
        SupportsNasca = false,
        SupportsExcelCom = false,
        SupportedExtensions = new() { ".synthetic.json" }
    };

    public Task<ClientNormalizationResult> NormalizeAsync(ClientNormalizationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing synthetic normalization for server job {JobId}", request.ServerImportJobId);

        var workbook = new NormalizedWorkbook
        {
            WorkbookName = "Synthetic_MasterPlan_Sample.xlsx",
            Sheets = new List<NormalizedSheet>
            {
                new NormalizedSheet
                {
                    SheetName = "MasterPlan",
                    Rows = new List<NormalizedRow>
                    {
                        new NormalizedRow
                        {
                            RowIndex = 1,
                            Cells = new List<NormalizedCell>
                            {
                                new NormalizedCell { ColumnName = "Model", ColumnIndex = 1, Value = "SYNTH-M01", DataType = "String" },
                                new NormalizedCell { ColumnName = "BasicKey", ColumnIndex = 2, Value = "BK-2026-001", DataType = "String" },
                                new NormalizedCell { ColumnName = "CatKey", ColumnIndex = 3, Value = "CAT-A", DataType = "String" },
                                new NormalizedCell { ColumnName = "PvrTargetDate", ColumnIndex = 4, Value = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"), DataType = "Date" },
                                new NormalizedCell { ColumnName = "Status", ColumnIndex = 5, Value = "Ready", DataType = "String" }
                            }
                        },
                        new NormalizedRow
                        {
                            RowIndex = 2,
                            Cells = new List<NormalizedCell>
                            {
                                new NormalizedCell { ColumnName = "Model", ColumnIndex = 1, Value = "SYNTH-M02", DataType = "String" },
                                new NormalizedCell { ColumnName = "BasicKey", ColumnIndex = 2, Value = "BK-2026-002", DataType = "String" },
                                new NormalizedCell { ColumnName = "CatKey", ColumnIndex = 3, Value = "CAT-B", DataType = "String" },
                                new NormalizedCell { ColumnName = "PvrTargetDate", ColumnIndex = 4, Value = DateTime.UtcNow.AddDays(14).ToString("yyyy-MM-dd"), DataType = "Date" },
                                new NormalizedCell { ColumnName = "Status", ColumnIndex = 5, Value = "Future", DataType = "String" }
                            }
                        }
                    }
                }
            }
        };

        var rawStr = $"{request.ServerImportJobId}_synthetic_normalized_payload";
        using var sha = SHA256.Create();
        var fp = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(rawStr)));

        return Task.FromResult(new ClientNormalizationResult
        {
            IsSuccess = true,
            NormalizedWorkbook = workbook,
            RecordCount = 2,
            SourceFingerprint = fp,
            DiagnosticsSummary = "Synthetic normalization generated successfully (2 records).",
            ErrorCode = null
        });
    }
}

public class ClientDataProviderRegistry : IClientDataProviderRegistry
{
    private readonly Dictionary<string, IClientDataProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    public ClientDataProviderRegistry(IEnumerable<IClientDataProvider> providers)
    {
        foreach (var p in providers)
        {
            RegisterProvider(p);
        }
    }

    public void RegisterProvider(IClientDataProvider provider)
    {
        _providers[provider.Capabilities.ProviderId] = provider;
    }

    public IClientDataProvider? GetProvider(string providerId)
    {
        _providers.TryGetValue(providerId, out var provider);
        return provider;
    }

    public IReadOnlyList<IClientDataProvider> GetAvailableProviders()
    {
        return _providers.Values.ToList();
    }
}
