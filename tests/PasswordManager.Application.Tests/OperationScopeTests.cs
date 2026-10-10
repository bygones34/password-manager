using System.Security.Cryptography;
using PasswordManager.Application.Session;
using Xunit;

namespace PasswordManager.Application.Tests;

public sealed class OperationScopeTests
{
    private static VaultSession CreateValidSession(long generation = 1)
    {
        var rootKey = new byte[32];
        var recordKey = new byte[32];
        var manifestKey = new byte[32];
        RandomNumberGenerator.Fill(rootKey);
        RandomNumberGenerator.Fill(recordKey);
        RandomNumberGenerator.Fill(manifestKey);
        return new VaultSession(Guid.NewGuid(), generation, rootKey, recordKey, manifestKey);
    }

    [Fact]
    public void OperationScope_IsActive_WhenSessionAndGenerationAreValid()
    {
        using var session = CreateValidSession(generation: 1);
        long currentGen = 1;

        using var scope = new OperationScope(session, () => currentGen);

        Assert.Equal(1, scope.Generation);
        Assert.True(scope.IsActive);
        Assert.False(scope.CancellationToken.IsCancellationRequested);

        // Does not throw when valid
        scope.ThrowIfCanceledOrExpired();
    }

    [Fact]
    public void OperationScope_CancelsAndThrows_WhenSessionIsDisposed_SEC_F02()
    {
        var session = CreateValidSession(generation: 1);
        long currentGen = 1;

        using var scope = new OperationScope(session, () => currentGen);

        // Simulating vault lock by disposing the session
        session.Dispose();

        Assert.False(scope.IsActive);
        Assert.True(scope.CancellationToken.IsCancellationRequested);

        var ex = Assert.Throws<OperationCanceledException>(() => scope.ThrowIfCanceledOrExpired());
        Assert.Contains("expired or vault was locked", ex.Message);
    }

    [Fact]
    public void OperationScope_Throws_WhenSessionGenerationAdvances()
    {
        using var session = CreateValidSession(generation: 1);
        long currentGen = 1;

        using var scope = new OperationScope(session, () => currentGen);

        // Simulating subsequent unlock incrementing generation
        currentGen = 2;

        Assert.False(scope.IsActive);
        var ex = Assert.Throws<OperationCanceledException>(() => scope.ThrowIfCanceledOrExpired());
        Assert.Contains("expired or vault was locked", ex.Message);
    }

    [Fact]
    public void OperationScope_HonorsExternalCancellationToken()
    {
        using var session = CreateValidSession(generation: 1);
        long currentGen = 1;
        using var externalCts = new CancellationTokenSource();

        using var scope = new OperationScope(session, () => currentGen, externalCts.Token);

        Assert.True(scope.IsActive);

        externalCts.Cancel();

        Assert.False(scope.IsActive);
        Assert.True(scope.CancellationToken.IsCancellationRequested);
        Assert.Throws<OperationCanceledException>(() => scope.ThrowIfCanceledOrExpired());
    }

    [Fact]
    public void OperationScope_Dispose_DeactivatesScope()
    {
        using var session = CreateValidSession(generation: 1);
        long currentGen = 1;

        var scope = new OperationScope(session, () => currentGen);
        Assert.True(scope.IsActive);

        scope.Dispose();

        Assert.False(scope.IsActive);
        Assert.Throws<OperationCanceledException>(() => scope.ThrowIfCanceledOrExpired());
    }
}
