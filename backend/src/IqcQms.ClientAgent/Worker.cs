using IqcQms.ClientAgent.Application.Config;
using IqcQms.ClientAgent.Application.Identity;
using IqcQms.ClientAgent.Application.Providers;
using IqcQms.ClientAgent.Application.Runtime;
using IqcQms.ClientAgent.Contracts;
using IqcQms.ClientAgent.Infrastructure.Http;
using IqcQms.ClientAgent.Infrastructure.Queue;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IqcQms.ClientAgent;

public class Worker : BackgroundService
{
    private readonly IDeviceIdentityStore _identityStore;
    private readonly ISecureCredentialStore _credentialStore;
    private readonly ILocalAgentQueue _localQueue;
    private readonly IAgentApiClient _apiClient;
    private readonly IClientDataProviderRegistry _providerRegistry;
    private readonly ISingleInstanceLock _singleInstanceLock;
    private readonly AgentOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<Worker> _logger;

    private int _consecutiveHeartbeatFailures;
    private DateTime? _lastCompletedJobUtc;

    public Worker(
        IDeviceIdentityStore identityStore,
        ISecureCredentialStore credentialStore,
        ILocalAgentQueue localQueue,
        IAgentApiClient apiClient,
        IClientDataProviderRegistry providerRegistry,
        ISingleInstanceLock singleInstanceLock,
        IOptions<AgentOptions> options,
        IHostEnvironment environment,
        ILogger<Worker> logger)
    {
        _identityStore = identityStore;
        _credentialStore = credentialStore;
        _localQueue = localQueue;
        _apiClient = apiClient;
        _providerRegistry = providerRegistry;
        _singleInstanceLock = singleInstanceLock;
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IQC Nexus Client Agent starting. Version: 1.0.0, Protocol: 1.0, Profile: {Profile}", _options.AgentProfile);

        // Validate options
        _options.Validate(_environment.IsDevelopment() || _environment.IsEnvironment("Testing"));

        // Single-instance enforcement per profile
        if (!await _singleInstanceLock.TryAcquireAsync(stoppingToken))
        {
            _logger.LogWarning("Another Agent process is already running for profile '{Profile}'. Exiting process.", _options.AgentProfile);
            return;
        }

        try
        {
            await _localQueue.InitializeAsync(stoppingToken);

            var identity = await _identityStore.GetOrCreateIdentityAsync(stoppingToken);
            var (accessToken, accessExpiry, refreshToken, refreshExpiry) = await _credentialStore.LoadCredentialsAsync(stoppingToken);

            // Attempt pairing if not yet paired and pairing code is supplied
            if (string.IsNullOrWhiteSpace(refreshToken) && !string.IsNullOrWhiteSpace(_options.PairingCode))
            {
                _logger.LogInformation("Attempting auto-pairing with provided pairing code...");
                try
                {
                    var pairReq = new AgentDevicePairRequest
                    {
                        PairingCode = _options.PairingCode,
                        DeviceId = identity.DeviceId,
                        DisplayName = identity.DisplayName,
                        AgentVersion = "1.0.0",
                        ProtocolVersion = _options.ProtocolVersion,
                        Capabilities = GetCurrentCapabilities()
                    };
                    var pairResp = await _apiClient.PairAsync(pairReq, stoppingToken);
                    await _credentialStore.SaveCredentialsAsync(pairResp.AccessToken, pairResp.AccessExpiresAtUtc, pairResp.RefreshToken, pairResp.RefreshExpiresAtUtc, stoppingToken);
                    accessToken = pairResp.AccessToken;
                    accessExpiry = pairResp.AccessExpiresAtUtc;
                    refreshToken = pairResp.RefreshToken;
                    refreshExpiry = pairResp.RefreshExpiresAtUtc;
                    _logger.LogInformation("Auto-pairing successful!");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Auto-pairing failed. Agent will operate in offline/unpaired state until paired.");
                }
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Ensure valid token if paired
                    if (!string.IsNullOrWhiteSpace(refreshToken))
                    {
                        if (string.IsNullOrWhiteSpace(accessToken) || accessExpiry < DateTime.UtcNow.AddMinutes(1))
                        {
                            if (refreshExpiry < DateTime.UtcNow)
                            {
                                _logger.LogWarning("Refresh token expired. Device credentials cleared; re-pairing required.");
                                await _credentialStore.ClearCredentialsAsync(stoppingToken);
                                accessToken = null;
                                refreshToken = null;
                            }
                            else
                            {
                                try
                                {
                                    var currentOpId = $"agt_op_{Guid.NewGuid():N}";
                                    var refResp = await _apiClient.RefreshTokenAsync(new AgentTokenRefreshRequest
                                    {
                                        DeviceId = identity.DeviceId,
                                        RefreshToken = refreshToken,
                                        RefreshOperationId = currentOpId
                                    }, stoppingToken);

                                    if (!refResp.IsDuplicateRetry && !string.IsNullOrWhiteSpace(refResp.RefreshToken))
                                    {
                                        await _credentialStore.SaveCredentialsAsync(refResp.AccessToken, refResp.AccessExpiresAtUtc, refResp.RefreshToken, refResp.RefreshExpiresAtUtc, stoppingToken);
                                        accessToken = refResp.AccessToken;
                                        accessExpiry = refResp.AccessExpiresAtUtc;
                                        refreshToken = refResp.RefreshToken;
                                        refreshExpiry = refResp.RefreshExpiresAtUtc;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to refresh agent session token.");
                                }
                            }
                        }
                    }

                    // Send periodic heartbeat
                    if (!string.IsNullOrWhiteSpace(identity.DeviceId))
                    {
                        var pendingCount = await _localQueue.GetPendingCountAsync(stoppingToken);
                        var hbReq = new AgentHeartbeatRequest
                        {
                            DeviceId = identity.DeviceId,
                            AgentVersion = "1.0.0",
                            ProtocolVersion = _options.ProtocolVersion,
                            Status = "Online",
                            Capabilities = GetCurrentCapabilities(),
                            SafeQueueCount = pendingCount,
                            LastCompletedJobUtc = _lastCompletedJobUtc
                        };

                        try
                        {
                            var hbResp = await _apiClient.SendHeartbeatAsync(hbReq, accessToken, stoppingToken);
                            _consecutiveHeartbeatFailures = 0;

                            if (hbResp.State == "Revoked")
                            {
                                _logger.LogError("Server reported device state REVOKED. Clearing credentials.");
                                await _credentialStore.ClearCredentialsAsync(stoppingToken);
                                accessToken = null;
                                refreshToken = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            _consecutiveHeartbeatFailures++;
                            _logger.LogWarning(ex, "Heartbeat attempt failed ({FailureCount}).", _consecutiveHeartbeatFailures);
                        }
                    }

                    // Process next queue job if leased
                    await ProcessNextLocalJobAsync(identity, accessToken, stoppingToken);

                    // Heartbeat interval with backoff jitter
                    var intervalSeconds = CalculateNextIntervalSeconds(_options.HeartbeatIntervalSeconds, _consecutiveHeartbeatFailures);
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Agent worker loop.");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        finally
        {
            _singleInstanceLock.Release();
        }

        _logger.LogInformation("IQC Nexus Client Agent shutting down gracefully.");
    }

    private async Task ProcessNextLocalJobAsync(DeviceIdentity identity, string? accessToken, CancellationToken stoppingToken)
    {
        var job = await _localQueue.AcquireNextLeaseAsync($"worker-{Environment.ProcessId}", TimeSpan.FromMinutes(2), stoppingToken);
        if (job == null) return;

        _logger.LogInformation("Leased local job {LocalJobId} for server job {ServerJobId}", job.LocalJobId, job.ServerJobId);

        try
        {
            var provider = _providerRegistry.GetProvider("SyntheticProvider");
            if (provider == null)
            {
                await _localQueue.FailJobAsync(job.LocalJobId, "PROVIDER_NOT_FOUND", TimeSpan.FromSeconds(30), stoppingToken);
                return;
            }

            var normResult = await provider.NormalizeAsync(new ClientNormalizationRequest
            {
                ServerImportJobId = job.ServerJobId,
                InputPathOrReference = job.PayloadReference
            }, stoppingToken);

            if (!normResult.IsSuccess || normResult.NormalizedWorkbook == null)
            {
                await _localQueue.FailJobAsync(job.LocalJobId, normResult.ErrorCode ?? "NORMALIZATION_FAILED", TimeSpan.FromSeconds(30), stoppingToken);
                return;
            }

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                var uploadReq = new NormalizedWorkbookUploadRequest
                {
                    CanonicalSchemaVersion = "1.0",
                    DeviceId = identity.DeviceId,
                    ServerImportJobId = job.ServerJobId,
                    ProviderId = provider.Capabilities.ProviderId,
                    ProviderVersion = provider.Capabilities.ProviderVersion,
                    SourceFingerprint = normResult.SourceFingerprint,
                    NormalizedWorkbook = normResult.NormalizedWorkbook,
                    RecordCount = normResult.RecordCount,
                    DiagnosticsSummary = normResult.DiagnosticsSummary
                };

                await _apiClient.UploadNormalizedWorkbookAsync(uploadReq, accessToken, stoppingToken);
            }

            await _localQueue.CompleteJobAsync(job.LocalJobId, stoppingToken);
            _lastCompletedJobUtc = DateTime.UtcNow;
            _logger.LogInformation("Successfully completed and uploaded job {LocalJobId}", job.LocalJobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process job {LocalJobId}", job.LocalJobId);
            await _localQueue.FailJobAsync(job.LocalJobId, "PROCESS_EXCEPTION", TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private static AgentCapabilitiesDto GetCurrentCapabilities()
    {
        return new AgentCapabilitiesDto
        {
            SyntheticProviderSupported = true,
            CsvProviderSupported = true,
            NascaProviderSupported = false,
            ExcelComSupported = false,
            ExcelAutomationSupported = false,
            SupportedSchemaVersions = new List<string> { "1.0" },
            MaxPayloadBytes = 10 * 1024 * 1024
        };
    }

    private static int CalculateNextIntervalSeconds(int baseInterval, int consecutiveFailures)
    {
        if (consecutiveFailures <= 0) return baseInterval;
        var backoff = baseInterval * (int)Math.Pow(2, Math.Min(consecutiveFailures, 5));
        var jitter = Random.Shared.Next(0, 5);
        return Math.Min(backoff + jitter, 300);
    }
}
