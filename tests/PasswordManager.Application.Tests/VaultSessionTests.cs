using System.Security.Cryptography;
using PasswordManager.Application.Session;
using Xunit;

namespace PasswordManager.Application.Tests;

public sealed class VaultSessionTests
{
    [Fact]
    public void Constructor_InitializesPropertiesAndClonesKeys()
    {
        var vaultId = Guid.NewGuid();
        var generation = 42L;
        var rootKey = new byte[32];
        var recordKey = new byte[32];
        var manifestKey = new byte[32];

        RandomNumberGenerator.Fill(rootKey);
        RandomNumberGenerator.Fill(recordKey);
        RandomNumberGenerator.Fill(manifestKey);

        using var session = new VaultSession(vaultId, generation, rootKey, recordKey, manifestKey);

        Assert.Equal(vaultId, session.VaultId);
        Assert.Equal(generation, session.SessionGeneration);
        Assert.False(session.IsDisposed);
        Assert.False(session.CancellationToken.IsCancellationRequested);

        Assert.Equal(rootKey, session.RootKey);
        Assert.Equal(recordKey, session.RecordKey);
        Assert.Equal(manifestKey, session.ManifestKey);

        // Cloned arrays: modifying input buffers should not alter session keys
        rootKey[0] ^= 0xFF;
        Assert.NotEqual(rootKey[0], session.RootKey[0]);
    }

    [Theory]
    [InlineData(16, 32, 32)]
    [InlineData(32, 24, 32)]
    [InlineData(32, 32, 64)]
    public void Constructor_ThrowsArgumentException_WhenKeyLengthIsNot32Bytes(int rootLen, int recordLen, int manifestLen)
    {
        var rootKey = new byte[rootLen];
        var recordKey = new byte[recordLen];
        var manifestKey = new byte[manifestLen];

        Assert.Throws<ArgumentException>(() =>
            new VaultSession(Guid.NewGuid(), 1, rootKey, recordKey, manifestKey));
    }

    [Fact]
    public void Dispose_ZeroesKeysAndCancelsCancellationToken_SEC_F01()
    {
        var rootKey = new byte[32];
        var recordKey = new byte[32];
        var manifestKey = new byte[32];
        RandomNumberGenerator.Fill(rootKey);
        RandomNumberGenerator.Fill(recordKey);
        RandomNumberGenerator.Fill(manifestKey);

        var session = new VaultSession(Guid.NewGuid(), 1, rootKey, recordKey, manifestKey);

        // Verify keys contain non-zero bytes before disposal
        Assert.Contains(session.RootKey, b => b != 0);
        Assert.Contains(session.RecordKey, b => b != 0);
        Assert.Contains(session.ManifestKey, b => b != 0);

        session.Dispose();

        Assert.True(session.IsDisposed);
        Assert.True(session.CancellationToken.IsCancellationRequested);

        // SEC-F01: All key buffers must be completely zeroed in memory
        Assert.All(session.RootKey, b => Assert.Equal(0, b));
        Assert.All(session.RecordKey, b => Assert.Equal(0, b));
        Assert.All(session.ManifestKey, b => Assert.Equal(0, b));

        // ThrowIfDisposed must throw ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => session.ThrowIfDisposed());

        // Idempotent disposal: subsequent Dispose must not throw
        session.Dispose();
    }
}
