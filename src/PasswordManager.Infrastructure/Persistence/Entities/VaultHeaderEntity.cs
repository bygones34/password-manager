namespace PasswordManager.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core entity for VaultHeader table. Single row constraint (Id = 1).
/// Stores only raw 132-byte header bytes; contains zero plaintext credentials.
/// </summary>
public sealed class VaultHeaderEntity
{
    public int Id { get; set; } = 1;
    public byte[] HeaderBytes { get; set; } = [];
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
