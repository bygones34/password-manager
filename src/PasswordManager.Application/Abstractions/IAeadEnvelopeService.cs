using PasswordManager.Application.Models;

namespace PasswordManager.Application.Abstractions;

/// <summary>
/// Cryptographic port for AES-256-GCM envelope encryption, key wrapping and HKDF-SHA-256 separation.
/// </summary>
public interface IAeadEnvelopeService
{
    /// <summary>
    /// Generates CSPRNG random bytes (e.g. for root key, salt or nonce).
    /// </summary>
    byte[] GenerateRandomBytes(int length);

    /// <summary>
    /// Wraps the 256-bit root key with KEK and canonical header AAD using AES-256-GCM.
    /// </summary>
    WrappedKeyData WrapRootKey(byte[] kek, byte[] rootKey, byte[] headerAad);

    /// <summary>
    /// Unwraps the root key using KEK and canonical header AAD. Throws CryptoAuthenticationException on failure.
    /// </summary>
    byte[] UnwrapRootKey(byte[] kek, WrappedKeyData wrappedKey, byte[] headerAad);

    /// <summary>
    /// Derives 256-bit RecordKey from RootKey and VaultId using HKDF-SHA-256.
    /// </summary>
    byte[] DeriveRecordKey(byte[] rootKey, Guid vaultId);

    /// <summary>
    /// Derives 256-bit ManifestKey from RootKey and VaultId using HKDF-SHA-256.
    /// </summary>
    byte[] DeriveManifestKey(byte[] rootKey, Guid vaultId);

    /// <summary>
    /// Encrypts record plaintext payload into an AES-256-GCM envelope bound with Record AAD.
    /// </summary>
    EncryptedEnvelope EncryptRecord(
        byte[] recordKey,
        Guid vaultId,
        Guid recordId,
        byte[] plaintextPayload,
        int envelopeVersion = EncryptedEnvelope.CurrentVersion);

    /// <summary>
    /// Decrypts record envelope using RecordKey and verifies Record AAD. Throws CryptoAuthenticationException on failure.
    /// </summary>
    byte[] DecryptRecord(byte[] recordKey, Guid vaultId, Guid recordId, EncryptedEnvelope envelope);

    /// <summary>
    /// Encrypts manifest plaintext payload into an AES-256-GCM envelope bound with Manifest AAD.
    /// </summary>
    EncryptedEnvelope EncryptManifest(
        byte[] manifestKey,
        Guid vaultId,
        byte[] plaintextPayload,
        int envelopeVersion = EncryptedEnvelope.CurrentVersion);

    /// <summary>
    /// Decrypts manifest envelope using ManifestKey and verifies Manifest AAD. Throws CryptoAuthenticationException on failure.
    /// </summary>
    byte[] DecryptManifest(byte[] manifestKey, Guid vaultId, EncryptedEnvelope envelope);
}
