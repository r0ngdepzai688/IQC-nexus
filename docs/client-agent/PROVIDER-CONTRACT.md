# IQC Nexus Client Agent — Provider Abstraction Contract

## Interface Contract

All data providers implement `IClientDataProvider`:

```csharp
public interface IClientDataProvider
{
    ClientProviderCapabilities Capabilities { get; }
    Task<ClientNormalizationResult> NormalizeAsync(ClientNormalizationRequest request, CancellationToken cancellationToken = default);
}
```

## Foundation Status

- **Synthetic Provider**: Implemented (`SyntheticClientDataProvider`). Supported.
- **CSV Provider**: Optional safe provider.
- **NASCA Provider**: DEFERRED.
- **Excel COM / Interop**: DEFERRED.
