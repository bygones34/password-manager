namespace PasswordManager.Application.Models;

/// <summary>
/// Domain model for the 132-byte binary VaultHeader defined in VAULT_FORMAT.md.
/// </summary>
public sealed class VaultHeaderData
{
    public const int HeaderSize = 132;
    public const uint ExpectedMagic = 0x50574D56; // ASCII "PWMV" in big-endian
    public const uint CurrentFormatVersion = 1;
    public const ushort Argon2idAlgorithmId = 0x0001;

    public uint Magic { get; }
    public uint CryptoFormatVersion { get; }
    public Guid VaultId { get; }
    public ushort KdfAlgorithmId { get; }
    public KdfParameters Parameters { get; }
    public byte[] Salt { get; }
    public WrappedKeyData WrappedRootKey { get; }

    public VaultHeaderData(
        Guid vaultId,
        KdfParameters parameters,
        byte[] salt,
        WrappedKeyData wrappedRootKey,
        uint magic = ExpectedMagic,
        uint cryptoFormatVersion = CurrentFormatVersion,
        ushort kdfAlgorithmId = Argon2idAlgorithmId)
    {
        ArgumentNullException.ThrowIfNull(salt);
        ArgumentNullException.ThrowIfNull(wrappedRootKey);

        Magic = magic;
        CryptoFormatVersion = cryptoFormatVersion;
        VaultId = vaultId;
        KdfAlgorithmId = kdfAlgorithmId;
        Parameters = parameters;
        Salt = salt;
        WrappedRootKey = wrappedRootKey;
    }
}
