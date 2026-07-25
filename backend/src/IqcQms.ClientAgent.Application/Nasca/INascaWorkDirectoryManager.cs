namespace IqcQms.ClientAgent.Application.Nasca;

public interface INascaWorkDirectoryManager
{
    Task<NascaWorkManifest> CreateWorkDirectoryAsync(string correlationId, string executionId, CancellationToken cancellationToken = default);
    Task<NascaWorkManifest> StageInputFileAsync(string correlationId, string sourceFilePath, CancellationToken cancellationToken = default);
    Task<NascaWorkManifest?> GetManifestAsync(string correlationId, CancellationToken cancellationToken = default);
    Task<NascaWorkManifest?> GetManifestByWorkIdAsync(string workDirectoryId, CancellationToken cancellationToken = default);
    string GetWorkDirectoryPath(string workDirectoryId);
    string GetWorkRootDirectory();
    Task<NascaWorkManifest> UpdateLifecycleAsync(string correlationId, NascaWorkLifecycle targetLifecycle, CancellationToken cancellationToken = default);
    Task QuarantineWorkDirectoryAsync(string correlationId, string reasonCode, CancellationToken cancellationToken = default);
    Task<int> CleanupExpiredWorkDirectoriesAsync(TimeSpan standardRetention, TimeSpan recoveryRetention, int maxCleanupBatch = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NascaWorkManifest>> DiscoverRecoverableWorkDirectoriesAsync(CancellationToken cancellationToken = default);
}
