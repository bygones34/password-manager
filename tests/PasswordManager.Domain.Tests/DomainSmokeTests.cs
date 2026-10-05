using Xunit;

namespace PasswordManager.Domain.Tests;

public class DomainSmokeTests
{
    [Fact]
    public void DomainAssembly_ShouldBeAvailable()
    {
        Assert.NotNull(typeof(DomainMarker).Assembly);
    }
}
