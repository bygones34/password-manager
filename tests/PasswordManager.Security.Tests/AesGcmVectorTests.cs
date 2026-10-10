using System.Security.Cryptography;
using Xunit;

namespace PasswordManager.Security.Tests;

public sealed class AesGcmVectorTests
{
    /// <summary>
    /// Validates AES-256-GCM against NIST SP 800-38D Test Case (Key 256-bit, IV 96-bit, Plaintext 128-bit).
    /// Key: 32 bytes of 0x00
    /// IV: 12 bytes of 0x00
    /// PT: 16 bytes of 0x00
    /// Expected Ciphertext: cea7403d4d606b6e074ec5d3baf39d18
    /// Expected Tag: d0d1c8a799996bf0265b98b5d48ab919
    /// </summary>
    [Fact]
    public void NIST_SP800_38D_Aes256Gcm_TestVector_ShouldMatchExactly()
    {
        // Arrange
        byte[] key = new byte[32];
        byte[] nonce = new byte[12];
        byte[] plaintext = new byte[16];
        byte[] ciphertext = new byte[16];
        byte[] tag = new byte[16];

        const string expectedCiphertextHex = "cea7403d4d606b6e074ec5d3baf39d18";
        const string expectedTagHex = "d0d1c8a799996bf0265b98b5d48ab919";

        // Act
        using (var aesGcm = new AesGcm(key, 16))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, ReadOnlySpan<byte>.Empty);
        }

        string actualCiphertextHex = Convert.ToHexStringLower(ciphertext);
        string actualTagHex = Convert.ToHexStringLower(tag);

        // Assert
        Assert.Equal(expectedCiphertextHex, actualCiphertextHex);
        Assert.Equal(expectedTagHex, actualTagHex);

        // Decrypt verification
        byte[] decrypted = new byte[16];
        using (var aesGcm = new AesGcm(key, 16))
        {
            aesGcm.Decrypt(nonce, ciphertext, tag, decrypted, ReadOnlySpan<byte>.Empty);
        }

        Assert.Equal(plaintext, decrypted);
    }
}
