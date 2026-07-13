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

        public async Task<IEnumerable<MasterPlanDisplayDto>> GetLatestMasterPlanRecordsAsync()
        {
            var masterPlans = await _context.MasterPlans.ToListAsync();
            var dtos = new List<MasterPlanDisplayDto>();

            var now = DateTime.UtcNow;

            foreach (var mp in masterPlans)
            {
                var dto = new MasterPlanDisplayDto
                {
                    Id = mp.Id,
                    ProjectName = mp.ProjectName,
                    Basic = mp.Basic,
                    Area = mp.Area,
                    Grade = mp.Grade,
                    Sku = mp.Sku,
                    QtyLpr = mp.QtyLpr,
                    QtyLsr = mp.QtyLsr,
                    PvrTargetDate = mp.PvrTargetDate,
                    PraTargetDate = mp.PraTargetDate,
                    SraTargetDate = mp.SraTargetDate,
                    HwPic = mp.HwPic,
                    LinkedProjectId = mp.LinkedProjectId
                };

                // Calculate Status
                if (mp.LinkedProjectId.HasValue)
                {
                    dto.DisplayStatus = "Created";
                    dto.DisplayAction = "View Project";
                }
                else if (mp.ImportedStatus == "Review Required") // Assuming we save unresolved review flags here, or we just rely on dates
                {
                    dto.DisplayStatus = "Review Required";
                    dto.DisplayAction = "Fix Data";
                }
                else if (mp.PvrTargetDate.HasValue)
                {
                    var daysToPvr = (mp.PvrTargetDate.Value - now).TotalDays;
                    if (daysToPvr < 0)
                    {
                        dto.DisplayStatus = "Urgent";
                        dto.DisplayAction = "Create Project";
                    }
                    else if (daysToPvr <= 7)
                    {
                        dto.DisplayStatus = "Ready";
                        dto.DisplayAction = "Create Project";
                    }
                    else
                    {
                        dto.DisplayStatus = "Future";
                        dto.DisplayAction = "Create Project";
                    }
                }
                else
                {
                    dto.DisplayStatus = "Review Required";
                    dto.DisplayAction = "Fix Data";
                }

                dtos.Add(dto);
            }

            return dtos;
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

        public Task<MasterPlanUpload> UploadMasterPlanAsync(Stream fileStream, string fileName, string uploadedBy)
        {
            throw new NotImplementedException("Legacy upload is deprecated. Use Data Hub ingestion pipeline.");
        }
    }
}
