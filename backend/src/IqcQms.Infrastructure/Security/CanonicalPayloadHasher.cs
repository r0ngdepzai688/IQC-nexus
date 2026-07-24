using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IqcQms.ClientAgent.Contracts;

namespace IqcQms.Infrastructure.Security;

public static class CanonicalPayloadHasher
{
    public static string ComputeCanonicalHash(NormalizedWorkbookUploadRequest request)
    {
        var canonicalObj = new
        {
            schema = request.CanonicalSchemaVersion ?? "1.0",
            device = request.DeviceId ?? string.Empty,
            submissionId = request.PayloadSubmissionId ?? string.Empty,
            nonce = request.Nonce ?? string.Empty,
            jobId = request.ServerImportJobId.ToString("N"),
            sourceFingerprint = request.SourceFingerprint ?? string.Empty,
            workbookName = request.NormalizedWorkbook?.WorkbookName ?? string.Empty,
            sheets = request.NormalizedWorkbook?.Sheets?.Select(s => new
            {
                name = s.SheetName ?? string.Empty,
                rows = s.Rows?.Select(r => new
                {
                    idx = r.RowIndex,
                    cells = r.Cells?.Select(c => new
                    {
                        colName = c.ColumnName ?? string.Empty,
                        colIdx = c.ColumnIndex,
                        val = c.Value ?? string.Empty,
                        type = c.DataType ?? "String"
                    })
                })
            })
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(canonicalObj, jsonOptions);
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes);
    }

    public static string ComputeSourceFingerprint(NormalizedWorkbook workbook)
    {
        if (workbook == null) return string.Empty;
        var sb = new StringBuilder();
        sb.Append("WB:").Append(workbook.WorkbookName ?? "").Append(';');
        if (workbook.Sheets != null)
        {
            foreach (var sheet in workbook.Sheets)
            {
                sb.Append("SH:").Append(sheet.SheetName ?? "").Append(';');
                if (sheet.Rows != null)
                {
                    foreach (var row in sheet.Rows)
                    {
                        sb.Append("R:").Append(row.RowIndex).Append(';');
                        if (row.Cells != null)
                        {
                            foreach (var cell in row.Cells)
                            {
                                sb.Append("C:").Append(cell.ColumnName).Append('=').Append(cell.Value ?? "").Append(';');
                            }
                        }
                    }
                }
            }
        }

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}
