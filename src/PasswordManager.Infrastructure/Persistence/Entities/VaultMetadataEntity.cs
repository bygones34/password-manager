namespace PasswordManager.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core entity for VaultMetadata table. Stores unencrypted non-sensitive key-value pairs (e.g. SchemaVersion).
/// </summary>
public sealed class VaultMetadataEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
