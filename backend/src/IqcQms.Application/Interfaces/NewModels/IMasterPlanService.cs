using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IqcQms.Domain.Entities.NewModels;

namespace IqcQms.Application.Interfaces.NewModels
{
    public class MasterPlanDisplayDto
    {
        public int Id { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Basic { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Cat { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int QtyLpr { get; set; }
        public int QtyLprLqv { get => QtyLpr; set => QtyLpr = value; }
        public int QtyLsr { get; set; }
        public DateTime? PvrTargetDate { get; set; }
        public DateTime? PraTargetDate { get; set; }
        public DateTime? SraTargetDate { get; set; }
        public DateTime? MainLprLqvDate { get; set; }
        public DateTime? MainLsrDate { get; set; }
        public string HwPic { get; set; } = string.Empty;
        public string DisplayStatus { get; set; } = string.Empty;
        public string DisplayAction { get; set; } = string.Empty;
        public int? LinkedProjectId { get; set; }
    }

    public interface IMasterPlanService
    {
        Task<MasterPlanUpload> UploadMasterPlanAsync(Stream fileStream, string fileName, string uploadedBy);
        Task<IEnumerable<MasterPlanDisplayDto>> GetLatestMasterPlanRecordsAsync();
        Task<ProjectWorkspace> ActivateProjectAsync(int recordId, string ownerId);
    }
}
