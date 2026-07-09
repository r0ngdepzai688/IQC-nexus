using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExcelDataReader;
using IqcQms.Application.Interfaces.NewModels;
using IqcQms.Domain.Entities.NewModels;
using IqcQms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IqcQms.Infrastructure.Services.NewModels
{
    public class MasterPlanService : IMasterPlanService
    {
        private readonly AppDbContext _context;

        public MasterPlanService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MasterPlanRecord>> GetLatestMasterPlanRecordsAsync()
        {
            var latestUpload = await _context.MasterPlanUploads
                .OrderByDescending(u => u.UploadDate)
                .FirstOrDefaultAsync();

            if (latestUpload == null) return new List<MasterPlanRecord>();

            return await _context.MasterPlanRecords
                .Where(r => r.UploadId == latestUpload.Id)
                .ToListAsync();
        }

        public async Task<ProjectWorkspace> ActivateProjectAsync(int recordId, string ownerId)
        {
            var record = await _context.MasterPlanRecords.FindAsync(recordId);
            if (record == null) throw new Exception("Record not found.");

            if (record.IsActivated) throw new Exception("Project is already activated.");

            var workspace = new ProjectWorkspace
            {
                SourceRecordId = record.Id,
                ProjectName = record.ProjectName,
                Sku = record.Sku,
                OwnerId = ownerId,
                ActivatedDate = DateTime.UtcNow,
                Status = "Preparation",
                CompletionPercentage = 0
            };

            record.IsActivated = true;
            record.ActionStatus = "Active";

            _context.ProjectWorkspaces.Add(workspace);
            await _context.SaveChangesAsync();

            return workspace;
        }

        public async Task<MasterPlanUpload> UploadMasterPlanAsync(Stream fileStream, string fileName, string uploadedBy)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var upload = new MasterPlanUpload
            {
                FileName = fileName,
                UploadDate = DateTime.UtcNow,
                UploadedBy = uploadedBy,
            };

            _context.MasterPlanUploads.Add(upload);
            await _context.SaveChangesAsync();

            var records = new List<MasterPlanRecord>();
            int changesCount = 0;

            var prevRecords = await GetLatestMasterPlanRecordsAsync();
            var prevMap = prevRecords.ToDictionary(r => r.ProjectName);

            using (var reader = ExcelReaderFactory.CreateReader(fileStream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });

                if (result.Tables.Count > 0)
                {
                    var table = result.Tables[0];
                    foreach (System.Data.DataRow row in table.Rows)
                    {
                        var projectName = row["Project name"]?.ToString();
                        if (string.IsNullOrWhiteSpace(projectName)) continue;

                        var pvrStr = row["PVR Target"]?.ToString();
                        DateTime? pvrDate = null;
                        if (DateTime.TryParse(pvrStr, out var pd)) pvrDate = pd;

                        var praStr = row["PRA Target"]?.ToString();
                        DateTime? praDate = null;
                        if (DateTime.TryParse(praStr, out var prad)) praDate = prad;

                        var sraStr = row["SRA Target"]?.ToString();
                        DateTime? sraDate = null;
                        if (DateTime.TryParse(sraStr, out var srad)) sraDate = srad;

                        int.TryParse(row["Q'ty (LPR)"]?.ToString(), out int qtyLpr);
                        int.TryParse(row["Q'ty (LSR)"]?.ToString(), out int qtyLsr);

                        var record = new MasterPlanRecord
                        {
                            UploadId = upload.Id,
                            ProjectName = projectName,
                            Basic = row["Basic"]?.ToString() ?? "",
                            AreaRegion = row["Area (Region)"]?.ToString() ?? "",
                            Grade = row["Grade"]?.ToString() ?? "",
                            Sku = row["SKU"]?.ToString() ?? "",
                            Category = row["Cat"]?.ToString() ?? "",
                            QtyLpr = qtyLpr,
                            QtyLsr = qtyLsr,
                            PvrTargetDate = pvrDate,
                            PraTargetDate = praDate,
                            SraTargetDate = sraDate,
                            PicIqc = row["HW 검증 (IQC)"]?.ToString() ?? "",
                        };

                        if (record.PvrTargetDate.HasValue)
                        {
                            var weeksToPvr = (record.PvrTargetDate.Value - DateTime.UtcNow).TotalDays / 7;
                            if (weeksToPvr < 4) record.ActionStatus = "Urgent";
                            else if (weeksToPvr <= 8) record.ActionStatus = "Ready";
                            else record.ActionStatus = "Future";
                        }

                        if (prevMap.TryGetValue(projectName, out var prevRecord))
                        {
                            if (prevRecord.PvrTargetDate != record.PvrTargetDate)
                            {
                                changesCount++;
                            }
                            if (prevRecord.IsActivated)
                            {
                                record.IsActivated = true;
                                record.ActionStatus = "Active";
                            }
                        }

                        records.Add(record);
                    }
                }
            }

            _context.MasterPlanRecords.AddRange(records);
            upload.TotalRecordsParsed = records.Count;
            upload.DeltaSummary = $"Phát hiện {records.Count} models. Có {changesCount} models thay đổi lịch PVR so với bản trước.";
            
            await _context.SaveChangesAsync();

            return upload;
        }
    }
}
