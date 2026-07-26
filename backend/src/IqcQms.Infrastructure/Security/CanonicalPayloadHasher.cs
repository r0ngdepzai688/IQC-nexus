using System.Security.Cryptography;
using System.Text;
using IqcQms.ClientAgent.Contracts;

namespace IqcQms.Infrastructure.Security;

public static class CanonicalPayloadHasher
{
    private const string CanonicalDigestVersion = "v1";

    public static string ComputeCanonicalHash(NormalizedWorkbookUploadRequest request, INormalizedWorkbookCanonicalizer canonicalizer)
    {
        var workbookBytes = canonicalizer.CanonicalizeWorkbook(request.NormalizedWorkbook);
        var sourceFingerprint = string.IsNullOrWhiteSpace(request.SourceFingerprint)
            ? canonicalizer.ComputeSourceFingerprint(request.NormalizedWorkbook)
            : request.SourceFingerprint.Trim();

        using var sha256 = SHA256.Create();
        var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(CanonicalDigestVersion);
            writer.Write(request.CanonicalSchemaVersion ?? "1.0");
            writer.Write(request.DeviceId ?? string.Empty);
            writer.Write(request.PayloadSubmissionId ?? string.Empty);
            writer.Write(request.Nonce ?? string.Empty);
            writer.Write(sourceFingerprint);
            writer.Write(workbookBytes.Length);
            writer.Write(workbookBytes);
            writer.Flush();
        }

        var digestBytes = sha256.ComputeHash(ms.ToArray());
        return $"v1_{Convert.ToHexString(digestBytes)}";
    }
}
