using IqcQms.ClientAgent.Contracts;

namespace IqcQms.Application.Services;

public interface IAgentService
{
    Task<AgentPairingCreateResponse> CreatePairingCodeAsync(int ownerUserId, string? ownerDisplayName = null);
    Task<AgentDevicePairResponse> PairDeviceAsync(AgentDevicePairRequest request);
    Task<AgentTokenRefreshResponse> RefreshTokenAsync(AgentTokenRefreshRequest request);
    Task<AgentHeartbeatResponse> HeartbeatAsync(AgentHeartbeatRequest request);
    Task<NormalizedWorkbookUploadResponse> UploadNormalizedWorkbookAsync(NormalizedWorkbookUploadRequest request);
    Task<List<AgentDeviceDto>> GetDevicesAsync();
    Task<AgentDeviceDto?> GetDeviceByIdAsync(string deviceId);
    Task<bool> RevokeDeviceAsync(string deviceId);
}
