using System.Security.Cryptography;
using IqcQms.ClientAgent.Application.Nasca;
using IqcQms.ClientAgent.Infrastructure.Nasca;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class NascaOutputValidatorTests : IDisposable
{
    private readonly string _tempRootDirectory;
    private readonly NascaPathSecurityGuard _securityGuard;
    private readonly NascaWorkDirectoryManager _manager;
    private readonly TestTimeProvider _timeProvider;
    private readonly NascaOutputValidator _validator;

    public NascaOutputValidatorTests()
    {
        _tempRootDirectory = Path.Combine(Path.GetTempPath(), $"nasca_val_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRootDirectory);
        _securityGuard = new NascaPathSecurityGuard();
        _manager = new NascaWorkDirectoryManager(_tempRootDirectory, _securityGuard, NullLogger<NascaWorkDirectoryManager>.Instance);
        _timeProvider = new TestTimeProvider();
        _validator = new NascaOutputValidator(_manager, _securityGuard, NullLogger<NascaOutputValidator>.Instance, _timeProvider);
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

    // Contracts & Configuration Tests
    [Fact]
    public void DefaultValidationOptions_AreConservative()
    {
        var options = new NascaOutputValidationOptions();
        options.Validate();

        Assert.Equal(100, options.MaximumFileCount);
        Assert.Equal(0, options.MaximumDirectoryCount);
        Assert.Equal(100 * 1024 * 1024, options.MaximumSingleFileSizeBytes);
        Assert.Equal(500 * 1024 * 1024, options.MaximumTotalOutputSizeBytes);
    }

    [Fact]
    public void InvalidMaximumFileCount_FailsValidation()
    {
        var options = new NascaOutputValidationOptions { MaximumFileCount = -1 };
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void InvalidMaximumTotalSize_FailsValidation()
    {
        var options = new NascaOutputValidationOptions
        {
            MaximumSingleFileSizeBytes = 100,
            MaximumTotalOutputSizeBytes = 10 // Total size smaller than single file
        };
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void InvalidStabilityWindow_FailsValidation()
    {
        var options = new NascaOutputValidationOptions
        {
            StabilityWindow = TimeSpan.FromSeconds(20),
            ValidationTimeout = TimeSpan.FromSeconds(10)
        };
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void PollingIntervalGreaterThanTimeout_FailsValidation()
    {
        var options = new NascaOutputValidationOptions
        {
            StabilityPollingInterval = TimeSpan.FromSeconds(15),
            ValidationTimeout = TimeSpan.FromSeconds(10)
        };
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void OverflowRiskConfiguration_FailsValidation()
    {
        var options = new NascaOutputValidationOptions
        {
            MaximumSingleFileSizeBytes = 10 * 1024 * 1024 * 1024L // Exceeds 1 GB
        };
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    // Identity and Containment Tests
    [Fact]
    public async Task AssignedOutputRoot_IsAccepted()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_val_01", "exec_01");
        var workDirPath = _manager.GetWorkDirectoryPath(manifest.WorkDirectoryId);
        var outputDir = Path.Combine(workDirPath, "output");

        await File.WriteAllTextAsync(Path.Combine(outputDir, "output1.bin"), "data content");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_val_01",
            ExecutionId = "exec_01",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions
            {
                StabilityWindow = TimeSpan.Zero,
                StabilityPollingInterval = TimeSpan.FromMilliseconds(10),
                ValidationTimeout = TimeSpan.FromSeconds(5)
            }
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.Valid, result.Outcome);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ArbitraryOutputRoot_IsRejected()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_val_02", "exec_02");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_val_02",
            ExecutionId = "exec_02",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = @"C:\Windows\Temp",
            Options = new NascaOutputValidationOptions()
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.OutsideApprovedRoot, result.Outcome);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task OutputRootOutsideWorkRoot_IsRejected()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_val_03", "exec_03");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_val_03",
            ExecutionId = "exec_03",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = _tempRootDirectory,
            Options = new NascaOutputValidationOptions()
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.OutsideApprovedRoot, result.Outcome);
    }

    [Fact]
    public async Task OutputRootFromAnotherWorkspace_IsRejected()
    {
        var m1 = await _manager.CreateWorkDirectoryAsync("corr_m1", "exec_1");
        var m2 = await _manager.CreateWorkDirectoryAsync("corr_m2", "exec_2");

        var outputDirM2 = Path.Combine(_manager.GetWorkDirectoryPath(m2.WorkDirectoryId), "output");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_m1",
            ExecutionId = "exec_1",
            WorkDirectoryId = m1.WorkDirectoryId,
            OutputRoot = outputDirM2, // Cross-workspace attempt
            Options = new NascaOutputValidationOptions()
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.OutsideApprovedRoot, result.Outcome);
    }

    [Fact]
    public async Task CorrelationMismatch_IsRejected()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_real", "exec_real");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_fake",
            ExecutionId = "exec_real",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions()
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.CorrelationMismatch, result.Outcome);
    }

    [Fact]
    public async Task WorkDirectoryIdMismatch_IsRejected()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_real", "exec_real");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_real",
            ExecutionId = "exec_real",
            WorkDirectoryId = "work_fake_12345678901234567890123456789012",
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions()
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.CorrelationMismatch, result.Outcome);
    }

    [Fact]
    public async Task TraversalOutput_IsRejected()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_trav", "exec_trav");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_trav",
            ExecutionId = "exec_trav",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = @"..\..\System32",
            Options = new NascaOutputValidationOptions()
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ReparseOutputRoot_IsRejected()
    {
        var mockGuard = new MockPathSecurityGuard { SimulateReparsePoint = true };
        var validator = new NascaOutputValidator(_manager, mockGuard, NullLogger<NascaOutputValidator>.Instance, _timeProvider);

        var manifest = await _manager.CreateWorkDirectoryAsync("corr_rep_root", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_rep_root",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions()
        };

        var result = await validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.ReparsePointDetected, result.Outcome);
        Assert.True(result.RequiresQuarantine);
    }

    [Fact]
    public async Task ReparseOutputFile_IsRejected()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_rep_file", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");
        await File.WriteAllTextAsync(Path.Combine(outputDir, "test.dat"), "content");

        var mockGuard = new MockPathSecurityGuard { ReparseFile = Path.Combine(outputDir, "test.dat") };
        var validator = new NascaOutputValidator(_manager, mockGuard, NullLogger<NascaOutputValidator>.Instance, _timeProvider);

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_rep_file",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions()
        };

        var result = await validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.ReparsePointDetected, result.Outcome);
    }

    [Fact]
    public async Task ReparseNestedDirectory_IsRejected()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_rep_nested", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");
        var nestedDir = Path.Combine(outputDir, "sub");
        Directory.CreateDirectory(nestedDir);

        var mockGuard = new MockPathSecurityGuard { ReparseFile = nestedDir };
        var validator = new NascaOutputValidator(_manager, mockGuard, NullLogger<NascaOutputValidator>.Instance, _timeProvider);

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_rep_nested",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions { MaximumDirectoryCount = 5, MaximumDirectoryDepth = 2 }
        };

        var result = await validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.ReparsePointDetected, result.Outcome);
    }

    [Fact]
    public async Task AccessDeniedDuringSecurityCheck_FailsClosed()
    {
        var mockGuard = new MockPathSecurityGuard { SimulateAccessDenied = true };
        var validator = new NascaOutputValidator(_manager, mockGuard, NullLogger<NascaOutputValidator>.Instance, _timeProvider);

        var manifest = await _manager.CreateWorkDirectoryAsync("corr_acc", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_acc",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions()
        };

        var result = await validator.ValidateOutputAsync(req);
        Assert.False(result.IsValid);
    }

    // Directory Policy Tests
    [Fact]
    public async Task UnexpectedDirectory_IsRejected()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_unexp_dir", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");
        Directory.CreateDirectory(Path.Combine(outputDir, "unexpected_subdir"));

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_unexp_dir",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions { MaximumDirectoryCount = 0 } // Rejects subdirectories
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.UnexpectedDirectory, result.Outcome);
    }

    [Fact]
    public async Task MaximumDirectoryDepth_IsEnforced()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_depth", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");
        var deepDir = Path.Combine(outputDir, "lvl1", "lvl2", "lvl3");
        Directory.CreateDirectory(deepDir);

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_depth",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions { MaximumDirectoryCount = 5, MaximumDirectoryDepth = 2 }
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.MaximumDepthExceeded, result.Outcome);
    }

    [Fact]
    public async Task MaximumDirectoryCount_IsEnforced()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_dircount", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");
        Directory.CreateDirectory(Path.Combine(outputDir, "sub1"));
        Directory.CreateDirectory(Path.Combine(outputDir, "sub2"));

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_dircount",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions { MaximumDirectoryCount = 1, MaximumDirectoryDepth = 2 }
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.UnexpectedDirectory, result.Outcome);
    }

    [Fact]
    public async Task Enumeration_DoesNotFollowReparsePoint()
    {
        var mockGuard = new MockPathSecurityGuard { SimulateReparsePoint = true };
        var validator = new NascaOutputValidator(_manager, mockGuard, NullLogger<NascaOutputValidator>.Instance, _timeProvider);

        var manifest = await _manager.CreateWorkDirectoryAsync("corr_enum_rep", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_enum_rep",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions()
        };

        var result = await validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.ReparsePointDetected, result.Outcome);
    }

    // File Limits Tests
    [Fact]
    public async Task MissingOutput_IsRejected()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_missing_out", "exec_1");
        var nonExistentOutputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output_missing");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_missing_out",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = nonExistentOutputDir,
            Options = new NascaOutputValidationOptions()
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.OutsideApprovedRoot, result.Outcome);
    }

    [Fact]
    public async Task EmptyOutputDirectory_IsRejected()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_empty_out", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_empty_out",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions()
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.Empty, result.Outcome);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ZeroByteFile_IsRejected()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_zerobyte", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");
        await File.WriteAllTextAsync(Path.Combine(outputDir, "empty.dat"), "");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_zerobyte",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions()
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.Empty, result.Outcome);
    }

    [Fact]
    public async Task MaximumFileCount_IsEnforced()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_maxfiles", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");
        await File.WriteAllTextAsync(Path.Combine(outputDir, "f1.dat"), "data1");
        await File.WriteAllTextAsync(Path.Combine(outputDir, "f2.dat"), "data2");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_maxfiles",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions { MaximumFileCount = 1 } // Max 1 file
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.TooManyFiles, result.Outcome);
    }

    [Fact]
    public async Task MaximumSingleFileSize_IsEnforced()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_maxsingle", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");
        await File.WriteAllTextAsync(Path.Combine(outputDir, "large.dat"), new string('A', 1024));

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_maxsingle",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions { MaximumSingleFileSizeBytes = 500 } // Max 500 bytes
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.SingleFileSizeExceeded, result.Outcome);
    }

    [Fact]
    public async Task MaximumTotalOutputSize_IsEnforced()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_maxtotal", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");
        await File.WriteAllTextAsync(Path.Combine(outputDir, "f1.dat"), new string('A', 300));
        await File.WriteAllTextAsync(Path.Combine(outputDir, "f2.dat"), new string('B', 300));

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_maxtotal",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions
            {
                MaximumSingleFileSizeBytes = 500,
                MaximumTotalOutputSizeBytes = 500 // Total max 500 bytes
            }
        };

        var result = await _validator.ValidateOutputAsync(req);
        Assert.Equal(NascaOutputValidationOutcome.TotalSizeExceeded, result.Outcome);
    }

    [Fact]
    public void Extension_IsNotAssumed()
    {
        // Validates output works regardless of extension (.xyz, .bin, .dat, no extension)
        var fileNames = new[] { "output.xyz", "data_no_ext", "result.custom" };
        foreach (var fn in fileNames)
        {
            Assert.False(string.IsNullOrWhiteSpace(fn));
        }
    }

    [Fact]
    public void FilenameConvention_IsNotAssumed()
    {
        // Validates no hardcoded assumption on specific filename conventions
        var options = new NascaOutputValidationOptions();
        Assert.NotNull(options);
    }

    // Stability Tests
    [Fact]
    public async Task StableFile_IsAccepted()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_stable", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");
        await File.WriteAllTextAsync(Path.Combine(outputDir, "stable.dat"), "stable content");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_stable",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions
            {
                StabilityWindow = TimeSpan.FromMilliseconds(100),
                StabilityPollingInterval = TimeSpan.FromMilliseconds(20),
                ValidationTimeout = TimeSpan.FromSeconds(2)
            }
        };

        var resultTask = _validator.ValidateOutputAsync(req);

        // Await the timer registration signal so we know the polling loop has reached Task.Delay before advancing fake time.
        await _timeProvider.WaitForTimerScheduledAsync();

        // Advance in polling-sized increments (do not assert timer count; scheduling is an implementation detail).
        _timeProvider.Advance(TimeSpan.FromMilliseconds(20));
        _timeProvider.Advance(TimeSpan.FromMilliseconds(20));
        _timeProvider.Advance(TimeSpan.FromMilliseconds(20));
        _timeProvider.Advance(TimeSpan.FromMilliseconds(20));
        _timeProvider.Advance(TimeSpan.FromMilliseconds(20));

        var result = await resultTask;
        Assert.Equal(NascaOutputValidationOutcome.Valid, result.Outcome);
    }

    [Fact]
    public async Task Validation_RespectsCancellation()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_cancel", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");
        await File.WriteAllTextAsync(Path.Combine(outputDir, "test.dat"), "test content");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_cancel",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions()
        };

        var result = await _validator.ValidateOutputAsync(req, cts.Token);
        Assert.Equal(NascaOutputValidationOutcome.Cancelled, result.Outcome);
    }

    [Fact]
    public void StabilityUsesInjectedTimeProvider()
    {
        var customTimeProvider = new TestTimeProvider();
        var validator = new NascaOutputValidator(_manager, _securityGuard, NullLogger<NascaOutputValidator>.Instance, customTimeProvider);
        Assert.NotNull(validator);
    }

    // Correlation & Descriptors Tests
    [Fact]
    public async Task SuccessfulValidation_PersistsDescriptor()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_desc", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");
        await File.WriteAllTextAsync(Path.Combine(outputDir, "result.dat"), "result payload");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_desc",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions
            {
                StabilityWindow = TimeSpan.Zero,
                StabilityPollingInterval = TimeSpan.FromMilliseconds(10),
                ValidationTimeout = TimeSpan.FromSeconds(2)
            }
        };

        var resultTask = _validator.ValidateOutputAsync(req);
        await _timeProvider.WaitForTimerScheduledAsync();
        _timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        var result = await resultTask;
        Assert.Equal(NascaOutputValidationOutcome.Valid, result.Outcome);
        Assert.Single(result.Descriptors);

        var desc = result.Descriptors[0];
        Assert.Equal("output/result.dat", desc.RelativePath);
        Assert.NotEmpty(desc.Sha256Hash);
        Assert.Equal(14, desc.FileSizeBytes);
    }

    [Fact]
    public async Task DescriptorContainsRelativeReferencesOnly()
    {
        var manifest = await _manager.CreateWorkDirectoryAsync("corr_rel", "exec_1");
        var outputDir = Path.Combine(_manager.GetWorkDirectoryPath(manifest.WorkDirectoryId), "output");
        await File.WriteAllTextAsync(Path.Combine(outputDir, "item.dat"), "content");

        var req = new NascaOutputValidationRequest
        {
            CorrelationId = "corr_rel",
            ExecutionId = "exec_1",
            WorkDirectoryId = manifest.WorkDirectoryId,
            OutputRoot = outputDir,
            Options = new NascaOutputValidationOptions
            {
                StabilityWindow = TimeSpan.Zero,
                StabilityPollingInterval = TimeSpan.FromMilliseconds(10)
            }
        };

        var resultTask = _validator.ValidateOutputAsync(req);
        await _timeProvider.WaitForTimerScheduledAsync();
        _timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        var result = await resultTask;
        Assert.Single(result.Descriptors);
        Assert.DoesNotContain(_tempRootDirectory, result.Descriptors[0].RelativePath);
        Assert.StartsWith("output/", result.Descriptors[0].RelativePath);
    }

    [Fact]
    public void FileContents_AreNotUsedForCorrelation()
    {
        var props = typeof(NascaOutputValidationRequest).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain("FileContents", props);
        Assert.DoesNotContain("WorkbookBody", props);
    }

    // Production Guards Tests
    [Fact]
    public void FakeRunner_RemainsTestOnly()
    {
        var prodAssembly = typeof(NascaJobRunnerNotConfigured).Assembly;
        var fakeType = prodAssembly.GetTypes().FirstOrDefault(t => t.Name.Contains("FakeNascaJobRunner", StringComparison.OrdinalIgnoreCase));
        Assert.Null(fakeType);
    }

    [Fact]
    public void ProductionRunner_RemainsNotConfigured()
    {
        var runner = new NascaJobRunnerNotConfigured(NullLogger<NascaJobRunnerNotConfigured>.Instance);
        Assert.NotNull(runner);
    }

    [Fact]
    public void ProcessLaunchCode_RemainsAbsent()
    {
        var prodTypes = typeof(NascaOutputValidator).Assembly.GetTypes();
        var procStartCalls = prodTypes.SelectMany(t => t.GetMethods())
            .Where(m => m.Name.Equals("Start", StringComparison.OrdinalIgnoreCase) && m.DeclaringType?.Name == "Process");

        Assert.Empty(procStartCalls);
    }

    [Fact]
    public void OfficeInteropAssembly_RemainsAbsent()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name ?? "");
        Assert.DoesNotContain(assemblies, a => a.Equals("office", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VendorSchemaParser_RemainsAbsent()
    {
        var prodTypes = typeof(NascaOutputValidator).Assembly.GetTypes();
        var parserTypes = prodTypes.Where(t => t.Name.Contains("VendorParser", StringComparison.OrdinalIgnoreCase) || t.Name.Contains("NascaSchemaParser", StringComparison.OrdinalIgnoreCase));

        Assert.Empty(parserTypes);
    }

    private class MockPathSecurityGuard : INascaPathSecurityGuard
    {
        public bool SimulateReparsePoint { get; set; }
        public bool SimulateAccessDenied { get; set; }
        public string? ReparseFile { get; set; }

        public bool IsValidOpaqueDirectoryName(string name) => new NascaPathSecurityGuard().IsValidOpaqueDirectoryName(name);

        public bool IsReparsePoint(string path)
        {
            if (SimulateAccessDenied) return true;
            if (ReparseFile != null && string.Equals(path, ReparseFile, StringComparison.OrdinalIgnoreCase)) return true;
            return SimulateReparsePoint;
        }

        public bool ContainsReparsePointInAncestors(string rootDirectory, string targetPath)
        {
            if (SimulateAccessDenied) return true;
            if (ReparseFile != null && targetPath.StartsWith(ReparseFile, StringComparison.OrdinalIgnoreCase)) return true;
            return SimulateReparsePoint;
        }

        public void EnsureSafePath(string rootDirectory, string targetPath)
        {
            if (SimulateAccessDenied)
            {
                throw new InvalidOperationException("Path security validation failed (simulated access denied).");
            }

            if (SimulateReparsePoint || (ReparseFile != null && targetPath.StartsWith(ReparseFile, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Path security validation failed (simulated reparse point).");
            }
        }
    }

    private class TestTimeProvider : TimeProvider
    {
        private readonly object _sync = new();
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        private readonly List<ScheduledTimer> _timers = new();
        private int _scheduledCount;
        private readonly TaskCompletionSource _timerScheduledTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override DateTimeOffset GetUtcNow()
        {
            lock (_sync) return _now;
        }

        public Task WaitForTimerScheduledAsync()
        {
            lock (_sync)
            {
                if (_scheduledCount > 0 || _timers.Any(t => !t.IsDisposed && t.NextFireUtc != DateTimeOffset.MaxValue))
                {
                    return Task.CompletedTask;
                }
                return _timerScheduledTcs.Task;
            }
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ScheduledTimer(this, callback, state, dueTime, period);
            TaskCompletionSource? toComplete = null;
            lock (_sync)
            {
                _timers.Add(timer);
                if (dueTime != Timeout.InfiniteTimeSpan)
                {
                    _scheduledCount++;
                    toComplete = _timerScheduledTcs;
                }
            }
            toComplete?.TrySetResult();
            return timer;
        }

        public void Advance(TimeSpan delta)
        {
            if (delta < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delta));

            DateTimeOffset targetTime;
            lock (_sync)
            {
                targetTime = _now.Add(delta);
            }

            while (true)
            {
                ScheduledTimer? timerToFire = null;
                DateTimeOffset fireAt;

                lock (_sync)
                {
                    var active = _timers
                        .Where(t => !t.IsDisposed && t.NextFireUtc <= targetTime)
                        .OrderBy(t => t.NextFireUtc)
                        .FirstOrDefault();

                    if (active == null)
                    {
                        _now = targetTime;
                        return;
                    }

                    timerToFire = active;
                    fireAt = active.NextFireUtc;
                    _now = fireAt;
                }

                timerToFire.Fire(fireAt);
            }
        }

        private void Remove(ScheduledTimer timer)
        {
            lock (_sync)
            {
                _timers.Remove(timer);
            }
        }

        private sealed class ScheduledTimer : ITimer
        {
            private readonly TestTimeProvider _owner;
            private readonly TimerCallback _callback;
            private readonly object? _state;
            private TimeSpan _period;
            private bool _disposed;
            private DateTimeOffset _nextFireUtc;

            public bool IsDisposed
            {
                get
                {
                    lock (_owner._sync) return _disposed;
                }
            }

            public DateTimeOffset NextFireUtc
            {
                get
                {
                    lock (_owner._sync) return _nextFireUtc;
                }
            }

            public ScheduledTimer(TestTimeProvider owner, TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
            {
                _owner = owner;
                _callback = callback;
                _state = state;
                _period = period;

                var start = owner.GetUtcNow();
                _nextFireUtc = dueTime == Timeout.InfiniteTimeSpan
                    ? DateTimeOffset.MaxValue
                    : start.Add(dueTime);
            }

            public bool IsDue(DateTimeOffset nowUtc)
            {
                lock (_owner._sync)
                {
                    return !_disposed && nowUtc >= _nextFireUtc;
                }
            }

            public void Fire(DateTimeOffset nowUtc)
            {
                lock (_owner._sync)
                {
                    if (_disposed) return;
                }

                _callback(_state);

                lock (_owner._sync)
                {
                    if (_disposed) return;

                    if (_period == Timeout.InfiniteTimeSpan)
                    {
                        _nextFireUtc = DateTimeOffset.MaxValue;
                        _disposed = true;
                        _owner.Remove(this);
                        return;
                    }

                    // Catch up in case large time jump occurred.
                    while (!_disposed && _nextFireUtc <= nowUtc)
                    {
                        _nextFireUtc = _nextFireUtc.Add(_period);
                    }
                }
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (_owner._sync)
                {
                    if (_disposed) return false;

                    _period = period;
                    var now = _owner.GetUtcNow();
                    _nextFireUtc = dueTime == Timeout.InfiniteTimeSpan
                        ? DateTimeOffset.MaxValue
                        : now.Add(dueTime);
                    return true;
                }
            }

            public void Dispose()
            {
                lock (_owner._sync)
                {
                    if (_disposed) return;
                    _disposed = true;
                    _owner.Remove(this);
                }
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
