using System.Security.Cryptography;
using System.Text.Json;
using IqcQms.ClientAgent.Application.Nasca;
using Microsoft.Extensions.Logging;

namespace IqcQms.ClientAgent.Infrastructure.Nasca;

public class NascaWorkDirectoryManager : INascaWorkDirectoryManager
{
    private readonly string _workRootDirectory;
    private readonly ILogger<NascaWorkDirectoryManager> _logger;

    public NascaWorkDirectoryManager(string rootDataDirectory, ILogger<NascaWorkDirectoryManager> logger)
    {
        _logger = logger;
        _workRootDirectory = Path.Combine(rootDataDirectory, "NascaWork");
        Directory.CreateDirectory(_workRootDirectory);
    }

    public string GetWorkRootDirectory() => _workRootDirectory;

    public string GetWorkDirectoryPath(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Contains('/') || correlationId.Contains('\\') || correlationId.Contains(".."))
        {
            throw new InvalidOperationException("Path traversal or invalid path characters detected in CorrelationId.");
        }

        var opaqueId = GetOpaqueDirectoryId(correlationId);
        var dirPath = Path.Combine(_workRootDirectory, opaqueId);
        EnsurePathIsContained(_workRootDirectory, dirPath);
        return dirPath;
    }

    private static string GetOpaqueDirectoryId(string correlationId)
    {
        // Sanitize correlationId to ensure no path traversal or invalid path characters exist
        var safeId = string.Concat(correlationId.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));
        if (string.IsNullOrWhiteSpace(safeId))
        {
            safeId = "default";
        }
        return $"work_{safeId}";
    }

    private static void EnsurePathIsContained(string rootDir, string targetPath)
    {
        var fullRoot = Path.GetFullPath(rootDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullTarget = Path.GetFullPath(targetPath);

        if (!fullTarget.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Path traversal attempt detected: target directory escapes root work directory boundary.");
        }
    }

    public async Task<NascaWorkManifest> CreateWorkDirectoryAsync(string correlationId, string executionId, CancellationToken cancellationToken = default)
    {
        var dirPath = GetWorkDirectoryPath(correlationId);
        Directory.CreateDirectory(dirPath);

        Directory.CreateDirectory(Path.Combine(dirPath, "input"));
        Directory.CreateDirectory(Path.Combine(dirPath, "output"));
        Directory.CreateDirectory(Path.Combine(dirPath, "state"));
        Directory.CreateDirectory(Path.Combine(dirPath, "quarantine"));

        var manifest = new NascaWorkManifest
        {
            SchemaVersion = 1,
            CorrelationId = correlationId,
            ExecutionId = executionId,
            AttemptNumber = 1,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
            CurrentLifecycle = NascaWorkLifecycle.Active,
            RetentionCategory = "Standard"
        };

        await WriteManifestAtomicAsync(dirPath, manifest, cancellationToken);
        _logger.LogInformation("Created secure work directory for CorrelationId {CorrelationId}", correlationId);
        return manifest;
    }

    public async Task<NascaWorkManifest> StageInputFileAsync(string correlationId, string sourceFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Source file for staging does not exist.", sourceFilePath);
        }

        var dirPath = GetWorkDirectoryPath(correlationId);
        var manifest = await GetManifestAsync(correlationId, cancellationToken)
            ?? throw new InvalidOperationException($"Work directory manifest missing for CorrelationId {correlationId}.");

        var inputSubDir = Path.Combine(dirPath, "input");
        EnsurePathIsContained(dirPath, inputSubDir);

        var tempStagingPath = Path.Combine(inputSubDir, $"staging_{Guid.NewGuid():N}.tmp");
        var finalStagedPath = Path.Combine(inputSubDir, "input.dat");

        // 1. Safe copy to temporary staging file
        using (var sourceStream = File.OpenRead(sourceFilePath))
        using (var stagingStream = File.Create(tempStagingPath))
        {
            await sourceStream.CopyToAsync(stagingStream, cancellationToken);
        }

        // 2. Compute SHA-256 hash and file size of staged file
        string stagedHash;
        long fileSizeBytes;
        using (var hashAlg = SHA256.Create())
        using (var stagedStream = File.OpenRead(tempStagingPath))
        {
            var hashBytes = await hashAlg.ComputeHashAsync(stagedStream, cancellationToken);
            stagedHash = Convert.ToHexString(hashBytes);
            fileSizeBytes = stagedStream.Length;
        }

        // 3. Compute hash of original source to verify integrity
        string originalHash;
        using (var hashAlg = SHA256.Create())
        using (var sourceStream = File.OpenRead(sourceFilePath))
        {
            var hashBytes = await hashAlg.ComputeHashAsync(sourceStream, cancellationToken);
            originalHash = Convert.ToHexString(hashBytes);
        }

        if (!originalHash.Equals(stagedHash, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(tempStagingPath);
            await QuarantineWorkDirectoryAsync(correlationId, "STAGING_HASH_MISMATCH", cancellationToken);
            throw new InvalidOperationException("Input staging hash mismatch: source file hash does not match staged copy.");
        }

        // 4. Atomic rename to finalStagedPath
        if (File.Exists(finalStagedPath))
        {
            File.Delete(finalStagedPath);
        }
        File.Move(tempStagingPath, finalStagedPath);

        // 5. Update manifest metadata
        manifest.InputMetadata = new NascaInputMetadata
        {
            StagedFileName = "input.dat",
            OriginalHashSha256 = originalHash,
            StagedHashSha256 = stagedHash,
            FileSizeBytes = fileSizeBytes
        };
        manifest.UpdatedUtc = DateTime.UtcNow;

        await WriteManifestAtomicAsync(dirPath, manifest, cancellationToken);
        _logger.LogInformation("Staged input file atomically for CorrelationId {CorrelationId}", correlationId);
        return manifest;
    }

    public async Task<NascaWorkManifest?> GetManifestAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        var dirPath = GetWorkDirectoryPath(correlationId);
        var manifestPath = Path.Combine(dirPath, "manifest.json");

        if (!File.Exists(manifestPath)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            var manifest = JsonSerializer.Deserialize<NascaWorkManifest>(json);

            if (manifest == null || manifest.CorrelationId != correlationId)
            {
                await QuarantineWorkDirectoryAsync(correlationId, "CORRUPT_OR_MISMATCHED_MANIFEST", cancellationToken);
                return null;
            }

            return manifest;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Corrupt JSON manifest for CorrelationId {CorrelationId}. Quarantining.", correlationId);
            await QuarantineWorkDirectoryAsync(correlationId, "CORRUPT_MANIFEST_JSON", cancellationToken);
            return null;
        }
    }

    public async Task<NascaWorkManifest> UpdateLifecycleAsync(string correlationId, NascaWorkLifecycle targetLifecycle, CancellationToken cancellationToken = default)
    {
        var dirPath = GetWorkDirectoryPath(correlationId);
        var manifest = await GetManifestAsync(correlationId, cancellationToken)
            ?? throw new InvalidOperationException($"Cannot update lifecycle: Manifest missing for CorrelationId {correlationId}.");

        manifest.CurrentLifecycle = targetLifecycle;
        manifest.UpdatedUtc = DateTime.UtcNow;

        if (targetLifecycle == NascaWorkLifecycle.RecoveryRequired)
        {
            manifest.RetentionCategory = "Recovery";
        }
        else if (targetLifecycle == NascaWorkLifecycle.Quarantined)
        {
            manifest.RetentionCategory = "Quarantine";
        }

        await WriteManifestAtomicAsync(dirPath, manifest, cancellationToken);
        _logger.LogInformation("Updated lifecycle for CorrelationId {CorrelationId} to {Lifecycle}", correlationId, targetLifecycle);
        return manifest;
    }

    public async Task QuarantineWorkDirectoryAsync(string correlationId, string reasonCode, CancellationToken cancellationToken = default)
    {
        var dirPath = GetWorkDirectoryPath(correlationId);
        if (!Directory.Exists(dirPath)) return;

        var manifestPath = Path.Combine(dirPath, "manifest.json");
        NascaWorkManifest manifest;

        if (File.Exists(manifestPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                manifest = JsonSerializer.Deserialize<NascaWorkManifest>(json) ?? new NascaWorkManifest();
            }
            catch
            {
                manifest = new NascaWorkManifest();
            }
        }
        else
        {
            manifest = new NascaWorkManifest();
        }

        manifest.CorrelationId = correlationId;
        manifest.CurrentLifecycle = NascaWorkLifecycle.Quarantined;
        manifest.RetentionCategory = "Quarantine";
        manifest.UpdatedUtc = DateTime.UtcNow;

        await WriteManifestAtomicAsync(dirPath, manifest, cancellationToken);
        _logger.LogWarning("Quarantined work directory for CorrelationId {CorrelationId}: {ReasonCode}", correlationId, reasonCode);
    }

    public async Task<int> CleanupExpiredWorkDirectoriesAsync(TimeSpan standardRetention, TimeSpan recoveryRetention, int maxCleanupBatch = 50, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_workRootDirectory)) return 0;

        int cleaned = 0;
        var subDirs = Directory.GetDirectories(_workRootDirectory);
        var now = DateTime.UtcNow;

        foreach (var dir in subDirs)
        {
            if (cleaned >= maxCleanupBatch) break;

            var manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath)) continue;

            try
            {
                var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                var manifest = JsonSerializer.Deserialize<NascaWorkManifest>(json);
                if (manifest == null) continue;

                // Active and Quarantined work directories are NEVER deleted
                if (manifest.CurrentLifecycle == NascaWorkLifecycle.Active || manifest.CurrentLifecycle == NascaWorkLifecycle.Quarantined)
                {
                    continue;
                }

                var age = now - manifest.UpdatedUtc;
                bool shouldDelete = false;

                if (manifest.CurrentLifecycle == NascaWorkLifecycle.Completed && age > standardRetention)
                {
                    shouldDelete = true;
                }
                else if (manifest.CurrentLifecycle == NascaWorkLifecycle.RecoveryRequired && age > recoveryRetention)
                {
                    shouldDelete = true;
                }
                else if (manifest.CurrentLifecycle == NascaWorkLifecycle.Failed && age > standardRetention)
                {
                    shouldDelete = true;
                }

                if (shouldDelete)
                {
                    Directory.Delete(dir, recursive: true);
                    cleaned++;
                    _logger.LogInformation("Cleaned up expired work directory for CorrelationId {CorrelationId}", manifest.CorrelationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to evaluate work directory cleanup.");
            }
        }

        return cleaned;
    }

    public async Task<IReadOnlyList<NascaWorkManifest>> DiscoverRecoverableWorkDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        var recoverable = new List<NascaWorkManifest>();
        if (!Directory.Exists(_workRootDirectory)) return recoverable;

        var subDirs = Directory.GetDirectories(_workRootDirectory);
        foreach (var dir in subDirs)
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath)) continue;

            try
            {
                var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                var manifest = JsonSerializer.Deserialize<NascaWorkManifest>(json);

                if (manifest != null && (manifest.CurrentLifecycle == NascaWorkLifecycle.Active || manifest.CurrentLifecycle == NascaWorkLifecycle.RecoveryRequired))
                {
                    recoverable.Add(manifest);
                }
            }
            catch { }
        }

        return recoverable;
    }

    private static async Task WriteManifestAtomicAsync(string workDirPath, NascaWorkManifest manifest, CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(workDirPath, "manifest.json");
        var tempManifestPath = Path.Combine(workDirPath, "manifest.json.tmp");

        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(tempManifestPath, json, cancellationToken);

        if (File.Exists(manifestPath))
        {
            File.Delete(manifestPath);
        }
        File.Move(tempManifestPath, manifestPath);
    }
}
