using System.Security.Cryptography;
using IqcQms.ClientAgent.Application.Nasca;
using Microsoft.Extensions.Logging;

namespace IqcQms.ClientAgent.Infrastructure.Nasca;

public class NascaOutputValidator : INascaOutputValidator
{
    private readonly INascaWorkDirectoryManager _workDirectoryManager;
    private readonly INascaPathSecurityGuard _securityGuard;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<NascaOutputValidator> _logger;

    public NascaOutputValidator(
        INascaWorkDirectoryManager workDirectoryManager,
        INascaPathSecurityGuard securityGuard,
        ILogger<NascaOutputValidator> logger,
        TimeProvider? timeProvider = null)
    {
        _workDirectoryManager = workDirectoryManager;
        _securityGuard = securityGuard;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<NascaOutputValidationResult> ValidateOutputAsync(NascaOutputValidationRequest request, CancellationToken cancellationToken = default)
    {
        var startedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var result = new NascaOutputValidationResult { ValidationStartedUtc = startedUtc };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Validate Options
            try
            {
                if (request.Options == null)
                {
                    result.Outcome = NascaOutputValidationOutcome.UnknownFailure;
                    result.SanitizedReasonCode = "INVALID_VALIDATION_OPTIONS";
                    result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return result;
                }

                request.Options.Validate();
            }
            catch (InvalidOperationException)
            {
                result.Outcome = NascaOutputValidationOutcome.UnknownFailure;
                result.SanitizedReasonCode = "INVALID_VALIDATION_OPTIONS";
                result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                return result;
            }

            // 2. Validate Ownership & WorkDirectory Identity
            NascaWorkManifest? manifest = null;
            try
            {
                manifest = await _workDirectoryManager.GetManifestByWorkIdAsync(request.WorkDirectoryId, cancellationToken);
            }
            catch (Exception)
            {
                result.Outcome = NascaOutputValidationOutcome.CorrelationMismatch;
                result.SanitizedReasonCode = "CORRELATION_IDENTITY_MISMATCH";
                result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                return result;
            }

            if (manifest == null ||
                !string.Equals(manifest.CorrelationId, request.CorrelationId, StringComparison.Ordinal) ||
                !string.Equals(manifest.WorkDirectoryId, request.WorkDirectoryId, StringComparison.Ordinal) ||
                !string.Equals(manifest.ExecutionId, request.ExecutionId, StringComparison.Ordinal))
            {
                result.Outcome = NascaOutputValidationOutcome.CorrelationMismatch;
                result.SanitizedReasonCode = "CORRELATION_IDENTITY_MISMATCH";
                result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                return result;
            }

            var assignedWorkDir = _workDirectoryManager.GetWorkDirectoryPath(request.WorkDirectoryId);
            var expectedOutputRoot = Path.Combine(assignedWorkDir, "output");

            if (!IsOutputRootContained(request.OutputRoot, expectedOutputRoot))
            {
                result.Outcome = NascaOutputValidationOutcome.OutsideApprovedRoot;
                result.SanitizedReasonCode = "OUTPUT_ROOT_NOT_APPROVED";
                result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                return result;
            }

            // 3-6. Security-First Snapshot, Limit Validation & Stability Loop
            var validationStart = _timeProvider.GetUtcNow();
            var timeoutEnd = validationStart.Add(request.Options.ValidationTimeout);

            if (!TakeSecurityValidatedSnapshot(request, out var initialSnapshot, out var snapshotError))
            {
                return snapshotError!;
            }

            var files = initialSnapshot.Keys.OrderBy(f => f, StringComparer.Ordinal).ToList();
            var totalSizeBytes = initialSnapshot.Values.Sum(v => v.Size);

            if (request.Options.StabilityWindow > TimeSpan.Zero)
            {
                var stabilityStart = _timeProvider.GetUtcNow();
                var isStable = false;
                var currentSnapshot = initialSnapshot;

                while (true)
                {
                    var now = _timeProvider.GetUtcNow();

                    if (now >= timeoutEnd)
                    {
                        break;
                    }

                    var remainingStability = stabilityStart.Add(request.Options.StabilityWindow) - now;
                    if (remainingStability <= TimeSpan.Zero)
                    {
                        isStable = true;
                        files = currentSnapshot.Keys.OrderBy(f => f, StringComparer.Ordinal).ToList();
                        totalSizeBytes = currentSnapshot.Values.Sum(v => v.Size);
                        break;
                    }

                    var remainingTimeout = timeoutEnd - now;
                    var delay = request.Options.StabilityPollingInterval;
                    if (delay > remainingStability) delay = remainingStability;
                    if (delay > remainingTimeout) delay = remainingTimeout;

                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.Outcome = NascaOutputValidationOutcome.Cancelled;
                        result.SanitizedReasonCode = "VALIDATION_CANCELLED";
                        result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                        return result;
                    }

                    await Task.Delay(delay, _timeProvider, cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.Outcome = NascaOutputValidationOutcome.Cancelled;
                        result.SanitizedReasonCode = "VALIDATION_CANCELLED";
                        result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                        return result;
                    }

                    if (!TakeSecurityValidatedSnapshot(request, out var nextSnapshot, out var nextError))
                    {
                        return nextError!;
                    }

                    if (AreSnapshotsEqual(currentSnapshot, nextSnapshot))
                    {
                        var elapsed = _timeProvider.GetUtcNow() - stabilityStart;
                        if (elapsed >= request.Options.StabilityWindow)
                        {
                            isStable = true;
                            files = nextSnapshot.Keys.OrderBy(f => f, StringComparer.Ordinal).ToList();
                            totalSizeBytes = nextSnapshot.Values.Sum(v => v.Size);
                            break;
                        }
                    }
                    else
                    {
                        // Output modified during polling interval -> reset continuous stability window timer
                        currentSnapshot = nextSnapshot;
                        stabilityStart = _timeProvider.GetUtcNow();
                    }
                }

                if (!isStable)
                {
                    result.Outcome = NascaOutputValidationOutcome.ValidationTimedOut;
                    result.SanitizedReasonCode = "OUTPUT_STABILITY_TIMEOUT";
                    result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return result;
                }
            }

            // 7. Compute Streaming Hashes & Persist Safe Descriptors
            int fileIndex = 0;
            var descriptors = new List<NascaValidatedFileDescriptor>();

            foreach (var filePath in files.OrderBy(f => f, StringComparer.Ordinal))
            {
                _securityGuard.EnsureSafePath(request.OutputRoot, filePath);
                var fileInfo = new FileInfo(filePath);

                string fileHash;
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var sha256 = SHA256.Create())
                {
                    var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
                    fileHash = Convert.ToHexString(hashBytes);
                }

                var relPath = "output/" + Path.GetRelativePath(request.OutputRoot, filePath).Replace('\\', '/');
                descriptors.Add(new NascaValidatedFileDescriptor
                {
                    RelativePath = relPath,
                    FileSizeBytes = fileInfo.Length,
                    Sha256Hash = fileHash,
                    FileIndex = fileIndex++,
                    ValidatedUtc = _timeProvider.GetUtcNow().UtcDateTime
                });
            }

            result.Outcome = NascaOutputValidationOutcome.Valid;
            result.SanitizedReasonCode = "OUTPUT_VALIDATION_SUCCESS";
            result.ValidatedFileCount = descriptors.Count;
            result.ValidatedTotalSizeBytes = totalSizeBytes;
            result.Descriptors = descriptors;
            result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            return result;
        }
        catch (OperationCanceledException)
        {
            result.Outcome = NascaOutputValidationOutcome.Cancelled;
            result.SanitizedReasonCode = "VALIDATION_CANCELLED";
            result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            return result;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("reparse point", StringComparison.OrdinalIgnoreCase))
        {
            result.Outcome = NascaOutputValidationOutcome.ReparsePointDetected;
            result.SanitizedReasonCode = "REPARSE_POINT_DETECTED_DURING_SECURITY_CHECK";
            result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            return result;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase))
        {
            result.Outcome = NascaOutputValidationOutcome.AccessDenied;
            result.SanitizedReasonCode = "ACCESS_DENIED_DURING_SECURITY_CHECK";
            result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation error occurred for CorrelationId {CorrelationId}", request.CorrelationId);
            result.Outcome = NascaOutputValidationOutcome.UnknownFailure;
            result.SanitizedReasonCode = "UNEXPECTED_VALIDATION_ERROR";
            result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            return result;
        }
    }

    private bool TakeSecurityValidatedSnapshot(
        NascaOutputValidationRequest request,
        out Dictionary<string, (long Size, DateTime LastWrite)> snapshot,
        out NascaOutputValidationResult? errorResult)
    {
        snapshot = new Dictionary<string, (long Size, DateTime LastWrite)>(StringComparer.Ordinal);
        errorResult = null;

        if (!Directory.Exists(request.OutputRoot))
        {
            errorResult = new NascaOutputValidationResult
            {
                Outcome = NascaOutputValidationOutcome.Missing,
                SanitizedReasonCode = "OUTPUT_DIRECTORY_MISSING",
                ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
            };
            return false;
        }

        if (_securityGuard.IsReparsePoint(request.OutputRoot) ||
            _securityGuard.ContainsReparsePointInAncestors(_workDirectoryManager.GetWorkRootDirectory(), request.OutputRoot))
        {
            errorResult = new NascaOutputValidationResult
            {
                Outcome = NascaOutputValidationOutcome.ReparsePointDetected,
                SanitizedReasonCode = "REPARSE_POINT_DETECTED_IN_OUTPUT_ROOT",
                ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
            };
            return false;
        }

        var dirQueue = new Queue<(string Path, int Depth)>();
        dirQueue.Enqueue((request.OutputRoot, 0));

        var discoveredDirectoriesCount = 0;
        var discoveredFilesCount = 0;
        long totalSizeBytes = 0;

        while (dirQueue.Count > 0)
        {
            var (currentDir, currentDepth) = dirQueue.Dequeue();

            string[] immediateSubDirs;
            try
            {
                immediateSubDirs = Directory.GetDirectories(currentDir, "*", SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                errorResult = new NascaOutputValidationResult
                {
                    Outcome = NascaOutputValidationOutcome.AccessDenied,
                    SanitizedReasonCode = "ACCESS_DENIED_DURING_SECURITY_CHECK",
                    ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
                };
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                errorResult = new NascaOutputValidationResult
                {
                    Outcome = NascaOutputValidationOutcome.Missing,
                    SanitizedReasonCode = "OUTPUT_DIRECTORY_MISSING",
                    ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
                };
                return false;
            }

            Array.Sort(immediateSubDirs, StringComparer.Ordinal);

            foreach (var subDir in immediateSubDirs)
            {
                _securityGuard.EnsureSafePath(request.OutputRoot, subDir);
                if (_securityGuard.IsReparsePoint(subDir))
                {
                    errorResult = new NascaOutputValidationResult
                    {
                        Outcome = NascaOutputValidationOutcome.ReparsePointDetected,
                        SanitizedReasonCode = "REPARSE_POINT_DETECTED_IN_SUBDIRECTORY",
                        ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
                    };
                    return false;
                }

                if (discoveredDirectoriesCount + 1 > request.Options.MaximumDirectoryCount)
                {
                    errorResult = new NascaOutputValidationResult
                    {
                        Outcome = NascaOutputValidationOutcome.UnexpectedDirectory,
                        SanitizedReasonCode = "UNEXPECTED_SUBDIRECTORY_COUNT_EXCEEDED",
                        ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
                    };
                    return false;
                }

                var nextDepth = currentDepth + 1;
                if (nextDepth > request.Options.MaximumDirectoryDepth)
                {
                    errorResult = new NascaOutputValidationResult
                    {
                        Outcome = NascaOutputValidationOutcome.MaximumDepthExceeded,
                        SanitizedReasonCode = "DIRECTORY_DEPTH_EXCEEDED",
                        ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
                    };
                    return false;
                }

                discoveredDirectoriesCount++;
                dirQueue.Enqueue((subDir, nextDepth));
            }

            string[] immediateFiles;
            try
            {
                immediateFiles = Directory.GetFiles(currentDir, "*", SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                errorResult = new NascaOutputValidationResult
                {
                    Outcome = NascaOutputValidationOutcome.AccessDenied,
                    SanitizedReasonCode = "ACCESS_DENIED_DURING_SECURITY_CHECK",
                    ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
                };
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                errorResult = new NascaOutputValidationResult
                {
                    Outcome = NascaOutputValidationOutcome.Missing,
                    SanitizedReasonCode = "OUTPUT_DIRECTORY_MISSING",
                    ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
                };
                return false;
            }

            Array.Sort(immediateFiles, StringComparer.Ordinal);

            foreach (var filePath in immediateFiles)
            {
                _securityGuard.EnsureSafePath(request.OutputRoot, filePath);
                if (_securityGuard.IsReparsePoint(filePath))
                {
                    errorResult = new NascaOutputValidationResult
                    {
                        Outcome = NascaOutputValidationOutcome.ReparsePointDetected,
                        SanitizedReasonCode = "REPARSE_POINT_DETECTED_IN_OUTPUT_FILE",
                        ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
                    };
                    return false;
                }

                if (discoveredFilesCount + 1 > request.Options.MaximumFileCount)
                {
                    errorResult = new NascaOutputValidationResult
                    {
                        Outcome = NascaOutputValidationOutcome.TooManyFiles,
                        SanitizedReasonCode = "FILE_COUNT_EXCEEDED",
                        ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
                    };
                    return false;
                }

                FileInfo fileInfo;
                try
                {
                    fileInfo = new FileInfo(filePath);
                }
                catch (UnauthorizedAccessException)
                {
                    errorResult = new NascaOutputValidationResult
                    {
                        Outcome = NascaOutputValidationOutcome.AccessDenied,
                        SanitizedReasonCode = "ACCESS_DENIED_DURING_SECURITY_CHECK",
                        ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
                    };
                    return false;
                }

                if (fileInfo.Length == 0)
                {
                    errorResult = new NascaOutputValidationResult
                    {
                        Outcome = NascaOutputValidationOutcome.Empty,
                        SanitizedReasonCode = "ZERO_BYTE_OUTPUT_FILE_DETECTED",
                        ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
                    };
                    return false;
                }

                if (fileInfo.Length > request.Options.MaximumSingleFileSizeBytes)
                {
                    errorResult = new NascaOutputValidationResult
                    {
                        Outcome = NascaOutputValidationOutcome.SingleFileSizeExceeded,
                        SanitizedReasonCode = "SINGLE_FILE_SIZE_EXCEEDED",
                        ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
                    };
                    return false;
                }

                try
                {
                    totalSizeBytes = checked(totalSizeBytes + fileInfo.Length);
                }
                catch (OverflowException)
                {
                    errorResult = new NascaOutputValidationResult
                    {
                        Outcome = NascaOutputValidationOutcome.TotalSizeExceeded,
                        SanitizedReasonCode = "TOTAL_OUTPUT_SIZE_OVERFLOW",
                        ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
                    };
                    return false;
                }

                if (totalSizeBytes > request.Options.MaximumTotalOutputSizeBytes)
                {
                    errorResult = new NascaOutputValidationResult
                    {
                        Outcome = NascaOutputValidationOutcome.TotalSizeExceeded,
                        SanitizedReasonCode = "TOTAL_OUTPUT_SIZE_EXCEEDED",
                        ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
                    };
                    return false;
                }

                discoveredFilesCount++;
                snapshot[filePath] = (fileInfo.Length, fileInfo.LastWriteTimeUtc);
            }
        }

        if (discoveredFilesCount == 0)
        {
            errorResult = new NascaOutputValidationResult
            {
                Outcome = NascaOutputValidationOutcome.Empty,
                SanitizedReasonCode = "OUTPUT_DIRECTORY_EMPTY",
                ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime
            };
            return false;
        }

        return true;
    }

    private static bool AreSnapshotsEqual(Dictionary<string, (long Size, DateTime LastWrite)> s1, Dictionary<string, (long Size, DateTime LastWrite)> s2)
    {
        if (s1.Count != s2.Count) return false;
        foreach (var kvp in s1)
        {
            if (!s2.TryGetValue(kvp.Key, out var v2)) return false;
            if (kvp.Value.Size != v2.Size || kvp.Value.LastWrite != v2.LastWrite) return false;
        }
        return true;
    }

    private static bool IsOutputRootContained(string candidatePath, string approvedRoot)
    {
        if (string.IsNullOrWhiteSpace(candidatePath) || string.IsNullOrWhiteSpace(approvedRoot))
            return false;

        try
        {
            var fullCandidate = Path.GetFullPath(candidatePath);
            var fullRoot = Path.GetFullPath(approvedRoot);

            if (string.Equals(fullCandidate, fullRoot, StringComparison.OrdinalIgnoreCase))
                return true;

            var rootWithSeparator = fullRoot.EndsWith(Path.DirectorySeparatorChar)
                ? fullRoot
                : fullRoot + Path.DirectorySeparatorChar;

            return fullCandidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
