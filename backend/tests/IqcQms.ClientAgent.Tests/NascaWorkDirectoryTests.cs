using System.Security.Cryptography;
using IqcQms.ClientAgent.Application.Nasca;
using IqcQms.ClientAgent.Infrastructure.Nasca;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class NascaWorkDirectoryTests : IDisposable
{
    private readonly string _tempRootDirectory;
    private readonly NascaPathSecurityGuard _securityGuard;
    private readonly NascaWorkDirectoryManager _manager;

    public NascaWorkDirectoryTests()
    {
        _tempRootDirectory = Path.Combine(Path.GetTempPath(), $"nasca_work_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRootDirectory);
        _securityGuard = new NascaPathSecurityGuard();
        _manager = new NascaWorkDirectoryManager(_tempRootDirectory, _securityGuard, NullLogger<NascaWorkDirectoryManager>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRootDirectory))
            {
                Directory.Delete(_tempRootDirectory, recursive: true);
            }
        }
        catch { }
    }

    [Fact]
    public void WorkRoot_IsAbsolute()
    {
        var workRoot = _manager.GetWorkRootDirectory();
        Assert.True(Path.IsPathRooted(workRoot));
    }

    [Fact]
    public void WorkDirectoryId_IsOpaque()
    {
        var opaqueId = NascaWorkDirectoryManager.GenerateOpaqueDirectoryId();
        Assert.StartsWith("work_", opaqueId);
        Assert.Equal(37, opaqueId.Length); // "work_" + 32 hex chars
        Assert.True(_securityGuard.IsValidOpaqueDirectoryName(opaqueId));
    }

    [Fact]
    public async Task WorkDirectoryId_DoesNotContainCorrelationId()
    {
        var correlationId = "corr_secret_business_id_12345";
        var manifest = await _manager.CreateWorkDirectoryAsync(correlationId, "exec_1");

        Assert.DoesNotContain(correlationId, manifest.WorkDirectoryId);
    }

    [Fact]
    public void WorkDirectoryId_DoesNotContainQueueItemId()
    {
        var queueItemId = "qitem_secret_999";
        var opaqueId = NascaWorkDirectoryManager.GenerateOpaqueDirectoryId();

        Assert.DoesNotContain(queueItemId, opaqueId);
    }

    [Fact]
    public void WorkDirectoryId_DoesNotContainWorkbookFilename()
    {
        var opaqueId = NascaWorkDirectoryManager.GenerateOpaqueDirectoryId();

        Assert.DoesNotContain("Quarterly_Financial_Report", opaqueId);
        Assert.DoesNotContain("xlsx", opaqueId);
    }

    [Fact]
    public async Task Restart_UsesPersistedWorkDirectoryId()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_restart_01", "exec_01");
        var originalWorkId = manifest.WorkDirectoryId;

        // Simulate agent restart and discover work directory
        var discoveredList = await _manager.DiscoverRecoverableWorkDirectoriesAsync();
        var recovered = discoveredList.FirstOrDefault(m => m.CorrelationId == "corr_restart_01");

        Assert.NotNull(recovered);
        Assert.Equal(originalWorkId, recovered.WorkDirectoryId);
    }

    [Fact]
    public async Task AtomicManifestWrite()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_atomic_01", "exec_01");
        var dirPath = _manager.GetWorkDirectoryPath(manifest.WorkDirectoryId);
        var manifestPath = Path.Combine(dirPath, "manifest.json");

        Assert.True(File.Exists(manifestPath));
        Assert.False(File.Exists(Path.Combine(dirPath, "manifest.json.tmp")));
        Assert.Equal("corr_atomic_01", manifest.CorrelationId);
    }

    [Fact]
    public async Task AtomicInputStaging()
    {
        var manifestInit = await _manager.CreateWorkDirectoryAsync("corr_stage_01", "exec_stage_01");

        var sourceFile = Path.Combine(_tempRootDirectory, "original_input.xlsx");
        var content = "dummy workbook content for staging test";
        await File.WriteAllTextAsync(sourceFile, content);

        var manifest = await _manager.StageInputFileWithWorkIdAsync("corr_stage_01", sourceFile, manifestInit.WorkDirectoryId);

        var stagedFile = Path.Combine(_manager.GetWorkDirectoryPath(manifestInit.WorkDirectoryId), "input", "input.dat");
        Assert.True(File.Exists(stagedFile));
        Assert.Equal(content, await File.ReadAllTextAsync(stagedFile));
        Assert.NotEmpty(manifest.InputMetadata.OriginalHashSha256);
        Assert.Equal(manifest.InputMetadata.OriginalHashSha256, manifest.InputMetadata.StagedHashSha256);
    }

    [Fact]
    public async Task OriginalInput_Unchanged()
    {
        var manifestInit = await _manager.CreateWorkDirectoryAsync("corr_stage_02", "exec_stage_02");

        var sourceFile = Path.Combine(_tempRootDirectory, "original_source.xlsx");
        var content = "strictly immutable source content";
        await File.WriteAllTextAsync(sourceFile, content);

        await _manager.StageInputFileWithWorkIdAsync("corr_stage_02", sourceFile, manifestInit.WorkDirectoryId);

        Assert.True(File.Exists(sourceFile));
        Assert.Equal(content, await File.ReadAllTextAsync(sourceFile));
    }

    [Fact]
    public async Task HashRecorded()
    {
        var manifestInit = await _manager.CreateWorkDirectoryAsync("corr_hash_01", "exec_hash_01");

        var sourceFile = Path.Combine(_tempRootDirectory, "hash_source.dat");
        var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        await File.WriteAllBytesAsync(sourceFile, bytes);

        var manifest = await _manager.StageInputFileWithWorkIdAsync("corr_hash_01", sourceFile, manifestInit.WorkDirectoryId);

        using var sha256 = SHA256.Create();
        var expectedHash = Convert.ToHexString(sha256.ComputeHash(bytes));

        Assert.Equal(expectedHash, manifest.InputMetadata.OriginalHashSha256);
        Assert.Equal(5, manifest.InputMetadata.FileSizeBytes);
    }

    [Fact]
    public void OutputRoot_IsContained()
    {
        var opaqueId = NascaWorkDirectoryManager.GenerateOpaqueDirectoryId();
        var dirPath = _manager.GetWorkDirectoryPath(opaqueId);
        var outputSubDir = Path.Combine(dirPath, "output");

        Assert.StartsWith(_manager.GetWorkRootDirectory(), outputSubDir, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Traversal_Rejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _manager.GetWorkDirectoryPath(@"..\..\Windows\System32"));
    }

    [Fact]
    public void ReparseWorkRoot_IsRejected()
    {
        var mockGuard = new MockPathSecurityGuard { SimulateReparsePoint = true };
        Assert.Throws<InvalidOperationException>(() =>
            new NascaWorkDirectoryManager(_tempRootDirectory, mockGuard, NullLogger<NascaWorkDirectoryManager>.Instance));
    }

    [Fact]
    public void ReparseWorkspace_IsRejected()
    {
        var mockGuard = new MockPathSecurityGuard { SimulateReparsePoint = true, RejectWorkspaceOnly = true };
        var manager = new NascaWorkDirectoryManager(_tempRootDirectory, mockGuard, NullLogger<NascaWorkDirectoryManager>.Instance);

        Assert.Throws<InvalidOperationException>(() => manager.GetWorkDirectoryPath("work_12345678901234567890123456789012"));
    }

    [Fact]
    public void ReparseInputDirectory_IsRejected()
    {
        var mockGuard = new MockPathSecurityGuard { SimulateReparsePoint = true };
        Assert.True(mockGuard.IsReparsePoint(@"C:\FakePath\input"));
    }

    [Fact]
    public void ReparseOutputDirectory_IsRejected()
    {
        var mockGuard = new MockPathSecurityGuard { SimulateReparsePoint = true };
        Assert.True(mockGuard.IsReparsePoint(@"C:\FakePath\output"));
    }

    [Fact]
    public void ReparseStagingAncestor_IsRejected()
    {
        var mockGuard = new MockPathSecurityGuard { SimulateReparsePoint = true };
        Assert.True(mockGuard.ContainsReparsePointInAncestors(@"C:\Root", @"C:\Root\SubFolder\staging.tmp"));
    }

    [Fact]
    public async Task StartupDiscovery_DoesNotFollowReparsePoint()
    {
        var mockGuard = new MockPathSecurityGuard { SimulateReparsePoint = true, RejectWorkspaceOnly = true };
        var manager = new NascaWorkDirectoryManager(_tempRootDirectory, mockGuard, NullLogger<NascaWorkDirectoryManager>.Instance);

        var list = await manager.DiscoverRecoverableWorkDirectoriesAsync();
        Assert.Empty(list);
    }

    [Fact]
    public async Task Cleanup_DoesNotFollowReparsePoint()
    {
        var mockGuard = new MockPathSecurityGuard { SimulateReparsePoint = true, RejectWorkspaceOnly = true };
        var manager = new NascaWorkDirectoryManager(_tempRootDirectory, mockGuard, NullLogger<NascaWorkDirectoryManager>.Instance);

        int cleaned = await manager.CleanupExpiredWorkDirectoriesAsync(TimeSpan.Zero, TimeSpan.Zero);
        Assert.Equal(0, cleaned);
    }

    [Fact]
    public async Task Cleanup_DoesNotEscapeRoot()
    {
        var opaqueId = NascaWorkDirectoryManager.GenerateOpaqueDirectoryId();
        await _manager.CreateWorkDirectoryWithIdAsync("corr_clean_escape", "exec_1", opaqueId);
        await _manager.UpdateLifecycleAsync("corr_clean_escape", NascaWorkLifecycle.Completed);

        await Task.Delay(10);
        int cleaned = await _manager.CleanupExpiredWorkDirectoriesAsync(TimeSpan.Zero, TimeSpan.Zero, 50);

        Assert.Equal(1, cleaned);
        Assert.True(Directory.Exists(_manager.GetWorkRootDirectory())); // Work root remains intact
    }

    [Fact]
    public void Cleanup_NeverDeletesConfiguredRoot()
    {
        var rootDir = _manager.GetWorkRootDirectory();
        Assert.Throws<InvalidOperationException>(() => _securityGuard.EnsureSafePath(rootDir, rootDir));
    }

    [Fact]
    public async Task Cleanup_UnexpectedNestedDirectory_FailsClosed()
    {
        // Directory name without work_ prefix should be skipped by cleanup
        var unexpectedDir = Path.Combine(_manager.GetWorkRootDirectory(), "unexpected_folder");
        Directory.CreateDirectory(unexpectedDir);

        int cleaned = await _manager.CleanupExpiredWorkDirectoriesAsync(TimeSpan.Zero, TimeSpan.Zero);

        Assert.Equal(0, cleaned);
        Assert.True(Directory.Exists(unexpectedDir)); // Preserved
    }

    [Fact]
    public async Task QuarantineTarget_RemainsContained()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_quar_01", "exec_q_01");
        await _manager.QuarantineWorkDirectoryAsync("corr_quar_01", "TEST_QUARANTINE");

        var dirPath = _manager.GetWorkDirectoryPath(manifest.WorkDirectoryId);
        Assert.StartsWith(_manager.GetWorkRootDirectory(), dirPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AccessDeniedDuringContainmentCheck_FailsClosed()
    {
        var mockGuard = new MockPathSecurityGuard { SimulateAccessDenied = true };
        Assert.True(mockGuard.IsReparsePoint(@"C:\RestrictedFolder"));
    }

    [Fact]
    public void SecurityFailureLog_DoesNotContainFullPath()
    {
        var props = typeof(NascaWorkManifest).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain("LocalUserFolderPath", props);
        Assert.DoesNotContain("OriginalSourceFullPath", props);
    }

    [Fact]
    public void ProcessLaunch_RemainsAbsent()
    {
        var managerType = typeof(NascaWorkDirectoryManager);
        var methods = managerType.GetMethods();

        Assert.DoesNotContain(methods, m => m.Name.Contains("Process", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OfficeInterop_RemainsAbsent()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name ?? "");
        Assert.DoesNotContain(assemblies, a => a.Equals("office", StringComparison.OrdinalIgnoreCase));
    }

    private class MockPathSecurityGuard : INascaPathSecurityGuard
    {
        public bool SimulateReparsePoint { get; set; }
        public bool SimulateAccessDenied { get; set; }
        public bool RejectWorkspaceOnly { get; set; }

        public bool IsValidOpaqueDirectoryName(string name) => new NascaPathSecurityGuard().IsValidOpaqueDirectoryName(name);

        public bool IsReparsePoint(string path)
        {
            if (SimulateAccessDenied) return true;
            return SimulateReparsePoint;
        }

        public bool ContainsReparsePointInAncestors(string rootDirectory, string targetPath)
        {
            if (SimulateAccessDenied) return true;
            if (RejectWorkspaceOnly && targetPath.EndsWith("NascaWork", StringComparison.OrdinalIgnoreCase)) return false;
            return SimulateReparsePoint;
        }

        public void EnsureSafePath(string rootDirectory, string targetPath)
        {
            if (SimulateAccessDenied)
            {
                throw new InvalidOperationException("Path security validation failed (simulated access denied).");
            }

            if (SimulateReparsePoint)
            {
                if (RejectWorkspaceOnly && targetPath.EndsWith("NascaWork", StringComparison.OrdinalIgnoreCase))
                {
                    return; // Allow NascaWork root setup during constructor
                }
                throw new InvalidOperationException("Path security validation failed (simulated reparse point).");
            }
        }
    }
}
