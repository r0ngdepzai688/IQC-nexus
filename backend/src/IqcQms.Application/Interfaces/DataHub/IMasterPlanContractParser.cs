using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IqcQms.Domain.Entities.DataHub;

namespace IqcQms.Application.Interfaces.DataHub
{
    public interface IMasterPlanContractParser
    {
        Task<(List<StagingMasterPlan> Records, List<string> MissingHardColumns)> ParseExcelAsync(Stream excelStream, string batchId);
    }
}
