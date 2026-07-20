using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IqcQms.Domain.Entities.DataHub;

namespace IqcQms.Application.Interfaces.DataHub
{
    public sealed record MasterPlanParseResult(
        List<StagingMasterPlan> Records,
        List<string> MissingHardColumns,
        List<string> DuplicateColumns);
    public sealed record HeaderColumnDto(
        int ColumnIndex, string Header, string? SuggestedCanonical, bool Ambiguous,
        string ParentHeader = "", string ChildHeader = "", string EffectiveHeaderPath = "",
        List<string>? SampleValues = null, string DetectedDataType = "Text", double Confidence = 0,
        string Reason = "", bool LearnedSuggestion = false);
    public sealed record HeaderInspectionDto(int HeaderRow, List<HeaderColumnDto> Columns, List<string> CanonicalFields, List<string> RequiredFields, int HeaderDepth = 1, int DataStartRow = 0, string WorkbookFingerprint = "");
    public sealed record HeaderMappingDto(int ColumnIndex, string CanonicalField, string NormalizedHeaderPath = "", bool ConfirmLearning = false, string WorkbookFingerprint = "", string DetectedDataType = "");

    public interface IMasterPlanContractParser
    {
        Task<MasterPlanParseResult> ParseExcelAsync(Stream excelStream, string batchId, IReadOnlyCollection<HeaderMappingDto>? mappings = null);
        Task<HeaderInspectionDto> InspectHeadersAsync(Stream excelStream);
    }
}
