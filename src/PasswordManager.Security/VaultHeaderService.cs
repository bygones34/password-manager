using System.Buffers.Binary;
using PasswordManager.Application.Abstractions;
using PasswordManager.Application.Exceptions;
using PasswordManager.Application.Models;
using PasswordManager.Application.Validation;

namespace PasswordManager.Security;

/// <summary>
/// Implements binary serialization, bounded validation and canonical AAD computation for the 132-byte VaultHeader.
/// </summary>
public sealed class VaultHeaderService : IVaultHeaderService
{
    public const int HeaderSize = VaultHeaderData.HeaderSize; // 132 bytes
    public const int HeaderAadSize = 72; // First 72 bytes (Magic + FormatVersion + VaultId + KdfId + Params + Salt)

    public byte[] Serialize(VaultHeaderData headerData)
    {
        ArgumentNullException.ThrowIfNull(headerData);
        KdfParametersValidator.ValidateInputs(headerData.Parameters, [1], headerData.Salt);

        byte[] buffer = new byte[HeaderSize];
        Span<byte> span = buffer.AsSpan();

        // 1. Magic (4 bytes)
        BinaryPrimitives.WriteUInt32BigEndian(span[0..4], headerData.Magic);

        // 2. CryptoFormatVersion (4 bytes)
        BinaryPrimitives.WriteUInt32BigEndian(span[4..8], headerData.CryptoFormatVersion);

        // 3. VaultId (16 bytes, RFC 4122 big-endian)
        if (!headerData.VaultId.TryWriteBytes(span[8..24], bigEndian: true, out _))
        {
            throw new InvalidOperationException("Failed to write VaultId bytes.");
        }

        // 4. KdfAlgorithmId (2 bytes)
        BinaryPrimitives.WriteUInt16BigEndian(span[24..26], headerData.KdfAlgorithmId);

        // 5. MemoryKiB (4 bytes)
        BinaryPrimitives.WriteUInt32BigEndian(span[26..30], (uint)headerData.Parameters.MemoryKiB);

        // 6. Iterations (4 bytes)
        BinaryPrimitives.WriteUInt32BigEndian(span[30..34], (uint)headerData.Parameters.Iterations);

        // 7. Parallelism (4 bytes)
        BinaryPrimitives.WriteUInt32BigEndian(span[34..38], (uint)headerData.Parameters.DegreeOfParallelism);

        // 8. SaltLength (2 bytes)
        BinaryPrimitives.WriteUInt16BigEndian(span[38..40], (ushort)headerData.Parameters.SaltLength);

        // 9. Salt (32 bytes)
        headerData.Salt.CopyTo(span[40..72]);

        // 10. WrappedKeyNonce (12 bytes)
        headerData.WrappedRootKey.Nonce.CopyTo(span[72..84]);

        // 11. WrappedKeyTag (16 bytes)
        headerData.WrappedRootKey.Tag.CopyTo(span[84..100]);

        // 12. WrappedKeyCiphertext (32 bytes)
        headerData.WrappedRootKey.Ciphertext.CopyTo(span[100..132]);

        return buffer;
    }

    public VaultHeaderData Deserialize(byte[] headerBytes)
    {
        if (headerBytes is null || headerBytes.Length != HeaderSize)
        {
            throw new CryptoAuthenticationException(
                $"Kasa başlığı boyutu geçersiz (beklenen {HeaderSize} bayt, alınan {headerBytes?.Length ?? 0} bayt).");
        }

        ReadOnlySpan<byte> span = headerBytes.AsSpan();

        // 1. Magic
        uint magic = BinaryPrimitives.ReadUInt32BigEndian(span[0..4]);
        if (magic != VaultHeaderData.ExpectedMagic)
        {
            throw new CryptoAuthenticationException("Geçersiz kasa dosyası: Magic baytları eşleşmiyor.");
        }

        // 2. Format Version
        uint formatVersion = BinaryPrimitives.ReadUInt32BigEndian(span[4..8]);
        if (formatVersion != VaultHeaderData.CurrentFormatVersion)
        {
            throw new CryptoAuthenticationException($"Desteklenmeyen kripto format sürümü: {formatVersion}.");
        }

        // 3. VaultId
        Guid vaultId = new(span[8..24], bigEndian: true);

        // 4. KdfAlgorithmId
        ushort kdfId = BinaryPrimitives.ReadUInt16BigEndian(span[24..26]);
        if (kdfId != VaultHeaderData.Argon2idAlgorithmId)
        {
            throw new CryptoAuthenticationException($"Desteklenmeyen KDF algoritması: {kdfId}.");
        }

        // 5..8 KdfParameters
        int memoryKiB = (int)BinaryPrimitives.ReadUInt32BigEndian(span[26..30]);
        int iterations = (int)BinaryPrimitives.ReadUInt32BigEndian(span[30..34]);
        int parallelism = (int)BinaryPrimitives.ReadUInt32BigEndian(span[34..38]);
        int saltLength = BinaryPrimitives.ReadUInt16BigEndian(span[38..40]);

        var parameters = new KdfParameters(memoryKiB, iterations, parallelism, saltLength, keyLength: 32);

        // Katı Anti-DoS sınır denetimi
        KdfParametersValidator.Validate(parameters);

        // 9. Salt
        byte[] salt = span[40..72].ToArray();

        // 10..12 WrappedRootKey
        byte[] nonce = span[72..84].ToArray();
        byte[] tag = span[84..100].ToArray();
        byte[] ciphertext = span[100..132].ToArray();

        var wrappedKey = new WrappedKeyData(nonce, tag, ciphertext);

        return new VaultHeaderData(
            vaultId: vaultId,
            parameters: parameters,
            salt: salt,
            wrappedRootKey: wrappedKey,
            magic: magic,
            cryptoFormatVersion: formatVersion,
            kdfAlgorithmId: kdfId);
    }

    public byte[] ComputeHeaderAad(
        Guid vaultId,
        ushort kdfAlgorithmId,
        in KdfParameters parameters,
        byte[] salt,
        uint magic = VaultHeaderData.ExpectedMagic,
        uint cryptoFormatVersion = VaultHeaderData.CurrentFormatVersion)
    {
        ArgumentNullException.ThrowIfNull(salt);
        byte[] aad = new byte[HeaderAadSize];
        Span<byte> span = aad.AsSpan();

        BinaryPrimitives.WriteUInt32BigEndian(span[0..4], magic);
        BinaryPrimitives.WriteUInt32BigEndian(span[4..8], cryptoFormatVersion);
        vaultId.TryWriteBytes(span[8..24], bigEndian: true, out _);
        BinaryPrimitives.WriteUInt16BigEndian(span[24..26], kdfAlgorithmId);
        BinaryPrimitives.WriteUInt32BigEndian(span[26..30], (uint)parameters.MemoryKiB);
        BinaryPrimitives.WriteUInt32BigEndian(span[30..34], (uint)parameters.Iterations);
        BinaryPrimitives.WriteUInt32BigEndian(span[34..38], (uint)parameters.DegreeOfParallelism);
        BinaryPrimitives.WriteUInt16BigEndian(span[38..40], (ushort)parameters.SaltLength);
        salt.CopyTo(span[40..72]);

        return aad;
    }
}
