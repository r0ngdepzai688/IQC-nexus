using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExcelDataReader;
using IqcQms.Application.Interfaces.DataHub;
using IqcQms.Domain.Entities.DataHub;

namespace IqcQms.Infrastructure.Services.DataHub
{
    public class MasterPlanContractParser : IMasterPlanContractParser
    {
        private static readonly Dictionary<string, string[]> Aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "ProjectName", new[] { "project name", "project", "model", "model name" } },
            { "Basic", new[] { "basic", "base", "basic model" } },
            { "Area", new[] { "area", "product area", "category", "area buyer" } },
            { "Grade", new[] { "grade", "rank" } },
            { "SKU", new[] { "sku", "sku code", "model code" } },
            { "QtyLPR", new[] { "q'ty lpr", "qty lpr", "lpr qty", "quantity lpr", "lpr/lcv" } },
            { "QtyLSR", new[] { "q'ty lsr", "qty lsr", "lsr qty", "quantity lsr", "lsr" } },
            { "PVRTarget", new[] { "pvr target", "pvr", "pvr date", "pvr target pre pra" } },
            { "PRATarget", new[] { "pra target", "pra", "pra date" } },
            { "SRATarget", new[] { "sra target", "sra", "sra date" } },
            { "HWPIC", new[] { "iqc pic", "hw 검증 (iqc)", "hw 검증", "hw 검증 rqe", "pic", "owner", "responsible person" } },
            { "ImportedStatus", new[] { "status", "trạng thái", "plan status" } },
            { "Remark", new[] { "remark", "remarks", "note", "notes", "comment", "other" } }
        };

        private readonly string[] RequiredHard = { "ProjectName", "SKU", "PVRTarget" };

        private string NormalizeHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header)) return "";
            var h = header.ToLowerInvariant();
            h = h.Replace("\n", " ").Replace("\r", " ");
            h = h.Replace("q'ty", "qty");
            h = Regex.Replace(h, @"[^\w\s\(\)가-힣]", " ");
            h = Regex.Replace(h, @"\s+", " ").Trim();
            return h;
        }

        private string FindCanonical(string normalizedHeader)
        {
            foreach (var kvp in Aliases)
            {
                foreach (var alias in kvp.Value)
                {
                    var normAlias = NormalizeHeader(alias);
                    if (normalizedHeader == normAlias || normalizedHeader.Contains(normAlias))
                    {
                        return kvp.Key;
                    }
                }
            }
            return null;
        }

        public Task<(List<StagingMasterPlan> Records, List<string> MissingHardColumns)> ParseExcelAsync(Stream excelStream, string batchId)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            
            var records = new List<StagingMasterPlan>();
            int rawRowNumber = 0;
            var missingHardColumns = new List<string>();

            using (var reader = ExcelReaderFactory.CreateReader(excelStream))
            {
                // Scan first 20 rows for header
                int bestRowIdx = -1;
                int maxMatches = 0;
                var bestMapping = new Dictionary<string, int>(); // Canonical -> ColIndex

                var previewRows = new List<object[]>();
                while (reader.Read() && rawRowNumber < 20)
                {
                    rawRowNumber++;
                    var rowData = new object[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++) rowData[i] = reader.GetValue(i);
                    previewRows.Add(rowData);

                    int matches = 0;
                    var mapping = new Dictionary<string, int>();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var cell = rowData[i]?.ToString();
                        var normH = NormalizeHeader(cell);
                        if (string.IsNullOrEmpty(normH)) continue;

                        var canonical = FindCanonical(normH);
                        if (canonical != null && !mapping.ContainsKey(canonical))
                        {
                            mapping[canonical] = i;
                            matches++;
                        }
                    }

                    if (matches > maxMatches)
                    {
                        maxMatches = matches;
                        bestRowIdx = rawRowNumber;
                        bestMapping = mapping;
                    }
                }

                if (bestRowIdx == -1 || maxMatches == 0)
                {
                    throw new Exception("Could not find a valid Master Plan header row.");
                }

                // Check missing hard columns
                foreach (var hardCol in RequiredHard)
                {
                    if (!bestMapping.ContainsKey(hardCol))
                    {
                        missingHardColumns.Add(hardCol);
                    }
                }

                if (missingHardColumns.Any())
                {
                    return Task.FromResult((records, missingHardColumns));
                }

                // Reset stream to read data
                reader.Reset();
                rawRowNumber = 0;
                while (rawRowNumber < bestRowIdx && reader.Read())
                {
                    rawRowNumber++;
                }

                // Now at data row
                while (reader.Read())
                {
                    rawRowNumber++;

                    bool isEmptyRow = true;
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (reader.GetValue(i) != null && !string.IsNullOrWhiteSpace(reader.GetValue(i).ToString()))
                        {
                            isEmptyRow = false;
                            break;
                        }
                    }

                    if (isEmptyRow) continue;

                    var record = new StagingMasterPlan
                    {
                        BatchId = batchId,
                        RawRowNumber = rawRowNumber,
                        ProjectName = GetStringValue(reader, bestMapping, "ProjectName"),
                        Basic = GetStringValue(reader, bestMapping, "Basic"),
                        Area = GetStringValue(reader, bestMapping, "Area"),
                        Grade = GetStringValue(reader, bestMapping, "Grade"),
                        Sku = GetStringValue(reader, bestMapping, "SKU"),
                        QtyLpr = GetIntValue(reader, bestMapping, "QtyLPR"),
                        QtyLsr = GetIntValue(reader, bestMapping, "QtyLSR"),
                        PvrTargetDate = GetDateValue(reader, bestMapping, "PVRTarget"),
                        PraTargetDate = GetDateValue(reader, bestMapping, "PRATarget"),
                        SraTargetDate = GetDateValue(reader, bestMapping, "SRATarget"),
                        HwPic = GetStringValue(reader, bestMapping, "HWPIC"),
                        RawStatus = GetStringValue(reader, bestMapping, "ImportedStatus"),
                        Remark = GetStringValue(reader, bestMapping, "Remark"),
                        RowStatus = "Parsed",
                        CreatedAt = DateTime.UtcNow
                    };

                    records.Add(record);
                }
            }

            return Task.FromResult((records, missingHardColumns));
        }

        private string GetStringValue(IExcelDataReader reader, Dictionary<string, int> mapping, string canonical)
        {
            if (!mapping.TryGetValue(canonical, out int index) || index >= reader.FieldCount) return string.Empty;
            var val = reader.GetValue(index);
            return val == null ? string.Empty : val.ToString().Trim();
        }

        private int? GetIntValue(IExcelDataReader reader, Dictionary<string, int> mapping, string canonical)
        {
            if (!mapping.TryGetValue(canonical, out int index) || index >= reader.FieldCount) return null;
            var val = reader.GetValue(index);
            if (val == null) return null;
            string strVal = val.ToString().Trim().ToUpperInvariant().Replace("K", "");
            if (int.TryParse(strVal, out int result))
            {
                return result;
            }
            if (double.TryParse(strVal, out double doubleResult))
            {
                return (int)doubleResult;
            }
            return null;
        }

        private DateTime? GetDateValue(IExcelDataReader reader, Dictionary<string, int> mapping, string canonical)
        {
            if (!mapping.TryGetValue(canonical, out int index) || index >= reader.FieldCount) return null;
            
            try
            {
                if (reader.GetFieldType(index) == typeof(DateTime))
                {
                    return reader.GetDateTime(index);
                }
                
                var val = reader.GetValue(index);
                if (val == null) return null;
                
                string strVal = val.ToString().Trim();
                
                if (strVal.Contains("월") && strVal.Contains("일"))
                {
                    var parts = strVal.Replace("일", "").Split('월');
                    if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[0].Trim(), out int month) && int.TryParse(parts[1].Trim(), out int day))
                        {
                            return new DateTime(DateTime.UtcNow.Year, month, day);
                        }
                    }
                }
                
                if (double.TryParse(strVal, out double oaDate))
                {
                    return DateTime.FromOADate(oaDate);
                }
                
                if (DateTime.TryParse(strVal, out DateTime result))
                {
                    return result;
                }
            }
            catch
            {
            }
            return null;
        }
    }
}
