using System.Diagnostics;
using PasswordManager.Application.Models;
using PasswordManager.Security;
using Xunit;

namespace PasswordManager.Security.Tests;

public sealed class Argon2idBenchmarkTests
{
    private readonly Argon2idKeyDerivationService _service = new();

    [Fact]
    public void DefaultDesktopParameters_ShouldExecuteWithinAcceptableDesktopBudget()
    {
        // Arrange
        byte[] syntheticPassword = [1, 2, 3, 4, 5, 6, 7, 8];
        byte[] syntheticSalt = new byte[32];
        Array.Fill<byte>(syntheticSalt, 0x42);
        var parameters = KdfParameters.Default;

        // Act
        var stopwatch = Stopwatch.StartNew();
        byte[] key = _service.DeriveKey(syntheticPassword, syntheticSalt, parameters);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(key);
        Assert.Equal(32, key.Length);
        // On modern development/desktop systems, 64 MiB, 3 it, 4 p typically completes in 100 - 800 ms.
        // We set a defensive 4000 ms upper bound for loaded CI or lower-spec test runners.
        Assert.True(stopwatch.ElapsedMilliseconds < 4000,
            $"Argon2id execution took {stopwatch.ElapsedMilliseconds} ms, exceeding defensive threshold of 4000 ms.");
    }

    [Fact]
    public async Task DeriveKeyAsync_WithDefaultParameters_ShouldSucceed()
    {
        // Arrange
        byte[] syntheticPassword = [9, 8, 7, 6, 5];
        byte[] syntheticSalt = new byte[32];
        Array.Fill<byte>(syntheticSalt, 0x17);
        var parameters = KdfParameters.Default;

        // Act
        byte[] key = await _service.DeriveKeyAsync(syntheticPassword, syntheticSalt, parameters);

        // Assert
        Assert.NotNull(key);
        Assert.Equal(32, key.Length);
    }

    [Fact]
    public async Task DeriveKeyAsync_CancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        byte[] syntheticPassword = [1, 2, 3];
        byte[] syntheticSalt = new byte[32];
        var parameters = KdfParameters.Default;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _service.DeriveKeyAsync(syntheticPassword, syntheticSalt, parameters, cts.Token));
    }
}
