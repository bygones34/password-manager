using Microsoft.EntityFrameworkCore;
using PasswordManager.Application.Abstractions;
using PasswordManager.Infrastructure.Persistence.Entities;

namespace PasswordManager.Infrastructure.Persistence;

/// <summary>
/// Initializes SQLite database schema, applies WAL configuration and validates schema versioning.
/// </summary>
public sealed class VaultDatabaseInitializer : IVaultDatabaseInitializer
{
    public async Task InitializeDatabaseAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        string? directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var context = VaultDbContext.CreateForDatabase(databasePath);

        // Ensure database tables exist
        await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        // Configure WAL mode and foreign keys for durability
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", cancellationToken).ConfigureAwait(false);
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;", cancellationToken).ConfigureAwait(false);
        await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL;", cancellationToken).ConfigureAwait(false);

        // Verify or initialize schema version in VaultMetadata
        var schemaEntry = await context.Metadata
            .FirstOrDefaultAsync(m => m.Key == "SchemaVersion", cancellationToken)
            .ConfigureAwait(false);

        if (schemaEntry is null)
        {
            context.Metadata.Add(new VaultMetadataEntity
            {
                Key = "SchemaVersion",
                Value = IVaultDatabaseInitializer.CurrentSchemaVersion.ToString()
            });

            context.Metadata.Add(new VaultMetadataEntity
            {
                Key = "CreatedAt",
                Value = DateTime.UtcNow.ToString("O")
            });

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            if (int.TryParse(schemaEntry.Value, out int recordedVersion))
            {
                if (recordedVersion > IVaultDatabaseInitializer.CurrentSchemaVersion)
                {
                    throw new InvalidOperationException(
                        $"Veritabanı daha yeni bir şema sürümü (v{recordedVersion}) ile oluşturulmuş. Bu uygulama yalnızca v{IVaultDatabaseInitializer.CurrentSchemaVersion} sürümünü desteklemektedir.");
                }
            }
        }
    }

    public async Task<int> GetSchemaVersionAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        if (!File.Exists(databasePath))
        {
            return 0;
        }

        await using var context = VaultDbContext.CreateForDatabase(databasePath);
        var schemaEntry = await context.Metadata
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Key == "SchemaVersion", cancellationToken)
            .ConfigureAwait(false);

        if (schemaEntry is not null && int.TryParse(schemaEntry.Value, out int version))
        {
            return version;
        }

        return 0;
    }
}
