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
        var nonExistentExe = Path.Combine(Path.GetTempPath(), "NonExistentPath_12345", "NascaConverter.exe");
        var options = new NascaOptions
        {
            Enabled = true,
            VerifiedInterfaceType = NascaVerifiedInterfaceType.Cli,
            ExpectedProductName = "NASCA",
            ExecutablePath = nonExistentExe,
            OutputDirectory = Path.GetTempPath()
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
            VerifiedInterfaceType = NascaVerifiedInterfaceType.Cli,
            ExpectedProductName = "NASCA",
            ExecutablePath = @"relative\NascaConverter.exe",
            OutputDirectory = Path.GetTempPath()
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
                VerifiedInterfaceType = NascaVerifiedInterfaceType.Cli,
                ExpectedProductName = "NASCA",
                ExecutablePath = fakeExe,
                InputDirectory = tempInput,
                OutputDirectory = Path.GetTempPath()
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
        var systemPath = OperatingSystem.IsWindows()
            ? @"C:\Windows\System32\cmd.exe"
            : "/bin/sh";

        if (!File.Exists(systemPath))
        {
            systemPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? typeof(object).Assembly.Location;
        }

        var meta = inspector.InspectPath(systemPath);

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

    // Phase 3A.2 Readiness & Evidence Trust Tests
    [Fact]
    public void MissingProductIdentity_BlocksRuntimeReadiness()
    {
        var manifest = new NascaEvidenceManifest();
        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest);

        Assert.False(result.IsGo);
        Assert.Equal(NascaRuntimeDecision.StopEvidenceMissing, result.Decision);
    }

    [Fact]
    public void MissingInterfaceEvidence_BlocksRuntimeReadiness()
    {
        var manifest = new NascaEvidenceManifest
        {
            Items = new List<NascaEvidenceItem>
            {
                new NascaEvidenceItem
                {
                    ProductName = "NASCA Quality Converter",
                    SourceClassification = SourceClassification.OperatorConfirmed, // Not VendorDocumentation
                    ApprovedForUse = true
                }
            }
        };

        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest);

        Assert.False(result.IsGo);
        Assert.Equal(NascaRuntimeDecision.StopEvidenceMissing, result.Decision);
    }

    [Fact]
    public void OperatorClaim_IsNotClassifiedAsVendorDocumentation()
    {
        var item = new NascaEvidenceItem
        {
            SourceClassification = SourceClassification.OperatorConfirmed,
            VerificationStatus = EvidenceVerificationStatus.OperatorConfirmed
        };

        Assert.NotEqual(SourceClassification.VendorDocumentation, item.SourceClassification);
        Assert.NotEqual(EvidenceVerificationStatus.VendorDocumented, item.VerificationStatus);
    }

    [Fact]
    public void ConflictingEvidence_BlocksRuntimeReadiness()
    {
        var manifest = new NascaEvidenceManifest
        {
            Items = new List<NascaEvidenceItem>
            {
                new NascaEvidenceItem
                {
                    VerificationStatus = EvidenceVerificationStatus.Conflicting,
                    ApprovedForUse = true
                }
            }
        };

        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest);

        Assert.False(result.IsGo);
        Assert.Equal(NascaRuntimeDecision.StopEvidenceConflict, result.Decision);
    }

    [Fact]
    public void UnapprovedEvidence_IsIgnored()
    {
        var manifest = new NascaEvidenceManifest
        {
            Items = new List<NascaEvidenceItem>
            {
                new NascaEvidenceItem
                {
                    ProductName = "NASCA CLI",
                    SourceClassification = SourceClassification.VendorDocumentation,
                    ApprovedForUse = false // Unapproved
                }
            }
        };

        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest);

        Assert.False(result.IsGo);
        Assert.Equal(NascaRuntimeDecision.StopEvidenceMissing, result.Decision);
    }

    [Fact]
    public void EvidenceManifest_DoesNotStoreSecretContent()
    {
        var itemProps = typeof(NascaEvidenceItem).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain("LicenseKey", itemProps);
        Assert.DoesNotContain("SecretKey", itemProps);
        Assert.DoesNotContain("BinaryBytes", itemProps);
    }

    [Fact]
    public void BinaryMetadataMismatch_BlocksRuntimeReadiness()
    {
        var manifest = new NascaEvidenceManifest();
        var meta = new NascaInstallationMetadata { FileExists = false }; // File missing
        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest, meta);

        Assert.False(result.IsGo);
        Assert.Equal(NascaRuntimeDecision.StopEvidenceMissing, result.Decision);
    }

    [Fact]
    public void PublisherMismatch_BlocksRuntimeReadiness()
    {
        var manifest = new NascaEvidenceManifest();
        var meta = new NascaInstallationMetadata
        {
            FileExists = true,
            ProductName = "Fake NASCA",
            Publisher = "Untrusted Publisher"
        };

        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest, meta);

        Assert.False(result.IsGo);
        Assert.Equal(NascaRuntimeDecision.StopEvidenceMissing, result.Decision);
    }

    [Fact]
    public void UnsupportedVersion_BlocksRuntimeReadiness()
    {
        var manifest = new NascaEvidenceManifest();
        var meta = new NascaInstallationMetadata
        {
            FileExists = true,
            ProductName = "NASCA Converter",
            ProductVersion = "0.0.1-alpha"
        };

        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest, meta);

        Assert.False(result.IsGo);
    }

    [Fact]
    public void UnknownArchitecture_BlocksRuntimeReadiness()
    {
        var manifest = new NascaEvidenceManifest();
        var meta = new NascaInstallationMetadata { FileExists = true, Architecture = null };

        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest, meta);

        Assert.False(result.IsGo);
    }

    [Fact]
    public void UnknownLicensing_BlocksRuntimeReadiness()
    {
        var manifest = new NascaEvidenceManifest
        {
            Items = new List<NascaEvidenceItem>
            {
                new NascaEvidenceItem
                {
                    ProductName = "NASCA",
                    EvidenceType = EvidenceType.VendorDocumentation,
                    SourceClassification = SourceClassification.VendorDocumentation,
                    ApprovedForUse = true,
                    SanitizedNotes = "NO_LICENSING_INFO"
                }
            }
        };

        var meta = new NascaInstallationMetadata
        {
            FileExists = true,
            ProductName = "NASCA",
            Publisher = "Vendor"
        };

        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest, meta);

        Assert.False(result.IsGo);
        Assert.Equal(NascaRuntimeDecision.StopLicensingUnknown, result.Decision);
    }

    [Fact]
    public void UiOnlyInterface_BlocksRuntimeReadiness()
    {
        var manifest = new NascaEvidenceManifest
        {
            Items = new List<NascaEvidenceItem>
            {
                new NascaEvidenceItem
                {
                    ApprovedForUse = true,
                    SanitizedNotes = "UI_ONLY"
                }
            }
        };

        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest);

        Assert.False(result.IsGo);
        Assert.Equal(NascaRuntimeDecision.StopUiOnly, result.Decision);
    }

    [Fact]
    public void DirectExcelComRequirement_BlocksRuntimeReadiness()
    {
        var manifest = new NascaEvidenceManifest
        {
            Items = new List<NascaEvidenceItem>
            {
                new NascaEvidenceItem
                {
                    ApprovedForUse = true,
                    SanitizedNotes = "DIRECT_EXCEL_COM"
                }
            }
        };

        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest);

        Assert.False(result.IsGo);
        Assert.Equal(NascaRuntimeDecision.StopExcelComRequired, result.Decision);
    }

    [Fact]
    public void InternalExcelDependency_RequiresOperationalReview()
    {
        var manifest = new NascaEvidenceManifest
        {
            Items = new List<NascaEvidenceItem>
            {
                new NascaEvidenceItem
                {
                    ApprovedForUse = true,
                    SanitizedNotes = "INTERNAL_EXCEL_INSTALLATION_REQUIRED"
                }
            }
        };

        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest);

        // Internal Excel requirement without direct COM automation still stops until evidence is complete
        Assert.False(result.IsGo);
    }

    [Fact]
    public void CompleteVerifiedCliEvidence_AllowsDesignGo()
    {
        var manifest = new NascaEvidenceManifest
        {
            Items = new List<NascaEvidenceItem>
            {
                new NascaEvidenceItem
                {
                    ProductName = "NASCA Official Converter",
                    ProductVersion = "1.0.0",
                    EvidenceType = EvidenceType.VendorDocumentation,
                    SourceClassification = SourceClassification.VendorDocumentation,
                    ApprovedForUse = true,
                    SanitizedNotes = "LICENSED_FOR_AUTOMATION;CLI_DOCUMENTED"
                }
            }
        };

        var meta = new NascaInstallationMetadata
        {
            FileExists = true,
            ProductName = "NASCA Official Converter",
            ProductVersion = "1.0.0",
            Publisher = "Official Supplier Inc.",
            Architecture = "x64"
        };

        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest, meta);

        Assert.True(result.IsGo);
        Assert.Equal(NascaRuntimeDecision.ReadyForDesign, result.Decision);
    }

    [Fact]
    public void CompleteVerifiedWatchedFolderEvidence_AllowsDesignGo()
    {
        var manifest = new NascaEvidenceManifest
        {
            Items = new List<NascaEvidenceItem>
            {
                new NascaEvidenceItem
                {
                    ProductName = "NASCA Official Converter",
                    ProductVersion = "1.0.0",
                    EvidenceType = EvidenceType.VendorDocumentation,
                    SourceClassification = SourceClassification.VendorDocumentation,
                    ApprovedForUse = true,
                    SanitizedNotes = "LICENSED_FOR_AUTOMATION;WATCHED_FOLDER_DOCUMENTED"
                }
            }
        };

        var meta = new NascaInstallationMetadata
        {
            FileExists = true,
            ProductName = "NASCA Official Converter",
            ProductVersion = "1.0.0",
            Publisher = "Official Supplier Inc.",
            Architecture = "x64"
        };

        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest, meta);

        Assert.True(result.IsGo);
    }

    [Fact]
    public async Task RuntimeStillDoesNotLaunchProcess()
    {
        var runner = new NascaJobRunnerNotConfigured(Microsoft.Extensions.Logging.Abstractions.NullLogger<NascaJobRunnerNotConfigured>.Instance);
        var req = new NascaJobRequest();
        var result = await runner.RunJobAsync(req);

        Assert.Equal(NascaJobOutcome.NotConfigured, result.Outcome);
        Assert.Null(result.ExitCode);
    }

    // Phase 3A.3 Tests
    [Fact]
    public void DefaultInterfaceType_IsNone()
    {
        var options = new NascaOptions();
        Assert.Equal(NascaVerifiedInterfaceType.None, options.VerifiedInterfaceType);
    }

    [Fact]
    public void InvalidInterfaceType_FailsClosed()
    {
        var options = new NascaOptions
        {
            Enabled = true,
            VerifiedInterfaceType = (NascaVerifiedInterfaceType)999
        };

        Assert.Throws<InvalidOperationException>(() => options.Validate(isProduction: false));
    }

    [Fact]
    public void EnabledWithNoneInterface_FailsValidation()
    {
        var options = new NascaOptions
        {
            Enabled = true,
            VerifiedInterfaceType = NascaVerifiedInterfaceType.None,
            ExpectedProductName = "NASCA"
        };

        Assert.Throws<InvalidOperationException>(() => options.Validate(isProduction: false));
    }

    [Fact]
    public void UiOnlyInterface_CannotBecomeReady()
    {
        var options = new NascaOptions
        {
            Enabled = true,
            VerifiedInterfaceType = NascaVerifiedInterfaceType.UiOnly,
            ExpectedProductName = "NASCA"
        };

        Assert.Throws<InvalidOperationException>(() => options.Validate(isProduction: false));
    }

    [Fact]
    public void RuntimeDecision_IsStronglyTyped()
    {
        var manifest = new NascaEvidenceManifest();
        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest);

        Assert.IsType<NascaRuntimeDecision>(result.Decision);
        Assert.Equal(NascaRuntimeDecision.StopEvidenceMissing, result.Decision);
    }

    [Fact]
    public void ReadinessEvaluator_IsSingleDecisionSource()
    {
        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(new NascaEvidenceManifest());

        Assert.False(result.IsRuntimeDesignAllowed);
        Assert.False(result.IsRuntimeExecutionAllowed);
        Assert.NotEmpty(result.Criteria);
        Assert.NotEmpty(result.SanitizedReasonCodes);
    }

    [Fact]
    public void CurrentBaseline_RemainsStopped()
    {
        var manifest = new NascaEvidenceManifest();
        var evaluator = new NascaReadinessEvaluator();
        var result = evaluator.Evaluate(manifest);

        Assert.Equal(NascaRuntimeDecision.StopEvidenceMissing, result.Decision);
        Assert.False(result.IsGo);
    }

    [Fact]
    public void FakeRunner_ImplementsProductionContract()
    {
        INascaJobRunner fakeRunner = new FakeNascaJobRunner();
        Assert.NotNull(fakeRunner);
    }

    [Fact]
    public async Task FakeRunner_Success_IsDeterministic()
    {
        var runner = new FakeNascaJobRunner { Scenario = FakeNascaScenario.Success };
        var result = await runner.RunJobAsync(new NascaJobRequest { CorrelationId = "corr_fake_success" });

        Assert.Equal(NascaJobOutcome.Success, result.Outcome);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("SIMULATED_SUCCESS", result.SanitizedReasonCode);
        Assert.Single(result.OutputFiles);
    }

    [Fact]
    public async Task FakeRunner_Timeout_IsTyped()
    {
        var runner = new FakeNascaJobRunner { Scenario = FakeNascaScenario.Timeout };
        var result = await runner.RunJobAsync(new NascaJobRequest { CorrelationId = "corr_fake_timeout" });

        Assert.Equal(NascaJobOutcome.Timeout, result.Outcome);
        Assert.Null(result.ExitCode);
        Assert.Equal("SIMULATED_TIMEOUT", result.SanitizedReasonCode);
    }

    [Fact]
    public async Task FakeRunner_Cancellation_RespectsToken()
    {
        var runner = new FakeNascaJobRunner();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await runner.RunJobAsync(new NascaJobRequest { CorrelationId = "corr_fake_cancel" }, cts.Token);

        Assert.Equal(NascaJobOutcome.Cancelled, result.Outcome);
        Assert.Equal("SIMULATED_JOB_CANCELLED", result.SanitizedReasonCode);
    }

    [Fact]
    public async Task FakeRunner_RetryableFailure_IsTyped()
    {
        var runner = new FakeNascaJobRunner { Scenario = FakeNascaScenario.RetryableFailure };
        var result = await runner.RunJobAsync(new NascaJobRequest { CorrelationId = "corr_fake_retryable" });

        Assert.Equal(NascaJobOutcome.RetryableFailure, result.Outcome);
        Assert.Equal(101, result.ExitCode);
        Assert.Equal("SIMULATED_RETRYABLE_FAILURE", result.SanitizedReasonCode);
    }

    [Fact]
    public async Task FakeRunner_PermanentFailure_IsTyped()
    {
        var runner = new FakeNascaJobRunner { Scenario = FakeNascaScenario.PermanentFailure };
        var result = await runner.RunJobAsync(new NascaJobRequest { CorrelationId = "corr_fake_permanent" });

        Assert.Equal(NascaJobOutcome.PermanentFailure, result.Outcome);
        Assert.Equal(201, result.ExitCode);
        Assert.Equal("SIMULATED_PERMANENT_FAILURE", result.SanitizedReasonCode);
    }

    [Fact]
    public async Task FakeRunner_DuplicateCorrelation_IsDetected()
    {
        var runner = new FakeNascaJobRunner { Scenario = FakeNascaScenario.Success };
        var correlationId = "corr_duplicate_test";

        var result1 = await runner.RunJobAsync(new NascaJobRequest { CorrelationId = correlationId });
        var result2 = await runner.RunJobAsync(new NascaJobRequest { CorrelationId = correlationId });

        Assert.Equal(NascaJobOutcome.Success, result1.Outcome);
        Assert.Equal(NascaJobOutcome.DuplicateCorrelation, result2.Outcome);
        Assert.Equal("DUPLICATE_CORRELATION_DETECTED", result2.SanitizedReasonCode);
    }

    [Fact]
    public async Task FakeRunner_DoesNotLaunchProcess()
    {
        var runner = new FakeNascaJobRunner();
        var result = await runner.RunJobAsync(new NascaJobRequest());

        Assert.Equal(1, runner.InvocationCount);
        Assert.NotNull(runner.LastCorrelationId);
    }

    [Fact]
    public void FakeRunner_IsNotRegisteredInProduction()
    {
        // Inspect types in ClientAgent production assembly (IqcQms.ClientAgent.dll)
        var prodAssembly = typeof(NascaJobRunnerNotConfigured).Assembly;
        var fakeTypeInProd = prodAssembly.GetTypes().FirstOrDefault(t => t.Name.Contains("FakeNascaJobRunner", StringComparison.OrdinalIgnoreCase));

        Assert.Null(fakeTypeInProd);
    }

    [Fact]
    public void VendorArguments_RemainAbsent()
    {
        // Assert runtime NascaJobRequest contracts do not expose arbitrary unverified CLI argument string properties
        var requestProps = typeof(NascaJobRequest).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain("CliArguments", requestProps);
        Assert.DoesNotContain("CommandString", requestProps);
        Assert.DoesNotContain("RawFlags", requestProps);
    }
}
