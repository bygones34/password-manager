using PasswordManager.Application.Abstractions;
using PasswordManager.Platform.Windows.Interop;
using PasswordManager.Platform.Windows.Services;
using Xunit;

namespace PasswordManager.Windows.Tests;

public sealed class WindowsSessionLockListenerTests
{
    [Fact]
    public void ProcessWindowMessage_WtsSessionLock_TriggersWorkstationLocked()
    {
        using var listener = new WindowsSessionLockListener();
        SystemLockReason? receivedReason = null;
        listener.SystemLockTriggered += r => receivedReason = r;

        var handled = listener.ProcessWindowMessage(
            NativeMethods.WM_WTSSESSION_CHANGE,
            new IntPtr(NativeMethods.WTS_SESSION_LOCK),
            IntPtr.Zero);

        Assert.True(handled);
        Assert.Equal(SystemLockReason.WorkstationLocked, receivedReason);
    }

    [Fact]
    public void ProcessWindowMessage_WtsLogoff_TriggersSessionLogoff()
    {
        using var listener = new WindowsSessionLockListener();
        SystemLockReason? receivedReason = null;
        listener.SystemLockTriggered += r => receivedReason = r;

        var handled = listener.ProcessWindowMessage(
            NativeMethods.WM_WTSSESSION_CHANGE,
            new IntPtr(NativeMethods.WTS_SESSION_LOGOFF),
            IntPtr.Zero);

        Assert.True(handled);
        Assert.Equal(SystemLockReason.SessionLogoff, receivedReason);
    }

    [Fact]
    public void ProcessWindowMessage_WtsRemoteDisconnect_TriggersRemoteDisconnect()
    {
        using var listener = new WindowsSessionLockListener();
        SystemLockReason? receivedReason = null;
        listener.SystemLockTriggered += r => receivedReason = r;

        var handled = listener.ProcessWindowMessage(
            NativeMethods.WM_WTSSESSION_CHANGE,
            new IntPtr(NativeMethods.WTS_SESSION_REMOTE_DISCONNECT),
            IntPtr.Zero);

        Assert.True(handled);
        Assert.Equal(SystemLockReason.RemoteDisconnect, receivedReason);
    }

    [Fact]
    public void ProcessWindowMessage_PowerSuspend_TriggersSystemSuspend()
    {
        using var listener = new WindowsSessionLockListener();
        SystemLockReason? receivedReason = null;
        listener.SystemLockTriggered += r => receivedReason = r;

        var handled = listener.ProcessWindowMessage(
            NativeMethods.WM_POWERBROADCAST,
            new IntPtr(NativeMethods.PBM_APMSUSPEND),
            IntPtr.Zero);

        Assert.True(handled);
        Assert.Equal(SystemLockReason.SystemSuspend, receivedReason);
    }

    [Fact]
    public void ProcessWindowMessage_UnrelatedMessage_DoesNotTriggerLock()
    {
        using var listener = new WindowsSessionLockListener();
        SystemLockReason? receivedReason = null;
        listener.SystemLockTriggered += r => receivedReason = r;

        // Arbitrary window message (e.g., WM_PAINT = 0x000F)
        var handled = listener.ProcessWindowMessage(0x000F, IntPtr.Zero, IntPtr.Zero);

        Assert.False(handled);
        Assert.Null(receivedReason);
    }

    [Fact]
    public void Dispose_UnsubscribesAndCleansUpSafely()
    {
        var listener = new WindowsSessionLockListener();
        listener.Dispose();

        // Calling Dispose again should be safe and idempotent
        listener.Dispose();

        var handled = listener.ProcessWindowMessage(
            NativeMethods.WM_POWERBROADCAST,
            new IntPtr(NativeMethods.PBM_APMSUSPEND),
            IntPtr.Zero);

        Assert.False(handled);
    }
}
