using PasswordManager.Application.Exceptions;
using PasswordManager.Application.Models;
using PasswordManager.Security;
using Xunit;

namespace PasswordManager.Security.Tests;

public sealed class VaultHeaderSerializationTests
{
    private readonly VaultHeaderService _headerService = new();

    [Fact]
    public void SerializeAndDeserialize_ShouldRoundtripAllFieldsExactly()
    {
        // Arrange
        Guid vaultId = Guid.NewGuid();
        var parameters = KdfParameters.Default;
        byte[] salt = new byte[32];
        Array.Fill<byte>(salt, 0xAA);

        byte[] nonce = new byte[12];
        Array.Fill<byte>(nonce, 0xBB);
        byte[] tag = new byte[16];
        Array.Fill<byte>(tag, 0xCC);
        byte[] ciphertext = new byte[32];
        Array.Fill<byte>(ciphertext, 0xDD);

        var wrappedKey = new WrappedKeyData(nonce, tag, ciphertext);
        var originalHeader = new VaultHeaderData(vaultId, parameters, salt, wrappedKey);

        // Act
        byte[] serializedBytes = _headerService.Serialize(originalHeader);
        VaultHeaderData deserializedHeader = _headerService.Deserialize(serializedBytes);

        // Assert
        Assert.Equal(VaultHeaderData.HeaderSize, serializedBytes.Length);
        Assert.Equal(VaultHeaderData.ExpectedMagic, deserializedHeader.Magic);
        Assert.Equal(VaultHeaderData.CurrentFormatVersion, deserializedHeader.CryptoFormatVersion);
        Assert.Equal(vaultId, deserializedHeader.VaultId);
        Assert.Equal(VaultHeaderData.Argon2idAlgorithmId, deserializedHeader.KdfAlgorithmId);
        Assert.Equal(parameters.MemoryKiB, deserializedHeader.Parameters.MemoryKiB);
        Assert.Equal(parameters.Iterations, deserializedHeader.Parameters.Iterations);
        Assert.Equal(parameters.DegreeOfParallelism, deserializedHeader.Parameters.DegreeOfParallelism);
        Assert.Equal(parameters.SaltLength, deserializedHeader.Parameters.SaltLength);
        Assert.Equal(salt, deserializedHeader.Salt);
        Assert.Equal(nonce, deserializedHeader.WrappedRootKey.Nonce);
        Assert.Equal(tag, deserializedHeader.WrappedRootKey.Tag);
        Assert.Equal(ciphertext, deserializedHeader.WrappedRootKey.Ciphertext);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(131)]
    [InlineData(133)]
    public void Deserialize_InvalidByteLength_ShouldThrowCryptoAuthenticationException(int byteLength)
    {
        byte[] invalidBytes = new byte[byteLength];

        Assert.Throws<CryptoAuthenticationException>(() =>
            _headerService.Deserialize(invalidBytes));
    }
}
