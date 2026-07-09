using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IqcQms.Domain.Entities.NewModels;

namespace IqcQms.Application.Interfaces.NewModels
{
    public interface IMasterPlanService
    {
        Task<MasterPlanUpload> UploadMasterPlanAsync(Stream fileStream, string fileName, string uploadedBy);
        Task<IEnumerable<MasterPlanRecord>> GetLatestMasterPlanRecordsAsync();
        Task<ProjectWorkspace> ActivateProjectAsync(int recordId, string ownerId);
    }
}
