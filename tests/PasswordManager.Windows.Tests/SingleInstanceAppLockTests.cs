using System;
using System.Threading;
using PasswordManager.Platform.Windows.Lifecycle;
using Xunit;

namespace PasswordManager.Windows.Tests;

public class SingleInstanceAppLockTests
{
    [Fact]
    public void SingleInstance_FirstInstance_ShouldAcquireOwnership()
    {
        var testId = "TestScope_" + Guid.NewGuid().ToString("N");
        using var primary = new SingleInstanceAppLock(testId);

        var acquired = primary.TryAcquireOwnership();
        Assert.True(acquired);
    }

    [Fact]
    public void SingleInstance_SecondInstance_ShouldFailToAcquireOwnership()
    {
        var testId = "TestScope_" + Guid.NewGuid().ToString("N");
        using var primary = new SingleInstanceAppLock(testId);
        using var secondary = new SingleInstanceAppLock(testId);

        var primaryAcquired = primary.TryAcquireOwnership();
        Assert.True(primaryAcquired);

        var secondaryAcquired = secondary.TryAcquireOwnership();
        Assert.False(secondaryAcquired);
    }

    [Fact]
    public void SingleInstance_DisposedPrimary_ShouldAllowNewInstanceToAcquire()
    {
        var testId = "TestScope_" + Guid.NewGuid().ToString("N");

        using (var primary = new SingleInstanceAppLock(testId))
        {
            var firstAcquired = primary.TryAcquireOwnership();
            Assert.True(firstAcquired);
        }

        using var subsequent = new SingleInstanceAppLock(testId);
        var subsequentAcquired = subsequent.TryAcquireOwnership();
        Assert.True(subsequentAcquired);
    }

    [Fact]
    public void SingleInstance_SecondaryCanSignalPrimary()
    {
        var testId = "TestScope_" + Guid.NewGuid().ToString("N");
        using var primary = new SingleInstanceAppLock(testId);
        using var secondary = new SingleInstanceAppLock(testId);

        var primaryAcquired = primary.TryAcquireOwnership();
        Assert.True(primaryAcquired);

        var activationReceived = false;
        using var resetEvent = new ManualResetEvent(false);

        primary.ActivationRequested += () =>
        {
            activationReceived = true;
            resetEvent.Set();
        };

        var signaled = secondary.SignalExistingInstance();
        Assert.True(signaled);

        // Wait up to 2 seconds for background event thread to trigger
        var eventTriggered = resetEvent.WaitOne(TimeSpan.FromSeconds(2));
        Assert.True(eventTriggered);
        Assert.True(activationReceived);
    }
}
