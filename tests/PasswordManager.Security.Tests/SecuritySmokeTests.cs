using Xunit;

namespace PasswordManager.Security.Tests;

public class SecuritySmokeTests
{
    [Fact]
    public void SecurityAssembly_ShouldBeAvailable()
    {
        Assert.NotNull(typeof(SecurityMarker).Assembly);
    }
}
