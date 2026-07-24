namespace IqcQms.ClientAgent.Application.Storage;

public interface IAgentPathResolver
{
    string ProfileName { get; }
    string RootDataDirectory { get; }
    string IdentityFilePath { get; }
    string CredentialsFilePath { get; }
    string QueueDatabasePath { get; }
    string LogsDirectory { get; }
    string LockFilePath { get; }
    string GetNormalizedProfilePath(string subPath);
}
