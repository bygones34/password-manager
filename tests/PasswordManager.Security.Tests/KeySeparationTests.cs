using PasswordManager.Application.Exceptions;
using PasswordManager.Application.Models;
using PasswordManager.Security;
using Xunit;

namespace PasswordManager.Security.Tests;

public sealed class KeySeparationTests
{
    private readonly AeadEnvelopeService _envelopeService = new();

    [Fact]
    public void SEC_B01_KeySeparation_RecordKey_ManifestKey_RootKey_KEK_ShouldAllBeDistinct()
    {
        // Arrange
        Guid vaultId = Guid.NewGuid();
        byte[] kek = _envelopeService.GenerateRandomBytes(32);
        byte[] rootKey = _envelopeService.GenerateRandomBytes(32);

        // Act
        byte[] recordKey = _envelopeService.DeriveRecordKey(rootKey, vaultId);
        byte[] manifestKey = _envelopeService.DeriveManifestKey(rootKey, vaultId);

        // Assert
        Assert.NotEqual(kek, rootKey);
        Assert.NotEqual(rootKey, recordKey);
        Assert.NotEqual(rootKey, manifestKey);
        Assert.NotEqual(recordKey, manifestKey);
        Assert.NotEqual(kek, recordKey);
        Assert.NotEqual(kek, manifestKey);
    }

    [Fact]
    public void SEC_B02_DifferentVaultIds_ShouldProduceDifferentDerivedKeys()
    {
        // Arrange
        Guid vaultA = Guid.NewGuid();
        Guid vaultB = Guid.NewGuid();
        byte[] rootKey = _envelopeService.GenerateRandomBytes(32);

        // Act
        byte[] recordKeyA = _envelopeService.DeriveRecordKey(rootKey, vaultA);
        byte[] recordKeyB = _envelopeService.DeriveRecordKey(rootKey, vaultB);

        byte[] manifestKeyA = _envelopeService.DeriveManifestKey(rootKey, vaultA);
        byte[] manifestKeyB = _envelopeService.DeriveManifestKey(rootKey, vaultB);

        // Assert
        Assert.NotEqual(recordKeyA, recordKeyB);
        Assert.NotEqual(manifestKeyA, manifestKeyB);
    }

    [Fact]
    public void SEC_B03_DecryptRecord_WithManifestKey_ShouldThrowCryptoAuthenticationException()
    {
        // Arrange
        Guid vaultId = Guid.NewGuid();
        Guid recordId = Guid.NewGuid();
        byte[] rootKey = _envelopeService.GenerateRandomBytes(32);

        byte[] recordKey = _envelopeService.DeriveRecordKey(rootKey, vaultId);
        byte[] manifestKey = _envelopeService.DeriveManifestKey(rootKey, vaultId);

        byte[] payload = [1, 2, 3, 4, 5];
        EncryptedEnvelope envelope = _envelopeService.EncryptRecord(recordKey, vaultId, recordId, payload);

        // Act & Assert
        Assert.Throws<CryptoAuthenticationException>(() =>
            _envelopeService.DecryptRecord(manifestKey, vaultId, recordId, envelope));
    }

    [Fact]
    public void DecryptManifest_WithRecordKey_ShouldThrowCryptoAuthenticationException()
    {
        // Arrange
        Guid vaultId = Guid.NewGuid();
        byte[] rootKey = _envelopeService.GenerateRandomBytes(32);

        byte[] recordKey = _envelopeService.DeriveRecordKey(rootKey, vaultId);
        byte[] manifestKey = _envelopeService.DeriveManifestKey(rootKey, vaultId);

        byte[] payload = [9, 8, 7, 6];
        EncryptedEnvelope envelope = _envelopeService.EncryptManifest(manifestKey, vaultId, payload);

        // Act & Assert
        Assert.Throws<CryptoAuthenticationException>(() =>
            _envelopeService.DecryptManifest(recordKey, vaultId, envelope));
    }
}
