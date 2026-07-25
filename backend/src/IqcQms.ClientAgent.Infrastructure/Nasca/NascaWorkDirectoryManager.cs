using System.Security.Cryptography;
using System.Text.Json;
using IqcQms.ClientAgent.Application.Nasca;
using Microsoft.Extensions.Logging;

namespace IqcQms.ClientAgent.Infrastructure.Nasca;

public class NascaWorkDirectoryManager : INascaWorkDirectoryManager
{
    private readonly string _workRootDirectory;
    private readonly INascaPathSecurityGuard _securityGuard;
    private readonly ILogger<NascaWorkDirectoryManager> _logger;

    public NascaWorkDirectoryManager(string rootDataDirectory, INascaPathSecurityGuard securityGuard, ILogger<NascaWorkDirectoryManager> logger)
    {
        _logger = logger;
        _securityGuard = securityGuard;
        _workRootDirectory = Path.Combine(rootDataDirectory, "NascaWork");

        _securityGuard.EnsureSafePath(rootDataDirectory, _workRootDirectory);
        Directory.CreateDirectory(_workRootDirectory);
    }

    public string GetWorkRootDirectory() => _workRootDirectory;

    public static string GenerateOpaqueDirectoryId()
    {
        return $"work_{Guid.NewGuid():N}".ToLowerInvariant();
    }

    public string GetWorkDirectoryPath(string workDirectoryId)
    {
        if (string.IsNullOrWhiteSpace(workDirectoryId) || !_securityGuard.IsValidOpaqueDirectoryName(workDirectoryId))
        {
            throw new InvalidOperationException("Path security validation failed: Invalid or non-opaque WorkDirectoryId.");
        }

        var dirPath = Path.Combine(_workRootDirectory, workDirectoryId);
        _securityGuard.EnsureSafePath(_workRootDirectory, dirPath);
        return dirPath;
    }

    public async Task<NascaWorkManifest> CreateWorkDirectoryAsync(string correlationId, string executionId, CancellationToken cancellationToken = default)
    {
        return await CreateWorkDirectoryWithIdAsync(correlationId, executionId, GenerateOpaqueDirectoryId(), cancellationToken);
    }

    public async Task<NascaWorkManifest> CreateWorkDirectoryWithIdAsync(string correlationId, string executionId, string workDirectoryId, CancellationToken cancellationToken = default)
    {
        var dirPath = GetWorkDirectoryPath(workDirectoryId);
        Directory.CreateDirectory(dirPath);

        var inputDir = Path.Combine(dirPath, "input");
        var outputDir = Path.Combine(dirPath, "output");
        var stateDir = Path.Combine(dirPath, "state");
        var quarantineDir = Path.Combine(dirPath, "quarantine");

        _securityGuard.EnsureSafePath(dirPath, inputDir);
        _securityGuard.EnsureSafePath(dirPath, outputDir);
        _securityGuard.EnsureSafePath(dirPath, stateDir);
        _securityGuard.EnsureSafePath(dirPath, quarantineDir);

        Directory.CreateDirectory(inputDir);
        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(stateDir);
        Directory.CreateDirectory(quarantineDir);

        var manifest = new NascaWorkManifest
        {
            SchemaVersion = 1,
            CorrelationId = correlationId,
            WorkDirectoryId = workDirectoryId,
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
        return await StageInputFileWithWorkIdAsync(correlationId, sourceFilePath, null, cancellationToken);
    }

    public async Task<NascaWorkManifest> StageInputFileWithWorkIdAsync(string correlationId, string sourceFilePath, string? workDirectoryId = null, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Source file for staging does not exist.", "[REDACTED_PATH]");
        }

        // Part 3 TOCTOU Protections: Revalidate source before open
        if (_securityGuard.IsReparsePoint(sourceFilePath))
        {
            throw new InvalidOperationException("Path security validation failed: Source file is a reparse point or symlink.");
        }

        workDirectoryId ??= GenerateOpaqueDirectoryId();
        var dirPath = GetWorkDirectoryPath(workDirectoryId);
        _securityGuard.EnsureSafePath(_workRootDirectory, dirPath);

        if (!Directory.Exists(dirPath))
        {
            await CreateWorkDirectoryWithIdAsync(correlationId, Guid.NewGuid().ToString("N"), workDirectoryId, cancellationToken);
        }

        var inputSubDir = Path.Combine(dirPath, "input");
        _securityGuard.EnsureSafePath(dirPath, inputSubDir);

        var tempStagingPath = Path.Combine(inputSubDir, $"staging_{Guid.NewGuid():N}.tmp");
        var finalStagedPath = Path.Combine(inputSubDir, "input.dat");

        // Open source with safe FileShare.Read
        using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var stagingStream = new FileStream(tempStagingPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await sourceStream.CopyToAsync(stagingStream, cancellationToken);
            await stagingStream.FlushAsync(cancellationToken);
        }

        // Revalidate staging containment
        _securityGuard.EnsureSafePath(inputSubDir, tempStagingPath);

        // Compute hashes
        string stagedHash;
        long fileSizeBytes;
        using (var hashAlg = SHA256.Create())
        using (var stagedStream = File.OpenRead(tempStagingPath))
        {
            var hashBytes = await hashAlg.ComputeHashAsync(stagedStream, cancellationToken);
            stagedHash = Convert.ToHexString(hashBytes);
            fileSizeBytes = stagedStream.Length;
        }

        string originalHash;
        using (var hashAlg = SHA256.Create())
        using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var hashBytes = await hashAlg.ComputeHashAsync(sourceStream, cancellationToken);
            originalHash = Convert.ToHexString(hashBytes);
        }

        if (!originalHash.Equals(stagedHash, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(tempStagingPath);
            await QuarantineWorkDirectoryByPathAsync(dirPath, correlationId, "STAGING_HASH_MISMATCH", cancellationToken);
            throw new InvalidOperationException("Input staging hash mismatch: source file hash does not match staged copy.");
        }

        // Atomic rename
        if (File.Exists(finalStagedPath))
        {
            File.Delete(finalStagedPath);
        }
        File.Move(tempStagingPath, finalStagedPath);

        var manifest = await GetManifestByPathAsync(dirPath, cancellationToken) ?? new NascaWorkManifest
        {
            CorrelationId = correlationId,
            WorkDirectoryId = workDirectoryId
        };

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
        // Search discovery for manifest matching correlationId
        var list = await DiscoverRecoverableWorkDirectoriesAsync(cancellationToken);
        return list.FirstOrDefault(m => m.CorrelationId == correlationId);
    }

    public async Task<NascaWorkManifest?> GetManifestByWorkIdAsync(string workDirectoryId, CancellationToken cancellationToken = default)
    {
        var dirPath = GetWorkDirectoryPath(workDirectoryId);
        return await GetManifestByPathAsync(dirPath, cancellationToken);
    }

    private async Task<NascaWorkManifest?> GetManifestByPathAsync(string dirPath, CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(dirPath, "manifest.json");
        if (!File.Exists(manifestPath)) return null;

        if (_securityGuard.IsReparsePoint(manifestPath))
        {
            await QuarantineWorkDirectoryByPathAsync(dirPath, "UNKNOWN", "REPARSE_POINT_MANIFEST", cancellationToken);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            var manifest = JsonSerializer.Deserialize<NascaWorkManifest>(json);

            if (manifest == null)
            {
                await QuarantineWorkDirectoryByPathAsync(dirPath, "UNKNOWN", "CORRUPT_MANIFEST_JSON", cancellationToken);
                return null;
            }

            return manifest;
        }
        catch (JsonException)
        {
            _logger.LogError("Corrupt JSON manifest in work directory [REDACTED_PATH]. Quarantining.");
            await QuarantineWorkDirectoryByPathAsync(dirPath, "UNKNOWN", "CORRUPT_MANIFEST_JSON", cancellationToken);
            return null;
        }
    }

    public async Task<NascaWorkManifest> UpdateLifecycleAsync(string correlationId, NascaWorkLifecycle targetLifecycle, CancellationToken cancellationToken = default)
    {
        var manifest = await GetManifestAsync(correlationId, cancellationToken)
            ?? throw new InvalidOperationException($"Cannot update lifecycle: Manifest missing for CorrelationId {correlationId}.");

        var dirPath = GetWorkDirectoryPath(manifest.WorkDirectoryId);
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
        var manifest = await GetManifestAsync(correlationId, cancellationToken);
        if (manifest != null)
        {
            var dirPath = GetWorkDirectoryPath(manifest.WorkDirectoryId);
            await QuarantineWorkDirectoryByPathAsync(dirPath, correlationId, reasonCode, cancellationToken);
        }
    }

    private async Task QuarantineWorkDirectoryByPathAsync(string dirPath, string correlationId, string reasonCode, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(dirPath)) return;

        try
        {
            _securityGuard.EnsureSafePath(_workRootDirectory, dirPath);

            var manifestPath = Path.Combine(dirPath, "manifest.json");
            if (File.Exists(manifestPath) && !_securityGuard.IsReparsePoint(manifestPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                    var manifest = JsonSerializer.Deserialize<NascaWorkManifest>(json);
                    if (manifest != null)
                    {
                        manifest.CurrentLifecycle = NascaWorkLifecycle.Quarantined;
                        manifest.RetentionCategory = "Quarantine";
                        manifest.UpdatedUtc = DateTime.UtcNow;
                        await WriteManifestAtomicAsync(dirPath, manifest, cancellationToken);
                    }
                }
                catch
                {
                    // If manifest is corrupt, do NOT rewrite corrupt JSON as authoritative. Quarantine in place.
                }
            }

            _logger.LogWarning("Quarantined work directory for CorrelationId {CorrelationId}: {ReasonCode}", correlationId, reasonCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to quarantine work directory for CorrelationId {CorrelationId}.", correlationId);
        }
    }

    public async Task<int> CleanupExpiredWorkDirectoriesAsync(TimeSpan standardRetention, TimeSpan recoveryRetention, int maxCleanupBatch = 50, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_workRootDirectory)) return 0;
        _securityGuard.EnsureSafePath(Path.GetDirectoryName(_workRootDirectory)!, _workRootDirectory);

        int cleaned = 0;
        var subDirs = Directory.GetDirectories(_workRootDirectory);
        var now = DateTime.UtcNow;

        foreach (var dirPath in subDirs)
        {
            if (cleaned >= maxCleanupBatch) break;

            var dirName = Path.GetFileName(dirPath);

            // Part 4 & 5 Cleanup Containment Rules:
            // 1. Must match opaque directory pattern work_[a-f0-9]{32}
            if (!_securityGuard.IsValidOpaqueDirectoryName(dirName))
            {
                continue;
            }

            // 2. Reject reparse points
            if (_securityGuard.IsReparsePoint(dirPath) || _securityGuard.ContainsReparsePointInAncestors(_workRootDirectory, dirPath))
            {
                _logger.LogWarning("Skipping cleanup candidate [REDACTED_PATH]: Reparse point or junction detected.");
                continue;
            }

            // 3. Never delete _workRootDirectory or parent paths
            if (dirPath.Equals(_workRootDirectory, StringComparison.OrdinalIgnoreCase) || _workRootDirectory.StartsWith(dirPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var manifestPath = Path.Combine(dirPath, "manifest.json");
            if (!File.Exists(manifestPath) || _securityGuard.IsReparsePoint(manifestPath)) continue;

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
                    // Non-recursive verification of entries before deletion
                    DeleteDirectorySafely(dirPath);
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

    private void DeleteDirectorySafely(string dirPath)
    {
        _securityGuard.EnsureSafePath(_workRootDirectory, dirPath);

        // Delete files first without following reparse points
        foreach (var file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
        {
            if (!_securityGuard.IsReparsePoint(file))
            {
                File.Delete(file);
            }
        }

        Directory.Delete(dirPath, recursive: true);
    }

    public async Task<IReadOnlyList<NascaWorkManifest>> DiscoverRecoverableWorkDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        var recoverable = new List<NascaWorkManifest>();
        if (!Directory.Exists(_workRootDirectory)) return recoverable;

        var subDirs = Directory.GetDirectories(_workRootDirectory);
        foreach (var dirPath in subDirs)
        {
            var dirName = Path.GetFileName(dirPath);

            // Part 5 Startup Discovery Containment Rules:
            // 1. Reject nonconforming opaque directory names
            if (!_securityGuard.IsValidOpaqueDirectoryName(dirName))
            {
                continue;
            }

            // 2. Reject reparse points
            if (_securityGuard.IsReparsePoint(dirPath) || _securityGuard.ContainsReparsePointInAncestors(_workRootDirectory, dirPath))
            {
                continue;
            }

            var manifestPath = Path.Combine(dirPath, "manifest.json");
            if (!File.Exists(manifestPath) || _securityGuard.IsReparsePoint(manifestPath)) continue;

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

    private async Task WriteManifestAtomicAsync(string workDirPath, NascaWorkManifest manifest, CancellationToken cancellationToken)
    {
        _securityGuard.EnsureSafePath(_workRootDirectory, workDirPath);
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
