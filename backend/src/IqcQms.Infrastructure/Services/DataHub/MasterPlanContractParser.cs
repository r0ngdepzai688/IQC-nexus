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
    private static readonly HashSet<string> IntegerFields = ["QtyLprLqv", "QtyLsr"];
    private static readonly HashSet<string> DateFields = ["PvrTarget", "PraTarget", "SraTarget", "MainLprLqvDate", "MainLsrDate"];
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
        {
            if (IsBlank(workbook.Rows[start])) continue;
            for (var depth = 1; depth <= 3 && start + depth <= maxRow; depth++)
            {
                if (IsBlank(workbook.Rows[start + depth - 1])) continue;

                var headerPaths = ComposeHeaderPaths(workbook, start, depth);
                if (headerPaths.All(p => string.IsNullOrWhiteSpace(p.DisplayPath))) continue;

                var dataStartTemp = DetectDataStart(workbook, workbook.Rows, start + depth, headerPaths);
                if (dataStartTemp < start + depth || dataStartTemp >= workbook.Rows.Count) continue;

                var nonAmbiguous = headerPaths.Select(path => Score(path.NormalizedPath, learned)).Where(s => s.Canonical is not null && !s.Ambiguous).ToList();
                var aliasScore = nonAmbiguous.Sum(s => s.Score);
                var requiredHits = nonAmbiguous.Select(s => s.Canonical!).Intersect(Required, StringComparer.OrdinalIgnoreCase).Distinct().Count();

                var distinctDisplayPaths = headerPaths.Select(p => p.DisplayPath).Where(p => p.Length > 0).Distinct().Count();
                var hasMerges = Enumerable.Range(start, depth).Any(r => workbook.Merges.Any(m => r >= m.FromRow && r <= m.ToRow));
                var structuralScore = distinctDisplayPaths * 0.2 + (hasMerges ? 1.0 : 0.0);

                var totalScore = aliasScore + requiredHits * 5.0 + depth * 0.5 + structuralScore;

                if (best is null || totalScore > best.Score)
                {
                    best = new(start, depth, totalScore, headerPaths);
                }
            }
        }
        if (best is null || best.Score <= 0) throw new InvalidDataException("Could not identify a Master Plan header region in the first 50 rows.");
        var dataStart = DetectDataStart(workbook, workbook.Rows, best.Start + best.Depth, best.HeaderPaths);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", best.HeaderPaths.Select(hp => hp.NormalizedPath))))).ToLowerInvariant();
        var columns = best.HeaderPaths.Select((hp, index) =>
        {
            var suggestion = Score(hp.NormalizedPath, learned, fingerprint);
            var sampledCells = workbook.Rows.Select((row, rowIndex) => new { RowIndex = rowIndex, Value = ValueAt(row, index) })
                .Skip(dataStart).Take(20).Where(cell => !string.IsNullOrWhiteSpace(cell.Value)).Take(5).ToList();
            var samples = sampledCells.Select(cell => cell.Value).ToList();
            var dateFormattedSamples = sampledCells.Count(cell => workbook.DateFormattedCells.Contains((cell.RowIndex, index)));
            var detected = DetectType(samples, suggestion.Canonical, dateFormattedSamples);
            return new HeaderColumnDto(index, hp.DisplayPath, suggestion.Ambiguous ? null : suggestion.Canonical, suggestion.Ambiguous,
                best.Depth > 1 ? HeaderValue(workbook, best.Start, index) : "",
                best.Depth > 1 ? HeaderValue(workbook, best.Start + best.Depth - 1, index) : hp.DisplayPath,
                hp.DisplayPath, samples, detected, suggestion.Confidence, suggestion.Reason, suggestion.Learned);
        }).ToList();
        return new(best.Start + 1, columns, Aliases.Keys.ToList(), Required.ToList(), best.Depth, dataStart + 1, fingerprint);
    }

    private static int DetectDataStart(WorkbookData workbook, List<object?[]> rows, int afterHeader, List<HeaderPath> headerPaths)
    {
        for (var row = afterHeader; row < rows.Count; row++)
        {
            if (IsBlank(rows[row])) continue;
            var nonHeaderHits = 0;
            var totalNonEmpty = 0;
            for (var col = 0; col < headerPaths.Count; col++)
            {
                var cellVal = ValueAt(rows[row], col);
                if (string.IsNullOrWhiteSpace(cellVal)) continue;
                totalNonEmpty++;
                var normalizedCell = Normalize(cellVal);
                var isMerged = workbook.Merges.Any(m => row >= m.FromRow && row <= m.ToRow && col >= m.FromColumn && col <= m.ToColumn);
                var isHeaderLabel = isMerged
                                    || IsExactHeaderToken(cellVal)
                                    || headerPaths[col].Segments.Any(seg => Normalize(seg) == normalizedCell)
                                    || headerPaths[col].NormalizedPath == normalizedCell;
                if (!isHeaderLabel)
                {
                    nonHeaderHits++;
                }
            }
            if (totalNonEmpty >= 2 && nonHeaderHits >= totalNonEmpty / 2)
            {
                return row;
            }
        }
        return afterHeader;
    }

    private static List<HeaderPath> ComposeHeaderPaths(WorkbookData workbook, int start, int depth)
    {
        var width = Math.Min(ScanColumns, workbook.Rows.Skip(start).Take(depth).Select(value => value.Length).DefaultIfEmpty().Max());
        return Enumerable.Range(0, width).Select(column =>
        {
            var segments = new List<string>();
            for (var row = start; row < start + depth; row++)
            {
                var value = HeaderValue(workbook, row, column).Trim();
                if (value.Length > 0)
                {
                    if (segments.Count == 0 || !string.Equals(segments[^1], value, StringComparison.OrdinalIgnoreCase))
                    {
                        segments.Add(value);
                    }
                }
            }
            var displayPath = string.Join(" > ", segments);
            var normalizedPath = Normalize(displayPath);
            return new HeaderPath(column, segments, displayPath, normalizedPath);
        }).ToList();
    }

    private static List<string> ComposePaths(WorkbookData workbook, int start, int depth) =>
        ComposeHeaderPaths(workbook, start, depth).Select(hp => hp.DisplayPath).ToList();

    private static string HeaderValue(WorkbookData workbook, int row, int column)
    {
        var merge = workbook.Merges.FirstOrDefault(value => row >= value.FromRow && row <= value.ToRow && column >= value.FromColumn && column <= value.ToColumn);
        if (merge is not null)
        {
            return ValueAt(workbook.Rows[merge.FromRow], merge.FromColumn);
        }
        return ValueAt(workbook.Rows[row], column);
    }

    private static Suggestion Score(string pathOrNormalized, IReadOnlyCollection<HeaderMappingProfile> learned, string fingerprint = "")
    {
        var normalized = Normalize(pathOrNormalized);
        if (normalized.Length == 0) return new(null, 0, false, 0, "No reliable alias match", false);

        var learnedMatch = learned.FirstOrDefault(value => value.NormalizedHeaderPath == normalized && (fingerprint.Length == 0 || value.WorkbookFingerprint == fingerprint || value.WorkbookFingerprint.Length == 0));
        if (learnedMatch is not null) return new(learnedMatch.CanonicalField, 20, false, learnedMatch.Confidence, "Confirmed learned mapping", true);
        var matches = MatchAliases(normalized).OrderByDescending(value => value.Score).ToList();
        if (matches.Count == 0 || matches[0].Score < .65) return new(null, 0, false, 0, "No reliable alias match", false);
        var ambiguous = matches.Count > 1 && Math.Abs(matches[0].Score - matches[1].Score) < .08;
        return new(matches[0].Canonical, matches[0].Score * 10, ambiguous, matches[0].Score, matches[0].Score == 1 ? "Exact normalized alias" : "Token/fuzzy alias similarity", false);
    }

    private static IEnumerable<(string Canonical, double Score)> MatchAliases(string pathOrNormalized)
    {
        var normalized = Normalize(pathOrNormalized);
        if (normalized.Length == 0) yield break;
        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var leafToken = parts.Length > 0 ? parts[^1] : string.Empty;

        foreach (var pair in Aliases)
        {
            var maxScore = 0.0;
            foreach (var rawAlias in pair.Value.Append(pair.Key))
            {
                var normalizedAlias = Normalize(rawAlias);
                var aliasTokenCount = normalizedAlias.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                var sim = Similarity(normalized, normalizedAlias);
                // Elevate score to 1.0 when the leaf segment of a multi-segment path exactly matches
                // the alias, BUT only when the alias itself has >= 2 tokens.
                // This prevents single-word abbreviations like "pra", "sra", "pvr" from
                // claiming ownership of a path that merely contains that word.
                if (sim < 1.0 && leafToken.Length > 0 && aliasTokenCount >= 2 && Normalize(leafToken) == normalizedAlias)
                {
                    sim = 1.0;
                }
                if (sim > maxScore) maxScore = sim;
            }
            if (maxScore >= .45) yield return (pair.Key, maxScore);
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
        var a = left.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var b = right.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (a.Count == 0 || b.Count == 0) return 0;
        // Alias tokens are a meaningful subset of the path when the alias covers
        // at least (path_token_count - 1) tokens: e.g. a 2-token alias covers a
        // 3-token path but NOT a 4-token path (prevents "pra target" scoring 1.0
        // against "pvr target pre pra" when "target" and "pra" both appear by chance).
        if (b.Count > 1 && b.Count >= a.Count - 1 && b.IsSubsetOf(a)) return 1;
        var token = (double)a.Intersect(b).Count() / a.Union(b).Count();
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
        var dateFormattedCells = new HashSet<(int Row, int Column)>();
        while (reader.Read())
        {
            var rowIndex = rows.Count;
            var width = Math.Min(reader.FieldCount, ScanColumns);
            rows.Add(Enumerable.Range(0, width).Select(reader.GetValue).ToArray());
            for (var column = 0; column < width; column++)
            {
                if (IsDateNumberFormat(reader.GetNumberFormatString(column)))
                    dateFormattedCells.Add((rowIndex, column));
            }
        }
        var merges = reader.MergeCells?.Select(value => new Merge(value.FromRow, value.ToRow, value.FromColumn, value.ToColumn)).ToList() ?? [];
        return new(rows, merges, dateFormattedCells);
    }

    private static bool IsDateNumberFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format)) return false;
        var withoutLiterals = Regex.Replace(format, "\\\"[^\\\"]*\\\"|\\\\.", string.Empty);
        withoutLiterals = Regex.Replace(withoutLiterals, @"\[(?!h+\]|m+\]|s+\])[^\]]+\]", string.Empty, RegexOptions.IgnoreCase);
        return Regex.IsMatch(withoutLiterals, @"(?<![A-Za-z])(y{2,4}|m{1,4}|d{1,4}|h{1,2}|s{1,2})(?![A-Za-z])", RegexOptions.IgnoreCase);
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
    private static string DetectType(List<string> samples, string? canonicalField, int dateFormattedSamples)
    {
        if (samples.Count == 0) return "Empty";
        if (canonicalField is not null && IntegerFields.Contains(canonicalField)) return "Integer";
        if (canonicalField is not null && DateFields.Contains(canonicalField)) return "Date";

        var integerCount = samples.Count(value => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _));
        if (integerCount >= samples.Count * .8)
            return dateFormattedSamples >= samples.Count * .8 ? "Date" : "Integer";

        return samples.Count(value => GetDate([value], new Dictionary<string, int> { ["x"] = 0 }, "x") is not null) >= samples.Count * .8
            ? "Date"
            : "Text";
    }

    private sealed record HeaderPath(int ColumnIndex, List<string> Segments, string DisplayPath, string NormalizedPath);
    private sealed record WorkbookData(List<object?[]> Rows, List<Merge> Merges, HashSet<(int Row, int Column)> DateFormattedCells);
    private sealed record Merge(int FromRow, int ToRow, int FromColumn, int ToColumn);
    private sealed record Candidate(int Start, int Depth, double Score, List<HeaderPath> HeaderPaths);
    private sealed record Suggestion(string? Canonical, double Score, bool Ambiguous, double Confidence, string Reason, bool Learned);
}
