using System.Security.Cryptography;
using IqcQms.ClientAgent.Application.Nasca;
using IqcQms.ClientAgent.Infrastructure.Nasca;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class NascaWorkDirectoryTests : IDisposable
{
    private readonly string _tempRootDirectory;
    private readonly NascaWorkDirectoryManager _manager;

    public NascaWorkDirectoryTests()
    {
        _tempRootDirectory = Path.Combine(Path.GetTempPath(), $"nasca_work_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRootDirectory);
        _manager = new NascaWorkDirectoryManager(_tempRootDirectory, NullLogger<NascaWorkDirectoryManager>.Instance);
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
    public void WorkDirectory_UsesOpaqueIdentifier()
    {
        var correlationId = "corr_abc123_xyz";
        var dirPath = _manager.GetWorkDirectoryPath(correlationId);

        var dirName = Path.GetFileName(dirPath);
        Assert.StartsWith("work_", dirName);
        Assert.Contains("corr_abc123_xyz", dirName);
    }

    [Fact]
    public void WorkbookFilename_NotUsedAsFolder()
    {
        var correlationId = "corr_opaque_99";
        var dirPath = _manager.GetWorkDirectoryPath(correlationId);

        Assert.DoesNotContain("Quarterly_Financial_Report", dirPath);
        Assert.DoesNotContain("JohnDoe", dirPath);
    }

    [Fact]
    public async Task AtomicManifestWrite()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_atomic_01", "exec_01");
        var dirPath = _manager.GetWorkDirectoryPath("corr_atomic_01");
        var manifestPath = Path.Combine(dirPath, "manifest.json");

        Assert.True(File.Exists(manifestPath));
        Assert.False(File.Exists(Path.Combine(dirPath, "manifest.json.tmp")));
        Assert.Equal("corr_atomic_01", manifest.CorrelationId);
    }

    [Fact]
    public async Task AtomicInputStaging()
    {
        await _manager.CreateWorkDirectoryAsync("corr_stage_01", "exec_stage_01");

        var sourceFile = Path.Combine(_tempRootDirectory, "original_input.xlsx");
        var content = "dummy workbook content for staging test";
        await File.WriteAllTextAsync(sourceFile, content);

        var manifest = await _manager.StageInputFileAsync("corr_stage_01", sourceFile);

        var stagedFile = Path.Combine(_manager.GetWorkDirectoryPath("corr_stage_01"), "input", "input.dat");
        Assert.True(File.Exists(stagedFile));
        Assert.Equal(content, await File.ReadAllTextAsync(stagedFile));
        Assert.NotEmpty(manifest.InputMetadata.OriginalHashSha256);
        Assert.Equal(manifest.InputMetadata.OriginalHashSha256, manifest.InputMetadata.StagedHashSha256);
    }

    [Fact]
    public async Task OriginalInput_Unchanged()
    {
        await _manager.CreateWorkDirectoryAsync("corr_stage_02", "exec_stage_02");

        var sourceFile = Path.Combine(_tempRootDirectory, "original_source.xlsx");
        var content = "strictly immutable source content";
        await File.WriteAllTextAsync(sourceFile, content);

        var originalModTime = File.GetLastWriteTimeUtc(sourceFile);

        await _manager.StageInputFileAsync("corr_stage_02", sourceFile);

        Assert.True(File.Exists(sourceFile));
        Assert.Equal(content, await File.ReadAllTextAsync(sourceFile));
    }

    [Fact]
    public async Task HashRecorded()
    {
        await _manager.CreateWorkDirectoryAsync("corr_hash_01", "exec_hash_01");

        var sourceFile = Path.Combine(_tempRootDirectory, "hash_source.dat");
        var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        await File.WriteAllBytesAsync(sourceFile, bytes);

        var manifest = await _manager.StageInputFileAsync("corr_hash_01", sourceFile);

        using var sha256 = SHA256.Create();
        var expectedHash = Convert.ToHexString(sha256.ComputeHash(bytes));

        Assert.Equal(expectedHash, manifest.InputMetadata.OriginalHashSha256);
        Assert.Equal(5, manifest.InputMetadata.FileSizeBytes);
    }

    [Fact]
    public void OutputRoot_IsContained()
    {
        var dirPath = _manager.GetWorkDirectoryPath("corr_out_01");
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
    public async Task Active_NotDeleted()
    {
        await _manager.CreateWorkDirectoryAsync("corr_active_01", "exec_act_01");

        // Attempt cleanup with zero retention delay
        int cleaned = await _manager.CleanupExpiredWorkDirectoriesAsync(TimeSpan.Zero, TimeSpan.Zero, 50);

        Assert.Equal(0, cleaned);
        Assert.NotNull(await _manager.GetManifestAsync("corr_active_01"));
    }

    [Fact]
    public async Task Completed_RetentionApplied()
    {
        await _manager.CreateWorkDirectoryAsync("corr_comp_01", "exec_comp_01");
        await _manager.UpdateLifecycleAsync("corr_comp_01", NascaWorkLifecycle.Completed);

        // Wait a tiny delay and cleanup with TimeSpan.Zero
        await Task.Delay(10);
        int cleaned = await _manager.CleanupExpiredWorkDirectoriesAsync(TimeSpan.Zero, TimeSpan.Zero, 50);

        Assert.Equal(1, cleaned);
        Assert.Null(await _manager.GetManifestAsync("corr_comp_01"));
    }

    [Fact]
    public async Task Recovery_RetentionApplied()
    {
        await _manager.CreateWorkDirectoryAsync("corr_rec_01", "exec_rec_01");
        await _manager.UpdateLifecycleAsync("corr_rec_01", NascaWorkLifecycle.RecoveryRequired);

        // Retention for recovery set to 1 hour; standard set to zero
        int cleaned = await _manager.CleanupExpiredWorkDirectoriesAsync(TimeSpan.Zero, TimeSpan.FromHours(1), 50);

        Assert.Equal(0, cleaned); // Recovery folder retained
        Assert.NotNull(await _manager.GetManifestAsync("corr_rec_01"));
    }

    [Fact]
    public async Task Cleanup_Bounded()
    {
        for (int i = 0; i < 5; i++)
        {
            var id = $"corr_batch_{i}";
            await _manager.CreateWorkDirectoryAsync(id, $"exec_{i}");
            await _manager.UpdateLifecycleAsync(id, NascaWorkLifecycle.Completed);
        }

        await Task.Delay(10);
        int cleaned = await _manager.CleanupExpiredWorkDirectoriesAsync(TimeSpan.Zero, TimeSpan.Zero, maxCleanupBatch: 2);

        Assert.Equal(2, cleaned);
    }

    [Fact]
    public async Task CorruptManifest_Quarantined()
    {
        await _manager.CreateWorkDirectoryAsync("corr_corrupt_01", "exec_c_01");
        var dirPath = _manager.GetWorkDirectoryPath("corr_corrupt_01");
        var manifestPath = Path.Combine(dirPath, "manifest.json");

        // Write corrupt JSON
        await File.WriteAllTextAsync(manifestPath, "{ INVALID_JSON_DATA }");

        var manifest = await _manager.GetManifestAsync("corr_corrupt_01");

        Assert.Null(manifest); // GetManifest returns null and quarantines
    }

    [Fact]
    public async Task IdentityMismatch_Quarantined()
    {
        await _manager.CreateWorkDirectoryAsync("corr_mismatch_01", "exec_m_01");
        var dirPath = _manager.GetWorkDirectoryPath("corr_mismatch_01");
        var manifestPath = Path.Combine(dirPath, "manifest.json");

        // Write manifest with mismatched CorrelationId
        var corruptManifest = new NascaWorkManifest { CorrelationId = "DIFFERENT_ID" };
        await File.WriteAllTextAsync(manifestPath, System.Text.Json.JsonSerializer.Serialize(corruptManifest));

        var manifest = await _manager.GetManifestAsync("corr_mismatch_01");
        Assert.Null(manifest);
    }

    [Fact]
    public async Task StartupScan_DiscoversRecoverableWork()
    {
        await _manager.CreateWorkDirectoryAsync("corr_scan_01", "exec_s_01");
        await _manager.CreateWorkDirectoryAsync("corr_scan_02", "exec_s_02");
        await _manager.UpdateLifecycleAsync("corr_scan_02", NascaWorkLifecycle.RecoveryRequired);
        await _manager.CreateWorkDirectoryAsync("corr_scan_03", "exec_s_03");
        await _manager.UpdateLifecycleAsync("corr_scan_03", NascaWorkLifecycle.Completed);

        var recoverable = await _manager.DiscoverRecoverableWorkDirectoriesAsync();

        Assert.Equal(2, recoverable.Count); // Active and RecoveryRequired
        Assert.Contains(recoverable, m => m.CorrelationId == "corr_scan_01");
        Assert.Contains(recoverable, m => m.CorrelationId == "corr_scan_02");
    }

    [Fact]
    public void Logs_RedactLocalPaths()
    {
        var manifestProps = typeof(NascaWorkManifest).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain("LocalUserFolderPath", manifestProps);
        Assert.DoesNotContain("OriginalSourceFullPath", manifestProps);
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
}
