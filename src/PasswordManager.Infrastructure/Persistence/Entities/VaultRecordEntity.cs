namespace PasswordManager.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core entity for VaultRecords table.
/// Stores only AES-GCM encrypted envelope; contains zero plaintext credentials.
/// </summary>
public sealed class VaultRecordEntity
{
    public string RecordId { get; set; } = string.Empty;
    public int EnvelopeVersion { get; set; } = 1;
    public byte[] Nonce { get; set; } = [];
    public byte[] Tag { get; set; } = [];
    public byte[] Ciphertext { get; set; } = [];
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
