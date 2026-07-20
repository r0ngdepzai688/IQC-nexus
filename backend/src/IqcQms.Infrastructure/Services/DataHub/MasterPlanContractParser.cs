using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;
using IqcQms.Application;
using IqcQms.Application.Interfaces.DataHub;
using IqcQms.Domain.Entities.DataHub;
using IqcQms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IqcQms.Infrastructure.Services.DataHub;

public sealed class MasterPlanContractParser(AppDbContext? context = null) : IMasterPlanContractParser
{
    private const int ScanRows = 50;
    private const int ScanColumns = 50;
    private static readonly string[] Required = ["ProjectName", "Basic", "Grade", "Cat"];
    private static readonly Dictionary<string, string[]> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ProjectName"] = ["project name", "project", "model", "model name"],
        ["Basic"] = ["basic", "base", "basic model"],
        ["Area"] = ["area", "product area", "area buyer"],
        ["Grade"] = ["grade", "rank"],
        ["Cat"] = ["cat", "category", "type"],
        ["QtyLprLqv"] = ["qty lpr lqv", "qty lpr/lqv", "lpr/lqv qty", "quantity lpr lqv"],
        ["QtyLsr"] = ["qty lsr", "lsr qty", "quantity lsr"],
        ["PvrTarget"] = ["pvr target", "pvr", "pvr date", "pvr target pre pra"],
        ["PraTarget"] = ["pra target", "pra", "pra date"],
        ["SraTarget"] = ["sra target", "sra", "sra date"],
        ["HwPic"] = ["iqc pic", "hw pic", "pic", "owner", "responsible person"],
        ["MainLprLqvDate"] = ["main pra pqv", "main lpr lqv", "main lpr/lqv", "lpr lqv main date"],
        ["MainLsrDate"] = ["main lsr", "lsr main date"]
    };

    public async Task<MasterPlanParseResult> ParseExcelAsync(Stream excelStream, string batchId, IReadOnlyCollection<HeaderMappingDto>? mappings = null)
    {
        var workbook = ReadWorkbook(excelStream);
        var inspection = await InspectAsync(workbook);
        var duplicateColumns = new List<string>();
        Dictionary<string, int> map;
        if (mappings is { Count: > 0 })
        {
            var unknown = mappings.Where(value => !Aliases.ContainsKey(value.CanonicalField)).Select(value => value.CanonicalField).Distinct().ToList();
            if (unknown.Count > 0) throw new InvalidDataException($"Unknown canonical field(s): {string.Join(", ", unknown)}.");
            duplicateColumns = mappings.GroupBy(value => value.CanonicalField, StringComparer.OrdinalIgnoreCase).Where(value => value.Count() > 1).Select(value => value.Key).ToList();
            if (mappings.GroupBy(value => value.ColumnIndex).Any(value => value.Count() > 1)) throw new InvalidDataException("A workbook column cannot map to more than one canonical field.");
            map = mappings.GroupBy(value => value.CanonicalField, StringComparer.OrdinalIgnoreCase).ToDictionary(value => value.Key, value => value.First().ColumnIndex, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            map = inspection.Columns.Where(value => value.SuggestedCanonical is not null && !value.Ambiguous)
                .GroupBy(value => value.SuggestedCanonical!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(value => value.Key, value => value.First().ColumnIndex, StringComparer.OrdinalIgnoreCase);
            duplicateColumns = inspection.Columns.Where(value => value.SuggestedCanonical is not null)
                .GroupBy(value => value.SuggestedCanonical!, StringComparer.OrdinalIgnoreCase).Where(value => value.Count() > 1).Select(value => value.Key).ToList();
        }

        var missing = Required.Where(value => !map.ContainsKey(value)).ToList();
        if (missing.Count > 0 || duplicateColumns.Count > 0) return new([], missing, duplicateColumns);

        var records = new List<StagingMasterPlan>();
        for (var rowIndex = Math.Max(inspection.DataStartRow - 1, inspection.HeaderRow + inspection.HeaderDepth - 1); rowIndex < workbook.Rows.Count; rowIndex++)
        {
            var row = workbook.Rows[rowIndex];
            if (IsBlank(row) || IsRepeatedHeader(row, map) || IsRecognizedFooter(row, map)) continue;
            var basic = GetString(row, map, "Basic");
            var cat = GetString(row, map, "Cat");
            MasterPlanBusinessKey.TryCreate(basic, cat, out var basicKey, out var catKey, out _);
            records.Add(new StagingMasterPlan
            {
                BatchId = batchId, RawRowNumber = rowIndex + 1,
                ProjectName = GetString(row, map, "ProjectName"), Basic = basic, BasicKey = basicKey,
                Area = GetString(row, map, "Area"), Grade = GetString(row, map, "Grade"), Cat = cat, CatKey = catKey,
                QtyLpr = GetInt(row, map, "QtyLprLqv"), QtyLsr = GetInt(row, map, "QtyLsr"),
                PvrTargetDate = GetDate(row, map, "PvrTarget"), PraTargetDate = GetDate(row, map, "PraTarget"), SraTargetDate = GetDate(row, map, "SraTarget"),
                HwPic = GetString(row, map, "HwPic"), MainLprLqvDate = GetDate(row, map, "MainLprLqvDate"), MainLsrDate = GetDate(row, map, "MainLsrDate"),
                RowStatus = "Parsed"
            });
        }
        return new(records, missing, duplicateColumns);
    }

    public async Task<HeaderInspectionDto> InspectHeadersAsync(Stream excelStream) => await InspectAsync(ReadWorkbook(excelStream));

    private async Task<HeaderInspectionDto> InspectAsync(WorkbookData workbook)
    {
        var learned = context is null ? [] : await context.HeaderMappingProfiles.AsNoTracking().Where(value => value.IsApproved).ToListAsync();
        Candidate? best = null;
        var maxRow = Math.Min(ScanRows, workbook.Rows.Count);
        for (var start = 0; start < maxRow; start++)
        for (var depth = 1; depth <= 3 && start + depth <= maxRow; depth++)
        {
            if (depth > 1 && Enumerable.Range(start + 1, depth - 1).Any(row =>
                    Enumerable.Range(0, Math.Min(ScanColumns, workbook.Rows[row].Length))
                        .Count(column => IsExactHeaderToken(HeaderValue(workbook, row, column))) < 2))
                continue;
            var paths = ComposePaths(workbook, start, depth);
            var score = paths.Sum(path => Score(path, learned).Score);
            var requiredHits = paths.Select(path => Score(path, learned).Canonical).Where(value => value is not null).Intersect(Required, StringComparer.OrdinalIgnoreCase).Count();
            score += requiredHits * 5;
            score += depth * .5;
            if (best is null || score > best.Score) best = new(start, depth, score, paths);
        }
        if (best is null || best.Score <= 0) throw new InvalidDataException("Could not identify a Master Plan header region in the first 50 rows.");
        var dataStart = DetectDataStart(workbook.Rows, best.Start + best.Depth, best.Paths);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", best.Paths.Select(Normalize))))).ToLowerInvariant();
        var columns = best.Paths.Select((path, index) =>
        {
            var suggestion = Score(path, learned, fingerprint);
            var samples = workbook.Rows.Skip(dataStart).Take(20).Select(row => ValueAt(row, index)).Where(value => !string.IsNullOrWhiteSpace(value)).Take(5).ToList();
            var detected = DetectType(samples);
            return new HeaderColumnDto(index, path, suggestion.Ambiguous ? null : suggestion.Canonical, suggestion.Ambiguous,
                best.Depth > 1 ? HeaderValue(workbook, best.Start, index) : "", best.Depth > 1 ? HeaderValue(workbook, best.Start + best.Depth - 1, index) : path,
                path, samples, detected, suggestion.Confidence, suggestion.Reason, suggestion.Learned);
        }).ToList();
        return new(best.Start + 1, columns, Aliases.Keys.ToList(), Required.ToList(), best.Depth, dataStart + 1, fingerprint);
    }

    private static int DetectDataStart(List<object?[]> rows, int afterHeader, List<string> paths)
    {
        var mapped = paths.Select((path, index) => (index, canonical: MatchAliases(path).FirstOrDefault().Canonical)).Where(value => value.canonical is not null).ToList();
        for (var row = afterHeader; row < rows.Count; row++)
        {
            var hits = mapped.Count(value => !string.IsNullOrWhiteSpace(ValueAt(rows[row], value.index)));
            if (hits >= 2 && !mapped.Any(value => Normalize(ValueAt(rows[row], value.index)) == Normalize(value.canonical))) return row;
        }
        return afterHeader;
    }

    private static List<string> ComposePaths(WorkbookData workbook, int start, int depth)
    {
        var width = Math.Min(ScanColumns, workbook.Rows.Skip(start).Take(depth).Select(value => value.Length).DefaultIfEmpty().Max());
        return Enumerable.Range(0, width).Select(column =>
        {
            var values = new List<string>();
            for (var row = start; row < start + depth; row++)
            {
                var value = HeaderValue(workbook, row, column);
                if (value.Length > 0 && !values.Contains(value, StringComparer.OrdinalIgnoreCase)) values.Add(value);
            }
            return string.Join(" > ", values);
        }).ToList();
    }

    private static string HeaderValue(WorkbookData workbook, int row, int column)
    {
        var value = ValueAt(workbook.Rows[row], column);
        if (value.Length > 0) return value;
        var merge = workbook.Merges.FirstOrDefault(value => row >= value.FromRow && row <= value.ToRow && column >= value.FromColumn && column <= value.ToColumn);
        return merge is null ? string.Empty : ValueAt(workbook.Rows[merge.FromRow], merge.FromColumn);
    }

    private static Suggestion Score(string path, IReadOnlyCollection<HeaderMappingProfile> learned, string fingerprint = "")
    {
        var normalized = Normalize(path);
        var learnedMatch = learned.FirstOrDefault(value => value.NormalizedHeaderPath == normalized && (fingerprint.Length == 0 || value.WorkbookFingerprint == fingerprint || value.WorkbookFingerprint.Length == 0));
        if (learnedMatch is not null) return new(learnedMatch.CanonicalField, 20, false, learnedMatch.Confidence, "Confirmed learned mapping", true);
        var matches = MatchAliases(path).OrderByDescending(value => value.Score).ToList();
        if (matches.Count == 0 || matches[0].Score < .65) return new(null, 0, false, 0, "No reliable alias match", false);
        var ambiguous = matches.Count > 1 && Math.Abs(matches[0].Score - matches[1].Score) < .08;
        return new(matches[0].Canonical, matches[0].Score * 10, ambiguous, matches[0].Score, matches[0].Score == 1 ? "Exact normalized alias" : "Token/fuzzy alias similarity", false);
    }

    private static IEnumerable<(string Canonical, double Score)> MatchAliases(string path)
    {
        var normalized = Normalize(path);
        foreach (var pair in Aliases)
        {
            var score = pair.Value.Append(pair.Key).Select(alias => Similarity(normalized, Normalize(alias))).Max();
            if (score >= .45) yield return (pair.Key, score);
        }
    }

    private static bool IsExactHeaderToken(string value)
    {
        var normalized = Normalize(value);
        return normalized.Length > 0 && Aliases.Any(pair =>
            Normalize(pair.Key) == normalized || pair.Value.Any(alias => Normalize(alias) == normalized));
    }

    private static double Similarity(string left, string right)
    {
        if (left == right) return 1;
        if (left.EndsWith(right, StringComparison.Ordinal) || right.EndsWith(left, StringComparison.Ordinal)) return 1;
        var a = left.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var b = right.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var token = a.Count == 0 && b.Count == 0 ? 0 : (double)a.Intersect(b).Count() / a.Union(b).Count();
        var distance = Levenshtein(left, right);
        var fuzzy = 1d - (double)distance / Math.Max(1, Math.Max(left.Length, right.Length));
        return Math.Max(token, fuzzy);
    }

    private static int Levenshtein(string a, string b)
    {
        var costs = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++) { var previous = costs[0]; costs[0] = i; for (var j = 1; j <= b.Length; j++) { var old = costs[j]; costs[j] = Math.Min(Math.Min(costs[j] + 1, costs[j - 1] + 1), previous + (a[i - 1] == b[j - 1] ? 0 : 1)); previous = old; } }
        return costs[^1];
    }

    private static WorkbookData ReadWorkbook(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead || !stream.CanSeek) throw new InvalidDataException("The upload stream must be readable and seekable.");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        stream.Position = 0;
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var rows = new List<object?[]>();
        while (reader.Read()) rows.Add(Enumerable.Range(0, Math.Min(reader.FieldCount, ScanColumns)).Select(reader.GetValue).ToArray());
        var merges = reader.MergeCells?.Select(value => new Merge(value.FromRow, value.ToRow, value.FromColumn, value.ToColumn)).ToList() ?? [];
        return new(rows, merges);
    }

    private static string Normalize(string? value) => Regex.Replace((value ?? string.Empty).Normalize(NormalizationForm.FormKC).ToLowerInvariant().Replace("q'ty", "qty").Replace('/', ' '), @"[^\p{L}\p{N}]+", " ").Trim();
    private static string ValueAt(object?[] row, int index) => index < row.Length ? Convert.ToString(row[index], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty : string.Empty;
    private static bool IsBlank(object?[] row) => row.All(value => string.IsNullOrWhiteSpace(Convert.ToString(value, CultureInfo.InvariantCulture)));

    private static bool IsRecognizedFooter(object?[] row, IReadOnlyDictionary<string, int> map)
    {
        var projectName = GetString(row, map, "ProjectName").Trim();
        if (!Regex.IsMatch(projectName, @"^(grand\s+total|sub\s*total|total|end\s+of\s+(report|file))\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return false;
        return Required.Where(field => field != "ProjectName").All(field => string.IsNullOrWhiteSpace(GetString(row, map, field)));
    }
    private static string GetString(object?[] row, IReadOnlyDictionary<string, int> map, string field) => map.TryGetValue(field, out var index) ? ValueAt(row, index) : string.Empty;
    private static object? GetValue(object?[] row, IReadOnlyDictionary<string, int> map, string field) => map.TryGetValue(field, out var index) && index < row.Length ? row[index] : null;
    private static bool IsRepeatedHeader(object?[] row, IReadOnlyDictionary<string, int> map) => Required.Count(field =>
    {
        if (!map.TryGetValue(field, out var index) || !Aliases.TryGetValue(field, out var aliases)) return false;
        var normalized = Normalize(ValueAt(row, index));
        return normalized == Normalize(field) || aliases.Any(alias => normalized == Normalize(alias));
    }) >= 3;
    private static int? GetInt(object?[] row, IReadOnlyDictionary<string, int> map, string field) { var value = GetValue(row, map, field); if (value is double number) return (int)number; return int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var result) ? result : null; }
    private static DateTime? GetDate(object?[] row, IReadOnlyDictionary<string, int> map, string field) { var value = GetValue(row, map, field); if (value is DateTime date) return date; if (value is double serial && serial is >= 0 and <= 2958465) return DateTime.FromOADate(serial); var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim(); if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out serial) && serial is >= 0 and <= 2958465) return DateTime.FromOADate(serial); return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date) ? date : null; }
    private static string DetectType(List<string> samples) { if (samples.Count == 0) return "Empty"; if (samples.Count(value => GetDate([value], new Dictionary<string, int> { ["x"] = 0 }, "x") is not null) >= samples.Count * .8) return "Date"; if (samples.Count(value => int.TryParse(value, out _)) >= samples.Count * .8) return "Integer"; return "Text"; }

    private sealed record WorkbookData(List<object?[]> Rows, List<Merge> Merges);
    private sealed record Merge(int FromRow, int ToRow, int FromColumn, int ToColumn);
    private sealed record Candidate(int Start, int Depth, double Score, List<string> Paths);
    private sealed record Suggestion(string? Canonical, double Score, bool Ambiguous, double Confidence, string Reason, bool Learned);
}
