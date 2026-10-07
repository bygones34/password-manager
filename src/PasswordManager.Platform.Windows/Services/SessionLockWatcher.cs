using System;
using PasswordManager.Platform.Windows.Interop;

namespace PasswordManager.Platform.Windows.Services;

public enum SessionLockEventType
{
    Lock,
    Unlock,
    Logoff,
    RemoteDisconnect,
    Other
}

/// <summary>
/// Prototype service listening for Windows OS session lock/unlock events via Win32 WTS APIs.
/// Crucial for auto-locking vaults immediately when user workstation is locked.
/// </summary>
public sealed class SessionLockWatcher : IDisposable
{
    private IntPtr _registeredHwnd;
    private bool _isDisposed;

    public event Action<SessionLockEventType>? SessionEventOccurred;

    public SessionLockWatcher()
    {
    }

    /// <summary>
    /// Registers a window handle to receive WM_WTSSESSION_CHANGE messages from the OS.
    /// </summary>
    public bool RegisterWindow(IntPtr hWnd)
    {
        if (_isDisposed || hWnd == IntPtr.Zero) return false;

        if (_registeredHwnd != IntPtr.Zero)
        {
            UnregisterWindow();
        }

        var success = NativeMethods.WTSRegisterSessionNotification(hWnd, NativeMethods.NOTIFY_FOR_THIS_SESSION);
        if (success)
        {
            _registeredHwnd = hWnd;
        }

        return success;
    }

    /// <summary>
    /// Unregisters the monitored window handle.
    /// </summary>
    public bool UnregisterWindow()
    {
        if (_registeredHwnd == IntPtr.Zero) return false;

        var success = NativeMethods.WTSUnRegisterSessionNotification(_registeredHwnd);
        _registeredHwnd = IntPtr.Zero;
        return success;
    }

    /// <summary>
    /// Processes a WM_WTSSESSION_CHANGE message from the window procedure and triggers mapped events.
    /// </summary>
    public SessionLockEventType ProcessSessionMessage(uint wtsEventCode)
    {
        var mapped = wtsEventCode switch
        {
            NativeMethods.WTS_SESSION_LOCK => SessionLockEventType.Lock,
            NativeMethods.WTS_SESSION_UNLOCK => SessionLockEventType.Unlock,
            NativeMethods.WTS_SESSION_LOGOFF => SessionLockEventType.Logoff,
            NativeMethods.WTS_SESSION_REMOTE_DISCONNECT => SessionLockEventType.RemoteDisconnect,
            _ => SessionLockEventType.Other
        };

        SessionEventOccurred?.Invoke(mapped);
        return mapped;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_registeredHwnd != IntPtr.Zero)
        {
            UnregisterWindow();
        }
    }
}
