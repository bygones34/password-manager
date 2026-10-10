using Microsoft.EntityFrameworkCore;
using PasswordManager.Infrastructure.Persistence.Entities;

namespace PasswordManager.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for PasswordManager SQLite database.
/// Strictly maps encrypted envelopes and non-sensitive metadata only.
/// </summary>
public sealed class VaultDbContext : DbContext
{
    public const int CurrentSchemaVersion = 1;

    public DbSet<VaultHeaderEntity> Headers => Set<VaultHeaderEntity>();
    public DbSet<VaultManifestEntity> Manifests => Set<VaultManifestEntity>();
    public DbSet<VaultRecordEntity> Records => Set<VaultRecordEntity>();
    public DbSet<VaultMetadataEntity> Metadata => Set<VaultMetadataEntity>();

    public VaultDbContext(DbContextOptions<VaultDbContext> options) : base(options)
    {
    }

    public static VaultDbContext CreateForDatabase(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var optionsBuilder = new DbContextOptionsBuilder<VaultDbContext>();
        optionsBuilder.UseSqlite($"Data Source={databasePath}");
        return new VaultDbContext(optionsBuilder.Options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        // VaultHeader table mapping
        modelBuilder.Entity<VaultHeaderEntity>(entity =>
        {
            entity.ToTable("VaultHeader");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.HeaderBytes).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_VaultHeader_Id", "Id = 1"));
        });

        // VaultManifest table mapping
        modelBuilder.Entity<VaultManifestEntity>(entity =>
        {
            entity.ToTable("VaultManifest");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.EnvelopeVersion).IsRequired();
            entity.Property(e => e.Nonce).IsRequired();
            entity.Property(e => e.Tag).IsRequired();
            entity.Property(e => e.Ciphertext).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_VaultManifest_Id", "Id = 1"));
        });

        // VaultRecords table mapping
        modelBuilder.Entity<VaultRecordEntity>(entity =>
        {
            entity.ToTable("VaultRecords");
            entity.HasKey(e => e.RecordId);
            entity.Property(e => e.RecordId).IsRequired();
            entity.Property(e => e.EnvelopeVersion).IsRequired();
            entity.Property(e => e.Nonce).IsRequired();
            entity.Property(e => e.Tag).IsRequired();
            entity.Property(e => e.Ciphertext).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        // VaultMetadata table mapping
        modelBuilder.Entity<VaultMetadataEntity>(entity =>
        {
            entity.ToTable("VaultMetadata");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).IsRequired();
            entity.Property(e => e.Value).IsRequired();
        });
    }
}
