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

            if (manifest == null || manifest.CorrelationId != request.CorrelationId)
            {
                result.Outcome = NascaOutputValidationOutcome.CorrelationMismatch;
                result.SanitizedReasonCode = "CORRELATION_IDENTITY_MISMATCH";
                result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                return result;
            }

            var assignedWorkDir = _workDirectoryManager.GetWorkDirectoryPath(request.WorkDirectoryId);
            var expectedOutputRoot = Path.Combine(assignedWorkDir, "output");

            if (!string.Equals(Path.GetFullPath(request.OutputRoot), Path.GetFullPath(expectedOutputRoot), StringComparison.OrdinalIgnoreCase))
            {
                result.Outcome = NascaOutputValidationOutcome.OutsideApprovedRoot;
                result.SanitizedReasonCode = "OUTPUT_ROOT_NOT_APPROVED";
                result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                return result;
            }

            // 3. Check Directory Existence & Reparse Points
            if (!Directory.Exists(request.OutputRoot))
            {
                result.Outcome = NascaOutputValidationOutcome.Missing;
                result.SanitizedReasonCode = "OUTPUT_DIRECTORY_MISSING";
                result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                return result;
            }

            if (_securityGuard.IsReparsePoint(request.OutputRoot) ||
                _securityGuard.ContainsReparsePointInAncestors(_workDirectoryManager.GetWorkRootDirectory(), request.OutputRoot))
            {
                result.Outcome = NascaOutputValidationOutcome.ReparsePointDetected;
                result.SanitizedReasonCode = "REPARSE_POINT_DETECTED_IN_OUTPUT_ROOT";
                result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                return result;
            }

            // 4. Directory Structure Policy Checks
            var subDirectories = Directory.GetDirectories(request.OutputRoot, "*", SearchOption.AllDirectories);
            if (subDirectories.Length > request.Options.MaximumDirectoryCount)
            {
                result.Outcome = NascaOutputValidationOutcome.UnexpectedDirectory;
                result.SanitizedReasonCode = "UNEXPECTED_SUBDIRECTORY_COUNT_EXCEEDED";
                result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                return result;
            }

            foreach (var subDir in subDirectories)
            {
                _securityGuard.EnsureSafePath(request.OutputRoot, subDir);
                if (_securityGuard.IsReparsePoint(subDir))
                {
                    result.Outcome = NascaOutputValidationOutcome.ReparsePointDetected;
                    result.SanitizedReasonCode = "REPARSE_POINT_DETECTED_IN_SUBDIRECTORY";
                    result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return result;
                }

                var relSubDir = Path.GetRelativePath(request.OutputRoot, subDir);
                var depth = relSubDir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length;
                if (depth > request.Options.MaximumDirectoryDepth)
                {
                    result.Outcome = NascaOutputValidationOutcome.MaximumDepthExceeded;
                    result.SanitizedReasonCode = "DIRECTORY_DEPTH_EXCEEDED";
                    result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return result;
                }
            }

            // 5. File Enumeration & Limit Checks
            var files = Directory.GetFiles(request.OutputRoot, "*", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                result.Outcome = NascaOutputValidationOutcome.Empty;
                result.SanitizedReasonCode = "OUTPUT_DIRECTORY_EMPTY";
                result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                return result;
            }

            if (files.Length > request.Options.MaximumFileCount)
            {
                result.Outcome = NascaOutputValidationOutcome.TooManyFiles;
                result.SanitizedReasonCode = "FILE_COUNT_EXCEEDED";
                result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                return result;
            }

            long totalSizeBytes = 0;
            foreach (var filePath in files)
            {
                _securityGuard.EnsureSafePath(request.OutputRoot, filePath);
                if (_securityGuard.IsReparsePoint(filePath))
                {
                    result.Outcome = NascaOutputValidationOutcome.ReparsePointDetected;
                    result.SanitizedReasonCode = "REPARSE_POINT_DETECTED_IN_OUTPUT_FILE";
                    result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return result;
                }

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    result.Outcome = NascaOutputValidationOutcome.Empty;
                    result.SanitizedReasonCode = "ZERO_BYTE_OUTPUT_FILE_DETECTED";
                    result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return result;
                }

                if (fileInfo.Length > request.Options.MaximumSingleFileSizeBytes)
                {
                    result.Outcome = NascaOutputValidationOutcome.SingleFileSizeExceeded;
                    result.SanitizedReasonCode = "SINGLE_FILE_SIZE_EXCEEDED";
                    result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return result;
                }

                try
                {
                    totalSizeBytes = checked(totalSizeBytes + fileInfo.Length);
                }
                catch (OverflowException)
                {
                    result.Outcome = NascaOutputValidationOutcome.TotalSizeExceeded;
                    result.SanitizedReasonCode = "TOTAL_OUTPUT_SIZE_OVERFLOW";
                    result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return result;
                }

                if (totalSizeBytes > request.Options.MaximumTotalOutputSizeBytes)
                {
                    result.Outcome = NascaOutputValidationOutcome.TotalSizeExceeded;
                    result.SanitizedReasonCode = "TOTAL_OUTPUT_SIZE_EXCEEDED";
                    result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return result;
                }
            }

            // 6. Stability Validation Loop
            var stabilityStart = _timeProvider.GetUtcNow();
            var timeoutEnd = stabilityStart.Add(request.Options.ValidationTimeout);
            var isStable = false;

            while (_timeProvider.GetUtcNow() < timeoutEnd)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.Outcome = NascaOutputValidationOutcome.Cancelled;
                    result.SanitizedReasonCode = "VALIDATION_CANCELLED";
                    result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return result;
                }

                // Initial Snapshot
                var snapshot1 = GetFileSnapshots(request.OutputRoot);
                await Task.Delay(request.Options.StabilityPollingInterval, _timeProvider, cancellationToken);
                var snapshot2 = GetFileSnapshots(request.OutputRoot);

                if (AreSnapshotsEqual(snapshot1, snapshot2))
                {
                    var elapsed = _timeProvider.GetUtcNow() - stabilityStart;
                    if (elapsed >= request.Options.StabilityWindow)
                    {
                        isStable = true;
                        break;
                    }
                }
                else
                {
                    // Files modified/changed during polling interval
                    stabilityStart = _timeProvider.GetUtcNow(); // Reset stability timer
                }
            }

            if (!isStable)
            {
                result.Outcome = NascaOutputValidationOutcome.ValidationTimedOut;
                result.SanitizedReasonCode = "OUTPUT_STABILITY_TIMEOUT";
                result.ValidationCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                return result;
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

    private static Dictionary<string, (long Size, DateTime LastWrite)> GetFileSnapshots(string dirPath)
    {
        var dict = new Dictionary<string, (long, DateTime)>();
        if (!Directory.Exists(dirPath)) return dict;

        foreach (var file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
        {
            var info = new FileInfo(file);
            dict[file] = (info.Length, info.LastWriteTimeUtc);
        }
        return dict;
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
}
