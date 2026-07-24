using IqcQms.ClientAgent.Application.Providers;
using IqcQms.ClientAgent.Infrastructure.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class SyntheticProviderTests
{
    [Fact]
    public void SyntheticProviderCapabilities_ReflectsNoNascaOrOfficeCom()
    {
        var provider = new SyntheticClientDataProvider(NullLogger<SyntheticClientDataProvider>.Instance);
        var caps = provider.Capabilities;

        Assert.Equal("SyntheticProvider", caps.ProviderId);
        Assert.True(caps.IsSynthetic);
        Assert.False(caps.SupportsNasca);
        Assert.False(caps.SupportsExcelCom);
    }

    [Fact]
    public async Task NormalizeAsync_ProducesValidNormalizedWorkbook()
    {
        var provider = new SyntheticClientDataProvider(NullLogger<SyntheticClientDataProvider>.Instance);
        var req = new ClientNormalizationRequest
        {
            ServerImportJobId = Guid.NewGuid(),
            InputPathOrReference = "./sample.synthetic.json"
        };

        var result = await provider.NormalizeAsync(req);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NormalizedWorkbook);
        Assert.Equal(2, result.RecordCount);
        Assert.NotEmpty(result.SourceFingerprint);
        Assert.Single(result.NormalizedWorkbook.Sheets);
        Assert.Equal(2, result.NormalizedWorkbook.Sheets[0].Rows.Count);
    }

    [Fact]
    public void ProviderRegistry_RegistersAndRetrievesProvider()
    {
        var provider = new SyntheticClientDataProvider(NullLogger<SyntheticClientDataProvider>.Instance);
        var registry = new ClientDataProviderRegistry(new[] { provider });

        var fetched = registry.GetProvider("SyntheticProvider");
        Assert.NotNull(fetched);
        Assert.Equal("SyntheticProvider", fetched.Capabilities.ProviderId);
        Assert.Null(registry.GetProvider("NascaProvider"));
    }
}
