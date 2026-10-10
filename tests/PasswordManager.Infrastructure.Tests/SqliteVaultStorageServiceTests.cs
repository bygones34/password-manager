using PasswordManager.Application.Models;
using PasswordManager.Infrastructure.Persistence;
using Xunit;

namespace PasswordManager.Infrastructure.Tests;

public sealed class SqliteVaultStorageServiceTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly VaultDatabaseInitializer _initializer = new();
    private readonly SqliteVaultStorageService _storage = new();

    public SqliteVaultStorageServiceTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"pw_storage_test_{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempDbPath))
            {
                File.Delete(_tempDbPath);
            }

            string walFile = $"{_tempDbPath}-wal";
            if (File.Exists(walFile))
            {
                File.Delete(walFile);
            }

            string shmFile = $"{_tempDbPath}-shm";
            if (File.Exists(shmFile))
            {
                File.Delete(shmFile);
            }
        }
        catch
        {
            // Best effort cleanup in tests
        }
    }

    [Fact]
    public async Task VaultExistsAsync_NonExistentDbOrEmptyHeader_ShouldReturnFalse()
    {
        // Non-existent file
        bool exists = await _storage.VaultExistsAsync(_tempDbPath);
        Assert.False(exists);

        // Initialized database without header
        await _initializer.InitializeDatabaseAsync(_tempDbPath);
        exists = await _storage.VaultExistsAsync(_tempDbPath);
        Assert.False(exists);
    }

    [Fact]
    public async Task SaveAndGetHeaderAsync_ShouldPersistExact132Bytes()
    {
        // Arrange
        await _initializer.InitializeDatabaseAsync(_tempDbPath);
        byte[] expectedHeader = new byte[132];
        Array.Fill<byte>(expectedHeader, 0x42);

        // Act
        await _storage.SaveHeaderAsync(_tempDbPath, expectedHeader);
        bool exists = await _storage.VaultExistsAsync(_tempDbPath);
        byte[]? retrievedHeader = await _storage.GetHeaderBytesAsync(_tempDbPath);

        // Assert
        Assert.True(exists);
        Assert.NotNull(retrievedHeader);
        Assert.Equal(expectedHeader, retrievedHeader);
    }

    [Fact]
    public async Task SaveAndGetManifestAsync_ShouldPersistEnvelope()
    {
        // Arrange
        await _initializer.InitializeDatabaseAsync(_tempDbPath);
        byte[] nonce = new byte[12];
        byte[] tag = new byte[16];
        byte[] ciphertext = [1, 2, 3, 4, 5, 6, 7];
        var envelope = new EncryptedEnvelope(1, nonce, tag, ciphertext);

        // Act
        await _storage.SaveManifestAsync(_tempDbPath, envelope);
        EncryptedEnvelope? retrieved = await _storage.GetManifestAsync(_tempDbPath);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(envelope.EnvelopeVersion, retrieved.EnvelopeVersion);
        Assert.Equal(envelope.Nonce, retrieved.Nonce);
        Assert.Equal(envelope.Tag, retrieved.Tag);
        Assert.Equal(envelope.Ciphertext, retrieved.Ciphertext);
    }

    [Fact]
    public async Task RecordCrudOperations_ShouldSucceed()
    {
        // Arrange
        await _initializer.InitializeDatabaseAsync(_tempDbPath);
        Guid record1Id = Guid.NewGuid();
        Guid record2Id = Guid.NewGuid();

        var env1 = new EncryptedEnvelope(1, new byte[12], new byte[16], [10, 20]);
        var env2 = new EncryptedEnvelope(1, new byte[12], new byte[16], [30, 40]);

        // Act: Save 2 records
        await _storage.SaveRecordAsync(_tempDbPath, record1Id, env1);
        await _storage.SaveRecordAsync(_tempDbPath, record2Id, env2);

        // Read single
        EncryptedEnvelope? retrieved1 = await _storage.GetRecordAsync(_tempDbPath, record1Id);
        Assert.NotNull(retrieved1);
        Assert.Equal(env1.Ciphertext, retrieved1.Ciphertext);

        // Read all
        IReadOnlyList<EncryptedEnvelope> allRecords = await _storage.GetAllRecordsAsync(_tempDbPath);
        Assert.Equal(2, allRecords.Count);

        // Delete record1
        bool deleted = await _storage.DeleteRecordAsync(_tempDbPath, record1Id);
        Assert.True(deleted);

        // Verify record1 is gone
        EncryptedEnvelope? gone = await _storage.GetRecordAsync(_tempDbPath, record1Id);
        Assert.Null(gone);

        // Deleting non-existent record returns false
        bool deleteAgain = await _storage.DeleteRecordAsync(_tempDbPath, record1Id);
        Assert.False(deleteAgain);

        // Only record2 remains
        allRecords = await _storage.GetAllRecordsAsync(_tempDbPath);
        Assert.Single(allRecords);
    }
}
