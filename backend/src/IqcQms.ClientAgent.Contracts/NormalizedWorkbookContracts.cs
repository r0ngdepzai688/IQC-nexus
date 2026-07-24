namespace IqcQms.ClientAgent.Contracts;

public class NormalizedCell
{
    public string ColumnName { get; set; } = string.Empty;
    public int ColumnIndex { get; set; }
    public string? Value { get; set; }
    public string DataType { get; set; } = "String";
}

public class NormalizedRow
{
    public int RowIndex { get; set; }
    public List<NormalizedCell> Cells { get; set; } = new();
}

public class NormalizedSheet
{
    public string SheetName { get; set; } = string.Empty;
    public List<NormalizedRow> Rows { get; set; } = new();
}

public class NormalizedWorkbook
{
    public string WorkbookName { get; set; } = string.Empty;
    public List<NormalizedSheet> Sheets { get; set; } = new();
}

public class NormalizedWorkbookUploadRequest
{
    public string CanonicalSchemaVersion { get; set; } = "1.0";
    public string DeviceId { get; set; } = string.Empty;
    public Guid ServerImportJobId { get; set; }
    public string ProviderId { get; set; } = "SyntheticProvider";
    public string ProviderVersion { get; set; } = "1.0.0";
    public string SourceFingerprint { get; set; } = string.Empty;
    public NormalizedWorkbook NormalizedWorkbook { get; set; } = new();
    public int RecordCount { get; set; }
    public string DiagnosticsSummary { get; set; } = "Synthetic Normalization Complete";
    public DateTime RequestTimestampUtc { get; set; } = DateTime.UtcNow;
    public string Nonce { get; set; } = Guid.NewGuid().ToString("N");
    public string ContentFingerprint { get; set; } = string.Empty;
}

public class NormalizedWorkbookUploadResponse
{
    public Guid UploadId { get; set; } = Guid.NewGuid();
    public Guid ServerImportJobId { get; set; }
    public string Status { get; set; } = "Accepted";
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
}
