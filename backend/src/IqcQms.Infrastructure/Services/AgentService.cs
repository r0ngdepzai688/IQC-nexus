using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IqcQms.Application.Services;
using IqcQms.ClientAgent.Contracts;
using IqcQms.Domain.Entities.Agent;
using IqcQms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IqcQms.Infrastructure.Services;

public class AgentService : IAgentService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AgentService> _logger;

    public AgentService(AppDbContext db, ILogger<AgentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AgentPairingCreateResponse> CreatePairingCodeAsync(int ownerUserId, string? ownerDisplayName = null)
    {
        var rawCode = GenerateRandomPairingCode();
        var hashedCode = HashString(rawCode);

        var pairingRequest = new AgentPairingRequest
        {
            HashedCode = hashedCode,
            OwnerUserId = ownerUserId,
            OwnerDisplayName = ownerDisplayName,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            AttemptCount = 0
        };

        _db.AgentPairingRequests.Add(pairingRequest);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created pairing request for user {UserId}, expires at {ExpiresAtUtc}", ownerUserId, pairingRequest.ExpiresAtUtc);

        return new AgentPairingCreateResponse
        {
            PairingCode = rawCode,
            ExpiresAtUtc = pairingRequest.ExpiresAtUtc,
            OwnerUserId = ownerUserId
        };
    }

    public async Task<AgentDevicePairResponse> PairDeviceAsync(AgentDevicePairRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PairingCode) || string.IsNullOrWhiteSpace(request.DeviceId))
        {
            throw new ArgumentException("Pairing code and device ID are required.");
        }

        var inputHash = HashString(request.PairingCode.Trim());
        var pairingReqs = await _db.AgentPairingRequests
            .Where(p => p.ConsumedAtUtc == null && p.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync();

        var matchedReq = pairingReqs.FirstOrDefault(p => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(p.HashedCode),
            Encoding.UTF8.GetBytes(inputHash)));

        if (matchedReq == null)
        {
            _logger.LogWarning("Failed pairing attempt for device {DeviceId}: invalid or expired code", request.DeviceId);
            throw new InvalidOperationException("Invalid or expired pairing code.");
        }

        matchedReq.ConsumedAtUtc = DateTime.UtcNow;
        matchedReq.AttemptCount++;

        // Validate protocol version
        if (request.ProtocolVersion != "1.0")
        {
            _logger.LogWarning("Rejected pairing for device {DeviceId}: incompatible protocol {ProtocolVersion}", request.DeviceId, request.ProtocolVersion);
            throw new InvalidOperationException($"Unsupported protocol version '{request.ProtocolVersion}'. Supported: 1.0.");
        }

        var existingDevice = await _db.AgentDevices.FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId);
        if (existingDevice != null)
        {
            if (existingDevice.State == AgentDeviceState.Revoked)
            {
                throw new InvalidOperationException("This device has been revoked and cannot be re-paired without admin reset.");
            }

            existingDevice.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? existingDevice.DisplayName : request.DisplayName;
            existingDevice.AgentVersion = request.AgentVersion;
            existingDevice.ProtocolVersion = request.ProtocolVersion;
            existingDevice.CapabilitiesJson = JsonSerializer.Serialize(request.Capabilities);
            existingDevice.LastSeenAtUtc = DateTime.UtcNow;
            existingDevice.State = AgentDeviceState.Active;
        }
        else
        {
            existingDevice = new AgentDevice
            {
                DeviceId = request.DeviceId,
                OwnerUserId = matchedReq.OwnerUserId,
                OwnerDisplayName = matchedReq.OwnerDisplayName,
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? $"Device-{request.DeviceId[..Math.Min(8, request.DeviceId.Length)]}" : request.DisplayName,
                AgentVersion = request.AgentVersion,
                ProtocolVersion = request.ProtocolVersion,
                CapabilitiesJson = JsonSerializer.Serialize(request.Capabilities),
                State = AgentDeviceState.Active,
                PairedAtUtc = DateTime.UtcNow,
                LastSeenAtUtc = DateTime.UtcNow
            };
            _db.AgentDevices.Add(existingDevice);
        }

        await _db.SaveChangesAsync();

        // Issue tokens
        var (accessToken, accessExpiry, refreshToken, refreshExpiry) = await IssueTokensForDeviceAsync(existingDevice);

        _logger.LogInformation("Device {DeviceId} paired successfully for user {OwnerUserId}", existingDevice.DeviceId, existingDevice.OwnerUserId);

        return new AgentDevicePairResponse
        {
            DeviceId = existingDevice.DeviceId,
            AccessToken = accessToken,
            AccessExpiresAtUtc = accessExpiry,
            RefreshToken = refreshToken,
            RefreshExpiresAtUtc = refreshExpiry,
            AgentVersion = existingDevice.AgentVersion,
            ProtocolVersion = existingDevice.ProtocolVersion
        };
    }

    public async Task<AgentTokenRefreshResponse> RefreshTokenAsync(AgentTokenRefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new ArgumentException("DeviceId and RefreshToken are required.");
        }

        var device = await _db.AgentDevices
            .Include(d => d.Credentials)
            .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId);

        if (device == null || device.State == AgentDeviceState.Revoked)
        {
            throw new InvalidOperationException("Device not found or revoked.");
        }

        var inputHash = HashString(request.RefreshToken);
        var activeCreds = device.Credentials.Where(c => c.RevokedAtUtc == null).ToList();

        var matchedCred = activeCreds.FirstOrDefault(c => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(c.ProtectedVerifierHash),
            Encoding.UTF8.GetBytes(inputHash)));

        if (matchedCred == null)
        {
            // Check for replay attack: was this token already used/revoked?
            var replayedCred = device.Credentials.FirstOrDefault(c => CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(c.ProtectedVerifierHash),
                Encoding.UTF8.GetBytes(inputHash)));

            if (replayedCred != null)
            {
                _logger.LogError("REPLAY DETECTED for device {DeviceId}! Revoking all device credentials.", request.DeviceId);
                device.State = AgentDeviceState.Revoked;
                device.RevokedAtUtc = DateTime.UtcNow;
                foreach (var c in device.Credentials)
                {
                    c.RevokedAtUtc = DateTime.UtcNow;
                    c.IsReplayed = true;
                }
                await _db.SaveChangesAsync();
                throw new InvalidOperationException("Replayed refresh token detected. Device has been revoked for security.");
            }

            throw new InvalidOperationException("Invalid refresh token.");
        }

        if (matchedCred.ExpiresAtUtc < DateTime.UtcNow)
        {
            matchedCred.RevokedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            throw new InvalidOperationException("Refresh token has expired.");
        }

        // Invalidate old credential
        matchedCred.RevokedAtUtc = DateTime.UtcNow;
        matchedCred.IsReplayed = true;

        // Issue new token pair
        var (accessToken, accessExpiry, newRefreshToken, refreshExpiry) = await IssueTokensForDeviceAsync(device, matchedCred.RotationLineage);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Successfully refreshed token for device {DeviceId}", device.DeviceId);

        return new AgentTokenRefreshResponse
        {
            AccessToken = accessToken,
            AccessExpiresAtUtc = accessExpiry,
            RefreshToken = newRefreshToken,
            RefreshExpiresAtUtc = refreshExpiry
        };
    }

    public async Task<AgentHeartbeatResponse> HeartbeatAsync(AgentHeartbeatRequest request)
    {
        var device = await _db.AgentDevices.FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId);
        if (device == null)
        {
            throw new InvalidOperationException("Device not found.");
        }

        if (device.State == AgentDeviceState.Revoked)
        {
            throw new InvalidOperationException("Device has been revoked.");
        }

        if (request.ProtocolVersion != "1.0")
        {
            device.State = AgentDeviceState.Incompatible;
            await _db.SaveChangesAsync();
            return new AgentHeartbeatResponse
            {
                DeviceId = device.DeviceId,
                AcknowledgedAtUtc = DateTime.UtcNow,
                NextHeartbeatIntervalSeconds = 300,
                State = "Incompatible"
            };
        }

        device.LastSeenAtUtc = DateTime.UtcNow;
        device.AgentVersion = request.AgentVersion;
        device.CapabilitiesJson = JsonSerializer.Serialize(request.Capabilities);
        if (device.State != AgentDeviceState.Incompatible)
        {
            device.State = AgentDeviceState.Active;
        }

        await _db.SaveChangesAsync();

        return new AgentHeartbeatResponse
        {
            DeviceId = device.DeviceId,
            AcknowledgedAtUtc = DateTime.UtcNow,
            NextHeartbeatIntervalSeconds = 30,
            State = device.State.ToString()
        };
    }

    public async Task<NormalizedWorkbookUploadResponse> UploadNormalizedWorkbookAsync(NormalizedWorkbookUploadRequest request)
    {
        var device = await _db.AgentDevices.FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId);
        if (device == null || device.State == AgentDeviceState.Revoked)
        {
            throw new InvalidOperationException("Device not registered or revoked.");
        }

        if (request.CanonicalSchemaVersion != "1.0")
        {
            throw new InvalidOperationException($"Unsupported schema version '{request.CanonicalSchemaVersion}'.");
        }

        if (request.NormalizedWorkbook == null || string.IsNullOrWhiteSpace(request.NormalizedWorkbook.WorkbookName))
        {
            throw new ArgumentException("Normalized workbook content is invalid or missing.");
        }

        _logger.LogInformation("Accepted normalized payload from device {DeviceId}, records: {RecordCount}, job: {JobId}",
            request.DeviceId, request.RecordCount, request.ServerImportJobId);

        return new NormalizedWorkbookUploadResponse
        {
            UploadId = Guid.NewGuid(),
            ServerImportJobId = request.ServerImportJobId,
            Status = "Accepted",
            ReceivedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<List<AgentDeviceDto>> GetDevicesAsync()
    {
        var devices = await _db.AgentDevices.AsNoTracking().ToListAsync();
        var result = new List<AgentDeviceDto>();

        foreach (var d in devices)
        {
            var state = d.State;
            if (state == AgentDeviceState.Active && d.LastSeenAtUtc.HasValue && d.LastSeenAtUtc.Value < DateTime.UtcNow.AddMinutes(-5))
            {
                state = AgentDeviceState.Offline;
            }

            AgentCapabilitiesDto caps = new();
            try
            {
                if (!string.IsNullOrWhiteSpace(d.CapabilitiesJson))
                    caps = JsonSerializer.Deserialize<AgentCapabilitiesDto>(d.CapabilitiesJson) ?? new();
            }
            catch { }

            result.Add(new AgentDeviceDto
            {
                Id = d.Id,
                DeviceId = d.DeviceId,
                OwnerUserId = d.OwnerUserId,
                OwnerDisplayName = d.OwnerDisplayName,
                DisplayName = d.DisplayName,
                AgentVersion = d.AgentVersion,
                ProtocolVersion = d.ProtocolVersion,
                State = state.ToString(),
                Capabilities = caps,
                PairedAtUtc = d.PairedAtUtc,
                LastSeenAtUtc = d.LastSeenAtUtc,
                RevokedAtUtc = d.RevokedAtUtc
            });
        }

        return result;
    }

    public async Task<AgentDeviceDto?> GetDeviceByIdAsync(string deviceId)
    {
        var devices = await GetDevicesAsync();
        return devices.FirstOrDefault(d => d.DeviceId == deviceId);
    }

    public async Task<bool> RevokeDeviceAsync(string deviceId)
    {
        var device = await _db.AgentDevices
            .Include(d => d.Credentials)
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId);

        if (device == null) return false;

        device.State = AgentDeviceState.Revoked;
        device.RevokedAtUtc = DateTime.UtcNow;

        foreach (var cred in device.Credentials)
        {
            cred.RevokedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Device {DeviceId} revoked by admin", deviceId);
        return true;
    }

    private async Task<(string AccessToken, DateTime AccessExpiry, string RefreshToken, DateTime RefreshExpiry)> IssueTokensForDeviceAsync(AgentDevice device, string? existingLineage = null)
    {
        var accessExpiry = DateTime.UtcNow.AddMinutes(15);
        var refreshExpiry = DateTime.UtcNow.AddDays(7);

        var accessToken = $"agt_acc_{Guid.NewGuid():N}_{device.DeviceId[..Math.Min(6, device.DeviceId.Length)]}";
        var refreshToken = $"agt_ref_{Guid.NewGuid():N}_{Guid.NewGuid():N}";

        var lineage = existingLineage ?? Guid.NewGuid().ToString("N");
        var credential = new AgentCredential
        {
            AgentDeviceId = device.Id,
            DeviceId = device.DeviceId,
            CredentialIdentifier = Guid.NewGuid().ToString("N"),
            ProtectedVerifierHash = HashString(refreshToken),
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = refreshExpiry,
            RotationLineage = lineage,
            IsReplayed = false
        };

        _db.AgentCredentials.Add(credential);
        await _db.SaveChangesAsync();

        return (accessToken, accessExpiry, refreshToken, refreshExpiry);
    }

    private static string GenerateRandomPairingCode()
    {
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        var num = BitConverter.ToUInt32(bytes, 0) % 900000 + 100000;
        return num.ToString();
    }

    private static string HashString(string input)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
