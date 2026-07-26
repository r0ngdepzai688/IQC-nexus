namespace IqcQms.Application.DataPlatform;

public static class NormalizedWorkbookValidation
{
    public static NormalizedWorkbook EnsureSupported(NormalizedWorkbook workbook)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        EnsureProtocolSupported(workbook.ProtocolVersion);
        return workbook;
    }

    public static void EnsureProtocolSupported(string? protocolVersion)
    {
        if (!NormalizedWorkbookProtocol.IsSupported(protocolVersion ?? string.Empty))
        {
            throw new ImportPlatformException(
                ImportErrorCodes.ProtocolUnsupported,
                "The normalized workbook protocol version is not supported.");
        }
    }
}
