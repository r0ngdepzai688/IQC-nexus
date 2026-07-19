using System;
using System.IO;
using System.Threading.Tasks;
using IqcQms.Application.Interfaces.DataHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using IqcQms.Infrastructure.Services.DataHub;
using IqcQms.Domain.Entities.DataHub;

namespace IqcQms.Api.Controllers.NewModels
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DataHubController : ControllerBase
    {
        private readonly IDataHubIngestionService _dataHubService;
        private readonly ILogger<DataHubController> _logger;
        private readonly IMasterPlanContractParser _parser;

        public DataHubController(IDataHubIngestionService dataHubService, IMasterPlanContractParser parser, ILogger<DataHubController> logger)
        {
            _dataHubService = dataHubService;
            _logger = logger;
            _parser = parser;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadMasterPlan(IFormFile file, [FromForm] string module = "NewModels", [FromForm] string? headerMapping = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            string uploadedBy = User.Identity?.Name ?? "SystemAdmin";

            try
            {
                using var stream = file.OpenReadStream();
                IReadOnlyCollection<HeaderMappingDto>? mappings = null;
                if (!string.IsNullOrWhiteSpace(headerMapping))
                    mappings = JsonSerializer.Deserialize<List<HeaderMappingDto>>(headerMapping, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? throw new InvalidDataException("Header mapping is invalid.");
                var batch = await _dataHubService.ProcessUploadAsync(stream, file.FileName, uploadedBy, module, mappings);
                return Ok(batch);
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid Master Plan file", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ProblemDetails { Title = "Import conflict", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Master Plan upload failed");
                return Problem("The import could not be processed. Check server logs using the request trace identifier.");
            }
        }

        [HttpPost("inspect-headers")]
        public async Task<IActionResult> InspectHeaders(IFormFile file)
        {
            if (file is null || file.Length == 0) return BadRequest("No file uploaded.");
            if (file.Length > DataHubIngestionService.MaximumUploadBytes) return BadRequest("File exceeds the 50 MB limit.");
            try
            {
                using var stream = file.OpenReadStream();
                return Ok(await _parser.InspectHeadersAsync(stream));
            }
            catch (Exception ex) when (ex is InvalidDataException or JsonException)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid workbook headers", Detail = ex.Message, Status = 400 });
            }
        }

        [HttpGet("manual-files")]
        public async Task<IActionResult> GetManualUploadFiles()
        {
            var files = await _dataHubService.GetManualUploadFilesAsync();
            return Ok(files);
        }

        [HttpPost("process-manual")]
        public async Task<IActionResult> ProcessManualUpload([FromQuery] string fileName, [FromQuery] string module = "NewModels")
        {
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest("Filename required");

            string uploadedBy = User.Identity?.Name ?? "SystemAdmin";
            try
            {
                var batch = await _dataHubService.ProcessManualUploadAsync(fileName, uploadedBy, module);
                return Ok(batch);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid Master Plan file", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ProblemDetails { Title = "Import conflict", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manual Master Plan import failed for {FileName}", Path.GetFileName(fileName));
                return Problem("The import could not be processed. Check server logs using the request trace identifier.");
            }
        }

        [HttpGet("preview/{batchId}")]
        public async Task<IActionResult> GetPreview(string batchId)
        {
            var batch = await _dataHubService.GetBatchPreviewAsync(batchId);
            if (batch == null) return NotFound("Batch not found.");
            
            return Ok(batch);
        }

        [HttpGet("history")]
        [ProducesResponseType(typeof(List<ImportBatch>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetHistory([FromQuery] string module = "NewModels")
        {
            var history = await _dataHubService.GetHistoryAsync(module);
            return Ok(history);
        }

        [HttpGet("batch/{batchId}/staging")]
        public async Task<IActionResult> GetStagingRecords(string batchId)
        {
            var records = await _dataHubService.GetStagingRecordsAsync(batchId);
            return Ok(records);
        }

        [HttpPost("commit/{batchId}")]
        public async Task<IActionResult> CommitBatch(string batchId)
        {
            string committedBy = User.Identity?.Name ?? "SystemAdmin";
            try
            {
                var batch = await _dataHubService.CommitBatchAsync(batchId, committedBy);
                return Ok(batch);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Commit failed for batch {BatchId}", batchId);
                return Problem("The batch could not be committed. No partial import was retained.");
            }
        }

        [HttpPost("resolve-review/{reviewItemId}")]
        public async Task<IActionResult> ResolveReview(int reviewItemId, [FromBody] ResolveReviewDto dto)
        {
            if (reviewItemId <= 0 || dto == null || !new[] { "Override", "Ignore", "CreateMissing" }.Contains(dto.Action, StringComparer.OrdinalIgnoreCase))
                return BadRequest("A valid review action is required.");
            string resolvedBy = User.Identity?.Name ?? "SystemAdmin";
            var success = await _dataHubService.ResolveReviewItemAsync(reviewItemId, dto.Action, resolvedBy, dto.Note);
            
            if (!success) return BadRequest("Unable to resolve item.");
            return Ok(new { success = true });
        }

        [HttpGet("review/{batchId}")]
        public async Task<IActionResult> GetReview(string batchId)
        {
            var summary = await _dataHubService.GetReviewSummaryAsync(batchId);
            return summary is null ? NotFound("Batch not found.") : Ok(summary);
        }

        [HttpPost("resolve-existing/{batchId}")]
        public async Task<IActionResult> ResolveExistingSku(string batchId, [FromBody] ExistingSkuResolutionDto dto)
        {
            if (dto is null || dto.Resolution is not ("Skip" or "Cancel")) return BadRequest("Resolution must be Skip or Cancel.");
            try
            {
                return Ok(await _dataHubService.ResolveExistingSkuAsync(batchId, dto.Resolution, User.Identity?.Name ?? "AuthenticatedUser"));
            }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("resolve-warning/{batchId}/{rowNumber:int}")]
        public async Task<IActionResult> ResolveWarning(string batchId, int rowNumber, [FromBody] WarningResolutionDto dto)
        {
            if (dto is null || dto.Resolution is not ("Accept" or "Skip")) return BadRequest("Resolution must be Accept or Skip.");
            try
            {
                var resolved = await _dataHubService.ResolveWarningRowAsync(batchId, rowNumber, dto.Resolution, User.Identity?.Name ?? "AuthenticatedUser");
                return resolved ? Ok(new { success = true }) : NotFound("Review row not found or no longer pending.");
            }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }
    }

    public sealed class ExistingSkuResolutionDto
    {
        public string Resolution { get; set; } = "Cancel";
    }
    public sealed class WarningResolutionDto
    {
        public string Resolution { get; set; } = string.Empty;
    }

    public class ResolveReviewDto
    {
        public string Action { get; set; } = string.Empty; // e.g. "Override", "Ignore", "CreateMissing"
        public string? Note { get; set; }
    }
}
