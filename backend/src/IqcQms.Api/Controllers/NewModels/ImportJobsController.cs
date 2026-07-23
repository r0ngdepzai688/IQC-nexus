using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IqcQms.Application.Auth;
using IqcQms.Application.DataPlatform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IqcQms.Api.Controllers.NewModels;

[ApiController]
[Route("api/import-jobs")]
[Authorize]
public class ImportJobsController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".csv", ".xlsx", ".xls" };
    private static readonly HashSet<string> RejectedNascaExtensions = new(StringComparer.OrdinalIgnoreCase) { ".nasca", ".xlsm", ".xltm", ".xlam" };

    private readonly IImportPipelineOrchestrator _orchestrator;
    private readonly IDataSourceProviderRegistry _providerRegistry;
    private readonly IImportCommitEngine _commitEngine;
    private readonly IImportAuditService _auditService;
    private readonly IAuthorizationService _authorization;
    private readonly ILogger<ImportJobsController> _logger;

    public ImportJobsController(
        IImportPipelineOrchestrator orchestrator,
        IDataSourceProviderRegistry providerRegistry,
        IImportCommitEngine commitEngine,
        IImportAuditService auditService,
        IAuthorizationService authorization,
        ILogger<ImportJobsController> logger)
    {
        _orchestrator = orchestrator;
        _providerRegistry = providerRegistry;
        _commitEngine = commitEngine;
        _auditService = auditService;
        _authorization = authorization;
        _logger = logger;
    }

    [HttpPost("upload")]
    [Authorize(Policy = PlatformPermissions.ImportCreate)]
    [ProducesResponseType(typeof(ImportJobStateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadAndCreateJob(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = ImportErrorCodes.WorkbookEmpty, Detail = "No file uploaded.", Status = 400 });

        if (file.Length > ImportPlatformLimits.Default.MaximumPayloadBytes)
            return BadRequest(new ProblemDetails { Title = ImportErrorCodes.FileTooLarge, Detail = "File size exceeds payload limit (50 MB).", Status = 400 });

        var sanitizedFileName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(sanitizedFileName).ToLowerInvariant();

        if (RejectedNascaExtensions.Contains(ext) || sanitizedFileName.Contains("nasca", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ProblemDetails { Title = ImportErrorCodes.FileTypeUnsupported, Detail = "NASCA file formats are not permitted.", Status = 400 });
        }

        if (!AllowedExtensions.Contains(ext))
        {
            return BadRequest(new ProblemDetails { Title = ImportErrorCodes.FileTypeUnsupported, Detail = $"File format '{ext}' is unsupported. Only CSV and XLSX files are permitted.", Status = 400 });
        }

        string actor = User.Identity?.Name ?? "anonymous";

        try
        {
            using var stream = file.OpenReadStream();
            var kind = ext is ".xlsx" or ".xls" ? DataSourceProviderKind.Excel : DataSourceProviderKind.Csv;

            var descriptor = new DataSourceDescriptor(kind, sanitizedFileName, sanitizedFileName, file.ContentType, file.Length);
            var provider = _providerRegistry.GetRequired(descriptor);

            var context = new DataSourceProviderContext(descriptor, stream, ImportPlatformLimits.Default);
            var workbook = await provider.NormalizeAsync(context, HttpContext.RequestAborted);

            var record = await _orchestrator.CreateJobAsync(workbook, actor, HttpContext.RequestAborted);
            var dto = MapToStateDto(record);

            return CreatedAtAction(nameof(GetJobDetail), new { jobId = record.Job.JobId }, dto);
        }
        catch (ImportPlatformException ex)
        {
            return BadRequest(new ProblemDetails { Title = ex.Code, Detail = ex.Message, Status = 400 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload and normalize import file.");
            return Problem("The import file could not be processed. Contact system administrator.");
        }
    }

    [HttpGet]
    [Authorize(Policy = PlatformPermissions.ImportView)]
    [ProducesResponseType(typeof(List<ImportJobStateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListJobs()
    {
        string actor = User.Identity?.Name ?? "anonymous";
        bool isAdmin = await IsAdminAsync();

        var records = await _orchestrator.ListJobsAsync(actor, isAdmin, HttpContext.RequestAborted);
        var dtos = records.Select(MapToStateDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("{jobId}")]
    [Authorize(Policy = PlatformPermissions.ImportView)]
    [ProducesResponseType(typeof(ImportJobStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetJobDetail(string jobId)
    {
        string actor = User.Identity?.Name ?? "anonymous";
        bool isAdmin = await IsAdminAsync();

        try
        {
            var record = await _orchestrator.GetJobAsync(jobId, actor, isAdmin, HttpContext.RequestAborted);
            return Ok(MapToStateDto(record));
        }
        catch (ImportPlatformException ex) when (ex.Code == ImportErrorCodes.SessionForbidden)
        {
            return Forbid();
        }
        catch (ImportPlatformException ex)
        {
            return NotFound(new ProblemDetails { Title = ex.Code, Detail = ex.Message, Status = 404 });
        }
    }

    [HttpPost("{jobId}/mapping")]
    [Authorize(Policy = PlatformPermissions.ImportCreate)]
    [ProducesResponseType(typeof(MappingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteMapping(string jobId, [FromBody] MappingProfileRequestDto request)
    {
        if (request is null || request.Rules is null || request.Rules.Count == 0)
            return BadRequest(new ProblemDetails { Title = ImportErrorCodes.MappingInvalid, Detail = "At least one mapping rule is required.", Status = 400 });

        string actor = User.Identity?.Name ?? "anonymous";
        bool isAdmin = await IsAdminAsync();

        var profile = new MappingProfile(
            request.ProfileId ?? "default-profile",
            request.Version ?? "1.0",
            request.Name ?? "Default Mapping Profile",
            request.WorksheetName,
            request.HeaderRowNumber > 0 ? request.HeaderRowNumber : 1,
            request.CaseInsensitiveHeaderMatching,
            request.Rules.Select(r => new MappingRule(
                r.SourceColumnIdentifier,
                r.TargetField,
                r.IsRequired,
                r.TransformationType,
                r.DefaultValue,
                r.CultureName,
                r.DateTimeFormat,
                r.LookupDictionary)).ToList(),
            request.IgnoredSourceColumns);

        try
        {
            var result = await _orchestrator.ExecuteMappingAsync(jobId, actor, isAdmin, profile, HttpContext.RequestAborted);
            return Ok(MapToMappingResultDto(result));
        }
        catch (ImportPlatformException ex) when (ex.Code == ImportErrorCodes.SessionForbidden)
        {
            return Forbid();
        }
        catch (ImportPlatformException ex)
        {
            return BadRequest(new ProblemDetails { Title = ex.Code, Detail = ex.Message, Status = 400 });
        }
    }

    [HttpPost("{jobId}/validation")]
    [Authorize(Policy = PlatformPermissions.ImportReview)]
    [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteValidation(string jobId, [FromBody] ValidationProfileRequestDto request)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = ImportErrorCodes.ValidationFailed, Detail = "Validation profile is required.", Status = 400 });

        string actor = User.Identity?.Name ?? "anonymous";
        bool isAdmin = await IsAdminAsync();

        var profile = new ValidationProfile(
            request.ProfileId ?? "default-val-profile",
            request.Version ?? "1.0",
            request.Name ?? "Default Validation Profile",
            (request.Rules ?? new List<ValidationRuleConfigRequestDto>()).Select(r => new ValidationRuleConfig(
                r.RuleId ?? Guid.NewGuid().ToString("N"),
                r.Name ?? "Rule",
                r.Kind,
                r.Severity,
                r.TargetField,
                r.SecondaryTargetField,
                r.MinLength,
                r.MaxLength,
                r.MinNumeric,
                r.MaxNumeric,
                r.MinDate,
                r.MaxDate,
                r.AllowedValues,
                r.RegexPattern,
                r.RegexTimeoutMs > 0 ? r.RegexTimeoutMs : 100,
                r.ComparisonOperator)).ToList(),
            request.MaximumDiagnostics > 0 ? request.MaximumDiagnostics : 1000);

        try
        {
            var result = await _orchestrator.ExecuteValidationAsync(jobId, actor, isAdmin, profile, HttpContext.RequestAborted);
            return Ok(MapToValidationResultDto(result));
        }
        catch (ImportPlatformException ex) when (ex.Code == ImportErrorCodes.SessionForbidden)
        {
            return Forbid();
        }
        catch (ImportPlatformException ex)
        {
            return BadRequest(new ProblemDetails { Title = ex.Code, Detail = ex.Message, Status = 400 });
        }
    }

    [HttpPost("{jobId}/preview")]
    [Authorize(Policy = PlatformPermissions.ImportReview)]
    [ProducesResponseType(typeof(ImportPreviewDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GeneratePreview(string jobId, [FromQuery] int sampleSize = 50)
    {
        string actor = User.Identity?.Name ?? "anonymous";
        bool isAdmin = await IsAdminAsync();

        try
        {
            var preview = await _orchestrator.GeneratePreviewAsync(jobId, actor, isAdmin, sampleSize, HttpContext.RequestAborted);
            return Ok(preview);
        }
        catch (ImportPlatformException ex) when (ex.Code == ImportErrorCodes.SessionForbidden)
        {
            return Forbid();
        }
        catch (ImportPlatformException ex)
        {
            return BadRequest(new ProblemDetails { Title = ex.Code, Detail = ex.Message, Status = 400 });
        }
    }

    [HttpGet("{jobId}/preview")]
    [Authorize(Policy = PlatformPermissions.ImportReview)]
    [ProducesResponseType(typeof(ImportPreviewDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreview(string jobId)
    {
        string actor = User.Identity?.Name ?? "anonymous";
        bool isAdmin = await IsAdminAsync();

        try
        {
            var record = await _orchestrator.GetJobAsync(jobId, actor, isAdmin, HttpContext.RequestAborted);
            if (record.PreviewDetail == null)
                return NotFound(new ProblemDetails { Title = ImportErrorCodes.PreviewRequired, Detail = "Preview has not been generated for this job.", Status = 404 });

            return Ok(record.PreviewDetail);
        }
        catch (ImportPlatformException ex) when (ex.Code == ImportErrorCodes.SessionForbidden)
        {
            return Forbid();
        }
        catch (ImportPlatformException ex)
        {
            return NotFound(new ProblemDetails { Title = ex.Code, Detail = ex.Message, Status = 404 });
        }
    }

    [HttpGet("{jobId}/diagnostics")]
    [Authorize(Policy = PlatformPermissions.ImportReview)]
    [ProducesResponseType(typeof(PaginatedDiagnosticsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDiagnostics(
        string jobId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] ValidationSeverity? minSeverity = null)
    {
        string actor = User.Identity?.Name ?? "anonymous";
        bool isAdmin = await IsAdminAsync();

        try
        {
            var result = await _orchestrator.GetDiagnosticsAsync(jobId, actor, isAdmin, page, pageSize, minSeverity, HttpContext.RequestAborted);
            return Ok(result);
        }
        catch (ImportPlatformException ex) when (ex.Code == ImportErrorCodes.SessionForbidden)
        {
            return Forbid();
        }
        catch (ImportPlatformException ex)
        {
            return BadRequest(new ProblemDetails { Title = ex.Code, Detail = ex.Message, Status = 400 });
        }
    }

    [HttpPost("{jobId}/commit")]
    [Authorize(Policy = PlatformPermissions.ImportCommit)]
    [ProducesResponseType(typeof(ImportCommitResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ExecuteCommit(string jobId, [FromBody] ImportCommitRequestDto request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return BadRequest(new ProblemDetails { Title = ImportErrorCodes.IdempotencyConflict, Detail = "Idempotency key is required for commit.", Status = 400 });

        string actor = User.Identity?.Name ?? "anonymous";
        bool isAdmin = await IsAdminAsync();

        var commitReq = new ImportCommitRequest(jobId, request.IdempotencyKey, request.ExpectedVersion);

        try
        {
            var result = await _commitEngine.ExecuteCommitAsync(commitReq, actor, isAdmin, HttpContext.RequestAborted);
            return Ok(result);
        }
        catch (ImportPlatformException ex) when (ex.Code == ImportErrorCodes.SessionForbidden)
        {
            return Forbid();
        }
        catch (ImportPlatformException ex) when (ex.Code is ImportErrorCodes.CommitConflict or ImportErrorCodes.IdempotencyConflict)
        {
            return Conflict(new ProblemDetails { Title = ex.Code, Detail = ex.Message, Status = 409 });
        }
        catch (ImportPlatformException ex) when (ex.Code is ImportErrorCodes.PreviewExpired or ImportErrorCodes.ValidationFailed or ImportErrorCodes.PreviewRequired)
        {
            return UnprocessableEntity(new ProblemDetails { Title = ex.Code, Detail = ex.Message, Status = 422 });
        }
        catch (ImportPlatformException ex)
        {
            return BadRequest(new ProblemDetails { Title = ex.Code, Detail = ex.Message, Status = 400 });
        }
    }

    [HttpGet("{jobId}/audit")]
    [Authorize(Policy = PlatformPermissions.ImportReview)]
    [ProducesResponseType(typeof(PaginatedAuditResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditEvents(
        string jobId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        string actor = User.Identity?.Name ?? "anonymous";
        bool isAdmin = await IsAdminAsync();

        try
        {
            var result = await _auditService.GetAuditTrailAsync(jobId, actor, isAdmin, page, pageSize, HttpContext.RequestAborted);
            return Ok(result);
        }
        catch (ImportPlatformException ex) when (ex.Code == ImportErrorCodes.SessionForbidden)
        {
            return Forbid();
        }
        catch (ImportPlatformException ex)
        {
            return BadRequest(new ProblemDetails { Title = ex.Code, Detail = ex.Message, Status = 400 });
        }
    }

    private async Task<bool> IsAdminAsync() =>
        (await _authorization.AuthorizeAsync(User, null, PlatformPermissions.ImportAdmin)).Succeeded;

    private static ImportJobStateDto MapToStateDto(ImportJobStateRecord record) => new(
        record.Job.JobId,
        record.Job.OwnerUserId,
        record.Job.State.ToString(),
        record.Workbook.SourceKind.ToString(),
        Path.GetFileName(record.Workbook.SourceDisplayName),
        record.Workbook.Worksheets.Count,
        record.MappingResult?.MappedRecordCount ?? 0,
        record.ValidationResult?.Summary.WarningCount ?? 0,
        record.ValidationResult?.Summary.ErrorCount ?? 0,
        record.ValidationResult?.Summary.BlockingErrorCount ?? 0,
        record.Job.PreviewVersion,
        record.CreatedAt);

    private static MappingResultDto MapToMappingResultDto(MappingResult res) => new(
        res.ProfileId,
        res.ProfileVersion,
        res.TotalRowsProcessed,
        res.MappedRecordCount,
        res.HasBlockingErrors,
        res.Diagnostics,
        res.UnmappedSourceColumns,
        res.IgnoredSourceColumns);

    private static ValidationResultDto MapToValidationResultDto(ValidationResult res) => new(
        res.ValidationProfileId,
        res.ValidationProfileVersion,
        res.Summary,
        res.Diagnostics.Take(100).ToList());
}

public sealed record ImportJobStateDto(
    string JobId,
    string OwnerUserId,
    string State,
    string SourceKind,
    string SourceDisplayName,
    int WorksheetCount,
    int MappedRecordCount,
    int WarningCount,
    int ErrorCount,
    int BlockingErrorCount,
    string? PreviewVersion,
    DateTimeOffset CreatedAt);

public sealed record MappingResultDto(
    string ProfileId,
    string ProfileVersion,
    int TotalRowsProcessed,
    int MappedRecordCount,
    bool HasBlockingErrors,
    IReadOnlyList<MappingDiagnostic> Diagnostics,
    IReadOnlyList<string> UnmappedSourceColumns,
    IReadOnlyList<string> IgnoredSourceColumns);

public sealed record ValidationResultDto(
    string ValidationProfileId,
    string ValidationProfileVersion,
    ValidationSummary Summary,
    IReadOnlyList<ValidationDiagnostic> DiagnosticsSample);

public sealed record MappingProfileRequestDto(
    string? ProfileId,
    string? Version,
    string? Name,
    string? WorksheetName,
    int HeaderRowNumber,
    bool CaseInsensitiveHeaderMatching,
    List<MappingRuleRequestDto> Rules,
    List<string>? IgnoredSourceColumns);

public sealed record MappingRuleRequestDto(
    string SourceColumnIdentifier,
    string TargetField,
    bool IsRequired,
    FieldTransformationType TransformationType,
    string? DefaultValue,
    string? CultureName,
    string? DateTimeFormat,
    Dictionary<string, string>? LookupDictionary);

public sealed record ValidationProfileRequestDto(
    string? ProfileId,
    string? Version,
    string? Name,
    List<ValidationRuleConfigRequestDto>? Rules,
    int MaximumDiagnostics = 1000);

public sealed record ValidationRuleConfigRequestDto(
    string? RuleId,
    string? Name,
    ValidationRuleKind Kind,
    ValidationSeverity Severity,
    string? TargetField,
    string? SecondaryTargetField,
    int? MinLength,
    int? MaxLength,
    double? MinNumeric,
    double? MaxNumeric,
    DateTime? MinDate,
    DateTime? MaxDate,
    List<string>? AllowedValues,
    string? RegexPattern,
    int RegexTimeoutMs = 100,
    string? ComparisonOperator = null);

public sealed record ImportCommitRequestDto(
    string IdempotencyKey,
    long ExpectedVersion);
