using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace IqcQms.Application.DataPlatform;

public sealed class PreviewAttestationOptions
{
    public const string SectionName = "PreviewAttestation";
    public string SecretKey { get; set; } = string.Empty;
    public int TokenLifetimeHours { get; set; } = 2;
}

public interface IPreviewAttestationService
{
    ImportPreviewAttestation CreateAttestation(
        string jobId,
        string ownerUserId,
        string mappingProfileId,
        string mappingProfileVersion,
        string validationProfileId,
        string validationProfileVersion,
        int mappedRecordCount,
        int warningCount,
        int errorCount,
        int blockingErrorCount);

    bool VerifyAttestation(
        ImportPreviewAttestation attestation,
        string jobId,
        string ownerUserId,
        string mappingProfileId,
        string mappingProfileVersion,
        string validationProfileId,
        string validationProfileVersion,
        int mappedRecordCount,
        int warningCount,
        int errorCount,
        int blockingErrorCount);
}

public sealed class PreviewAttestationService : IPreviewAttestationService
{
    private readonly byte[] _keyBytes;
    private readonly TimeSpan _lifetime;

    public PreviewAttestationService(IOptions<PreviewAttestationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var opts = options.Value ?? throw new ArgumentException("Attestation options are missing.", nameof(options));
        if (string.IsNullOrWhiteSpace(opts.SecretKey))
        {
            throw new InvalidOperationException("PreviewAttestation:SecretKey must be configured and cannot be empty.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(opts.SecretKey);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("PreviewAttestation:SecretKey must be at least 256 bits (32 bytes) long for security.");
        }

        _keyBytes = keyBytes;
        _lifetime = TimeSpan.FromHours(opts.TokenLifetimeHours > 0 ? opts.TokenLifetimeHours : 2);
    }

    public static string ComputeContentFingerprint(
        string jobId,
        string ownerUserId,
        string mappingProfileId,
        string mappingProfileVersion,
        string validationProfileId,
        string validationProfileVersion,
        int mappedRecordCount,
        int warningCount,
        int errorCount,
        int blockingErrorCount)
    {
        var raw = $"{jobId}:{ownerUserId}:{mappingProfileId}:{mappingProfileVersion}:{validationProfileId}:{validationProfileVersion}:{mappedRecordCount}:{warningCount}:{errorCount}:{blockingErrorCount}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public ImportPreviewAttestation CreateAttestation(
        string jobId,
        string ownerUserId,
        string mappingProfileId,
        string mappingProfileVersion,
        string validationProfileId,
        string validationProfileVersion,
        int mappedRecordCount,
        int warningCount,
        int errorCount,
        int blockingErrorCount)
    {
        var generatedAt = DateTimeOffset.UtcNow;
        var expiresAt = generatedAt.Add(_lifetime);

        var fingerprint = ComputeContentFingerprint(
            jobId,
            ownerUserId,
            mappingProfileId,
            mappingProfileVersion,
            validationProfileId,
            validationProfileVersion,
            mappedRecordCount,
            warningCount,
            errorCount,
            blockingErrorCount);

        var signature = SignAttestationPayload(jobId, ownerUserId, fingerprint, generatedAt, expiresAt);

        return new ImportPreviewAttestation(
            jobId,
            ownerUserId,
            fingerprint,
            mappingProfileVersion,
            validationProfileVersion,
            generatedAt,
            expiresAt,
            signature);
    }

    public bool VerifyAttestation(
        ImportPreviewAttestation attestation,
        string jobId,
        string ownerUserId,
        string mappingProfileId,
        string mappingProfileVersion,
        string validationProfileId,
        string validationProfileVersion,
        int mappedRecordCount,
        int warningCount,
        int errorCount,
        int blockingErrorCount)
    {
        if (attestation == null) return false;
        if (DateTimeOffset.UtcNow > attestation.ExpiresAt) return false;
        if (!string.Equals(attestation.JobId, jobId, StringComparison.Ordinal)) return false;
        if (!string.Equals(attestation.OwnerUserId, ownerUserId, StringComparison.OrdinalIgnoreCase)) return false;

        var expectedFingerprint = ComputeContentFingerprint(
            jobId,
            ownerUserId,
            mappingProfileId,
            mappingProfileVersion,
            validationProfileId,
            validationProfileVersion,
            mappedRecordCount,
            warningCount,
            errorCount,
            blockingErrorCount);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedFingerprint),
                Encoding.UTF8.GetBytes(attestation.ContentFingerprint)))
        {
            return false;
        }

        var expectedSignature = SignAttestationPayload(
            jobId,
            ownerUserId,
            expectedFingerprint,
            attestation.GeneratedAt,
            attestation.ExpiresAt);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(attestation.Signature));
    }

    private string SignAttestationPayload(
        string jobId,
        string ownerUserId,
        string fingerprint,
        DateTimeOffset generatedAt,
        DateTimeOffset expiresAt)
    {
        var rawPayload = $"v1:{jobId}:{ownerUserId}:{fingerprint}:{generatedAt.ToUnixTimeSeconds()}:{expiresAt.ToUnixTimeSeconds()}";
        using var hmac = new HMACSHA256(_keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawPayload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
