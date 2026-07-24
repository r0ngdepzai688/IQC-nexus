namespace IqcQms.ClientAgent.Contracts;

public class AgentTokenRefreshRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class AgentTokenRefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshExpiresAtUtc { get; set; }
}
