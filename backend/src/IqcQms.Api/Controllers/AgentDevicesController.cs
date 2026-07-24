using System.Security.Claims;
using IqcQms.Application.Auth;
using IqcQms.Application.Services;
using IqcQms.ClientAgent.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IqcQms.Api.Controllers;

[ApiController]
[Route("api")]
public class AgentDevicesController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ILogger<AgentDevicesController> _logger;

    public AgentDevicesController(IAgentService agentService, ILogger<AgentDevicesController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    [HttpPost("agent-pairing-requests")]
    [Authorize(Policy = PlatformPermissions.AgentPair)]
    public async Task<ActionResult<AgentPairingCreateResponse>> CreatePairingCode([FromBody] AgentPairingCreateRequest? request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
        _ = int.TryParse(userIdClaim, out var userId);
        var ownerDisplayName = request?.OwnerDisplayName ?? User.Identity?.Name ?? $"User-{userId}";

        var response = await _agentService.CreatePairingCodeAsync(userId, ownerDisplayName);
        return Ok(response);
    }

    [HttpPost("agent-devices/pair")]
    [AllowAnonymous]
    public async Task<ActionResult<AgentDevicePairResponse>> PairDevice([FromBody] AgentDevicePairRequest request)
    {
        try
        {
            var response = await _agentService.PairDeviceAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("agent-devices/token/refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AgentTokenRefreshResponse>> RefreshToken([FromBody] AgentTokenRefreshRequest request)
    {
        try
        {
            var response = await _agentService.RefreshTokenAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    [HttpPost("agent-devices/{deviceId}/heartbeat")]
    [AllowAnonymous]
    public async Task<ActionResult<AgentHeartbeatResponse>> Heartbeat(string deviceId, [FromBody] AgentHeartbeatRequest request)
    {
        if (deviceId != request.DeviceId)
        {
            return BadRequest(new { Message = "DeviceId route parameter mismatch." });
        }

        try
        {
            var response = await _agentService.HeartbeatAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    [HttpPost("agent-devices/{deviceId}/normalized-workbooks")]
    [AllowAnonymous]
    public async Task<ActionResult<NormalizedWorkbookUploadResponse>> UploadNormalizedWorkbook(string deviceId, [FromBody] NormalizedWorkbookUploadRequest request)
    {
        if (deviceId != request.DeviceId)
        {
            return BadRequest(new { Message = "DeviceId route parameter mismatch." });
        }

        try
        {
            var response = await _agentService.UploadNormalizedWorkbookAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("agent-devices")]
    [Authorize(Policy = PlatformPermissions.AgentView)]
    public async Task<ActionResult<List<AgentDeviceDto>>> GetDevices()
    {
        var devices = await _agentService.GetDevicesAsync();
        return Ok(devices);
    }

    [HttpGet("agent-devices/{deviceId}")]
    [Authorize(Policy = PlatformPermissions.AgentView)]
    public async Task<ActionResult<AgentDeviceDto>> GetDevice(string deviceId)
    {
        var device = await _agentService.GetDeviceByIdAsync(deviceId);
        if (device == null)
        {
            return NotFound(new { Message = "Device not found." });
        }
        return Ok(device);
    }

    [HttpPost("agent-devices/{deviceId}/revoke")]
    [Authorize(Policy = PlatformPermissions.AgentRevoke)]
    public async Task<ActionResult> RevokeDevice(string deviceId)
    {
        var success = await _agentService.RevokeDeviceAsync(deviceId);
        if (!success)
        {
            return NotFound(new { Message = "Device not found." });
        }
        return Ok(new { Message = "Device revoked successfully." });
    }
}
