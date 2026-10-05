using PasswordManager.Autofill.Windows;
using PasswordManager.Platform.Windows;
using Xunit;

namespace PasswordManager.Windows.Tests;

public class WindowsSmokeTests
{
    [Fact]
    public void PlatformWindowsAssembly_ShouldBeAvailable()
    {
        Assert.NotNull(typeof(PlatformWindowsMarker).Assembly);
    }

    [Fact]
    public void AutofillWindowsAssembly_ShouldBeAvailable()
    {
        Assert.NotNull(typeof(AutofillWindowsMarker).Assembly);
    }
}
