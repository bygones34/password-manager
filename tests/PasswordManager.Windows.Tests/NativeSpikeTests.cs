using System;
using PasswordManager.Platform.Windows.Interop;
using PasswordManager.Platform.Windows.Services;
using Xunit;

namespace PasswordManager.Windows.Tests;

public class NativeSpikeTests
{
    [Fact]
    public void ForegroundTargetDetector_CanInstantiateAndQuery_WithoutException()
    {
        var detector = new ForegroundTargetDetector();
        var target = detector.CaptureForegroundTarget();

        if (target is not null)
        {
            Assert.True(target.IsValid);
            Assert.True(target.ProcessId > 0);
        }
    }

    [Fact]
    public void SessionLockWatcher_ProcessMessage_ShouldMapEventsCorrectly()
    {
        using var watcher = new SessionLockWatcher();
        SessionLockEventType? receivedEvent = null;

        watcher.SessionEventOccurred += e => receivedEvent = e;

        var resultLock = watcher.ProcessSessionMessage(NativeMethods.WTS_SESSION_LOCK);
        Assert.Equal(SessionLockEventType.Lock, resultLock);
        Assert.Equal(SessionLockEventType.Lock, receivedEvent);

        var resultUnlock = watcher.ProcessSessionMessage(NativeMethods.WTS_SESSION_UNLOCK);
        Assert.Equal(SessionLockEventType.Unlock, resultUnlock);
        Assert.Equal(SessionLockEventType.Unlock, receivedEvent);

        var resultLogoff = watcher.ProcessSessionMessage(NativeMethods.WTS_SESSION_LOGOFF);
        Assert.Equal(SessionLockEventType.Logoff, resultLogoff);
        Assert.Equal(SessionLockEventType.Logoff, receivedEvent);
    }

    [Fact]
    public void SessionLockWatcher_Lifecycle_ShouldDisposeWithoutError()
    {
        var watcher = new SessionLockWatcher();
        watcher.Dispose();
        watcher.Dispose();
    }

    [Fact]
    public void TrayIconManager_DisposedManager_ShouldSafelyIgnoreCalls()
    {
        var manager = new TrayIconManager(IntPtr.Zero, 99);
        manager.Dispose();

        var result = manager.AddOrUpdateIcon(IntPtr.Zero, "Test Tip");
        Assert.False(result);
    }
}
