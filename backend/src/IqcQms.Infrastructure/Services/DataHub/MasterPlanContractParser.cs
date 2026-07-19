using System.Globalization;
using System.Text.RegularExpressions;
using ExcelDataReader;
using IqcQms.Application.Interfaces.DataHub;
using IqcQms.Domain.Entities.DataHub;

namespace IqcQms.Infrastructure.Services.DataHub;

public sealed class MasterPlanContractParser : IMasterPlanContractParser
{
    private static readonly Dictionary<string, string[]> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ProjectName"] = ["project name", "project", "model", "model name"],
        ["Basic"] = ["basic", "base", "basic model"],
        ["Area"] = ["area", "product area", "category", "area buyer"],
        ["Grade"] = ["grade", "rank"],
        ["SKU"] = ["sku", "sku code", "model code"],
        ["QtyLPR"] = ["q'ty lpr", "qty lpr", "lpr qty", "quantity lpr", "lpr/lcv"],
        ["QtyLSR"] = ["q'ty lsr", "qty lsr", "lsr qty", "quantity lsr", "lsr"],
        ["PVRTarget"] = ["pvr target", "pvr", "pvr date", "pvr target pre pra"],
        ["PRATarget"] = ["pra target", "pra", "pra date"],
        ["SRATarget"] = ["sra target", "sra", "sra date"],
        ["HWPIC"] = ["iqc pic", "hw 검증 (iqc)", "hw 검증", "hw 검증 rqe", "pic", "owner", "responsible person"],
        ["ImportedStatus"] = ["status", "trạng thái", "plan status"],
        ["Remark"] = ["remark", "remarks", "note", "notes", "comment", "other"]
    };

    private static readonly string[] RequiredHard = ["ProjectName", "SKU", "PVRTarget"];

    public Task<MasterPlanParseResult> ParseExcelAsync(Stream excelStream, string batchId, IReadOnlyCollection<HeaderMappingDto>? mappings = null)
    {
        ArgumentNullException.ThrowIfNull(excelStream);
        if (!excelStream.CanRead || !excelStream.CanSeek)
            throw new InvalidDataException("The upload stream must be readable and seekable.");

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var records = new List<StagingMasterPlan>();
        using var reader = ExcelReaderFactory.CreateReader(excelStream);

        var headerRow = 0;
        var firstNonEmptyRow = 0;
        var maxMatches = 0;
        var mapping = new Dictionary<string, int>();
        var duplicateColumns = new List<string>();
        var rowNumber = 0;
        while (reader.Read() && rowNumber < 20)
        {
            rowNumber++;
            if (firstNonEmptyRow == 0 && Enumerable.Range(0, reader.FieldCount).Any(i => !string.IsNullOrWhiteSpace(Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture))))
                firstNonEmptyRow = rowNumber;
            var candidate = new Dictionary<string, int>();
            var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var column = 0; column < reader.FieldCount; column++)
            {
                var canonical = FindCanonical(NormalizeHeader(Convert.ToString(reader.GetValue(column), CultureInfo.InvariantCulture)));
                if (canonical is null) continue;
                if (!candidate.TryAdd(canonical, column)) duplicates.Add(canonical);
            }

            if (candidate.Count > maxMatches)
            {
                headerRow = rowNumber;
                maxMatches = candidate.Count;
                mapping = candidate;
                duplicateColumns = duplicates.OrderBy(value => value).ToList();
            }
        }

        if (headerRow == 0 && mappings is not null) headerRow = firstNonEmptyRow;
        if (headerRow == 0)
            throw new InvalidDataException("Could not find a recognized Master Plan header row in the first 20 rows.");

        if (mappings is not null)
        {
            var invalidCanonical = mappings.Where(value => !Aliases.ContainsKey(value.CanonicalField)).Select(value => value.CanonicalField).Distinct().ToList();
            if (invalidCanonical.Count > 0) throw new InvalidDataException($"Unknown canonical field(s): {string.Join(", ", invalidCanonical)}.");
            duplicateColumns = mappings.GroupBy(value => value.CanonicalField, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            if (mappings.GroupBy(value => value.ColumnIndex).Any(group => group.Count() > 1)) throw new InvalidDataException("A workbook column cannot map to more than one canonical field.");
            mapping = mappings.GroupBy(value => value.CanonicalField, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().ColumnIndex, StringComparer.OrdinalIgnoreCase);
        }

        var missing = RequiredHard.Where(column => !mapping.ContainsKey(column)).ToList();
        if (missing.Count > 0 || duplicateColumns.Count > 0)
            return Task.FromResult(new MasterPlanParseResult(records, missing, duplicateColumns));

        reader.Reset();
        rowNumber = 0;
        while (rowNumber < headerRow && reader.Read()) rowNumber++;
        while (reader.Read())
        {
            rowNumber++;
            if (Enumerable.Range(0, reader.FieldCount).All(i => string.IsNullOrWhiteSpace(Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture))))
                continue;

            records.Add(new StagingMasterPlan
            {
                BatchId = batchId,
                RawRowNumber = rowNumber,
                ProjectName = GetString(reader, mapping, "ProjectName"),
                Basic = GetString(reader, mapping, "Basic"),
                Area = GetString(reader, mapping, "Area"),
                Grade = GetString(reader, mapping, "Grade"),
                Sku = GetString(reader, mapping, "SKU"),
                QtyLpr = GetInt(reader, mapping, "QtyLPR"),
                QtyLsr = GetInt(reader, mapping, "QtyLSR"),
                PvrTargetDate = GetDate(reader, mapping, "PVRTarget"),
                PraTargetDate = GetDate(reader, mapping, "PRATarget"),
                SraTargetDate = GetDate(reader, mapping, "SRATarget"),
                HwPic = GetString(reader, mapping, "HWPIC"),
                RawStatus = GetString(reader, mapping, "ImportedStatus"),
                Remark = GetString(reader, mapping, "Remark"),
                RowStatus = "Parsed"
            });
        }

        return Task.FromResult(new MasterPlanParseResult(records, missing, duplicateColumns));
    }

    public Task<HeaderInspectionDto> InspectHeadersAsync(Stream excelStream)
    {
        ArgumentNullException.ThrowIfNull(excelStream);
        if (!excelStream.CanRead || !excelStream.CanSeek) throw new InvalidDataException("The upload stream must be readable and seekable.");
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateReader(excelStream);
        var bestRow = 0;
        var bestScore = -1;
        var bestHeaders = new List<string>();
        var row = 0;
        while (reader.Read() && row < 20)
        {
            row++;
            var headers = Enumerable.Range(0, reader.FieldCount).Select(i => Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty).ToList();
            if (headers.All(string.IsNullOrWhiteSpace)) continue;
            var score = headers.Count(value => FindCanonical(NormalizeHeader(value)) is not null);
            if (score <= bestScore) continue;
            bestScore = score;
            bestRow = row;
            bestHeaders = headers;
        }
        if (bestRow == 0) throw new InvalidDataException("The workbook does not contain a non-empty header row in the first 20 rows.");
        var columns = bestHeaders.Select((header, index) =>
        {
            var candidates = FindCanonicalCandidates(NormalizeHeader(header));
            return new HeaderColumnDto(index, header, candidates.Count == 1 ? candidates[0] : null, candidates.Count > 1);
        }).ToList();
        return Task.FromResult(new HeaderInspectionDto(bestRow, columns, Aliases.Keys.ToList(), RequiredHard.ToList()));
    }

    private static string NormalizeHeader(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.ToLowerInvariant().Replace('\n', ' ').Replace('\r', ' ').Replace("q'ty", "qty");
        return Regex.Replace(Regex.Replace(normalized, @"[^\w\s()가-힣]", " "), @"\s+", " ").Trim();
    }

    private static List<string> FindCanonicalCandidates(string header) => Aliases.Where(pair =>
        pair.Value.Select(NormalizeHeader).Any(alias => header == alias || header.Contains(alias, StringComparison.Ordinal))).Select(pair => pair.Key).ToList();

    private static string? FindCanonical(string header)
    {
        var candidates = FindCanonicalCandidates(header);
        return candidates.Count == 1 ? candidates[0] : null;
    }

    private static object? GetValue(IExcelDataReader reader, IReadOnlyDictionary<string, int> mapping, string field) =>
        mapping.TryGetValue(field, out var index) && index < reader.FieldCount ? reader.GetValue(index) : null;

    private static string GetString(IExcelDataReader reader, IReadOnlyDictionary<string, int> mapping, string field) =>
        Convert.ToString(GetValue(reader, mapping, field), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;

    private static int? GetInt(IExcelDataReader reader, IReadOnlyDictionary<string, int> mapping, string field)
    {
        var text = Convert.ToString(GetValue(reader, mapping, field), CultureInfo.InvariantCulture)?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer)) return integer;
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            && number >= int.MinValue && number <= int.MaxValue ? (int)number : null;
    }

    private static DateTime? GetDate(IExcelDataReader reader, IReadOnlyDictionary<string, int> mapping, string field)
    {
        var value = GetValue(reader, mapping, field);
        if (value is DateTime date) return date;
        var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        var korean = Regex.Match(text, @"^(\d{1,2})\s*월\s*(\d{1,2})\s*일$");
        if (korean.Success && int.TryParse(korean.Groups[1].Value, out var month) && int.TryParse(korean.Groups[2].Value, out var day)
            && month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(DateTime.UtcNow.Year, month))
            return new DateTime(DateTime.UtcNow.Year, month, day);
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var oaDate) && oaDate is >= 0 and <= 2958465)
            return DateTime.FromOADate(oaDate);
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed) ? parsed : null;
    }
}
