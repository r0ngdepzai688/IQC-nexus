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
}
