using PasswordManager.Application.Models;

namespace PasswordManager.Application.Abstractions;

/// <summary>
/// Port for binary serialization, parsing and canonical AAD computation of the 132-byte VaultHeader.
/// </summary>
public interface IVaultHeaderService
{
    /// <summary>
    /// Serializes VaultHeaderData into canonical 132-byte binary format.
    /// </summary>
    byte[] Serialize(VaultHeaderData headerData);

    /// <summary>
    /// Parses 132-byte binary into VaultHeaderData and performs bounded validation.
    /// </summary>
    VaultHeaderData Deserialize(byte[] headerBytes);

    /// <summary>
    /// Computes canonical Header Key Wrap AAD (first 68 bytes of header).
    /// </summary>
    byte[] ComputeHeaderAad(
        Guid vaultId,
        ushort kdfAlgorithmId,
        in KdfParameters parameters,
        byte[] salt,
        uint magic = VaultHeaderData.ExpectedMagic,
        uint cryptoFormatVersion = VaultHeaderData.CurrentFormatVersion);
}
