using PasswordManager.Application.Models;

namespace PasswordManager.Application.Abstractions;

/// <summary>
/// Port for Argon2id key derivation from master password and salt.
/// </summary>
public interface IKeyDerivationService
{
    /// <summary>
    /// Synchronously derives key bytes from supplied password and salt using validated KDF parameters.
    /// </summary>
    byte[] DeriveKey(byte[] passwordBytes, byte[] salt, in KdfParameters parameters);

    /// <summary>
    /// Asynchronously derives key bytes from supplied password and salt using validated KDF parameters.
    /// </summary>
    Task<byte[]> DeriveKeyAsync(byte[] passwordBytes, byte[] salt, KdfParameters parameters, CancellationToken cancellationToken = default);
}
