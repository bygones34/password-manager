using PasswordManager.Application.Abstractions;
using PasswordManager.Platform.Windows.Interop;

namespace PasswordManager.Platform.Windows.Services;

/// <summary>
/// Windows platform implementation of ISessionLockListener.
/// Listens to Win32 WTS session notifications and WM_POWERBROADCAST suspend messages
/// to trigger immediate vault locking on workstation lock or system suspend.
/// </summary>
public sealed class WindowsSessionLockListener : ISessionLockListener, IDisposable
{
    private readonly SessionLockWatcher _sessionWatcher;
    private bool _isDisposed;

    public event Action<SystemLockReason>? SystemLockTriggered;

    public WindowsSessionLockListener(SessionLockWatcher? sessionWatcher = null)
    {
        _sessionWatcher = sessionWatcher ?? new SessionLockWatcher();
        _sessionWatcher.SessionEventOccurred += OnSessionEventReceived;
    }

    /// <summary>
    /// Registers an HWND to receive WTS session change notifications.
    /// </summary>
    public bool RegisterWindow(IntPtr hWnd)
    {
        return _sessionWatcher.RegisterWindow(hWnd);
    }

    /// <summary>
    /// Unregisters the monitored HWND.
    /// </summary>
    public bool UnregisterWindow()
    {
        return _sessionWatcher.UnregisterWindow();
    }

    /// <summary>
    /// Processes top-level Win32 window messages (WM_WTSSESSION_CHANGE, WM_POWERBROADCAST).
    /// Returns true if a lock event was triggered.
    /// </summary>
    public bool ProcessWindowMessage(uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        if (_isDisposed)
        {
            return false;
        }

        switch (uMsg)
        {
            case NativeMethods.WM_WTSSESSION_CHANGE:
                _sessionWatcher.ProcessSessionMessage((uint)wParam.ToInt64());
                return true;

            case NativeMethods.WM_POWERBROADCAST:
                var powerCode = (uint)wParam.ToInt64();
                if (powerCode == NativeMethods.PBM_APMSUSPEND)
                {
                    SystemLockTriggered?.Invoke(SystemLockReason.SystemSuspend);
                    return true;
                }
                break;
        }

        return false;
    }

    private void OnSessionEventReceived(SessionLockEventType eventType)
    {
        SystemLockReason? mapped = eventType switch
        {
            SessionLockEventType.Lock => SystemLockReason.WorkstationLocked,
            SessionLockEventType.Logoff => SystemLockReason.SessionLogoff,
            SessionLockEventType.RemoteDisconnect => SystemLockReason.RemoteDisconnect,
            _ => null
        };

        if (mapped.HasValue)
        {
            SystemLockTriggered?.Invoke(mapped.Value);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _sessionWatcher.SessionEventOccurred -= OnSessionEventReceived;
        _sessionWatcher.Dispose();
    }
}
