using System.Diagnostics;
using IqcQms.ClientAgent.Application.Nasca;
using Microsoft.Extensions.Logging;

namespace IqcQms.ClientAgent.Infrastructure.Nasca;

public class NascaInstallationInspector : INascaInstallationInspector
{
    private readonly ILogger<NascaInstallationInspector> _logger;

    public NascaInstallationInspector(ILogger<NascaInstallationInspector> logger)
    {
        _logger = logger;
    }

    public NascaInstallationMetadata InspectPath(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            _logger.LogWarning("Installation inspection requested with empty executable path.");
            return new NascaInstallationMetadata
            {
                FileExists = false,
                SanitizedReasonCode = "PATH_EMPTY"
            };
        }

        if (!Path.IsPathRooted(executablePath))
        {
            _logger.LogWarning("Installation inspection rejected relative path.");
            return new NascaInstallationMetadata
            {
                FileExists = false,
                SanitizedReasonCode = "PATH_NOT_ROOTED"
            };
        }

        if (!File.Exists(executablePath))
        {
            _logger.LogWarning("Executable file does not exist at configured path.");
            return new NascaInstallationMetadata
            {
                FileExists = false,
                SanitizedReasonCode = "FILE_NOT_FOUND"
            };
        }

        try
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
            _logger.LogInformation("Inspected executable metadata successfully. Product: {ProductName}, Version: {ProductVersion}",
                versionInfo.ProductName ?? "Unknown", versionInfo.ProductVersion ?? "Unknown");

            return new NascaInstallationMetadata
            {
                FileExists = true,
                ProductName = versionInfo.ProductName,
                ProductVersion = versionInfo.ProductVersion,
                FileVersion = versionInfo.FileVersion,
                Publisher = versionInfo.CompanyName,
                IsAuthenticodeSigned = false, // Placeholder until Authenticode verification is configured
                Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86",
                SanitizedReasonCode = "METADATA_INSPECTED"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read file version metadata from configured executable path.");
            return new NascaInstallationMetadata
            {
                FileExists = true,
                SanitizedReasonCode = "METADATA_READ_ERROR"
            };
        }
    }
}
