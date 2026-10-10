namespace PasswordManager.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core entity for VaultManifest table. Single row constraint (Id = 1).
/// Stores only AES-GCM encrypted envelope; contains zero plaintext credentials.
/// </summary>
public sealed class VaultManifestEntity
{
    public int Id { get; set; } = 1;
    public int EnvelopeVersion { get; set; } = 1;
    public byte[] Nonce { get; set; } = [];
    public byte[] Tag { get; set; } = [];
    public byte[] Ciphertext { get; set; } = [];
    public string UpdatedAt { get; set; } = string.Empty;
}
