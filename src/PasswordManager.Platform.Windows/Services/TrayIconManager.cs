using System;
using System.Runtime.InteropServices;
using PasswordManager.Platform.Windows.Interop;

namespace PasswordManager.Platform.Windows.Services;

/// <summary>
/// Manages system tray notification icon lifecycle via Win32 Shell_NotifyIcon.
/// Prototype implementation for M0.5 native spike validation.
/// </summary>
public sealed class TrayIconManager : IDisposable
{
    private readonly uint _iconId;
    private readonly IntPtr _messageWindowHwnd;
    private bool _isAdded;
    private bool _isDisposed;

    public const uint WM_TRAY_CALLBACK = 0x8000 + 101; // WM_APP + 101

    public TrayIconManager(IntPtr messageWindowHwnd, uint iconId = 1)
    {
        _messageWindowHwnd = messageWindowHwnd;
        _iconId = iconId;
    }

    /// <summary>
    /// Registers or updates the notification tray icon.
    /// </summary>
    public bool AddOrUpdateIcon(IntPtr hIcon, string toolTip)
    {
        if (_isDisposed) return false;

        var nid = new NativeMethods.NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
            hWnd = _messageWindowHwnd,
            uID = _iconId,
            uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_TIP | (_isAdded && hIcon == IntPtr.Zero ? 0u : NativeMethods.NIF_ICON),
            uCallbackMessage = WM_TRAY_CALLBACK,
            hIcon = hIcon,
            szTip = toolTip.Length > 127 ? toolTip[..127] : toolTip
        };

        var message = _isAdded ? NativeMethods.NIM_MODIFY : NativeMethods.NIM_ADD;
        var result = NativeMethods.Shell_NotifyIconW(message, ref nid);

        if (result)
        {
            _isAdded = true;
        }

        return result;
    }

    /// <summary>
    /// Removes the icon from the system notification area.
    /// </summary>
    public bool RemoveIcon()
    {
        if (!_isAdded || _isDisposed) return false;

        var nid = new NativeMethods.NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
            hWnd = _messageWindowHwnd,
            uID = _iconId
        };

        var result = NativeMethods.Shell_NotifyIconW(NativeMethods.NIM_DELETE, ref nid);
        if (result)
        {
            _isAdded = false;
        }

        return result;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_isAdded)
        {
            RemoveIcon();
        }
    }
}
