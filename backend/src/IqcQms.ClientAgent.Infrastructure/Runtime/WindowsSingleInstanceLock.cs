using System.Security.Cryptography;
using System.Text;
using IqcQms.ClientAgent.Application.Runtime;
using Microsoft.Extensions.Logging;

namespace IqcQms.ClientAgent.Infrastructure.Runtime;

public class WindowsSingleInstanceLock : ISingleInstanceLock
{
    private readonly string _mutexName;
    private readonly ILogger<WindowsSingleInstanceLock> _logger;
    private Mutex? _mutex;
    private bool _isAcquired;

    public bool IsAcquired => _isAcquired;

    public WindowsSingleInstanceLock(string profileName, ILogger<WindowsSingleInstanceLock> logger)
    {
        _logger = logger;
        using var sha = SHA256.Create();
        var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(profileName.ToLowerInvariant())))[..16];
        _mutexName = $"Local\\IqcQmsClientAgent_Profile_{hash}";
    }

    public Task<bool> TryAcquireAsync(CancellationToken cancellationToken = default)
    {
        if (_isAcquired) return Task.FromResult(true);

        try
        {
            _mutex = new Mutex(false, _mutexName);
            try
            {
                _isAcquired = _mutex.WaitOne(TimeSpan.Zero, false);
            }
            catch (AbandonedMutexException)
            {
                _logger.LogWarning("Recovered abandoned single-instance mutex lock '{MutexName}' from terminated process.", _mutexName);
                _isAcquired = true;
            }

            if (_isAcquired)
            {
                _logger.LogInformation("Acquired single-instance lock for profile mutex '{MutexName}'.", _mutexName);
            }
            else
            {
                _logger.LogWarning("Another instance of Client Agent is already running for profile mutex '{MutexName}'.", _mutexName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate single-instance mutex lock '{MutexName}'.", _mutexName);
            _isAcquired = false;
        }

        return Task.FromResult(_isAcquired);
    }

    public void Release()
    {
        if (_isAcquired && _mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
                _logger.LogInformation("Released single-instance lock '{MutexName}'.", _mutexName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception releasing single-instance mutex lock '{MutexName}'.", _mutexName);
            }
            finally
            {
                _isAcquired = false;
            }
        }
    }

    public void Dispose()
    {
        Release();
        _mutex?.Dispose();
        _mutex = null;
    }
}

public class InMemorySingleInstanceLock : ISingleInstanceLock
{
    private static readonly HashSet<string> ActiveLocks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object LockObj = new();

    private readonly string _profileName;
    private bool _isAcquired;

    public bool IsAcquired => _isAcquired;

    public InMemorySingleInstanceLock(string profileName)
    {
        _profileName = profileName.ToLowerInvariant();
    }

    public Task<bool> TryAcquireAsync(CancellationToken cancellationToken = default)
    {
        lock (LockObj)
        {
            if (ActiveLocks.Contains(_profileName))
            {
                _isAcquired = false;
                return Task.FromResult(false);
            }
            ActiveLocks.Add(_profileName);
            _isAcquired = true;
            return Task.FromResult(true);
        }
    }

    public void Release()
    {
        lock (LockObj)
        {
            if (_isAcquired)
            {
                ActiveLocks.Remove(_profileName);
                _isAcquired = false;
            }
        }
    }

    public void Dispose()
    {
        Release();
    }
}
