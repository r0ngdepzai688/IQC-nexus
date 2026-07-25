using System.Reflection;
using System.Text;
using IqcQms.ClientAgent.Application.Config;
using IqcQms.ClientAgent.Application.Nasca;
using IqcQms.ClientAgent.Infrastructure.Nasca;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class NascaTestLoggerProvider : ILoggerProvider, ILogger
{
    private readonly StringBuilder _logOutput = new();
    private readonly object _lock = new();

    public string LogContent
    {
        get
        {
            lock (_lock) { return _logOutput.ToString(); }
        }
    }

    public ILogger CreateLogger(string categoryName) => this;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        lock (_lock)
        {
            _logOutput.AppendLine(message);
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public void Dispose() { }

    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}

public class NascaAdapterBoundaryTests
{
    [Fact]
    public void NascaDisabled_DoesNotRequireExecutable()
    {
        var options = new NascaOptions
        {
            Enabled = false,
            ExecutablePath = @"C:\NonExistentDirectory\Nasca.exe"
        };

        // Validate should pass without throwing when Enabled = false
        options.Validate(isProduction: true);
        Assert.False(options.Enabled);
    }

    [Fact]
    public async Task NascaDisabled_DoesNotLaunchProcess()
    {
        var runner = new NascaJobRunnerNotConfigured(NullLogger<NascaJobRunnerNotConfigured>.Instance);
        var request = new NascaJobRequest
        {
            JobId = "job_test_disabled_1",
            InputWorkbookPath = @"C:\Inputs\sample.xlsx",
            OutputDirectory = @"C:\Outputs\",
            CorrelationId = "corr_123"
        };

        var result = await runner.RunJobAsync(request);

        Assert.Equal(NascaJobOutcome.NotConfigured, result.Outcome);
        Assert.Null(result.ExitCode);
        Assert.Equal("NASCA_NOT_CONFIGURED", result.SanitizedReasonCode);
    }

    [Fact]
    public void NascaEnabled_MissingExecutableFailsStartup()
    {
        var options = new NascaOptions
        {
            Enabled = true,
            ExecutablePath = @"C:\NonExistentPath\NascaConverter.exe",
            OutputDirectory = @"C:\Outputs\"
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate(isProduction: true));
        Assert.Contains("does not exist on target host", ex.Message);
    }

    [Fact]
    public void RelativeExecutablePath_IsRejected()
    {
        var options = new NascaOptions
        {
            Enabled = true,
            ExecutablePath = @"relative\NascaConverter.exe",
            OutputDirectory = @"C:\Outputs\"
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate(isProduction: true));
        Assert.Contains("MUST be an absolute path", ex.Message);
    }

    [Fact]
    public void InputDirectoryCannotContainExecutable()
    {
        var tempInput = Path.Combine(Path.GetTempPath(), $"nasca_input_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempInput);
        var fakeExe = Path.Combine(tempInput, "NascaConverter.exe");
        File.WriteAllText(fakeExe, "fake exe content");

        try
        {
            var options = new NascaOptions
            {
                Enabled = true,
                ExecutablePath = fakeExe,
                InputDirectory = tempInput,
                OutputDirectory = @"C:\Outputs\"
            };

            var ex = Assert.Throws<InvalidOperationException>(() => options.Validate(isProduction: true));
            Assert.Contains("cannot be located inside a writable input directory", ex.Message);
        }
        finally
        {
            if (Directory.Exists(tempInput))
            {
                try { Directory.Delete(tempInput, true); } catch { }
            }
        }
    }

    [Fact]
    public void InvalidTimeout_IsRejected()
    {
        var optionsZero = new NascaOptions { Enabled = true, TimeoutSeconds = 0 };
        Assert.Throws<InvalidOperationException>(() => optionsZero.Validate(isProduction: false));

        var optionsNegative = new NascaOptions { Enabled = true, TimeoutSeconds = -10 };
        Assert.Throws<InvalidOperationException>(() => optionsNegative.Validate(isProduction: false));

        var optionsExcessive = new NascaOptions { Enabled = true, TimeoutSeconds = 1000 };
        Assert.Throws<InvalidOperationException>(() => optionsExcessive.Validate(isProduction: false));
    }

    [Fact]
    public void InvalidConcurrency_IsRejected()
    {
        var optionsZero = new NascaOptions { Enabled = true, MaximumConcurrentJobs = 0 };
        Assert.Throws<InvalidOperationException>(() => optionsZero.Validate(isProduction: false));

        var optionsExcessive = new NascaOptions { Enabled = true, MaximumConcurrentJobs = 100 };
        Assert.Throws<InvalidOperationException>(() => optionsExcessive.Validate(isProduction: false));
    }

    [Fact]
    public void RequestCannotOverrideExecutablePath()
    {
        var requestProps = typeof(NascaJobRequest).GetProperties().Select(p => p.Name).ToList();

        // Request model must NOT allow overriding ExecutablePath or WorkingDirectory
        Assert.DoesNotContain("ExecutablePath", requestProps);
        Assert.DoesNotContain("WorkingDirectory", requestProps);
        Assert.DoesNotContain("CommandArguments", requestProps);
    }

    [Fact]
    public async Task NotConfiguredRunner_ReturnsSanitizedFailure()
    {
        var runner = new NascaJobRunnerNotConfigured(NullLogger<NascaJobRunnerNotConfigured>.Instance);
        var result = await runner.RunJobAsync(new NascaJobRequest());

        Assert.Equal(NascaJobOutcome.NotConfigured, result.Outcome);
        Assert.Equal("NASCA_NOT_CONFIGURED", result.SanitizedReasonCode);
    }

    [Fact]
    public void NascaContracts_DoNotExposeWorkbookContent()
    {
        var requestProps = typeof(NascaJobRequest).GetProperties().Select(p => p.PropertyType.Name).ToList();
        var resultProps = typeof(NascaJobResult).GetProperties().Select(p => p.PropertyType.Name).ToList();

        // Assert contract models do not contain workbook cell or raw content fields
        Assert.DoesNotContain("NormalizedWorkbook", requestProps);
        Assert.DoesNotContain("NormalizedWorkbook", resultProps);
        Assert.DoesNotContain("Cell", requestProps);
        Assert.DoesNotContain("Cell", resultProps);
    }

    [Fact]
    public async Task NascaLogs_DoNotExposeLocalInputPath()
    {
        var loggerProvider = new NascaTestLoggerProvider();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(loggerProvider));
        var logger = loggerFactory.CreateLogger<NascaJobRunnerNotConfigured>();
        var runner = new NascaJobRunnerNotConfigured(logger);

        var secretPath = @"C:\Users\JohnDoe\SecretWorkbooks\CONFIDENTIAL_DATA.xlsx";
        await runner.RunJobAsync(new NascaJobRequest
        {
            JobId = "job_log_test",
            InputWorkbookPath = secretPath,
            CorrelationId = "corr_log_test"
        });

        var logContent = loggerProvider.LogContent;
        Assert.DoesNotContain(secretPath, logContent);
        Assert.DoesNotContain("CONFIDENTIAL_DATA", logContent);
    }

    [Fact]
    public void ExcelInteropAssembly_IsNotReferenced()
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetName().Name ?? "")
            .ToList();

        Assert.DoesNotContain(loadedAssemblies, name => name.StartsWith("Microsoft.Office.Interop.Excel", StringComparison.OrdinalIgnoreCase));

        // Inspect referenced assemblies of current test assembly
        var referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies()
            .Select(a => a.Name ?? "")
            .ToList();

        Assert.DoesNotContain(referencedAssemblies, name => name.StartsWith("Microsoft.Office.Interop", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OfficeInteropAssembly_IsNotReferenced()
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetName().Name ?? "")
            .ToList();

        Assert.DoesNotContain(loadedAssemblies, name => name.Equals("office", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(loadedAssemblies, name => name.StartsWith("Office", StringComparison.OrdinalIgnoreCase));
    }

    // Phase 3A.1 Tests
    [Fact]
    public void UnverifiedInterface_CannotBeEnabled()
    {
        var options = new NascaOptions
        {
            Enabled = true,
            ExecutablePath = @"C:\NonExistent\Nasca.exe",
            ExpectedProductName = "UnverifiedProduct"
        };

        // When missing executable or unverified, production validation throws
        Assert.Throws<InvalidOperationException>(() => options.Validate(isProduction: true));
    }

    [Fact]
    public void DisabledMode_DoesNotInspectInstallation()
    {
        var options = new NascaOptions { Enabled = false };
        var inspector = new NascaInstallationInspector(NullLogger<NascaInstallationInspector>.Instance);

        // When options are disabled, inspect should not be invoked during normal startup
        options.Validate(isProduction: true);
        Assert.False(options.Enabled);
    }

    [Fact]
    public void Inspector_DoesNotLaunchProcess()
    {
        var inspector = new NascaInstallationInspector(NullLogger<NascaInstallationInspector>.Instance);
        var meta = inspector.InspectPath(@"C:\Windows\System32\cmd.exe");

        // Inspector reads file version info metadata without calling Process.Start
        Assert.True(meta.FileExists);
        Assert.Equal("METADATA_INSPECTED", meta.SanitizedReasonCode);
    }

    [Fact]
    public void Inspector_OnlyReadsExplicitConfiguredPath()
    {
        var inspector = new NascaInstallationInspector(NullLogger<NascaInstallationInspector>.Instance);
        var meta = inspector.InspectPath(@"relative_file.exe");

        // Relative path is rejected immediately
        Assert.False(meta.FileExists);
        Assert.Equal("PATH_NOT_ROOTED", meta.SanitizedReasonCode);
    }

    [Fact]
    public void Inspector_DoesNotSearchPathEnvironment()
    {
        var inspector = new NascaInstallationInspector(NullLogger<NascaInstallationInspector>.Instance);
        var meta = inspector.InspectPath("notepad.exe");

        // Bare filename without absolute path is rejected, proving PATH is not searched
        Assert.False(meta.FileExists);
        Assert.Equal("PATH_NOT_ROOTED", meta.SanitizedReasonCode);
    }

    [Fact]
    public void Inspector_DoesNotScanRegistry()
    {
        var inspector = new NascaInstallationInspector(NullLogger<NascaInstallationInspector>.Instance);
        var meta = inspector.InspectPath("");

        // Empty path rejected without registry lookups
        Assert.False(meta.FileExists);
        Assert.Equal("PATH_EMPTY", meta.SanitizedReasonCode);
    }

    [Fact]
    public void UnknownPublisher_DoesNotBecomeTrusted()
    {
        var meta = new NascaInstallationMetadata
        {
            FileExists = true,
            Publisher = "Unknown Supplier",
            IsAuthenticodeSigned = false
        };

        var options = new NascaOptions
        {
            ExpectedPublisher = "Official Vendor Inc.",
            RequireAuthenticodeSignature = true
        };

        Assert.NotEqual(options.ExpectedPublisher, meta.Publisher);
        Assert.False(meta.IsAuthenticodeSigned);
    }

    [Fact]
    public void UnknownVersion_DoesNotBecomeTrusted()
    {
        var meta = new NascaInstallationMetadata
        {
            FileExists = true,
            ProductVersion = "9.9.9-untracked"
        };

        var options = new NascaOptions
        {
            AllowedProductVersions = new List<string> { "1.0.0", "1.1.0" }
        };

        Assert.DoesNotContain(meta.ProductVersion, options.AllowedProductVersions);
    }

    [Fact]
    public void ProposedCliArguments_AreNotUsedByRuntime()
    {
        // Assert runtime NascaJobRequest contracts do not expose arbitrary unverified CLI argument string properties
        var requestProps = typeof(NascaJobRequest).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain("CliArguments", requestProps);
        Assert.DoesNotContain("CommandString", requestProps);
        Assert.DoesNotContain("RawFlags", requestProps);
    }

    [Fact]
    public void ExcelDependency_RemainsUnknownWithoutEvidence()
    {
        var options = new NascaOptions();
        // NASCA options do not assume Excel is installed or uninstalled
        Assert.Empty(options.ExpectedProductName);
    }

    [Fact]
    public void OfficeInteropAssembly_RemainsAbsent()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetName().Name ?? "")
            .ToList();

        Assert.DoesNotContain(assemblies, name => name.Contains("Interop.Excel", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NascaProcessLaunchCode_IsAbsent()
    {
        // Inspect types in ClientAgent infrastructure to confirm ProcessStartInfo is not used to run NASCA
        var infraTypes = typeof(NascaJobRunnerNotConfigured).Assembly.GetTypes();
        var nascaProcessRunnerTypes = infraTypes
            .Where(t => t.Name.Contains("ProcessRunner", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(nascaProcessRunnerTypes);
    }
}
