using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IqcQms.ClientAgent.Contracts;
using Microsoft.Extensions.Options;

namespace IqcQms.Infrastructure.Security;

public interface IEnvelopeEncryptionService
{
    (byte[] Ciphertext, byte[] Nonce, byte[] Tag) EncryptResponse(AgentTokenRefreshResponse response, string deviceId, string tokenFamilyId, string refreshOperationId);
    AgentTokenRefreshResponse DecryptResponse(byte[] ciphertext, byte[] nonce, byte[] tag, string deviceId, string tokenFamilyId, string refreshOperationId);
}

public class EnvelopeEncryptionService : IEnvelopeEncryptionService
{
    private readonly byte[] _keyBytes;

    public EnvelopeEncryptionService(IOptions<AgentSecurityOptions> options)
    {
        var rawKey = options.Value.EnvelopeEncryptionKey;
        if (rawKey.Length == 64 && IsHexString(rawKey))
        {
            _keyBytes = Convert.FromHexString(rawKey);
        }
        else
        {
            using var sha = SHA256.Create();
            _keyBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
        }
    }

    public (byte[] Ciphertext, byte[] Nonce, byte[] Tag) EncryptResponse(AgentTokenRefreshResponse response, string deviceId, string tokenFamilyId, string refreshOperationId)
    {
        var jsonStr = JsonSerializer.Serialize(response);
        var plaintextBytes = Encoding.UTF8.GetBytes(jsonStr);

        var nonce = new byte[12]; // 96-bit nonce for AES-GCM
        RandomNumberGenerator.Fill(nonce);

        var tag = new byte[16]; // 128-bit authentication tag
        var ciphertext = new byte[plaintextBytes.Length];

        var aad = Encoding.UTF8.GetBytes($"{deviceId}:{tokenFamilyId}:{refreshOperationId}");

        using var aes = new AesGcm(_keyBytes, 16);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag, aad);

        return (ciphertext, nonce, tag);
    }

    public AgentTokenRefreshResponse DecryptResponse(byte[] ciphertext, byte[] nonce, byte[] tag, string deviceId, string tokenFamilyId, string refreshOperationId)
    {
        var plaintextBytes = new byte[ciphertext.Length];
        var aad = Encoding.UTF8.GetBytes($"{deviceId}:{tokenFamilyId}:{refreshOperationId}");

        using var aes = new AesGcm(_keyBytes, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintextBytes, aad);

        var jsonStr = Encoding.UTF8.GetString(plaintextBytes);
        var response = JsonSerializer.Deserialize<AgentTokenRefreshResponse>(jsonStr);

        return response ?? throw new InvalidOperationException("Failed to deserialize decrypted envelope response.");
    }

    private static bool IsHexString(string str)
    {
        return str.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
    }
}
