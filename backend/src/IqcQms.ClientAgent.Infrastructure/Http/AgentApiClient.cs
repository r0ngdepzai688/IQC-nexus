using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using IqcQms.ClientAgent.Application.Config;
using IqcQms.ClientAgent.Contracts;
using Microsoft.Extensions.Logging;

namespace IqcQms.ClientAgent.Infrastructure.Http;

public interface IAgentApiClient
{
    Task<AgentDevicePairResponse> PairAsync(AgentDevicePairRequest request, CancellationToken cancellationToken = default);
    Task<AgentTokenRefreshResponse> RefreshTokenAsync(AgentTokenRefreshRequest request, CancellationToken cancellationToken = default);
    Task<AgentHeartbeatResponse> SendHeartbeatAsync(AgentHeartbeatRequest request, string? accessToken = null, CancellationToken cancellationToken = default);
    Task<NormalizedWorkbookUploadResponse> UploadNormalizedWorkbookAsync(NormalizedWorkbookUploadRequest request, string? accessToken = null, CancellationToken cancellationToken = default);
}

public class AgentApiClient : IAgentApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AgentOptions _options;
    private readonly ILogger<AgentApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public AgentApiClient(HttpClient httpClient, AgentOptions options, ILogger<AgentApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.ServerBaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.ServerBaseUrl.TrimEnd('/') + "/");
        }
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
    }

    public async Task<AgentDevicePairResponse> PairAsync(AgentDevicePairRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending pairing request for device {DeviceId}", request.DeviceId);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/agent-devices/pair", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Pairing failed with status {StatusCode}", response.StatusCode);
            throw new HttpRequestException($"Pairing failed ({response.StatusCode}): {body}");
        }

        var result = JsonSerializer.Deserialize<AgentDevicePairResponse>(body, _jsonOptions);
        return result ?? throw new InvalidOperationException("Failed to deserialize pairing response.");
    }

    public async Task<AgentTokenRefreshResponse> RefreshTokenAsync(AgentTokenRefreshRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending token refresh for device {DeviceId}", request.DeviceId);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/agent-devices/token/refresh", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Token refresh failed with status {StatusCode}", response.StatusCode);
            throw new HttpRequestException($"Token refresh failed ({response.StatusCode}).");
        }

        var result = JsonSerializer.Deserialize<AgentTokenRefreshResponse>(body, _jsonOptions);
        return result ?? throw new InvalidOperationException("Failed to deserialize refresh response.");
    }

    public async Task<AgentHeartbeatResponse> SendHeartbeatAsync(AgentHeartbeatRequest request, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/agent-devices/{request.DeviceId}/heartbeat")
        {
            Content = content
        };

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Heartbeat failed with status {StatusCode}", response.StatusCode);
            throw new HttpRequestException($"Heartbeat failed ({response.StatusCode}).");
        }

        var result = JsonSerializer.Deserialize<AgentHeartbeatResponse>(body, _jsonOptions);
        return result ?? throw new InvalidOperationException("Failed to deserialize heartbeat response.");
    }

    public async Task<NormalizedWorkbookUploadResponse> UploadNormalizedWorkbookAsync(NormalizedWorkbookUploadRequest request, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading normalized workbook for job {JobId} from device {DeviceId}", request.ServerImportJobId, request.DeviceId);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/agent-devices/{request.DeviceId}/normalized-workbooks")
        {
            Content = content
        };

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Upload normalized workbook failed with status {StatusCode}", response.StatusCode);
            throw new HttpRequestException($"Upload normalized workbook failed ({response.StatusCode}): {body}");
        }

        var result = JsonSerializer.Deserialize<NormalizedWorkbookUploadResponse>(body, _jsonOptions);
        return result ?? throw new InvalidOperationException("Failed to deserialize upload response.");
    }
}
