using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IqcQms.Application.Services;
using IqcQms.ClientAgent.Contracts;
using IqcQms.Domain.Entities.Agent;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IqcQms.Infrastructure.Services;

public class AgentService : IAgentService
{
    private static readonly Regex ValidOperationIdRegex = new(@"^[a-zA-Z0-9_-]{8,64}$", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly AgentSecurityOptions _securityOptions;
    private readonly IEnvelopeEncryptionService _encryptionService;
    private readonly ILogger<AgentService> _logger;

    public AgentService(
        AppDbContext db,
        IOptions<AgentSecurityOptions> securityOptions,
        IEnvelopeEncryptionService encryptionService,
        ILogger<AgentService> logger)
    {
        _db = db;
        _securityOptions = securityOptions.Value;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public AgentService(AppDbContext db, ILogger<AgentService> logger)
        : this(db, Options.Create(new AgentSecurityOptions()), new EnvelopeEncryptionService(Options.Create(new AgentSecurityOptions())), logger)
    {
    }

    public async Task<AgentPairingCreateResponse> CreatePairingCodeAsync(int ownerUserId, string? ownerDisplayName = null)
    {
        var requestId = Guid.NewGuid();
        var rawCode = GenerateSixDigitPairingCode();
        var hashedCode = ComputeHmacPairingCode(requestId, rawCode, _securityOptions.PairingPepper);

        var pairingRequest = new AgentPairingRequest
        {
            Id = requestId,
            HashedCode = hashedCode,
            OwnerUserId = ownerUserId,
            OwnerDisplayName = ownerDisplayName,
            State = AgentPairingRequestState.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            FailedAttemptCount = 0,
            MaxFailedAttempts = _securityOptions.MaxPairingFailedAttempts,
            ConcurrencyVersion = 1
        };

        _db.AgentPairingRequests.Add(pairingRequest);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created pairing request {RequestId} for user {UserId}, expires at {ExpiresAtUtc}", requestId, ownerUserId, pairingRequest.ExpiresAtUtc);

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

        var normalizedCode = request.PairingCode.Trim();

        using var tx = await _db.Database.BeginTransactionAsync();

        var candidateReqs = await _db.AgentPairingRequests
            .Where(p => p.State == AgentPairingRequestState.Pending && p.ConsumedAtUtc == null && p.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync();

        AgentPairingRequest? matchedReq = null;
        foreach (var p in candidateReqs)
        {
            var testHash = ComputeHmacPairingCode(p.Id, normalizedCode, _securityOptions.PairingPepper);
            if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(p.HashedCode), Encoding.UTF8.GetBytes(testHash)))
            {
                matchedReq = p;
                break;
            }
        }

        if (matchedReq == null)
        {
            _logger.LogWarning("Failed pairing attempt for device {DeviceId}: invalid or expired code.", request.DeviceId);

            // Increment attempt count on any active pending request for auditing/locking
            foreach (var req in candidateReqs)
            {
                req.FailedAttemptCount++;
                req.LastFailedAttemptAtUtc = DateTime.UtcNow;
                if (req.FailedAttemptCount >= req.MaxFailedAttempts)
                {
                    req.State = AgentPairingRequestState.Locked;
                    req.LockedAtUtc = DateTime.UtcNow;
                    _logger.LogWarning("Pairing request {RequestId} reached max failed attempts and is now LOCKED.", req.Id);
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            throw new InvalidOperationException("Pairing failed or code is no longer valid.");
        }

        if (matchedReq.State != AgentPairingRequestState.Pending || matchedReq.ConsumedAtUtc != null || matchedReq.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Pairing failed or code is no longer valid.");
        }

        // Validate protocol version
        if (request.ProtocolVersion != "1.0")
        {
            _logger.LogWarning("Rejected pairing for device {DeviceId}: incompatible protocol {ProtocolVersion}", request.DeviceId, request.ProtocolVersion);
            throw new InvalidOperationException($"Unsupported protocol version '{request.ProtocolVersion}'. Supported: 1.0.");
        }

        matchedReq.State = AgentPairingRequestState.Consumed;
        matchedReq.ConsumedAtUtc = DateTime.UtcNow;
        matchedReq.ConcurrencyVersion++;

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
            existingDevice.ConcurrencyVersion++;
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
                LastSeenAtUtc = DateTime.UtcNow,
                ConcurrencyVersion = 1
            };
            _db.AgentDevices.Add(existingDevice);
        }

        await _db.SaveChangesAsync();

        var tokenFamilyId = Guid.NewGuid().ToString("N");
        var initialOpId = $"op_pair_{Guid.NewGuid():N}";
        var (accessToken, accessExpiry, refreshToken, refreshExpiry) = await IssueTokensForDeviceAsync(existingDevice, tokenFamilyId, initialOpId);

        await tx.CommitAsync();

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

        if (string.IsNullOrWhiteSpace(request.RefreshOperationId) || !ValidOperationIdRegex.IsMatch(request.RefreshOperationId.Trim()))
        {
            throw new ArgumentException("RefreshOperationId is required and must be a valid identifier (8-64 alphanumeric chars, hyphens, underscores).");
        }

        var operationId = request.RefreshOperationId.Trim();

        using var tx = await _db.Database.BeginTransactionAsync();

        var device = await _db.AgentDevices
            .Include(d => d.Credentials)
            .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId);

        if (device == null || device.State == AgentDeviceState.Revoked)
        {
            throw new InvalidOperationException("Device not found or revoked.");
        }

        var inputHash = HashSha256(request.RefreshToken);
        var activeCreds = device.Credentials.Where(c => c.ConsumedAtUtc == null && c.RevokedAtUtc == null).ToList();

        AgentCredential? matchedCred = null;
        foreach (var c in activeCreds)
        {
            if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(c.ProtectedVerifierHash), Encoding.UTF8.GetBytes(inputHash)))
            {
                matchedCred = c;
                break;
            }
        }

        if (matchedCred == null)
        {
            // Check for duplicate retry vs confirmed replay attack
            var consumedCred = device.Credentials.FirstOrDefault(c => CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(c.ProtectedVerifierHash),
                Encoding.UTF8.GetBytes(inputHash)));

            if (consumedCred != null)
            {
                var envelope = await _db.AgentRefreshOperationResults
                    .FirstOrDefaultAsync(r => r.AgentDeviceId == device.Id && r.RefreshOperationId == operationId);

                if (envelope != null)
                {
                    if (envelope.ExpiresAtUtc < DateTime.UtcNow)
                    {
                        await tx.CommitAsync();
                        throw new InvalidOperationException("Recovery window expired for this operation. Please re-authenticate or re-pair.");
                    }

                    try
                    {
                        var recoveredResponse = _encryptionService.DecryptResponse(envelope.EncryptedPayload, envelope.Nonce, envelope.Tag, device.DeviceId, envelope.TokenFamilyId, operationId);
                        recoveredResponse.IsDuplicateRetry = true;

                        await tx.CommitAsync();
                        _logger.LogInformation("Recovered exact original committed response for duplicate retry request (operation {OperationId})", operationId);
                        return recoveredResponse;
                    }
                    catch (CryptographicException ex)
                    {
                        _logger.LogError(ex, "Failed to decrypt idempotency recovery envelope for operation {OperationId}", operationId);
                    }
                }

                // Otherwise: CONFIRMED REPLAY ATTACK (different operation ID or un-enveloped replay)
                _logger.LogError("CONFIRMED TOKEN REPLAY DETECTED for device {DeviceId}, token family {TokenFamilyId}! Revoking entire token family and device.", request.DeviceId, consumedCred.TokenFamilyId);

                device.State = AgentDeviceState.Revoked;
                device.RevokedAtUtc = DateTime.UtcNow;
                device.ConcurrencyVersion++;

                var familyCreds = device.Credentials.Where(c => c.TokenFamilyId == consumedCred.TokenFamilyId || string.IsNullOrWhiteSpace(c.TokenFamilyId)).ToList();
                foreach (var c in familyCreds)
                {
                    c.RevokedAtUtc = DateTime.UtcNow;
                    c.IsReplayed = true;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                throw new InvalidOperationException("Replayed refresh token detected. Device has been revoked for security.");
            }

            throw new InvalidOperationException("Invalid refresh token.");
        }

        if (matchedCred.ExpiresAtUtc < DateTime.UtcNow)
        {
            matchedCred.RevokedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            throw new InvalidOperationException("Refresh token has expired.");
        }

        // Consume and rotate current credential
        matchedCred.ConsumedAtUtc = DateTime.UtcNow;
        matchedCred.RevokedAtUtc = DateTime.UtcNow;
        device.ConcurrencyVersion++;

        var (accessToken, accessExpiry, newRefreshToken, refreshExpiry) = await IssueTokensForDeviceAsync(device, matchedCred.TokenFamilyId, operationId, matchedCred.RotationLineage);

        var responseToReturn = new AgentTokenRefreshResponse
        {
            AccessToken = accessToken,
            AccessExpiresAtUtc = accessExpiry,
            RefreshToken = newRefreshToken,
            RefreshExpiresAtUtc = refreshExpiry,
            IsDuplicateRetry = false
        };

        // Encrypt and persist short-lived recovery envelope
        var (ciphertext, nonce, tag) = _encryptionService.EncryptResponse(responseToReturn, device.DeviceId, matchedCred.TokenFamilyId, operationId);

        var recoveryEnvelope = new AgentRefreshOperationResult
        {
            AgentDeviceId = device.Id,
            DeviceId = device.DeviceId,
            TokenFamilyId = matchedCred.TokenFamilyId,
            RefreshOperationId = operationId,
            EncryptedPayload = ciphertext,
            Nonce = nonce,
            Tag = tag,
            KeyVersion = 1,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(120)
        };

        _db.AgentRefreshOperationResults.Add(recoveryEnvelope);
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        _logger.LogInformation("Successfully refreshed token for device {DeviceId}, family {TokenFamilyId}, op {OperationId}", device.DeviceId, matchedCred.TokenFamilyId, operationId);

        return responseToReturn;
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

    private async Task<(string AccessToken, DateTime AccessExpiry, string RefreshToken, DateTime RefreshExpiry)> IssueTokensForDeviceAsync(AgentDevice device, string tokenFamilyId, string refreshOperationId, string? existingLineage = null)
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
            ProtectedVerifierHash = HashSha256(refreshToken),
            TokenFamilyId = tokenFamilyId,
            RefreshOperationId = refreshOperationId,
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = refreshExpiry,
            RotationLineage = lineage,
            IsReplayed = false
        };

        _db.AgentCredentials.Add(credential);
        await _db.SaveChangesAsync();

        return (accessToken, accessExpiry, refreshToken, refreshExpiry);
    }

    public static string GenerateSixDigitPairingCode()
    {
        var val = RandomNumberGenerator.GetInt32(0, 1000000);
        return val.ToString("D6");
    }

    public static string ComputeHmacPairingCode(Guid requestId, string code, string pepper)
    {
        var normalizedCode = code.Trim();
        var message = $"{requestId:N}:{normalizedCode}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(pepper));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash);
    }

    public static string HashSha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
