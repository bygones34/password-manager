using System;
using System.Security.Principal;
using System.Threading;
using PasswordManager.Application.Abstractions;

namespace PasswordManager.Platform.Windows.Lifecycle;

/// <summary>
/// User-session scoped single instance coordinator using named Mutex and EventWaitHandle.
/// Prevents concurrent process executions from corrupting the local vault database,
/// and allows secondary instances to notify the active primary instance to focus.
/// </summary>
public sealed class SingleInstanceAppLock : ISingleInstanceManager
{
    private readonly string _mutexName;
    private readonly string _activationEventName;
    private Mutex? _mutex;
    private EventWaitHandle? _activationEvent;
    private RegisteredWaitHandle? _registeredWait;
    private bool _hasOwnership;
    private bool _isDisposed;

    public event Action? ActivationRequested;

    public SingleInstanceAppLock(string? customIdentifier = null)
    {
        var userSid = GetCurrentUserSid();
        var id = customIdentifier ?? "Default";
        _mutexName = $@"Local\PasswordManager_SingleInstance_Mutex_{id}_{userSid}";
        _activationEventName = $@"Local\PasswordManager_Activation_Event_{id}_{userSid}";
    }

    public bool TryAcquireOwnership()
    {
        if (_isDisposed) return false;
        if (_hasOwnership) return true;

        try
        {
            _mutex = new Mutex(initiallyOwned: true, _mutexName, out var createdNew);
            if (createdNew)
            {
                _hasOwnership = true;
                StartActivationListener();
                return true;
            }

            // Could not acquire exclusive ownership; another instance is active
            _mutex.Dispose();
            _mutex = null;
            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool SignalExistingInstance()
    {
        if (_isDisposed || _hasOwnership) return false;

        try
        {
            using var waitHandle = EventWaitHandle.OpenExisting(_activationEventName);
            return waitHandle.Set();
        }
        catch
        {
            // The existing instance may be in the middle of closing or not listening
            return false;
        }
    }

    private void StartActivationListener()
    {
        try
        {
            _activationEvent = new EventWaitHandle(
                initialState: false,
                mode: EventResetMode.AutoReset,
                name: _activationEventName);

            _registeredWait = ThreadPool.RegisterWaitForSingleObject(
                _activationEvent,
                (state, timedOut) =>
                {
                    if (!timedOut)
                    {
                        ActivationRequested?.Invoke();
                    }
                },
                null,
                Timeout.Infinite,
                executeOnlyOnce: false);
        }
        catch
        {
            // Fallback gracefully if event creation fails
        }
    }

    private static string GetCurrentUserSid()
    {
        try
        {
            var user = WindowsIdentity.GetCurrent().User;
            return user?.Value.Replace("-", "_") ?? "UnknownUser";
        }
        catch
        {
            return "DefaultUser";
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _registeredWait?.Unregister(null);
        _activationEvent?.Dispose();

        if (_mutex is not null)
        {
            if (_hasOwnership)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                    // Ignore if thread no longer owns mutex during termination
                }
            }
            _mutex.Dispose();
            _mutex = null;
        }

        _hasOwnership = false;
    }
}
