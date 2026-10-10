using System.Security.Cryptography;
using Xunit;

namespace PasswordManager.Security.Tests;

public sealed class HkdfVectorTests
{
    /// <summary>
    /// Validates HKDF-SHA-256 against official RFC 5869 Test Case 1.
    /// IKM = 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b
    /// Salt = 000102030405060708090a0b0c
    /// PRK = 077709362c2e32df0ddc3f0dc47bba6390b6c73bb50f9c3122ec844ad7c2b3e5
    /// info = f0f1f2f3f4f5f6f7f8f9
    /// L = 42
    /// OKM = 3cb25f25faacd57a90434f64d0362f2a2d2d0a90cf1a5a4c5db02d56ecc4c5bf34007208d5b887185865
    /// </summary>
    [Fact]
    public void RFC5869_TestCase1_HkdfExpand_ShouldMatchOfficialVector()
    {
        // Arrange
        byte[] prk = Convert.FromHexString("077709362c2e32df0ddc3f0dc47bba6390b6c73bb50f9c3122ec844ad7c2b3e5");
        byte[] info = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9");
        const int outputLength = 42;
        const string expectedOkmHex = "3cb25f25faacd57a90434f64d0362f2a2d2d0a90cf1a5a4c5db02d56ecc4c5bf34007208d5b887185865";

        // Act
        byte[] okm = HKDF.Expand(HashAlgorithmName.SHA256, prk, outputLength, info);
        string actualOkmHex = Convert.ToHexStringLower(okm);

        // Assert
        Assert.Equal(expectedOkmHex, actualOkmHex);
    }

    [Fact]
    public void RFC5869_TestCase1_HkdfDeriveKey_FullExtractAndExpand_ShouldMatchOfficialVector()
    {
        // Arrange
        byte[] ikm = Convert.FromHexString("0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b");
        byte[] salt = Convert.FromHexString("000102030405060708090a0b0c");
        byte[] info = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9");
        const int outputLength = 42;
        const string expectedOkmHex = "3cb25f25faacd57a90434f64d0362f2a2d2d0a90cf1a5a4c5db02d56ecc4c5bf34007208d5b887185865";

        // Act
        byte[] okm = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, outputLength, salt, info);
        string actualOkmHex = Convert.ToHexStringLower(okm);

        // Assert
        Assert.Equal(expectedOkmHex, actualOkmHex);
    }
}
