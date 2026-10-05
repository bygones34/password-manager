using Xunit;

namespace PasswordManager.Application.Tests;

public class ApplicationSmokeTests
{
    [Fact]
    public void ApplicationAssembly_ShouldBeAvailable()
    {
        Assert.NotNull(typeof(ApplicationMarker).Assembly);
    }
}
