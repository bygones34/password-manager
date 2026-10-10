using Microsoft.Data.Sqlite;
using PasswordManager.Application.Abstractions;
using PasswordManager.Infrastructure.Persistence;
using PasswordManager.Infrastructure.Persistence.Entities;
using Xunit;

namespace PasswordManager.Infrastructure.Tests;

public sealed class VaultDatabaseInitializerTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly VaultDatabaseInitializer _initializer = new();

    public VaultDatabaseInitializerTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"pw_test_{Guid.NewGuid():N}.db");
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
    public async Task InitializeDatabaseAsync_ShouldCreateTables_AndSetSchemaVersion()
    {
        // Act
        await _initializer.InitializeDatabaseAsync(_tempDbPath);

        // Assert
        Assert.True(File.Exists(_tempDbPath));
        int schemaVersion = await _initializer.GetSchemaVersionAsync(_tempDbPath);
        Assert.Equal(IVaultDatabaseInitializer.CurrentSchemaVersion, schemaVersion);

        // Verify WAL mode via raw connection
        using var connection = new SqliteConnection($"Data Source={_tempDbPath}");
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";
        var mode = await command.ExecuteScalarAsync();
        Assert.Equal("wal", mode?.ToString()?.ToLowerInvariant());
    }

    [Fact]
    public async Task InitializeDatabaseAsync_IdempotentCall_ShouldNotThrow()
    {
        // Act: Initialize twice
        await _initializer.InitializeDatabaseAsync(_tempDbPath);
        await _initializer.InitializeDatabaseAsync(_tempDbPath);

        // Assert
        int schemaVersion = await _initializer.GetSchemaVersionAsync(_tempDbPath);
        Assert.Equal(IVaultDatabaseInitializer.CurrentSchemaVersion, schemaVersion);
    }

    [Fact]
    public async Task InitializeDatabaseAsync_UnsupportedFutureSchemaVersion_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await _initializer.InitializeDatabaseAsync(_tempDbPath);

        // Set metadata to future schema version (e.g. 99)
        await using (var context = VaultDbContext.CreateForDatabase(_tempDbPath))
        {
            var meta = await context.Metadata.FindAsync("SchemaVersion");
            if (meta is not null)
            {
                meta.Value = "99";
                await context.SaveChangesAsync();
            }
        }

        // Act & Assert: subsequent initialization must fail closed
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _initializer.InitializeDatabaseAsync(_tempDbPath));
    }
}
