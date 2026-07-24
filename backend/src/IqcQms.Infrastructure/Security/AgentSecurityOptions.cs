namespace IqcQms.Infrastructure.Security;

public class AgentSecurityOptions
{
    public string PairingPepper { get; set; } = "IqcQmsDefaultServerSidePairingPepper2026";
    public int MaxPairingFailedAttempts { get; set; } = 5;
    public string EnvelopeEncryptionKey { get; set; } = "4a6f686e446f655365637265744b657932303236497163516d73456e76656c6f"; // 32-byte default test key in hex

    public void Validate(bool isDevelopmentOrTesting = false)
    {
        if (!isDevelopmentOrTesting)
        {
            if (string.IsNullOrWhiteSpace(PairingPepper) || PairingPepper == "IqcQmsDefaultServerSidePairingPepper2026")
            {
                throw new InvalidOperationException("AgentSecurityOptions:PairingPepper MUST be explicitly configured with a strong secret in production.");
            }

            if (string.IsNullOrWhiteSpace(EnvelopeEncryptionKey) || EnvelopeEncryptionKey.Length < 32)
            {
                throw new InvalidOperationException("AgentSecurityOptions:EnvelopeEncryptionKey MUST be configured with at least 256 bits (32 bytes) of entropy in production.");
            }
        }
    }
}
