namespace IqcQms.Application.Interfaces.DataHub
{
    public class DataHubPathConfig
    {
        public string BasePath { get; set; } = @"D:\Viber_Code\DataHub";
        public string NewModelsMasterPlanBasePath { get; set; } = @"D:\Viber_Code\DataHub\NewModels\MasterPlan";
        public string NewModelsMasterPlanManualUploadPath { get; set; } = @"D:\Viber_Code\DataHub\NewModels\MasterPlan\ManualUpload";
        public string NewModelsMasterPlanRawPath { get; set; } = @"D:\Viber_Code\DataHub\NewModels\MasterPlan\Raw";
        public string NewModelsMasterPlanProcessedPath { get; set; } = @"D:\Viber_Code\DataHub\NewModels\MasterPlan\Processed";
        public string NewModelsMasterPlanRejectedPath { get; set; } = @"D:\Viber_Code\DataHub\NewModels\MasterPlan\Rejected";
        public string NewModelsMasterPlanReportsPath { get; set; } = @"D:\Viber_Code\DataHub\NewModels\MasterPlan\Reports";
        public string NewModelsMasterPlanTempPath { get; set; } = @"D:\Viber_Code\DataHub\NewModels\MasterPlan\Temp";
    }
}
