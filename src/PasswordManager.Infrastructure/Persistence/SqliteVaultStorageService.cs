using Microsoft.EntityFrameworkCore;
using PasswordManager.Application.Abstractions;
using PasswordManager.Application.Models;
using PasswordManager.Infrastructure.Persistence.Entities;

namespace PasswordManager.Infrastructure.Persistence;

/// <summary>
/// SQLite implementation of IVaultStorageService using EF Core.
/// Operates strictly with encrypted envelopes and raw header bytes.
/// </summary>
public sealed class SqliteVaultStorageService : IVaultStorageService
{
    public async Task<bool> VaultExistsAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        if (!File.Exists(databasePath))
        {
            return false;
        }

        await using var context = VaultDbContext.CreateForDatabase(databasePath);
        return await context.Headers
            .AsNoTracking()
            .AnyAsync(h => h.Id == 1, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveHeaderAsync(string databasePath, byte[] headerBytes, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(headerBytes);

        if (headerBytes.Length != VaultHeaderData.HeaderSize)
        {
            throw new ArgumentException($"Header bytes must be exactly {VaultHeaderData.HeaderSize} bytes.", nameof(headerBytes));
        }

        await using var context = VaultDbContext.CreateForDatabase(databasePath);
        var existing = await context.Headers
            .FirstOrDefaultAsync(h => h.Id == 1, cancellationToken)
            .ConfigureAwait(false);

        string now = DateTime.UtcNow.ToString("O");

        if (existing is null)
        {
            context.Headers.Add(new VaultHeaderEntity
            {
                Id = 1,
                HeaderBytes = (byte[])headerBytes.Clone(),
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            existing.HeaderBytes = (byte[])headerBytes.Clone();
            existing.UpdatedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<byte[]?> GetHeaderBytesAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        if (!File.Exists(databasePath))
        {
            return null;
        }

        await using var context = VaultDbContext.CreateForDatabase(databasePath);
        var header = await context.Headers
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == 1, cancellationToken)
            .ConfigureAwait(false);

        return header?.HeaderBytes;
    }

    public async Task SaveManifestAsync(string databasePath, EncryptedEnvelope manifestEnvelope, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(manifestEnvelope);

        await using var context = VaultDbContext.CreateForDatabase(databasePath);
        var existing = await context.Manifests
            .FirstOrDefaultAsync(m => m.Id == 1, cancellationToken)
            .ConfigureAwait(false);

        string now = DateTime.UtcNow.ToString("O");

        if (existing is null)
        {
            context.Manifests.Add(new VaultManifestEntity
            {
                Id = 1,
                EnvelopeVersion = manifestEnvelope.EnvelopeVersion,
                Nonce = (byte[])manifestEnvelope.Nonce.Clone(),
                Tag = (byte[])manifestEnvelope.Tag.Clone(),
                Ciphertext = (byte[])manifestEnvelope.Ciphertext.Clone(),
                UpdatedAt = now
            });
        }
        else
        {
            existing.EnvelopeVersion = manifestEnvelope.EnvelopeVersion;
            existing.Nonce = (byte[])manifestEnvelope.Nonce.Clone();
            existing.Tag = (byte[])manifestEnvelope.Tag.Clone();
            existing.Ciphertext = (byte[])manifestEnvelope.Ciphertext.Clone();
            existing.UpdatedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<EncryptedEnvelope?> GetManifestAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        if (!File.Exists(databasePath))
        {
            return null;
        }

        await using var context = VaultDbContext.CreateForDatabase(databasePath);
        var manifest = await context.Manifests
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == 1, cancellationToken)
            .ConfigureAwait(false);

        if (manifest is null)
        {
            return null;
        }

        return new EncryptedEnvelope(
            manifest.EnvelopeVersion,
            manifest.Nonce,
            manifest.Tag,
            manifest.Ciphertext);
    }

    public async Task SaveRecordAsync(
        string databasePath,
        Guid recordId,
        EncryptedEnvelope recordEnvelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(recordEnvelope);

        string recordIdStr = recordId.ToString();
        string now = DateTime.UtcNow.ToString("O");

        await using var context = VaultDbContext.CreateForDatabase(databasePath);
        var existing = await context.Records
            .FirstOrDefaultAsync(r => r.RecordId == recordIdStr, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            context.Records.Add(new VaultRecordEntity
            {
                RecordId = recordIdStr,
                EnvelopeVersion = recordEnvelope.EnvelopeVersion,
                Nonce = (byte[])recordEnvelope.Nonce.Clone(),
                Tag = (byte[])recordEnvelope.Tag.Clone(),
                Ciphertext = (byte[])recordEnvelope.Ciphertext.Clone(),
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            existing.EnvelopeVersion = recordEnvelope.EnvelopeVersion;
            existing.Nonce = (byte[])recordEnvelope.Nonce.Clone();
            existing.Tag = (byte[])recordEnvelope.Tag.Clone();
            existing.Ciphertext = (byte[])recordEnvelope.Ciphertext.Clone();
            existing.UpdatedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<EncryptedEnvelope?> GetRecordAsync(
        string databasePath,
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        if (!File.Exists(databasePath))
        {
            return null;
        }

        string recordIdStr = recordId.ToString();
        await using var context = VaultDbContext.CreateForDatabase(databasePath);
        var record = await context.Records
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RecordId == recordIdStr, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return null;
        }

        return new EncryptedEnvelope(
            record.EnvelopeVersion,
            record.Nonce,
            record.Tag,
            record.Ciphertext);
    }

    public async Task<IReadOnlyList<EncryptedEnvelope>> GetAllRecordsAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        if (!File.Exists(databasePath))
        {
            return [];
        }

        await using var context = VaultDbContext.CreateForDatabase(databasePath);
        var records = await context.Records
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .Select(r => new EncryptedEnvelope(r.EnvelopeVersion, r.Nonce, r.Tag, r.Ciphertext))
            .ToList();
    }

    public async Task<bool> DeleteRecordAsync(
        string databasePath,
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        if (!File.Exists(databasePath))
        {
            return false;
        }

        string recordIdStr = recordId.ToString();
        await using var context = VaultDbContext.CreateForDatabase(databasePath);
        var record = await context.Records
            .FirstOrDefaultAsync(r => r.RecordId == recordIdStr, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return false;
        }

        context.Records.Remove(record);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
