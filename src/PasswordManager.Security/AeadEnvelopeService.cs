using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using PasswordManager.Application.Abstractions;
using PasswordManager.Application.Exceptions;
using PasswordManager.Application.Models;

namespace PasswordManager.Security;

/// <summary>
/// Implements AES-256-GCM envelope encryption, key wrapping and HKDF-SHA-256 separation.
/// </summary>
public sealed class AeadEnvelopeService : IAeadEnvelopeService
{
    private static readonly byte[] RecordKeyInfoPrefix = Encoding.UTF8.GetBytes("PWMV1-RECORD-KEY");
    private static readonly byte[] ManifestKeyInfoPrefix = Encoding.UTF8.GetBytes("PWMV1-MANIFEST-KEY");
    private static readonly byte[] RecordAadSuffix = Encoding.UTF8.GetBytes("PWMV1-RECORD");
    private static readonly byte[] ManifestAadSuffix = Encoding.UTF8.GetBytes("PWMV1-MANIFEST");

    public byte[] GenerateRandomBytes(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        byte[] bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    public WrappedKeyData WrapRootKey(byte[] kek, byte[] rootKey, byte[] headerAad)
    {
        ValidateKey(kek, 32, nameof(kek));
        ValidateKey(rootKey, 32, nameof(rootKey));
        ArgumentNullException.ThrowIfNull(headerAad);

        byte[] nonce = GenerateRandomBytes(EncryptedEnvelope.NonceLength);
        byte[] tag = new byte[EncryptedEnvelope.TagLength];
        byte[] ciphertext = new byte[rootKey.Length];

        using var aesGcm = new AesGcm(kek, EncryptedEnvelope.TagLength);
        aesGcm.Encrypt(nonce, rootKey, ciphertext, tag, headerAad);

        return new WrappedKeyData(nonce, tag, ciphertext);
    }

    public byte[] UnwrapRootKey(byte[] kek, WrappedKeyData wrappedKey, byte[] headerAad)
    {
        ValidateKey(kek, 32, nameof(kek));
        ArgumentNullException.ThrowIfNull(wrappedKey);
        ArgumentNullException.ThrowIfNull(headerAad);

        byte[] rootKey = new byte[wrappedKey.Ciphertext.Length];

        try
        {
            using var aesGcm = new AesGcm(kek, EncryptedEnvelope.TagLength);
            aesGcm.Decrypt(wrappedKey.Nonce, wrappedKey.Ciphertext, wrappedKey.Tag, rootKey, headerAad);
            return rootKey;
        }
        catch (CryptographicException ex)
        {
            CryptographicOperations.ZeroMemory(rootKey);
            throw new CryptoAuthenticationException(CryptoAuthenticationException.DefaultSafeMessage, ex);
        }
    }

    public byte[] DeriveRecordKey(byte[] rootKey, Guid vaultId)
    {
        ValidateKey(rootKey, 32, nameof(rootKey));

        byte[] info = CombineInfo(RecordKeyInfoPrefix, vaultId);
        return HKDF.Expand(HashAlgorithmName.SHA256, rootKey, 32, info);
    }

    public byte[] DeriveManifestKey(byte[] rootKey, Guid vaultId)
    {
        ValidateKey(rootKey, 32, nameof(rootKey));

        byte[] info = CombineInfo(ManifestKeyInfoPrefix, vaultId);
        return HKDF.Expand(HashAlgorithmName.SHA256, rootKey, 32, info);
    }

    public EncryptedEnvelope EncryptRecord(
        byte[] recordKey,
        Guid vaultId,
        Guid recordId,
        byte[] plaintextPayload,
        int envelopeVersion = EncryptedEnvelope.CurrentVersion)
    {
        ValidateKey(recordKey, 32, nameof(recordKey));
        ArgumentNullException.ThrowIfNull(plaintextPayload);

        byte[] nonce = GenerateRandomBytes(EncryptedEnvelope.NonceLength);
        byte[] tag = new byte[EncryptedEnvelope.TagLength];
        byte[] ciphertext = new byte[plaintextPayload.Length];
        byte[] aad = BuildRecordAad(vaultId, recordId, envelopeVersion);

        using var aesGcm = new AesGcm(recordKey, EncryptedEnvelope.TagLength);
        aesGcm.Encrypt(nonce, plaintextPayload, ciphertext, tag, aad);

        return new EncryptedEnvelope(envelopeVersion, nonce, tag, ciphertext);
    }

    public byte[] DecryptRecord(byte[] recordKey, Guid vaultId, Guid recordId, EncryptedEnvelope envelope)
    {
        ValidateKey(recordKey, 32, nameof(recordKey));
        ArgumentNullException.ThrowIfNull(envelope);

        byte[] plaintext = new byte[envelope.Ciphertext.Length];
        byte[] aad = BuildRecordAad(vaultId, recordId, envelope.EnvelopeVersion);

        try
        {
            using var aesGcm = new AesGcm(recordKey, EncryptedEnvelope.TagLength);
            aesGcm.Decrypt(envelope.Nonce, envelope.Ciphertext, envelope.Tag, plaintext, aad);
            return plaintext;
        }
        catch (CryptographicException ex)
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw new CryptoAuthenticationException(CryptoAuthenticationException.DefaultSafeMessage, ex);
        }
    }

    public EncryptedEnvelope EncryptManifest(
        byte[] manifestKey,
        Guid vaultId,
        byte[] plaintextPayload,
        int envelopeVersion = EncryptedEnvelope.CurrentVersion)
    {
        ValidateKey(manifestKey, 32, nameof(manifestKey));
        ArgumentNullException.ThrowIfNull(plaintextPayload);

        byte[] nonce = GenerateRandomBytes(EncryptedEnvelope.NonceLength);
        byte[] tag = new byte[EncryptedEnvelope.TagLength];
        byte[] ciphertext = new byte[plaintextPayload.Length];
        byte[] aad = BuildManifestAad(vaultId, envelopeVersion);

        using var aesGcm = new AesGcm(manifestKey, EncryptedEnvelope.TagLength);
        aesGcm.Encrypt(nonce, plaintextPayload, ciphertext, tag, aad);

        return new EncryptedEnvelope(envelopeVersion, nonce, tag, ciphertext);
    }

    public byte[] DecryptManifest(byte[] manifestKey, Guid vaultId, EncryptedEnvelope envelope)
    {
        ValidateKey(manifestKey, 32, nameof(manifestKey));
        ArgumentNullException.ThrowIfNull(envelope);

        byte[] plaintext = new byte[envelope.Ciphertext.Length];
        byte[] aad = BuildManifestAad(vaultId, envelope.EnvelopeVersion);

        try
        {
            using var aesGcm = new AesGcm(manifestKey, EncryptedEnvelope.TagLength);
            aesGcm.Decrypt(envelope.Nonce, envelope.Ciphertext, envelope.Tag, plaintext, aad);
            return plaintext;
        }
        catch (CryptographicException ex)
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw new CryptoAuthenticationException(CryptoAuthenticationException.DefaultSafeMessage, ex);
        }
    }

    public static byte[] BuildRecordAad(Guid vaultId, Guid recordId, int envelopeVersion)
    {
        // VaultId (16B) || RecordId (16B) || EnvelopeVersion (4B) || "PWMV1-RECORD" (12B) = 48 bytes
        byte[] aad = new byte[48];
        Span<byte> span = aad.AsSpan();

        vaultId.TryWriteBytes(span[0..16], bigEndian: true, out _);
        recordId.TryWriteBytes(span[16..32], bigEndian: true, out _);
        BinaryPrimitives.WriteInt32BigEndian(span[32..36], envelopeVersion);
        RecordAadSuffix.CopyTo(span[36..48]);

        return aad;
    }

    public static byte[] BuildManifestAad(Guid vaultId, int envelopeVersion)
    {
        // VaultId (16B) || EnvelopeVersion (4B) || "PWMV1-MANIFEST" (14B) = 34 bytes
        byte[] aad = new byte[34];
        Span<byte> span = aad.AsSpan();

        vaultId.TryWriteBytes(span[0..16], bigEndian: true, out _);
        BinaryPrimitives.WriteInt32BigEndian(span[16..20], envelopeVersion);
        ManifestAadSuffix.CopyTo(span[20..34]);

        return aad;
    }

    private static byte[] CombineInfo(byte[] prefix, Guid vaultId)
    {
        byte[] info = new byte[prefix.Length + 16];
        prefix.CopyTo(info, 0);
        vaultId.TryWriteBytes(info.AsSpan(prefix.Length, 16), bigEndian: true, out _);
        return info;
    }

    private static void ValidateKey(byte[] key, int expectedLength, string paramName)
    {
        if (key is null || key.Length != expectedLength)
        {
            throw new ArgumentException($"Key must be exactly {expectedLength} bytes.", paramName);
        }
    }
}
