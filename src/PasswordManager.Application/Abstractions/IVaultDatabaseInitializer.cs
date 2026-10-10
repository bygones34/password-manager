namespace PasswordManager.Application.Abstractions;

/// <summary>
/// Port for initializing SQLite database schema, setting metadata and verifying schema version.
/// </summary>
public interface IVaultDatabaseInitializer
{
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Ensures database schema is created, SQLite WAL mode is configured, and initial metadata is recorded.
    /// </summary>
    Task InitializeDatabaseAsync(string databasePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads recorded SchemaVersion from VaultMetadata table. Returns 0 if metadata is missing.
    /// </summary>
    Task<int> GetSchemaVersionAsync(string databasePath, CancellationToken cancellationToken = default);
}
