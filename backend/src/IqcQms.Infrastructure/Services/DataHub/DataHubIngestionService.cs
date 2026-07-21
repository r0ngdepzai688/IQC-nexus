using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using IqcQms.Application;
using IqcQms.Application.Interfaces.DataHub;
using IqcQms.Domain.Entities.DataHub;
using IqcQms.Domain.Entities.NewModels;
using IqcQms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IqcQms.Infrastructure.Services.DataHub
{
    public class DataHubIngestionService : IDataHubIngestionService
    {
        public const long MaximumUploadBytes = 50L * 1024 * 1024;
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".xlsx", ".xls", ".xlsm" };
        private readonly AppDbContext _context;
        private readonly IMasterPlanContractParser _parser;
        private readonly ILogger<DataHubIngestionService> _logger;
        private readonly DataHubPathConfig _pathConfig;

        public DataHubIngestionService(AppDbContext context, IMasterPlanContractParser parser, ILogger<DataHubIngestionService> logger, IOptions<DataHubPathConfig> pathConfig)
        {
            _context = context;
            _parser = parser;
            _logger = logger;
            _pathConfig = pathConfig.Value;
        }

        private void EnsureDirectories()
        {
            Directory.CreateDirectory(_pathConfig.NewModelsMasterPlanManualUploadPath);
            Directory.CreateDirectory(_pathConfig.NewModelsMasterPlanRawPath);
            Directory.CreateDirectory(_pathConfig.NewModelsMasterPlanProcessedPath);
            Directory.CreateDirectory(_pathConfig.NewModelsMasterPlanRejectedPath);
            Directory.CreateDirectory(_pathConfig.NewModelsMasterPlanReportsPath);
            Directory.CreateDirectory(_pathConfig.NewModelsMasterPlanTempPath);
        }

        public async Task<ImportBatch> ProcessUploadAsync(Stream fileStream, string fileName, string uploadedBy, string module = "NewModels", IReadOnlyCollection<HeaderMappingDto>? mappings = null)
        {
            ValidateUpload(fileStream, fileName, module);
            EnsureDirectories();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            await using var ingestionTransaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            
            // 1. Generate Batch ID
            string dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
            int seq = await _context.ImportBatches.CountAsync(b => b.BatchId.StartsWith($"IMP-MP-{dateStr}")) + 1;
            string batchId = $"IMP-MP-{dateStr}-{seq:D3}";

            // 2. Hash File & Size
            long fileSize = fileStream.Length;
            fileStream.Position = 0;
            string fileHash;
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(fileStream);
                fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
            fileStream.Position = 0;

            // 2.2 Reject Exact Duplicates
            bool exists = await _context.ImportBatches.AnyAsync(b => b.FileHash == fileHash);
            if (exists)
            {
                throw new InvalidOperationException($"Duplicate file detected. A batch with this exact content (Hash: {fileHash}) already exists.");
            }

            // 2.5 Save to Raw Archive
            string nowYear = DateTime.UtcNow.ToString("yyyy");
            string nowMonth = DateTime.UtcNow.ToString("MM");
            string nowDay = DateTime.UtcNow.ToString("dd");
            string rawDir = Path.Combine(_pathConfig.NewModelsMasterPlanRawPath, nowYear, nowMonth, nowDay);
            Directory.CreateDirectory(rawDir);
            
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string safeFileName = Path.GetFileName(fileName);
            string rawFilePath = Path.Combine(rawDir, $"{batchId}_{safeFileName}");
            
            using (var fs = new FileStream(rawFilePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fs);
            }
            fileStream.Position = 0; // reset for parsing

            // 2.6 Save Metadata JSON
            var metadata = new {
                BatchId = batchId,
                OriginalFileName = fileName,
                RawArchivePath = rawFilePath,
                SourcePath = "ManualUpload",
                FileSize = fileSize,
                FileHash = fileHash,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                Module = "New Models",
                Source = "Master Plan",
                ImportMethod = "ManualUpload"
            };
            string metaPath = Path.Combine(rawDir, $"{batchId}_metadata.json");
            await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

            // 3. Create ImportBatch
            var dataSource = await _context.DataSources.FirstOrDefaultAsync(d => d.Module == module) 
                             ?? new DataSource { SourceName = "New Models Master Plan", Module = module };
            
            if (dataSource.Id == 0)
            {
                _context.DataSources.Add(dataSource);
                await _context.SaveChangesAsync();
            }

            var batch = new ImportBatch
            {
                BatchId = batchId,
                DataSourceId = dataSource.Id,
                SourceName = dataSource.SourceName,
                Module = module,
                UploadedBy = uploadedBy,
                OriginalFileName = fileName,
                FileSize = fileSize,
                FileHash = fileHash,
                Status = "Processing"
            };
            _context.ImportBatches.Add(batch);
            
            // 4. Create RawFile record (tracking the physical file saved above)
            var rawFile = new RawFile
            {
                BatchId = batchId,
                OriginalFileName = fileName,
                FileSize = fileSize,
                FileHash = fileHash,
                ArchivedPath = rawFilePath
            };
            _context.RawFiles.Add(rawFile);
            
            await _context.SaveChangesAsync();
            
            // 5. Parse Excel to Staging
            try
            {
                MasterPlanParseResult parseResult;
                try
                {
                    parseResult = await _parser.ParseExcelAsync(fileStream, batchId, mappings);
                }
                catch (InvalidDataException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException("The Excel workbook is malformed, encrypted, or cannot be parsed.", ex);
                }
                var stagingRecords = parseResult.Records;
                
                if (parseResult.MissingHardColumns.Any() || parseResult.DuplicateColumns.Any())
                {
                    batch.Status = parseResult.MissingHardColumns.Any() ? "Failed - Missing Columns" : "Failed - Duplicate Columns";
                    batch.TotalRows = 0;
                    await _context.SaveChangesAsync();
                    
                    var errorReportPath = Path.Combine(_pathConfig.NewModelsMasterPlanReportsPath, $"ValidationResult_{batchId}.json");
                    var report = new { BatchId = batchId, Status = "Failed", Error = "Invalid Columns", MissingColumns = parseResult.MissingHardColumns, DuplicateColumns = parseResult.DuplicateColumns };
                    await File.WriteAllTextAsync(errorReportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
                    await ingestionTransaction.CommitAsync();
                    return batch;
                }
                
                batch.TotalRows = stagingRecords.Count;
                
                _context.StagingMasterPlans.AddRange(stagingRecords);
                await _context.SaveChangesAsync();

                // 6 & 7 & 8: Validate, Clean, Core Validate
                await RunValidationPipelineAsync(batchId);

                foreach (var confirmed in mappings?.Where(value => value.ConfirmLearning && value.NormalizedHeaderPath.Length > 0) ?? [])
                {
                    var normalizedPath = System.Text.RegularExpressions.Regex.Replace(
                        confirmed.NormalizedHeaderPath.Normalize(System.Text.NormalizationForm.FormKC).ToLowerInvariant().Replace("q'ty", "qty").Replace('/', ' '),
                        @"[^\p{L}\p{N}]+", " ").Trim();
                    var rejectedProfiles = await _context.HeaderMappingProfiles.Where(value => value.NormalizedHeaderPath == normalizedPath && value.WorkbookFingerprint == confirmed.WorkbookFingerprint && value.CanonicalField != confirmed.CanonicalField && value.IsApproved).ToListAsync();
                    foreach (var rejected in rejectedProfiles) rejected.RejectionCount++;
                    var profile = await _context.HeaderMappingProfiles.FirstOrDefaultAsync(value => value.NormalizedHeaderPath == normalizedPath && value.CanonicalField == confirmed.CanonicalField && value.WorkbookFingerprint == confirmed.WorkbookFingerprint);
                    if (profile is null)
                    {
                        _context.HeaderMappingProfiles.Add(new HeaderMappingProfile { NormalizedHeaderPath = normalizedPath, CanonicalField = confirmed.CanonicalField, DetectedDataType = confirmed.DetectedDataType, WorkbookFingerprint = confirmed.WorkbookFingerprint, ConfirmationCount = 1, Confidence = 1, IsApproved = true, CreatedBy = uploadedBy, LastUsedAt = DateTime.UtcNow });
                    }
                    else { profile.ConfirmationCount++; profile.IsApproved = true; profile.LastUsedAt = DateTime.UtcNow; }
                }

                // Update Batch counts
                batch.ValidRows = await _context.StagingMasterPlans.CountAsync(s => s.BatchId == batchId && (s.RowStatus == "ReadyToInsert" || s.RowStatus == "ReadyToUpdate"));
                batch.ErrorRows = await _context.StagingMasterPlans.CountAsync(s => s.BatchId == batchId && s.RowStatus == "ValidationError");
                batch.ReviewRequiredRows = await _context.StagingMasterPlans.CountAsync(s => s.BatchId == batchId && s.RowStatus == "ReviewRequired");
                batch.NoChangeRecords = await _context.StagingMasterPlans.CountAsync(s => s.BatchId == batchId && s.RowStatus == "SkipNoChange");
                
                batch.Status = "Staged";
                watch.Stop();
                batch.DurationMs = watch.ElapsedMilliseconds;
                
                await _context.SaveChangesAsync();
                
                // Generate Report
                var reportRecords = await _context.StagingMasterPlans.Where(s => s.BatchId == batchId).ToListAsync();
                var summary = new {
                    BatchId = batchId,
                    TotalRows = batch.TotalRows,
                    ValidRows = batch.ValidRows,
                    ErrorRows = batch.ErrorRows,
                    ReviewRequiredRows = batch.ReviewRequiredRows,
                    Rows = reportRecords.Select(r => new { r.RawRowNumber, r.ProjectName, r.Basic, r.Cat, r.RowStatus, r.ValidationMessage, r.CoreValidationMessage })
                };
                string reportPath = Path.Combine(_pathConfig.NewModelsMasterPlanReportsPath, $"ValidationResult_{batchId}.json");
                await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));

                LogInfo(batchId, $"Batch {batchId} processed. Total: {batch.TotalRows}, Valid: {batch.ValidRows}, Errors: {batch.ErrorRows}, Review: {batch.ReviewRequiredRows}");
                await _context.SaveChangesAsync();
                await ingestionTransaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await ingestionTransaction.RollbackAsync();
                TryDeleteUncommittedFile(rawFilePath, batchId);
                TryDeleteUncommittedFile(metaPath, batchId);
                TryDeleteUncommittedFile(Path.Combine(_pathConfig.NewModelsMasterPlanReportsPath, $"ValidationResult_{batchId}.json"), batchId);
                LogError(batchId, $"Parsing failed: {ex.Message}");
                throw;
            }

            return batch;
        }

        private async Task RunValidationPipelineAsync(string batchId)
        {
            var records = await _context.StagingMasterPlans.Where(s => s.BatchId == batchId).ToListAsync();
            var mappings = await _context.MappingDictionaries.Where(m => m.IsActive).ToListAsync();
            var existingProjects = await _context.MasterPlans.ToListAsync();
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            var duplicatesInBatch = records
                .Where(r => MasterPlanBusinessKey.TryCreate(r.Basic, r.Cat, out _, out _, out _))
                .GroupBy(r => { MasterPlanBusinessKey.TryCreate(r.Basic, r.Cat, out _, out _, out var key); return key; }, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .Select(r => r.Id)
                .ToHashSet();

            foreach (var record in records)
            {
                var errors = new List<string>();
                var warnings = new List<string>();
                var needsReview = false;
                void RequiredValue(string field, string value)
                {
                    if (!string.IsNullOrWhiteSpace(value)) return;
                    var message = $"{field} is missing";
                    errors.Add(message);
                    AddValidationError(batchId, record.Id, record.RawRowNumber, record.ProjectName, string.Empty, field, "Required", message, value);
                }
                RequiredValue("ProjectName", record.ProjectName);
                RequiredValue("Basic", record.Basic);
                RequiredValue("Grade", record.Grade);
                RequiredValue("Cat", record.Cat);
                MasterPlanBusinessKey.TryCreate(record.Basic, record.Cat, out var basicKey, out var catKey, out _);
                record.BasicKey = basicKey;
                record.CatKey = catKey;
                if (duplicatesInBatch.Contains(record.Id))
                {
                    errors.Add("Duplicate normalized Basic + Cat in current batch");
                    AddValidationError(batchId, record.Id, record.RawRowNumber, record.ProjectName, string.Empty, "Basic+Cat", "DuplicateBusinessKey", "Duplicate normalized Basic + Cat in current batch", $"{record.Basic} + {record.Cat}");
                }
                if (record.PvrTargetDate.HasValue && record.PraTargetDate.HasValue && record.PvrTargetDate > record.PraTargetDate)
                {
                    warnings.Add("PVRTarget is after PRATarget");
                    needsReview = true;
                }
                if (record.PraTargetDate.HasValue && record.SraTargetDate.HasValue && record.PraTargetDate > record.SraTargetDate)
                {
                    warnings.Add("PRATarget is after SRATarget");
                    needsReview = true;
                }
                record.HwPic = ApplyMapping(mappings, "PIC", record.HwPic, ref needsReview, batchId, record.Id, "PIC Mapping Issue");
                record.Area = ApplyMapping(mappings, "Area", record.Area, ref needsReview, batchId, record.Id, "Area Mapping Issue");
                if (!string.IsNullOrWhiteSpace(record.HwPic))
                {
                    var userExists = users.Any(u => string.Equals(u.FullName, record.HwPic, StringComparison.OrdinalIgnoreCase) 
                                                 || string.Equals(u.Username, record.HwPic, StringComparison.OrdinalIgnoreCase));
                    if (!userExists)
                    {
                        var message = $"Unknown PIC '{record.HwPic}'.";
                        warnings.Add(message);
                        if (!_context.BusinessReviewQueues.Local.Any(value => value.StagingId == record.Id && value.ConflictType == "PIC"))
                        {
                            _context.BusinessReviewQueues.Add(new BusinessReviewQueue
                            {
                                BatchId = batchId,
                                StagingId = record.Id,
                                ConflictType = "PIC",
                                ConflictMessage = message
                            });
                        }
                        needsReview = true;
                    }
                }
                var existingMp = existingProjects.FirstOrDefault(value => value.BasicKey == basicKey && value.CatKey == catKey);
                if (existingMp is not null)
                {
                    record.TargetMasterPlanId = existingMp.Id;
                    record.TargetVersion = existingMp.Version;
                    var differences = Differences(existingMp, record);
                    if (differences.Count == 0) record.RowStatus = needsReview || warnings.Count > 0 ? "ReviewRequired" : "SkipNoChange";
                    else if (errors.Count == 0)
                    {
                        record.RowStatus = "ReviewRequired";
                        record.CoreValidationMessage = $"Existing Basic + Cat requires review: {string.Join(", ", differences.Select(value => value.Field))}";
                        _context.BusinessReviewQueues.Add(new BusinessReviewQueue { BatchId = batchId, StagingId = record.Id, ConflictType = "ExistingBusinessKey", ConflictMessage = JsonSerializer.Serialize(differences) });
                    }
                }
                if (errors.Count > 0)
                {
                    record.RowStatus = "ValidationError";
                    record.ValidationMessage = string.Join("; ", errors);
                }
                else if (existingMp is null && (needsReview || warnings.Count > 0))
                {
                    record.RowStatus = "ReviewRequired";
                    record.CoreValidationMessage = string.Join("; ", warnings);
                }
                else if (existingMp is null) record.RowStatus = "ReadyToInsert";
            }
            await _context.SaveChangesAsync();
        }

        private static List<FieldDifference> Differences(MasterPlan existing, StagingMasterPlan incoming)
        {
            var result = new List<FieldDifference>();
            void Compare(string field, object? oldValue, object? newValue) { if (!Equals(oldValue, newValue)) result.Add(new(field, Convert.ToString(oldValue) ?? string.Empty, Convert.ToString(newValue) ?? string.Empty)); }
            Compare("ProjectName", existing.ProjectName, incoming.ProjectName); Compare("Area", existing.Area, incoming.Area); Compare("Grade", existing.Grade, incoming.Grade);
            Compare("QtyLprLqv", existing.QtyLpr, incoming.QtyLpr ?? 0); Compare("QtyLsr", existing.QtyLsr, incoming.QtyLsr ?? 0);
            Compare("PvrTarget", existing.PvrTargetDate, incoming.PvrTargetDate); Compare("PraTarget", existing.PraTargetDate, incoming.PraTargetDate); Compare("SraTarget", existing.SraTargetDate, incoming.SraTargetDate);
            Compare("HwPic", existing.HwPic, incoming.HwPic); Compare("MainLprLqvDate", existing.MainLprLqvDate, incoming.MainLprLqvDate); Compare("MainLsrDate", existing.MainLsrDate, incoming.MainLsrDate);
            return result;
        }

        private sealed record FieldDifference(string Field, string OldValue, string NewValue);

        private string ApplyMapping(List<MappingDictionary> mappings, string type, string rawValue, ref bool needsReview, string batchId, int stagingId, string conflictMsg)
        {
            if (string.IsNullOrWhiteSpace(rawValue)) return rawValue;
            
            var map = mappings.FirstOrDefault(m => m.DictionaryType == type && string.Equals(m.RawValue, rawValue, StringComparison.OrdinalIgnoreCase));
            if (map != null)
            {
                return map.MappedValue;
            }
            
            // Add to Business Review Queue if not found
            _context.BusinessReviewQueues.Add(new BusinessReviewQueue
            {
                BatchId = batchId,
                StagingId = stagingId,
                ConflictType = type,
                ConflictMessage = type == "PIC"
                    ? $"Unknown PIC '{rawValue}'."
                    : $"{conflictMsg}: '{rawValue}' not found in dictionary."
            });
            
            needsReview = true;
            return rawValue; // Keep raw value until reviewed
        }

        private void AddValidationError(string batchId, int stagingId, int rowNumber, string projectName, string sku, string fieldName, string errorType, string errorMessage, string rawValue)
        {
            _context.ValidationErrors.Add(new ValidationError
            {
                BatchId = batchId,
                StagingId = stagingId,
                RawRowNumber = rowNumber,
                ProjectName = projectName,
                Sku = sku,
                FieldName = fieldName,
                ErrorType = errorType,
                ErrorMessage = errorMessage,
                RawValue = rawValue
            });
        }

        public async Task<ImportBatch> CommitBatchAsync(string batchId, string committedBy)
        {
            var batch = await _context.ImportBatches.FirstOrDefaultAsync(b => b.BatchId == batchId);
            if (batch == null || batch.Status != "Staged") throw new InvalidOperationException("Batch not ready for commit");

            var allRecords = await _context.StagingMasterPlans.Where(s => s.BatchId == batchId).ToListAsync();
            if (allRecords.Any(s => s.RowStatus is "ValidationError" or "ReviewRequired" or "Blocked"))
                throw new InvalidOperationException("Batch contains blocking validation or review items. Resolve every item before committing; partial import is not supported.");

            var readyRecords = allRecords.Where(s => s.RowStatus is "ReadyToInsert" or "ReadyToUpdate").ToList();
            if (readyRecords.Count == 0 && !allRecords.Any(value => value.RowStatus == "SkipNoChange"))
                throw new InvalidOperationException("Batch contains no records ready to insert or update.");
            
            int created = 0;
            int updated = 0;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var record in readyRecords)
                {
                    if (!MasterPlanBusinessKey.TryCreate(record.Basic, record.Cat, out var basicKey, out var catKey, out var businessKey))
                        throw new InvalidOperationException($"Row {record.RawRowNumber} has an invalid Basic + Cat business key.");
                    var existing = await _context.MasterPlans.FirstOrDefaultAsync(value => value.BasicKey == basicKey && value.CatKey == catKey);
                    int masterPlanId;

                    if (record.RowStatus == "ReadyToInsert" && existing == null)
                    {
                        var newMp = new MasterPlan
                        {
                            ProjectName = record.ProjectName,
                            Basic = record.Basic,
                            BasicKey = basicKey,
                            Area = record.Area,
                            Grade = record.Grade,
                            Cat = record.Cat,
                            CatKey = catKey,
                            QtyLpr = record.QtyLpr ?? 0,
                            QtyLsr = record.QtyLsr ?? 0,
                            PvrTargetDate = record.PvrTargetDate,
                            PraTargetDate = record.PraTargetDate,
                            SraTargetDate = record.SraTargetDate,
                            MainLprLqvDate = record.MainLprLqvDate,
                            MainLsrDate = record.MainLsrDate,
                            HwPic = record.HwPic,
                            ImportedStatus = record.RawStatus,
                            Remark = record.Remark,
                            LastImportBatchId = batchId,
                            ActionStatus = "Ready",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.MasterPlans.Add(newMp);
                        await _context.SaveChangesAsync();
                        masterPlanId = newMp.Id;
                        created++;
                        
                        AddAuditLog(batchId, "MasterPlans", businessKey, "All", "", "Created", committedBy, "Master Plan Import");
                    }
                    else if (record.RowStatus == "ReadyToUpdate" && existing is not null)
                    {
                        if (record.TargetMasterPlanId != existing.Id || record.TargetVersion != existing.Version)
                            throw new DbUpdateConcurrencyException($"Master Plan '{record.Basic} + {record.Cat}' changed after review. Review the batch again.");
                        foreach (var difference in Differences(existing, record)) AddAuditLog(batchId, "MasterPlans", businessKey, difference.Field, difference.OldValue, difference.NewValue, committedBy, "Master Plan Import");
                        ApplyMutableFields(existing, record);
                        existing.LastImportBatchId = batchId;
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.Version++;
                        masterPlanId = existing.Id;
                        updated++;
                    }
                    else throw new InvalidOperationException($"Business-key conflict for '{record.Basic} + {record.Cat}'. Refresh and review before committing.");

                    // Upsert Milestones
                    await UpsertMilestoneAsync(masterPlanId, record.ProjectName, "PVR", record.PvrTargetDate, batchId);
                    await UpsertMilestoneAsync(masterPlanId, record.ProjectName, "PRA", record.PraTargetDate, batchId);
                    await UpsertMilestoneAsync(masterPlanId, record.ProjectName, "SRA", record.SraTargetDate, batchId);
                    
                    record.RowStatus = "Committed";
                }

                batch.CreatedRecords = created;
                batch.UpdatedRecords = updated;
                batch.NoChangeRecords = allRecords.Count(value => value.RowStatus == "SkipNoChange");
                
                // Determine batch status
                batch.Status = "Committed";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                LogInfo(batchId, $"Batch {batchId} committed by {committedBy}. Status: {batch.Status}. Created: {created}, Updated: {updated}.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw new InvalidOperationException("A reviewed Master Plan record changed before commit. Refresh and review the batch again; no rows were committed.", ex);
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw new InvalidOperationException("A normalized Basic + Cat conflict was detected during commit. Refresh and review the batch again; no rows were committed.", ex);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                batch.Status = "Failed";
                LogError(batchId, $"Commit failed: {ex.Message}");
                await _context.SaveChangesAsync();
                throw;
            }

            // Copy to Processed Archive
            try 
            {
                var rawFile = await _context.RawFiles.FirstOrDefaultAsync(r => r.BatchId == batchId);
                if (rawFile != null && File.Exists(rawFile.ArchivedPath))
                {
                    string nowYear = DateTime.UtcNow.ToString("yyyy");
                    string nowMonth = DateTime.UtcNow.ToString("MM");
                    string nowDay = DateTime.UtcNow.ToString("dd");
                    string processedDir = Path.Combine(_pathConfig.NewModelsMasterPlanProcessedPath, nowYear, nowMonth, nowDay);
                    Directory.CreateDirectory(processedDir);
                    
                    string processedPath = Path.Combine(processedDir, Path.GetFileName(rawFile.ArchivedPath));
                    File.Copy(rawFile.ArchivedPath, processedPath, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not copy file to Processed archive for batch {batchId}. Error: {ex.Message}");
            }

            return batch;
        }

        private static void ApplyMutableFields(MasterPlan target, StagingMasterPlan source)
        {
            target.ProjectName = source.ProjectName; target.Area = source.Area; target.Grade = source.Grade;
            target.QtyLpr = source.QtyLpr ?? 0; target.QtyLsr = source.QtyLsr ?? 0;
            target.PvrTargetDate = source.PvrTargetDate; target.PraTargetDate = source.PraTargetDate; target.SraTargetDate = source.SraTargetDate;
            target.HwPic = source.HwPic; target.MainLprLqvDate = source.MainLprLqvDate; target.MainLsrDate = source.MainLsrDate;
        }

        public async Task<bool> ResolveReviewItemAsync(int reviewItemId, string action, string resolvedBy, string? note = null)
        {
            var item = await _context.BusinessReviewQueues.FindAsync(reviewItemId);
            if (item == null || item.Status != "Pending") return false;

            item.Status = "Resolved";
            item.ResolutionAction = action;
            item.ResolvedBy = resolvedBy;
            item.ResolvedAt = DateTime.UtcNow;

            var staging = await _context.StagingMasterPlans.FindAsync(item.StagingId);
            if (staging != null)
            {
                if (item.ConflictType == "ExistingBusinessKey" && action is not ("Update" or "Skip")) return false;
                staging.RowStatus = await CalculateResolvedRowStatusAsync(staging, action is "Ignore" or "Skip");
            }

            var batch = await _context.ImportBatches.FirstOrDefaultAsync(value => value.BatchId == item.BatchId);
            if (batch is not null)
            {
                var batchRows = await _context.StagingMasterPlans.Where(value => value.BatchId == item.BatchId).ToListAsync();
                batch.ValidRows = batchRows.Count(value => value.RowStatus is "ReadyToInsert" or "ReadyToUpdate");
                batch.ReviewRequiredRows = batchRows.Count(value => value.RowStatus == "ReviewRequired");
                batch.SkippedRows = batchRows.Count(value => value.RowStatus == "Skipped");
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ImportReviewSummaryDto?> GetReviewSummaryAsync(string batchId)
        {
            var batch = await _context.ImportBatches.AsNoTracking().FirstOrDefaultAsync(value => value.BatchId == batchId);
            if (batch is null) return null;
            var staging = await _context.StagingMasterPlans.AsNoTracking().Where(value => value.BatchId == batchId).OrderBy(value => value.RawRowNumber).ToListAsync();
            var errors = await _context.ValidationErrors.AsNoTracking().Where(value => value.BatchId == batchId).ToListAsync();
            var reviewItems = await _context.BusinessReviewQueues.AsNoTracking().Where(value => value.BatchId == batchId).ToListAsync();
            var pendingReviewItems = reviewItems.Where(value => value.Status == "Pending").ToList();
            var rows = new List<ImportReviewRowDto>();
            foreach (var record in staging)
            {
                var rowErrors = errors.Where(value => value.StagingId == record.Id).ToList();
                rows.AddRange(rowErrors.Select(error => new ImportReviewRowDto
                {
                    RowNumber = record.RawRowNumber, Basic = record.Basic, Cat = record.Cat, Field = error.FieldName,
                    CurrentValue = error.RawValue, Severity = "Error", Message = error.ErrorMessage,
                    Status = record.RowStatus, ConflictType = "ValidationError"
                }));
                if (rowErrors.Count == 0)
                {
                    var recordReviewItems = reviewItems.Where(value => value.StagingId == record.Id).ToList();
                    var pendingReview = recordReviewItems.Where(value => value.Status == "Pending")
                        .OrderBy(value => value.ConflictType == "ExistingBusinessKey" ? 0 : 1)
                        .FirstOrDefault();
                    if (pendingReview?.ConflictType == "ExistingBusinessKey")
                    {
                        var differences = JsonSerializer.Deserialize<List<FieldDifference>>(pendingReview.ConflictMessage) ?? [];
                        rows.AddRange(differences.Select(difference => new ImportReviewRowDto
                        {
                            RowNumber = record.RawRowNumber, Basic = record.Basic, Cat = record.Cat, Field = difference.Field,
                            CurrentValue = difference.NewValue, OldValue = difference.OldValue, NewValue = difference.NewValue,
                            Severity = "Warning", Message = "Existing Basic + Cat has a changed value.", Status = record.RowStatus,
                            ConflictType = "ExistingBusinessKey", ReviewItemId = pendingReview.Id, SupportedActions = ["Update", "Skip"]
                        }));
                        continue;
                    }
                    var contextualReview = pendingReview ?? ResolvedContextualReview(record, recordReviewItems);
                    rows.Add(new ImportReviewRowDto
                    {
                        RowNumber = record.RawRowNumber, Basic = record.Basic, Cat = record.Cat, Field = ReviewField(contextualReview),
                        CurrentValue = ReviewCurrentValue(record, contextualReview),
                        Severity = record.RowStatus == "ReviewRequired" ? "Warning" : record.RowStatus is "Skipped" or "SkipNoChange" ? "Skipped" : "Ready",
                        Message = ReviewMessage(record, contextualReview),
                        Status = record.RowStatus,
                        ConflictType = record.RowStatus == "SkipNoChange" ? "NoChange" : contextualReview?.ConflictType ?? string.Empty,
                        ReviewItemId = pendingReview?.Id,
                        SupportedActions = pendingReview is null ? [] : ["Override", "Ignore", "CreateMissing"]
                    });
                }
            }

            return new ImportReviewSummaryDto
            {
                BatchId = batch.BatchId, FileName = batch.OriginalFileName, ValidRows = batch.ValidRows,
                WarningRows = staging.Count(value => value.RowStatus == "ReviewRequired"),
                ErrorRows = staging.Count(value => value.RowStatus is "ValidationError" or "Blocked"),
                ExistingSkuConflicts = pendingReviewItems.Count(value => value.ConflictType == "ExistingBusinessKey"),
                ExistingBusinessKeyConflicts = pendingReviewItems.Count(value => value.ConflictType == "ExistingBusinessKey"),
                ReadyToUpdateRows = staging.Count(value => value.RowStatus == "ReadyToUpdate"),
                NoChangeRows = staging.Count(value => value.RowStatus == "SkipNoChange"),
                SkippedRows = staging.Count(value => value.RowStatus == "Skipped"), Rows = rows
            };
        }

        private static BusinessReviewQueue? ResolvedContextualReview(StagingMasterPlan record, IReadOnlyCollection<BusinessReviewQueue> reviews)
        {
            var recordMessage = !string.IsNullOrWhiteSpace(record.CoreValidationMessage)
                ? record.CoreValidationMessage
                : record.ValidationMessage;
            return reviews
                .Where(value => value.Status == "Resolved" && value.ConflictType != "ExistingBusinessKey")
                .OrderByDescending(value => string.Equals(value.ConflictMessage, recordMessage, StringComparison.Ordinal))
                .ThenByDescending(value => value.ResolvedAt ?? value.CreatedAt)
                .FirstOrDefault();
        }

        private static string ReviewField(BusinessReviewQueue? review) => review?.ConflictType switch
        {
            "PIC" => "HwPic",
            "Area" => "Area",
            _ => "Row"
        };

        private static string ReviewCurrentValue(StagingMasterPlan record, BusinessReviewQueue? review) => review?.ConflictType switch
        {
            "PIC" => record.HwPic,
            "Area" => record.Area,
            _ => record.ProjectName
        };

        private static string ReviewMessage(StagingMasterPlan record, BusinessReviewQueue? review)
        {
            if (!string.IsNullOrWhiteSpace(review?.ConflictMessage)) return review.ConflictMessage;
            if (!string.IsNullOrWhiteSpace(record.CoreValidationMessage)) return record.CoreValidationMessage;
            if (!string.IsNullOrWhiteSpace(record.ValidationMessage)) return record.ValidationMessage;
            return record.RowStatus switch
            {
                "SkipNoChange" => "No changes detected for this Basic + Cat.",
                "Skipped" => "Row was skipped.",
                "ReadyToInsert" => "Ready to insert.",
                "ReadyToUpdate" => "Ready to update.",
                "ReviewRequired" => "Row requires business review.",
                _ => $"Import row status: {record.RowStatus}."
            };
        }

        public async Task<ImportBatch> ResolveExistingSkuAsync(string batchId, string resolution, string resolvedBy)
            => await ResolveExistingBusinessKeyAsync(batchId, resolution, resolvedBy);

        public async Task<ImportBatch> ResolveExistingBusinessKeyAsync(string batchId, string resolution, string resolvedBy, int? rowNumber = null)
        {
            if (resolution is not ("Update" or "Skip" or "Cancel"))
                throw new InvalidOperationException("Resolution must be Update, Skip, or Cancel.");
            var batch = await _context.ImportBatches.FirstOrDefaultAsync(value => value.BatchId == batchId)
                ?? throw new InvalidOperationException("Batch not found.");
            if (batch.Status != "Staged") throw new InvalidOperationException("Only staged batches can be resolved.");
            if (resolution == "Cancel")
            {
                batch.Status = "Cancelled";
                LogInfo(batchId, $"Batch {batchId} cancelled by {resolvedBy}; no core records were changed.");
                await _context.SaveChangesAsync();
                return batch;
            }

            var pending = await _context.BusinessReviewQueues.Where(value => value.BatchId == batchId && value.Status == "Pending" && value.ConflictType == "ExistingBusinessKey").ToListAsync();
            var stagingIds = pending.Select(value => value.StagingId).ToHashSet();
            var conflicts = await _context.StagingMasterPlans.Where(value => value.BatchId == batchId && stagingIds.Contains(value.Id) && (!rowNumber.HasValue || value.RawRowNumber == rowNumber)).ToListAsync();
            if (conflicts.Count == 0) throw new InvalidOperationException("Batch has no unresolved existing-business-key conflicts.");
            foreach (var record in conflicts)
            {
                foreach (var item in pending.Where(value => value.StagingId == record.Id)) { item.Status = "Resolved"; item.ResolutionAction = resolution; item.ResolvedBy = resolvedBy; item.ResolvedAt = DateTime.UtcNow; }
                record.RowStatus = await CalculateResolvedRowStatusAsync(record, resolution == "Skip");
                AddAuditLog(batchId, "Staging_MasterPlan", $"{record.BasicKey}+{record.CatKey}", "RowStatus", "ReviewRequired", record.RowStatus, resolvedBy, "Explicit existing-business-key resolution");
            }
            var batchRows = await _context.StagingMasterPlans.Where(value => value.BatchId == batchId).ToListAsync();
            batch.SkippedRows = batchRows.Count(value => value.RowStatus == "Skipped");
            batch.ValidRows = batchRows.Count(value => value.RowStatus is "ReadyToInsert" or "ReadyToUpdate");
            batch.ReviewRequiredRows = batchRows.Count(value => value.RowStatus == "ReviewRequired");
            LogInfo(batchId, $"{conflicts.Count} existing-business-key row(s) resolved as {resolution} by {resolvedBy}.");
            await _context.SaveChangesAsync();
            return batch;
        }

        public async Task<bool> ResolveWarningRowAsync(string batchId, int rowNumber, string resolution, string resolvedBy)
        {
            if (resolution is not ("Accept" or "Skip")) throw new InvalidOperationException("Warning resolution must be Accept or Skip.");
            var batch = await _context.ImportBatches.FirstOrDefaultAsync(value => value.BatchId == batchId);
            if (batch is null || batch.Status != "Staged") return false;
            var record = await _context.StagingMasterPlans.FirstOrDefaultAsync(value => value.BatchId == batchId && value.RawRowNumber == rowNumber);
            if (record is null || record.RowStatus != "ReviewRequired") return false;
            if (await _context.BusinessReviewQueues.AnyAsync(value => value.StagingId == record.Id && value.Status == "Pending" && value.ConflictType == "ExistingBusinessKey"))
                throw new InvalidOperationException("Existing-business-key conflicts require the explicit Update, Skip, or Cancel choice.");
            var warningItems = await _context.BusinessReviewQueues
                .Where(value => value.StagingId == record.Id && value.Status == "Pending" && value.ConflictType != "ExistingBusinessKey")
                .ToListAsync();
            foreach (var warningItem in warningItems)
            {
                warningItem.Status = "Resolved";
                warningItem.ResolutionAction = resolution;
                warningItem.ResolvedBy = resolvedBy;
                warningItem.ResolvedAt = DateTime.UtcNow;
            }
            var nextStatus = await CalculateResolvedRowStatusAsync(record, resolution == "Skip");
            AddAuditLog(batchId, "Staging_MasterPlan", $"{record.BasicKey}+{record.CatKey}", "RowStatus", record.RowStatus, nextStatus, resolvedBy, "Explicit warning resolution");
            record.RowStatus = nextStatus;
            var batchRows = await _context.StagingMasterPlans.Where(value => value.BatchId == batchId).ToListAsync();
            batch.ValidRows = batchRows.Count(value => value.RowStatus is "ReadyToInsert" or "ReadyToUpdate");
            batch.ReviewRequiredRows = batchRows.Count(value => value.RowStatus == "ReviewRequired");
            batch.SkippedRows = batchRows.Count(value => value.RowStatus == "Skipped");
            batch.NoChangeRecords = batchRows.Count(value => value.RowStatus == "SkipNoChange");
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<string> CalculateResolvedRowStatusAsync(StagingMasterPlan record, bool skipRequested = false)
        {
            if (skipRequested) return "Skipped";

            var reviewItems = await _context.BusinessReviewQueues
                .Where(value => value.StagingId == record.Id)
                .ToListAsync();
            if (reviewItems.Any(value => value.Status == "Pending")) return "ReviewRequired";
            if (!record.TargetMasterPlanId.HasValue) return "ReadyToInsert";

            var keyResolution = reviewItems
                .LastOrDefault(value => value.ConflictType == "ExistingBusinessKey" && value.Status == "Resolved");
            if (keyResolution?.ResolutionAction == "Skip") return "Skipped";
            if (keyResolution?.ResolutionAction == "Update")
                return HasFieldDifferences(keyResolution.ConflictMessage) ? "ReadyToUpdate" : "SkipNoChange";

            return "SkipNoChange";
        }

        private static bool HasFieldDifferences(string serializedDifferences)
        {
            if (string.IsNullOrWhiteSpace(serializedDifferences)) return false;
            try
            {
                return (JsonSerializer.Deserialize<List<FieldDifference>>(serializedDifferences) ?? []).Count > 0;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public async Task<ImportBatch?> GetBatchPreviewAsync(string batchId)
        {
            return await _context.ImportBatches.FirstOrDefaultAsync(b => b.BatchId == batchId);
        }

        public async Task<List<ImportBatch>> GetHistoryAsync(string module = "NewModels")
        {
            return await _context.ImportBatches
                .Where(b => b.Module == module)
                .OrderByDescending(b => b.UploadedAt)
                .ToListAsync();
        }

        public async Task<List<StagingMasterPlan>> GetStagingRecordsAsync(string batchId)
        {
            return await _context.StagingMasterPlans
                .Where(s => s.BatchId == batchId)
                .OrderBy(s => s.RawRowNumber)
                .ToListAsync();
        }

        private void LogInfo(string batchId, string message)
        {
            _context.ImportLogs.Add(new ImportLog { BatchId = batchId, Level = "Info", Message = message });
            _logger.LogInformation(message);
        }

        private void LogError(string batchId, string message)
        {
            _context.ImportLogs.Add(new ImportLog { BatchId = batchId, Level = "Error", Message = message });
            _logger.LogError(message);
        }

        private void AddAuditLog(string batchId, string tableName, string recordKey, string fieldName, string oldVal, string newVal, string user, string reason)
        {
            _context.DataHubAuditLogs.Add(new DataHubAuditLog
            {
                BatchId = batchId,
                TableName = tableName,
                RecordKey = recordKey,
                FieldName = fieldName,
                OldValue = oldVal,
                NewValue = newVal,
                ChangedBy = user,
                ChangedAt = DateTime.UtcNow,
                Reason = reason
            });
        }

        private async Task UpsertMilestoneAsync(int masterPlanId, string projectName, string milestoneType, DateTime? targetDate, string batchId)
        {
            if (!targetDate.HasValue) return;

            var existing = await _context.ProjectMilestones.FirstOrDefaultAsync(m => m.MasterPlanId == masterPlanId && m.MilestoneType == milestoneType);
            if (existing == null)
            {
                _context.ProjectMilestones.Add(new ProjectMilestone
                {
                    MasterPlanId = masterPlanId,
                    ProjectName = projectName,
                    MilestoneType = milestoneType,
                    TargetDate = targetDate.Value,
                    SourceBatchId = batchId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                if (existing.TargetDate != targetDate.Value)
                {
                    existing.TargetDate = targetDate.Value;
                    existing.SourceBatchId = batchId;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        public async Task<List<ManualFileDto>> GetManualUploadFilesAsync()
        {
            EnsureDirectories();
            var manualDir = _pathConfig.NewModelsMasterPlanManualUploadPath;
            var result = new List<ManualFileDto>();

            if (Directory.Exists(manualDir))
            {
                var files = Directory.GetFiles(manualDir, "*.xls*");
                var batches = await _context.ImportBatches.AsNoTracking().Select(b => new { b.FileHash, b.OriginalFileName }).ToListAsync();

                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);
                    
                    // 10-second stability check
                    if ((DateTime.UtcNow - fileInfo.LastWriteTimeUtc).TotalSeconds < 10)
                        continue;

                    string fileHash;
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var sha256 = SHA256.Create())
                        {
                            var hashBytes = sha256.ComputeHash(stream);
                            fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                        }
                    }

                    string duplicateStatus = "None";
                    if (batches.Any(b => b.FileHash == fileHash))
                    {
                        duplicateStatus = "ExactDuplicate";
                    }
                    else if (batches.Any(b => b.OriginalFileName.Equals(fileInfo.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        duplicateStatus = "DuplicateContent";
                    }

                    result.Add(new ManualFileDto
                    {
                        FileName = fileInfo.Name,
                        SizeBytes = fileInfo.Length,
                        LastModifiedUtc = fileInfo.LastWriteTimeUtc,
                        Checksum = fileHash,
                        DuplicateStatus = duplicateStatus
                    });
                }
            }
            return result;
        }

        public async Task<ImportBatch> ProcessManualUploadAsync(string fileName, string uploadedBy, string module = "NewModels")
        {
            EnsureDirectories();
            var safeFileName = Path.GetFileName(fileName);
            if (!string.Equals(fileName, safeFileName, StringComparison.Ordinal))
                throw new InvalidDataException("Filename must not contain a path.");
            string filePath = Path.Combine(_pathConfig.NewModelsMasterPlanManualUploadPath, safeFileName);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Manual upload file not found: {fileName}");
            }
            
            ImportBatch batch;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                batch = await ProcessUploadAsync(fs, fileName, uploadedBy, module);
            }
            
            // Note: File is NO LONGER deleted from ManualUpload automatically here.
            
            return batch;
        }

        private static void ValidateUpload(Stream stream, string fileName, string module)
        {
            if (stream is null || !stream.CanRead || !stream.CanSeek || stream.Length == 0)
                throw new InvalidDataException("The uploaded file is empty or unreadable.");
            if (stream.Length > MaximumUploadBytes)
                throw new InvalidDataException($"The uploaded file exceeds the {MaximumUploadBytes / 1024 / 1024} MB limit.");
            if (!SupportedExtensions.Contains(Path.GetExtension(fileName)))
                throw new InvalidDataException("Only .xlsx, .xls, and .xlsm files are supported.");
            if (!string.Equals(module, "NewModels", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Only the NewModels module is supported by this import contract.");
        }

        private void TryDeleteUncommittedFile(string path, string batchId)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception cleanupError)
            {
                _logger.LogWarning(cleanupError, "Could not remove uncommitted Data Hub artifact for batch {BatchId}.", batchId);
            }
        }
    }
}
