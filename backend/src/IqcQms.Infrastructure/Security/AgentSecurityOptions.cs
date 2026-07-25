using System.Text.RegularExpressions;

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
            if (string.IsNullOrWhiteSpace(PairingPepper) ||
                PairingPepper == "IqcQmsDefaultServerSidePairingPepper2026" ||
                PairingPepper.Length < 16)
            {
                throw new InvalidOperationException("AgentSecurityOptions:PairingPepper MUST be explicitly configured with a strong secret (at least 16 characters) in production.");
            }

            if (string.IsNullOrWhiteSpace(EnvelopeEncryptionKey) ||
                EnvelopeEncryptionKey == "4a6f686e446f655365637265744b657932303236497163516d73456e76656c6f")
            {
                throw new InvalidOperationException("AgentSecurityOptions:EnvelopeEncryptionKey MUST be explicitly configured with a non-default production key.");
            }

            try
            {
                byte[] keyBytes = EnvelopeEncryptionKey.Length == 64 && Regex.IsMatch(EnvelopeEncryptionKey, @"^[0-9a-fA-F]+$")
                    ? Convert.FromHexString(EnvelopeEncryptionKey)
                    : Convert.FromBase64String(EnvelopeEncryptionKey);

                if (keyBytes.Length < 32)
                {
                    throw new InvalidOperationException("AgentSecurityOptions:EnvelopeEncryptionKey MUST contain at least 256 bits (32 bytes) of key entropy.");
                }
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("AgentSecurityOptions:EnvelopeEncryptionKey encoding is malformed (must be valid Hex or Base64).", ex);
            }
        }
    }
}
