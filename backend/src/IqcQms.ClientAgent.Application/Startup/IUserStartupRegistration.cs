namespace IqcQms.ClientAgent.Application.Startup;

public enum UserStartupState
{
    NotRegistered,
    Enabled,
    StaleExecutablePath,
    Disabled
}

public class UserStartupRegistrationInfo
{
    public string ProfileName { get; set; } = string.Empty;
    public string KeyName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string QuotedCommandLine { get; set; } = string.Empty;
    public UserStartupState State { get; set; } = UserStartupState.NotRegistered;
}

public interface IUserStartupRegistration
{
    UserStartupRegistrationInfo DescribeRegistrationInfo(string profileName, string executablePath);
    UserStartupState CheckStatus(string profileName, string executablePath);
    Task EnableAsync(string profileName, string executablePath, CancellationToken cancellationToken = default);
    Task DisableAsync(string profileName, CancellationToken cancellationToken = default);
}
