using System;
using System.IO;
using System.Text;
using PasswordManager.Platform.Windows.Interop;

namespace PasswordManager.Platform.Windows.Services;

/// <summary>
/// Descriptor representing the captured target window and associated process metadata.
/// </summary>
public record ForegroundTargetInfo(
    IntPtr Hwnd,
    uint ProcessId,
    string ProcessPath,
    string WindowTitle,
    string ClassName,
    DateTimeOffset CapturedAt)
{
    public string ProcessName => string.IsNullOrEmpty(ProcessPath)
        ? string.Empty
        : Path.GetFileName(ProcessPath);

    public bool IsValid => Hwnd != IntPtr.Zero && ProcessId != 0;
}

/// <summary>
/// Prototype service for capturing the foreground target window and its process identity.
/// </summary>
public class ForegroundTargetDetector
{
    public ForegroundTargetInfo? CaptureForegroundTarget()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0)
        {
            return null;
        }

        var processPath = GetProcessImagePath(processId);
        var windowTitle = GetWindowTitle(hwnd);
        var className = GetWindowClassName(hwnd);

        return new ForegroundTargetInfo(
            Hwnd: hwnd,
            ProcessId: processId,
            ProcessPath: processPath,
            WindowTitle: windowTitle,
            ClassName: className,
            CapturedAt: DateTimeOffset.UtcNow);
    }

    private static string GetProcessImagePath(uint processId)
    {
        var hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (hProcess == IntPtr.Zero)
        {
            return string.Empty;
        }

        try
        {
            var buffer = new StringBuilder(1024);
            var size = (uint)buffer.Capacity;
            if (NativeMethods.QueryFullProcessImageNameW(hProcess, 0, buffer, ref size))
            {
                return buffer.ToString();
            }
        }
        finally
        {
            NativeMethods.CloseHandle(hProcess);
        }

        return string.Empty;
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        var sb = new StringBuilder(512);
        var length = NativeMethods.GetWindowTextW(hwnd, sb, sb.Capacity);
        return length > 0 ? sb.ToString() : string.Empty;
    }

    private static string GetWindowClassName(IntPtr hwnd)
    {
        var sb = new StringBuilder(256);
        var length = NativeMethods.GetClassNameW(hwnd, sb, sb.Capacity);
        return length > 0 ? sb.ToString() : string.Empty;
    }
}
