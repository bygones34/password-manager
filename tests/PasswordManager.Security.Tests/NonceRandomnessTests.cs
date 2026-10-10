using PasswordManager.Application.Models;
using PasswordManager.Security;
using Xunit;

namespace PasswordManager.Security.Tests;

public sealed class NonceRandomnessTests
{
    private readonly AeadEnvelopeService _envelopeService = new();

    [Fact]
    public void SEC_D01_ConsecutiveEncryptions_ShouldNeverReuseNonce()
    {
        // Arrange
        Guid vaultId = Guid.NewGuid();
        Guid recordId = Guid.NewGuid();
        byte[] recordKey = _envelopeService.GenerateRandomBytes(32);
        byte[] constantPlaintext = "SensitivePassword123!"u8.ToArray();

        const int iterations = 1000;
        var nonces = new HashSet<string>(iterations);
        var ciphertexts = new HashSet<string>(iterations);

        // Act
        for (int i = 0; i < iterations; i++)
        {
            EncryptedEnvelope envelope = _envelopeService.EncryptRecord(
                recordKey, vaultId, recordId, constantPlaintext);

            string nonceHex = Convert.ToHexString(envelope.Nonce);
            string ctHex = Convert.ToHexString(envelope.Ciphertext);

            // Assert: no collisions
            Assert.DoesNotContain(nonceHex, nonces);
            Assert.DoesNotContain(ctHex, ciphertexts);

            nonces.Add(nonceHex);
            ciphertexts.Add(ctHex);
        }

        // Final verification
        Assert.Equal(iterations, nonces.Count);
        Assert.Equal(iterations, ciphertexts.Count);
    }

    [Fact]
    public void SEC_D02_GeneratedNonces_ShouldHaveHighEntropy()
    {
        // Assert nonces are not all zeroes, repeating or predictable
        byte[] nonce = _envelopeService.GenerateRandomBytes(12);

        Assert.Equal(12, nonce.Length);
        Assert.Contains(nonce, b => b != 0); // Not all zeroes
        Assert.True(nonce.Distinct().Count() > 2); // Non-trivial entropy
    }
}
