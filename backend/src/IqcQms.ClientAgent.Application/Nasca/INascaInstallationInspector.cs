namespace IqcQms.ClientAgent.Application.Nasca;

public class NascaInstallationMetadata
{
    public bool FileExists { get; set; }
    public string? ProductName { get; set; }
    public string? ProductVersion { get; set; }
    public string? FileVersion { get; set; }
    public string? Publisher { get; set; }
    public bool IsAuthenticodeSigned { get; set; }
    public string? Architecture { get; set; }
    public string SanitizedReasonCode { get; set; } = "UNINSPECTED";
}

public interface INascaInstallationInspector
{
    NascaInstallationMetadata InspectPath(string executablePath);
}
