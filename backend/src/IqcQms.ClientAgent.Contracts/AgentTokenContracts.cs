namespace IqcQms.ClientAgent.Contracts;

public class AgentTokenRefreshRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string RefreshOperationId { get; set; } = Guid.NewGuid().ToString("N");
}

public class AgentTokenRefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshExpiresAtUtc { get; set; }
    public bool IsDuplicateRetry { get; set; }
}
