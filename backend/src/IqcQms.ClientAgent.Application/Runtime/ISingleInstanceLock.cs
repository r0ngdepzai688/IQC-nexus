namespace IqcQms.ClientAgent.Application.Runtime;

public interface ISingleInstanceLock : IDisposable
{
    bool IsAcquired { get; }
    Task<bool> TryAcquireAsync(CancellationToken cancellationToken = default);
    void Release();
}
