using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IqcQms.Application.Interfaces.NewModels;
using Microsoft.Extensions.Logging;

namespace IqcQms.Api.Controllers.NewModels
{
    [ApiController]
    [Route("api/masterplan")]
    public class MasterPlanController : ControllerBase
    {
        private readonly IMasterPlanService _masterPlanService;
        private readonly ILogger<MasterPlanController> _logger;

        public MasterPlanController(IMasterPlanService masterPlanService, ILogger<MasterPlanController> logger)
        {
            _masterPlanService = masterPlanService;
            _logger = logger;
        }

        [HttpGet("records")]
        public async Task<IActionResult> GetRecords()
        {
            var records = await _masterPlanService.GetLatestMasterPlanRecordsAsync();
            return Ok(records);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                return BadRequest("Only Excel files (.xlsx, .xls) are allowed.");

            try
            {
                using var stream = file.OpenReadStream();
                // We'll mock the user id for now until Auth is fully integrated
                var uploadResult = await _masterPlanService.UploadMasterPlanAsync(stream, file.FileName, "current_user");
                
                return Ok(uploadResult);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error processing Excel file");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("activate/{recordId}")]
        public async Task<IActionResult> ActivateProject(int recordId)
        {
            try
            {
                var workspace = await _masterPlanService.ActivateProjectAsync(recordId, "current_user");
                return Ok(workspace);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error activating project for record {recordId}");
                return BadRequest(ex.Message);
            }
        }
    }
}
