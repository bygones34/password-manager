using PasswordManager.Application.Exceptions;
using PasswordManager.Application.Models;
using PasswordManager.Security;
using Xunit;

namespace PasswordManager.Security.Tests;

public sealed class EnvelopeTamperTests
{
    private readonly AeadEnvelopeService _envelopeService = new();
    private readonly VaultHeaderService _headerService = new();

    [Fact]
    public void SEC_C01_BitFlip_InCiphertext_ShouldThrowCryptoAuthenticationException()
    {
        // Arrange
        Guid vaultId = Guid.NewGuid();
        Guid recordId = Guid.NewGuid();
        byte[] recordKey = _envelopeService.GenerateRandomBytes(32);
        byte[] payload = [10, 20, 30, 40, 50, 60, 70, 80];

        EncryptedEnvelope envelope = _envelopeService.EncryptRecord(recordKey, vaultId, recordId, payload);

        // Mutate single bit in ciphertext
        byte[] tamperedCiphertext = (byte[])envelope.Ciphertext.Clone();
        tamperedCiphertext[0] ^= 0x01; // flip lowest bit

        var tamperedEnvelope = new EncryptedEnvelope(
            envelope.EnvelopeVersion,
            envelope.Nonce,
            envelope.Tag,
            tamperedCiphertext);

        // Act & Assert
        Assert.Throws<CryptoAuthenticationException>(() =>
            _envelopeService.DecryptRecord(recordKey, vaultId, recordId, tamperedEnvelope));
    }

    [Fact]
    public void SEC_C02_AuthTagTampering_ShouldThrowCryptoAuthenticationException()
    {
        // Arrange
        Guid vaultId = Guid.NewGuid();
        Guid recordId = Guid.NewGuid();
        byte[] recordKey = _envelopeService.GenerateRandomBytes(32);
        byte[] payload = [1, 2, 3, 4, 5];

        EncryptedEnvelope envelope = _envelopeService.EncryptRecord(recordKey, vaultId, recordId, payload);

        // Mutate tag
        byte[] tamperedTag = (byte[])envelope.Tag.Clone();
        tamperedTag[^1] ^= 0x80;

        var tamperedEnvelope = new EncryptedEnvelope(
            envelope.EnvelopeVersion,
            envelope.Nonce,
            tamperedTag,
            envelope.Ciphertext);

        // Act & Assert
        Assert.Throws<CryptoAuthenticationException>(() =>
            _envelopeService.DecryptRecord(recordKey, vaultId, recordId, tamperedEnvelope));
    }

    [Fact]
    public void SEC_C03_RecordSubstitution_WithDifferentRecordId_ShouldThrowCryptoAuthenticationException()
    {
        // Arrange
        Guid vaultId = Guid.NewGuid();
        Guid originalRecordId = Guid.NewGuid();
        Guid attackerRecordId = Guid.NewGuid();
        byte[] recordKey = _envelopeService.GenerateRandomBytes(32);
        byte[] payload = [11, 22, 33];

        EncryptedEnvelope envelope = _envelopeService.EncryptRecord(recordKey, vaultId, originalRecordId, payload);

        // Act & Assert: attempting to decrypt using substituted RecordId
        Assert.Throws<CryptoAuthenticationException>(() =>
            _envelopeService.DecryptRecord(recordKey, vaultId, attackerRecordId, envelope));
    }

    [Fact]
    public void SEC_C04_CrossVaultSwap_WithDifferentVaultId_ShouldThrowCryptoAuthenticationException()
    {
        // Arrange
        Guid vaultA = Guid.NewGuid();
        Guid vaultB = Guid.NewGuid();
        Guid recordId = Guid.NewGuid();
        byte[] recordKey = _envelopeService.GenerateRandomBytes(32);
        byte[] payload = [99, 88, 77];

        EncryptedEnvelope envelope = _envelopeService.EncryptRecord(recordKey, vaultA, recordId, payload);

        // Act & Assert: attempting to decrypt in different vault
        Assert.Throws<CryptoAuthenticationException>(() =>
            _envelopeService.DecryptRecord(recordKey, vaultB, recordId, envelope));
    }

    [Fact]
    public void SEC_C05_HeaderTampering_ShouldPreventRootKeyUnwrapping()
    {
        // Arrange
        Guid vaultId = Guid.NewGuid();
        byte[] kek = _envelopeService.GenerateRandomBytes(32);
        byte[] rootKey = _envelopeService.GenerateRandomBytes(32);
        byte[] salt = _envelopeService.GenerateRandomBytes(32);
        var parameters = KdfParameters.Default;

        byte[] headerAad = _headerService.ComputeHeaderAad(vaultId, VaultHeaderData.Argon2idAlgorithmId, parameters, salt);
        WrappedKeyData wrappedKey = _envelopeService.WrapRootKey(kek, rootKey, headerAad);

        // Valid unwrap succeeds
        byte[] unwrapped = _envelopeService.UnwrapRootKey(kek, wrappedKey, headerAad);
        Assert.Equal(rootKey, unwrapped);

        // Tamper with memory parameter in AAD
        var tamperedParams = new KdfParameters(parameters.MemoryKiB + 1024, parameters.Iterations, parameters.DegreeOfParallelism);
        byte[] tamperedAad = _headerService.ComputeHeaderAad(vaultId, VaultHeaderData.Argon2idAlgorithmId, tamperedParams, salt);

        // Act & Assert: unwrap with tampered AAD fails closed
        Assert.Throws<CryptoAuthenticationException>(() =>
            _envelopeService.UnwrapRootKey(kek, wrappedKey, tamperedAad));
    }

    [Fact]
    public void SEC_C06_InvalidMagicOrFormatVersion_ShouldThrowCryptoAuthenticationException()
    {
        // Arrange
        Guid vaultId = Guid.NewGuid();
        var parameters = KdfParameters.Default;
        byte[] salt = new byte[32];
        var wrappedKey = new WrappedKeyData(new byte[12], new byte[16], new byte[32]);

        // Wrong magic
        var headerWrongMagic = new VaultHeaderData(vaultId, parameters, salt, wrappedKey, magic: 0x12345678);
        byte[] bytesWrongMagic = _headerService.Serialize(headerWrongMagic);

        Assert.Throws<CryptoAuthenticationException>(() =>
            _headerService.Deserialize(bytesWrongMagic));

        // Unsupported version
        var headerWrongVersion = new VaultHeaderData(vaultId, parameters, salt, wrappedKey, cryptoFormatVersion: 2);
        byte[] bytesWrongVersion = _headerService.Serialize(headerWrongVersion);

        Assert.Throws<CryptoAuthenticationException>(() =>
            _headerService.Deserialize(bytesWrongVersion));
    }
}
