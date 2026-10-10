using PasswordManager.Application.Models;

namespace PasswordManager.Application.Abstractions;

/// <summary>
/// Port for persisting and retrieving encrypted envelopes and header bytes.
/// Strictly restricted to encrypted data; plaintext credentials never pass through this port.
/// </summary>
public interface IVaultStorageService
{
    /// <summary>
    /// Checks whether the vault database file exists and contains a valid VaultHeader row.
    /// </summary>
    Task<bool> VaultExistsAsync(string databasePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates the 132-byte VaultHeader bytes in the single-row VaultHeader table.
    /// </summary>
    Task SaveHeaderAsync(string databasePath, byte[] headerBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads raw 132-byte VaultHeader bytes, or returns null if not found.
    /// </summary>
    Task<byte[]?> GetHeaderBytesAsync(string databasePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates the encrypted VaultManifest envelope.
    /// </summary>
    Task SaveManifestAsync(string databasePath, EncryptedEnvelope manifestEnvelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the encrypted VaultManifest envelope, or null if not created yet.
    /// </summary>
    Task<EncryptedEnvelope?> GetManifestAsync(string databasePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates an encrypted VaultRecord envelope by RecordId.
    /// </summary>
    Task SaveRecordAsync(string databasePath, Guid recordId, EncryptedEnvelope recordEnvelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single encrypted VaultRecord envelope by RecordId, or null if not found.
    /// </summary>
    Task<EncryptedEnvelope?> GetRecordAsync(string databasePath, Guid recordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all encrypted VaultRecord envelopes in the vault.
    /// </summary>
    Task<IReadOnlyList<EncryptedEnvelope>> GetAllRecordsAsync(string databasePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an encrypted VaultRecord envelope by RecordId. Returns true if found and deleted.
    /// </summary>
    Task<bool> DeleteRecordAsync(string databasePath, Guid recordId, CancellationToken cancellationToken = default);
}
