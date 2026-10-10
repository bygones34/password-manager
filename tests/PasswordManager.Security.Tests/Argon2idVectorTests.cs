using PasswordManager.Security;
using Xunit;

namespace PasswordManager.Security.Tests;

public sealed class Argon2idVectorTests
{
    /// <summary>
    /// Validates the implementation against official RFC 9106 Section 5.3 Argon2id test vector.
    /// Memory: 32 KiB, Passes: 3, Parallelism: 4 lanes, Tag length: 32 bytes
    /// Password[32]: 0101...01
    /// Salt[16]: 0202...02
    /// Secret[8]: 0303...03
    /// Associated Data[12]: 0404...04
    /// Expected Tag: 0d640df58d78766c08c037a34a8b53c9d01ef0452d75b65eb52520e96b01e659
    /// </summary>
    [Fact]
    public void RFC9106_Section5_3_Argon2id_TestVector_ShouldMatchExactly()
    {
        // Arrange
        byte[] password = new byte[32];
        Array.Fill<byte>(password, 0x01);

        byte[] salt = new byte[16];
        Array.Fill<byte>(salt, 0x02);

        byte[] secret = new byte[8];
        Array.Fill<byte>(secret, 0x03);

        byte[] associatedData = new byte[12];
        Array.Fill<byte>(associatedData, 0x04);

        const int memoryKiB = 32;
        const int iterations = 3;
        const int parallelism = 4;
        const int tagLength = 32;

        const string expectedHexTag = "0d640df58d78766c08c037a34a8b53c9d01ef0452d75b65eb52520e96b01e659";

        // Act
        byte[] actualTag = Argon2idKeyDerivationService.DeriveRaw(
            passwordBytes: password,
            salt: salt,
            memoryKiB: memoryKiB,
            iterations: iterations,
            degreeOfParallelism: parallelism,
            outputLength: tagLength,
            knownSecret: secret,
            associatedData: associatedData);

        string actualHexTag = Convert.ToHexStringLower(actualTag);

        // Assert
        Assert.Equal(expectedHexTag, actualHexTag);
    }

    [Fact]
    public void Argon2id_IdenticalInputs_ShouldProduceDeterministicOutput()
    {
        byte[] password = [10, 20, 30, 40, 50];
        byte[] salt = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];

        byte[] out1 = Argon2idKeyDerivationService.DeriveRaw(password, salt, 64, 1, 1, 32);
        byte[] out2 = Argon2idKeyDerivationService.DeriveRaw(password, salt, 64, 1, 1, 32);

        Assert.Equal(out1, out2);
    }

    [Fact]
    public void Argon2id_DifferentPasswordOrSalt_ShouldProduceDrasticallyDifferentOutput()
    {
        byte[] password1 = [10, 20, 30, 40, 50];
        byte[] password2 = [10, 20, 30, 40, 51]; // 1-bit difference
        byte[] salt = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];

        byte[] out1 = Argon2idKeyDerivationService.DeriveRaw(password1, salt, 64, 1, 1, 32);
        byte[] out2 = Argon2idKeyDerivationService.DeriveRaw(password2, salt, 64, 1, 1, 32);

        Assert.NotEqual(out1, out2);
    }
}
