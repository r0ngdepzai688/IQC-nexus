using IqcQms.Application.DataPlatform;

namespace IqcQms.Infrastructure.DataPlatform;

public sealed class DataSourceProviderRegistry : IDataSourceProviderRegistry, IWorkbookNormalizer
{
    private readonly IReadOnlyDictionary<DataSourceProviderKind, IDataSourceProvider> _providers;

    public DataSourceProviderRegistry(IEnumerable<IDataSourceProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _providers = providers
            .GroupBy(provider => provider.Kind)
            .ToDictionary(
                group => group.Key,
                group => group.Count() == 1
                    ? group.Single()
                    : throw new InvalidOperationException($"More than one provider is registered for {group.Key}."));
    }

    public bool TryGet(DataSourceDescriptor source, out IDataSourceProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(source);
        return _providers.TryGetValue(source.Kind, out provider) && provider.CanHandle(source);
    }

    public IDataSourceProvider GetRequired(DataSourceDescriptor source) =>
        TryGet(source, out var provider)
            ? provider!
            : throw new ImportPlatformException(
                ImportErrorCodes.ProviderNotFound,
                $"No data source provider is available for {source.Kind}.");

    public async Task<NormalizedWorkbook> NormalizeAsync(
        DataSourceProviderContext context,
        CancellationToken cancellationToken = default)
    {
        var workbook = await GetRequired(context.Source).NormalizeAsync(context, cancellationToken);
        return NormalizedWorkbookValidation.EnsureSupported(workbook);
    }
}

internal static class ProviderInput
{
    public static async Task<byte[]> ReadBoundedAsync(
        DataSourceProviderContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Source.ContentLength > context.Limits.MaximumPayloadBytes)
            throw new ImportPlatformException(ImportErrorCodes.FileTooLarge, "The import payload exceeds the configured size limit.");

        using var output = new MemoryStream();
        var buffer = new byte[81_920];
        long total = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = await context.Payload.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0) break;
            total += read;
            if (total > context.Limits.MaximumPayloadBytes)
                throw new ImportPlatformException(ImportErrorCodes.FileTooLarge, "The import payload exceeds the configured size limit.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        if (total == 0)
            throw new ImportPlatformException(ImportErrorCodes.WorkbookEmpty, "The import payload is empty.");
        return output.ToArray();
    }

    public static ImportPlatformException Cancelled(OperationCanceledException exception) =>
        new(ImportErrorCodes.Cancelled, "The import was cancelled.", exception);
}
