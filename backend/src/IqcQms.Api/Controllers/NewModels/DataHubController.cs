using System;
using System.IO;
using System.Threading.Tasks;
using IqcQms.Application.Interfaces.DataHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IqcQms.Api.Controllers.NewModels
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] - Commented out for local testing without token if needed, or keep it depending on frontend config.
    public class DataHubController : ControllerBase
    {
        private readonly IDataHubIngestionService _dataHubService;

        public DataHubController(IDataHubIngestionService dataHubService)
        {
            _dataHubService = dataHubService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadMasterPlan(IFormFile file, [FromForm] string module = "NewModels")
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            string uploadedBy = User.Identity?.Name ?? "SystemAdmin";

            try
            {
                using var stream = file.OpenReadStream();
                var batch = await _dataHubService.ProcessUploadAsync(stream, file.FileName, uploadedBy, module);
                return Ok(batch);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
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
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
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
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("resolve-review/{reviewItemId}")]
        public async Task<IActionResult> ResolveReview(int reviewItemId, [FromBody] ResolveReviewDto dto)
        {
            string resolvedBy = User.Identity?.Name ?? "SystemAdmin";
            var success = await _dataHubService.ResolveReviewItemAsync(reviewItemId, dto.Action, resolvedBy, dto.Note);
            
            if (!success) return BadRequest("Unable to resolve item.");
            return Ok(new { success = true });
        }
    }

    public class ResolveReviewDto
    {
        public string Action { get; set; } = string.Empty; // e.g. "Override", "Ignore", "CreateMissing"
        public string? Note { get; set; }
    }
}
