using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using IqcQms.ClientAgent.Contracts;

namespace IqcQms.Infrastructure.Security;

public class NormalizedWorkbookCanonicalizer : INormalizedWorkbookCanonicalizer
{
    private const string CanonicalizerVersion = "v1";

    public byte[] CanonicalizeWorkbook(NormalizedWorkbook workbook)
    {
        if (workbook == null) return Array.Empty<byte>();

        var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        writer.Write(CanonicalizerVersion);
        writer.Write((workbook.WorkbookName ?? string.Empty).Normalize(NormalizationForm.FormC));

        var sortedSheets = (workbook.Sheets ?? new List<NormalizedSheet>())
            .OrderBy(s => s.SheetName ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        writer.Write(sortedSheets.Count);
        foreach (var sheet in sortedSheets)
        {
            writer.Write((sheet.SheetName ?? string.Empty).Normalize(NormalizationForm.FormC));

            var sortedRows = (sheet.Rows ?? new List<NormalizedRow>())
                .OrderBy(r => r.RowIndex)
                .ToList();

            writer.Write(sortedRows.Count);
            foreach (var row in sortedRows)
            {
                writer.Write(row.RowIndex);

                var sortedCells = (row.Cells ?? new List<NormalizedCell>())
                    .OrderBy(c => c.ColumnIndex)
                    .ThenBy(c => c.ColumnName ?? string.Empty, StringComparer.Ordinal)
                    .ToList();

                writer.Write(sortedCells.Count);
                foreach (var cell in sortedCells)
                {
                    writer.Write((cell.ColumnName ?? string.Empty).Normalize(NormalizationForm.FormC));
                    writer.Write(cell.ColumnIndex);
                    writer.Write(cell.DataType ?? "String");

                    if (cell.Value == null)
                    {
                        writer.Write(false); // isNotNull
                    }
                    else
                    {
                        writer.Write(true); // isNotNull
                        writer.Write(FormatCanonicalValue(cell.Value, cell.DataType ?? "String").Normalize(NormalizationForm.FormC));
                    }
                }
            }
        }

        writer.Flush();
        return ms.ToArray();
    }

    public string ComputeSourceFingerprint(NormalizedWorkbook workbook)
    {
        var canonicalBytes = CanonicalizeWorkbook(workbook);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(canonicalBytes);
        return $"v1_{Convert.ToHexString(hashBytes)}";
    }

    private static string FormatCanonicalValue(string rawValue, string dataType)
    {
        if (string.IsNullOrEmpty(rawValue)) return string.Empty;

        if (string.Equals(dataType, "Double", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "Number", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "Decimal", StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var dVal))
            {
                return dVal.ToString("G17", CultureInfo.InvariantCulture);
            }
        }

        if (string.Equals(dataType, "DateTime", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "Date", StringComparison.OrdinalIgnoreCase))
        {
            if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dtVal))
            {
                return dtVal.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            }
        }

        if (string.Equals(dataType, "Boolean", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "Bool", StringComparison.OrdinalIgnoreCase))
        {
            if (bool.TryParse(rawValue, out var bVal))
            {
                return bVal ? "TRUE" : "FALSE";
            }
        }

        return rawValue;
    }
}
