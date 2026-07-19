using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
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

                // Update Batch counts
                batch.ValidRows = await _context.StagingMasterPlans.CountAsync(s => s.BatchId == batchId && (s.RowStatus == "ReadyToInsert" || s.RowStatus == "ReadyToUpdate"));
                batch.ErrorRows = await _context.StagingMasterPlans.CountAsync(s => s.BatchId == batchId && s.RowStatus == "ValidationError");
                batch.ReviewRequiredRows = await _context.StagingMasterPlans.CountAsync(s => s.BatchId == batchId && s.RowStatus == "ReviewRequired");
                
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
                    Rows = reportRecords.Select(r => new { r.RawRowNumber, r.ProjectName, r.Sku, r.RowStatus, r.ValidationMessage, r.CoreValidationMessage })
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
                LogError(batchId, $"Parsing failed: {ex.Message}");
                throw;
            }

            return batch;
        }

        private async Task RunValidationPipelineAsync(string batchId)
        {
            var records = await _context.StagingMasterPlans.Where(s => s.BatchId == batchId).ToListAsync();
            var mappings = await _context.MappingDictionaries.Where(m => m.IsActive).ToListAsync();
            
            // 1. Fetch Core DB context
            var existingProjects = await _context.MasterPlans.ToListAsync();
            var existingWorkspaces = await _context.ProjectWorkspaces.ToListAsync();
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();

            // 2. Intra-batch duplicates
            var duplicatesInBatch = records
                .Where(r => !string.IsNullOrWhiteSpace(r.Sku))
                .GroupBy(r => r.Sku, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .Select(r => r.Id)
                .ToHashSet();

            foreach (var record in records)
            {
                bool hasError = false;
                bool needsReview = false;
                List<string> errorMsgs = new List<string>();
                List<string> reviewMsgs = new List<string>();
                bool isBlocked = false;
                bool skipNoChange = false;

                // Step 6: Basic Validation
                if (string.IsNullOrWhiteSpace(record.ProjectName))
                {
                    errorMsgs.Add("Project Name is missing");
                    AddValidationError(batchId, record.Id, record.RawRowNumber, record.ProjectName, record.Sku, "ProjectName", "Required", "Project Name is missing", record.ProjectName);
                    hasError = true;
                }
                if (string.IsNullOrWhiteSpace(record.Sku))
                {
                    errorMsgs.Add("SKU is missing");
                    AddValidationError(batchId, record.Id, record.RawRowNumber, record.ProjectName, record.Sku, "SKU", "Required", "SKU is missing", record.Sku);
                    hasError = true;
                }
                if (!record.PvrTargetDate.HasValue)
                {
                    errorMsgs.Add("PVR Target is missing or invalid");
                    AddValidationError(batchId, record.Id, record.RawRowNumber, record.ProjectName, record.Sku, "PVRTarget", "RequiredOrInvalid", "PVR Target is missing or invalid", string.Empty);
                    hasError = true;
                }
                
                if (string.IsNullOrWhiteSpace(record.Area)) reviewMsgs.Add("Area is missing");
                if (string.IsNullOrWhiteSpace(record.Grade)) reviewMsgs.Add("Grade is missing");
                if (string.IsNullOrWhiteSpace(record.HwPic)) reviewMsgs.Add("HWPIC is missing");
                if (!record.PraTargetDate.HasValue) reviewMsgs.Add("PRA Target is missing");
                if (!record.SraTargetDate.HasValue) reviewMsgs.Add("SRA Target is missing");
                
                if (record.PvrTargetDate.HasValue && record.PraTargetDate.HasValue && record.PvrTargetDate > record.PraTargetDate)
                {
                    reviewMsgs.Add("PVRTarget is after PRATarget");
                    needsReview = true;
                }
                if (record.PraTargetDate.HasValue && record.SraTargetDate.HasValue && record.PraTargetDate > record.SraTargetDate)
                {
                    reviewMsgs.Add("PRATarget is after SRATarget");
                    needsReview = true;
                }
                
                // Step 7: Mapping & Cleaning
                record.HwPic = ApplyMapping(mappings, "PIC", record.HwPic, ref needsReview, batchId, record.Id, "PIC Mapping Issue");
                record.Area = ApplyMapping(mappings, "Area", record.Area, ref needsReview, batchId, record.Id, "Area Mapping Issue");
                record.Grade = ApplyMapping(mappings, "Grade", record.Grade, ref needsReview, batchId, record.Id, "Grade Mapping Issue");
                record.RawStatus = ApplyMapping(mappings, "Status", record.RawStatus, ref needsReview, batchId, record.Id, "Status Mapping Issue");

                // Step 8: Core Business Validation
                
                // A. Check Intra-batch duplicate
                if (duplicatesInBatch.Contains(record.Id))
                {
                    errorMsgs.Add("Duplicate SKU in current batch");
                    AddValidationError(batchId, record.Id, record.RawRowNumber, record.ProjectName, record.Sku, "SKU", "DuplicateBusinessKey", "Duplicate SKU in current batch", record.Sku);
                    hasError = true;
                }

                // B. User validation
                if (!string.IsNullOrWhiteSpace(record.HwPic))
                {
                    var userExists = users.Any(u => string.Equals(u.FullName, record.HwPic, StringComparison.OrdinalIgnoreCase) 
                                                 || string.Equals(u.Username, record.HwPic, StringComparison.OrdinalIgnoreCase));
                    if (!userExists)
                    {
                        reviewMsgs.Add("Unknown PIC");
                        needsReview = true;
                    }
                }

                // C. Core Database Existing Project
                var existingMp = existingProjects.FirstOrDefault(p => string.Equals(p.Sku, record.Sku, StringComparison.OrdinalIgnoreCase));
                bool isExisting = existingMp is not null;
                
                if (existingMp is not null)
                {
                    // Check if data is exactly identical
                    bool identical = existingMp.QtyLpr == record.QtyLpr && 
                                     existingMp.QtyLsr == record.QtyLsr && 
                                     existingMp.PvrTargetDate == record.PvrTargetDate &&
                                     existingMp.PraTargetDate == record.PraTargetDate &&
                                     existingMp.SraTargetDate == record.SraTargetDate &&
                                     existingMp.HwPic == record.HwPic &&
                                     existingMp.Area == record.Area &&
                                     existingMp.Grade == record.Grade;
                    
                    if (identical)
                    {
                        skipNoChange = true;
                    }
                    else
                    {
                        reviewMsgs.Add("SKU already exists; imports do not overwrite existing Master Plan records");
                        needsReview = true;
                    }
                    if (existingMp.PvrTargetDate.HasValue && record.PvrTargetDate.HasValue)
                    {
                        if (record.PvrTargetDate > existingMp.PvrTargetDate)
                        {
                            reviewMsgs.Add("Newer Plan Version");
                        }
                        else if (record.PvrTargetDate < existingMp.PvrTargetDate)
                        {
                            reviewMsgs.Add("Older Plan Version");
                            needsReview = true; // Older plan might be a mistake
                        }
                    }
                }

                // D. Project Workspace Status
                var workspace = existingWorkspaces.FirstOrDefault(w => string.Equals(w.ProjectName, record.ProjectName, StringComparison.OrdinalIgnoreCase)
                                                                    && string.Equals(w.Sku, record.Sku, StringComparison.OrdinalIgnoreCase));
                if (workspace != null)
                {
                    if (workspace.Status == "Closed" || workspace.Status == "Completed")
                    {
                        isBlocked = true;
                        reviewMsgs.Add("Closed Project");
                    }
                }

                // Assign Final Status
                if (hasError)
                {
                    record.RowStatus = "ValidationError";
                    record.ValidationMessage = string.Join("; ", errorMsgs);
                }
                else if (isBlocked)
                {
                    record.RowStatus = "Blocked";
                    record.CoreValidationMessage = string.Join("; ", reviewMsgs);
                }
                else if (needsReview || reviewMsgs.Any())
                {
                    record.RowStatus = "ReviewRequired";
                    record.CoreValidationMessage = string.Join("; ", reviewMsgs);
                }
                else if (skipNoChange)
                {
                    record.RowStatus = "SkipNoChange";
                }
                else
                {
                    record.RowStatus = isExisting ? "ReadyToUpdate" : "ReadyToInsert";
                }
            }
            
            await _context.SaveChangesAsync();
        }

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
                ConflictMessage = $"{conflictMsg}: '{rawValue}' not found in dictionary."
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

            var readyRecords = allRecords.Where(s => s.RowStatus == "ReadyToInsert").ToList();
            if (readyRecords.Count == 0)
                throw new InvalidOperationException("Batch contains no records ready to insert.");
            
            int created = 0;
            int updated = 0;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var record in readyRecords)
                {
                    var existing = await _context.MasterPlans.FirstOrDefaultAsync(m => m.Sku.ToUpper() == record.Sku.ToUpper());
                    int masterPlanId;

                    if (existing == null)
                    {
                        // Create new
                        var newMp = new MasterPlan
                        {
                            ProjectName = record.ProjectName,
                            Basic = record.Basic,
                            Area = record.Area,
                            Grade = record.Grade,
                            Sku = record.Sku,
                            QtyLpr = record.QtyLpr ?? 0,
                            QtyLsr = record.QtyLsr ?? 0,
                            PvrTargetDate = record.PvrTargetDate,
                            PraTargetDate = record.PraTargetDate,
                            SraTargetDate = record.SraTargetDate,
                            HwPic = record.HwPic,
                            ImportedStatus = record.RawStatus,
                            Remark = record.Remark,
                            LastImportBatchId = batchId,
                            ActionStatus = "Ready",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.MasterPlans.Add(newMp);
                        await _context.SaveChangesAsync(); // Save to get ID
                        masterPlanId = newMp.Id;
                        created++;
                        
                        AddAuditLog(batchId, "MasterPlans", record.Sku, "All", "", "Created", committedBy, "Master Plan Manual Import");
                    }
                    else
                    {
                        throw new InvalidOperationException($"SKU '{record.Sku}' already exists. Existing records are never overwritten by import.");
                    }

                    // Upsert Milestones
                    await UpsertMilestoneAsync(masterPlanId, record.ProjectName, "PVR", record.PvrTargetDate, batchId);
                    await UpsertMilestoneAsync(masterPlanId, record.ProjectName, "PRA", record.PraTargetDate, batchId);
                    await UpsertMilestoneAsync(masterPlanId, record.ProjectName, "SRA", record.SraTargetDate, batchId);
                    
                    record.RowStatus = "Committed";
                }

                batch.CreatedRecords = created;
                batch.UpdatedRecords = updated;
                
                // Determine batch status
                batch.Status = "Committed";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                LogInfo(batchId, $"Batch {batchId} committed by {committedBy}. Status: {batch.Status}. Created: {created}, Updated: {updated}.");
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
                // Re-evaluate staging row status
                bool hasMorePending = await _context.BusinessReviewQueues.AnyAsync(q => q.StagingId == staging.Id && q.Id != reviewItemId && q.Status == "Pending");
                if (!hasMorePending && staging.RowStatus == "ReviewRequired")
                {
                    staging.RowStatus = action == "Ignore" ? "Skipped" : "ReadyToInsert";
                }
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
            var rows = new List<ImportReviewRowDto>();
            foreach (var record in staging)
            {
                var rowErrors = errors.Where(value => value.StagingId == record.Id).ToList();
                rows.AddRange(rowErrors.Select(error => new ImportReviewRowDto
                {
                    RowNumber = record.RawRowNumber, Sku = record.Sku, Field = error.FieldName,
                    CurrentValue = error.RawValue, Severity = "Error", Message = error.ErrorMessage, Status = record.RowStatus
                }));
                if (rowErrors.Count == 0)
                {
                    var existing = record.CoreValidationMessage.Contains("SKU already exists", StringComparison.OrdinalIgnoreCase);
                    rows.Add(new ImportReviewRowDto
                    {
                        RowNumber = record.RawRowNumber, Sku = record.Sku, Field = existing ? "SKU" : "Row",
                        CurrentValue = existing ? record.Sku : record.ProjectName,
                        Severity = record.RowStatus == "ReviewRequired" ? "Warning" : record.RowStatus == "Skipped" ? "Skipped" : "Ready",
                        Message = record.CoreValidationMessage.Length > 0 ? record.CoreValidationMessage : record.ValidationMessage,
                        Status = record.RowStatus
                    });
                }
            }

            return new ImportReviewSummaryDto
            {
                BatchId = batch.BatchId, FileName = batch.OriginalFileName, ValidRows = batch.ValidRows,
                WarningRows = staging.Count(value => value.RowStatus == "ReviewRequired"),
                ErrorRows = staging.Count(value => value.RowStatus is "ValidationError" or "Blocked"),
                ExistingSkuConflicts = staging.Count(value => value.CoreValidationMessage.Contains("SKU already exists", StringComparison.OrdinalIgnoreCase) && value.RowStatus != "Skipped"),
                SkippedRows = staging.Count(value => value.RowStatus == "Skipped"), Rows = rows
            };
        }

        public async Task<ImportBatch> ResolveExistingSkuAsync(string batchId, string resolution, string resolvedBy)
        {
            if (resolution is not ("Skip" or "Cancel"))
                throw new InvalidOperationException("Resolution must be Skip or Cancel.");
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

            var conflicts = await _context.StagingMasterPlans
                .Where(value => value.BatchId == batchId && value.RowStatus == "ReviewRequired" && value.CoreValidationMessage.Contains("SKU already exists"))
                .ToListAsync();
            if (conflicts.Count == 0) throw new InvalidOperationException("Batch has no unresolved existing-SKU conflicts.");
            foreach (var record in conflicts)
            {
                record.RowStatus = "Skipped";
                AddAuditLog(batchId, "Staging_MasterPlan", record.Sku, "RowStatus", "ReviewRequired", "Skipped", resolvedBy, "Explicit existing-SKU resolution");
            }
            batch.SkippedRows = await _context.StagingMasterPlans.CountAsync(value => value.BatchId == batchId && value.RowStatus == "Skipped") + conflicts.Count;
            batch.ReviewRequiredRows = await _context.StagingMasterPlans.CountAsync(value => value.BatchId == batchId && value.RowStatus == "ReviewRequired" && !conflicts.Select(c => c.Id).Contains(value.Id));
            LogInfo(batchId, $"{conflicts.Count} existing-SKU row(s) explicitly skipped by {resolvedBy}.");
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
            if (record.CoreValidationMessage.Contains("SKU already exists", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Existing-SKU conflicts require the explicit batch Skip or Cancel choice.");
            var nextStatus = resolution == "Accept" ? "ReadyToInsert" : "Skipped";
            AddAuditLog(batchId, "Staging_MasterPlan", record.Sku, "RowStatus", record.RowStatus, nextStatus, resolvedBy, "Explicit warning resolution");
            record.RowStatus = nextStatus;
            batch.ReviewRequiredRows = Math.Max(0, batch.ReviewRequiredRows - 1);
            if (nextStatus == "Skipped") batch.SkippedRows++;
            else batch.ValidRows++;
            await _context.SaveChangesAsync();
            return true;
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
    }
}
