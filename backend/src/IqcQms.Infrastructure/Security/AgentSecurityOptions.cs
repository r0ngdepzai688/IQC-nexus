namespace IqcQms.Infrastructure.Security;

public class AgentSecurityOptions
{
    public string PairingPepper { get; set; } = "IqcQmsDefaultServerSidePairingPepper2026";
    public int MaxPairingFailedAttempts { get; set; } = 5;

    public void Validate(bool isDevelopmentOrTesting = false)
    {
        if (!isDevelopmentOrTesting && (string.IsNullOrWhiteSpace(PairingPepper) || PairingPepper == "IqcQmsDefaultServerSidePairingPepper2026"))
        {
            throw new InvalidOperationException("AgentSecurityOptions:PairingPepper MUST be explicitly configured with a strong secret in production.");
        }
    }
}
