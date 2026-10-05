using Xunit;

namespace PasswordManager.Infrastructure.Tests;

public class InfrastructureSmokeTests
{
    [Fact]
    public void InfrastructureAssembly_ShouldBeAvailable()
    {
        Assert.NotNull(typeof(InfrastructureMarker).Assembly);
    }
}
