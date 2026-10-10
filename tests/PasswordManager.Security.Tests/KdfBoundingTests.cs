using PasswordManager.Application.Exceptions;
using PasswordManager.Application.Models;
using PasswordManager.Application.Validation;
using PasswordManager.Security;
using Xunit;

namespace PasswordManager.Security.Tests;

public sealed class KdfBoundingTests
{
    private readonly Argon2idKeyDerivationService _service = new();

    [Fact]
    public void DefaultParameters_ShouldPassValidation()
    {
        var parameters = KdfParameters.Default;
        KdfParametersValidator.Validate(parameters);
        Assert.True(true);
    }

    [Theory]
    [InlineData(16383)]        // Below min 16 MiB
    [InlineData(524289)]       // Above max 512 MiB
    [InlineData(-1)]           // Negative memory
    [InlineData(2097152)]      // 2 GiB DoS attempt
    public void MemoryKiB_OutOfSafeBounds_ShouldThrowKdfValidationException(int memoryKiB)
    {
        var parameters = new KdfParameters(memoryKiB, 3, 4);

        var ex = Assert.Throws<KdfValidationException>(() => KdfParametersValidator.Validate(parameters));
        Assert.Contains("Memory cost", ex.Message);
    }

    [Theory]
    [InlineData(0)]            // Zero iterations
    [InlineData(-1)]           // Negative iterations
    [InlineData(11)]           // Above max 10
    [InlineData(1000)]         // CPU DoS attempt
    public void Iterations_OutOfSafeBounds_ShouldThrowKdfValidationException(int iterations)
    {
        var parameters = new KdfParameters(65536, iterations, 4);

        var ex = Assert.Throws<KdfValidationException>(() => KdfParametersValidator.Validate(parameters));
        Assert.Contains("Iterations count", ex.Message);
    }

    [Theory]
    [InlineData(0)]            // Zero lanes
    [InlineData(-2)]           // Negative
    [InlineData(17)]           // Above max 16
    [InlineData(256)]          // Extreme parallelism
    public void Parallelism_OutOfSafeBounds_ShouldThrowKdfValidationException(int parallelism)
    {
        var parameters = new KdfParameters(65536, 3, parallelism);

        var ex = Assert.Throws<KdfValidationException>(() => KdfParametersValidator.Validate(parameters));
        Assert.Contains("Parallelism", ex.Message);
    }

    [Theory]
    [InlineData(15)]           // Less than 16 bytes
    [InlineData(65)]           // More than 64 bytes
    public void SaltLength_OutOfSafeBounds_ShouldThrowKdfValidationException(int saltLength)
    {
        var parameters = new KdfParameters(65536, 3, 4, saltLength: saltLength);

        var ex = Assert.Throws<KdfValidationException>(() => KdfParametersValidator.Validate(parameters));
        Assert.Contains("Salt length", ex.Message);
    }

    [Theory]
    [InlineData(15)]           // Less than 16 bytes
    [InlineData(65)]           // More than 64 bytes
    public void KeyLength_OutOfSafeBounds_ShouldThrowKdfValidationException(int keyLength)
    {
        var parameters = new KdfParameters(65536, 3, 4, keyLength: keyLength);

        var ex = Assert.Throws<KdfValidationException>(() => KdfParametersValidator.Validate(parameters));
        Assert.Contains("Key length", ex.Message);
    }

    [Fact]
    public void Service_DeriveKey_WithMismatchedSaltLength_ShouldThrowKdfValidationException()
    {
        var parameters = KdfParameters.Default; // requires 32 bytes salt
        byte[] password = [1, 2, 3];
        byte[] salt16 = new byte[16];           // only 16 bytes

        var ex = Assert.Throws<KdfValidationException>(() =>
            _service.DeriveKey(password, salt16, parameters));

        Assert.Contains("Supplied salt length", ex.Message);
    }

    [Fact]
    public void Service_DeriveKey_WithNullInputs_ShouldThrowKdfValidationException()
    {
        var parameters = KdfParameters.Default;
        byte[] validSalt = new byte[32];

        Assert.Throws<KdfValidationException>(() =>
            _service.DeriveKey(null!, validSalt, parameters));

        Assert.Throws<KdfValidationException>(() =>
            _service.DeriveKey([1, 2, 3], null!, parameters));
    }
}
