namespace IqcQms.Application.Interfaces.DataHub
{
    public class DataHubPathConfig
    {
        public string BasePath { get; set; } = string.Empty;
        public string NewModelsMasterPlanBasePath { get; set; } = string.Empty;
        public string NewModelsMasterPlanManualUploadPath { get; set; } = string.Empty;
        public string NewModelsMasterPlanRawPath { get; set; } = string.Empty;
        public string NewModelsMasterPlanProcessedPath { get; set; } = string.Empty;
        public string NewModelsMasterPlanRejectedPath { get; set; } = string.Empty;
        public string NewModelsMasterPlanReportsPath { get; set; } = string.Empty;
        public string NewModelsMasterPlanTempPath { get; set; } = string.Empty;
    }
}
