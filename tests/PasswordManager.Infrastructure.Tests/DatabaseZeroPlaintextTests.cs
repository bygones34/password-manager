using System.Text;
using Microsoft.Data.Sqlite;
using PasswordManager.Application.Models;
using PasswordManager.Infrastructure.Persistence;
using PasswordManager.Security;
using Xunit;

namespace PasswordManager.Infrastructure.Tests;

public sealed class DatabaseZeroPlaintextTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly VaultDatabaseInitializer _initializer = new();
    private readonly SqliteVaultStorageService _storage = new();
    private readonly AeadEnvelopeService _envelopeService = new();

    public DatabaseZeroPlaintextTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"pw_canary_test_{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        try
        {
            SqliteConnection.ClearAllPools();

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
    public async Task SEC_G01_DatabaseFile_ShouldContainZeroPlaintextCanaries_OrSensitiveColumns()
    {
        // Arrange
        await _initializer.InitializeDatabaseAsync(_tempDbPath);

        Guid vaultId = Guid.NewGuid();
        Guid recordId = Guid.NewGuid();
        byte[] recordKey = _envelopeService.GenerateRandomBytes(32);

        const string syntheticSecretPassword = "CanarySyntheticSecretPassword9988!!";
        const string syntheticUsername = "alice.canary@internal.test";
        string jsonPayload = $"{{\"username\":\"{syntheticUsername}\",\"password\":\"{syntheticSecretPassword}\"}}";
        byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);

        // Encrypt and store record envelope
        EncryptedEnvelope envelope = _envelopeService.EncryptRecord(recordKey, vaultId, recordId, payloadBytes);
        await _storage.SaveRecordAsync(_tempDbPath, recordId, envelope);

        // 1. Verify schema: ensure no plaintext credential column exists in SQLite
        using (var connection = new SqliteConnection($"Data Source={_tempDbPath}"))
        {
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(VaultRecords);";
            using var reader = await cmd.ExecuteReaderAsync();

            var columns = new List<string>();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(1).ToLowerInvariant());
            }

            Assert.DoesNotContain("password", columns);
            Assert.DoesNotContain("username", columns);
            Assert.DoesNotContain("title", columns);
            Assert.DoesNotContain("url", columns);
            Assert.DoesNotContain("notes", columns);
        }

        // Release any pooled connections before reading raw bytes
        SqliteConnection.ClearAllPools();

        // 2. Perform raw byte canary scan on .db and .db-wal files using shared read
        byte[] dbBytes = ReadFileShared(_tempDbPath);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(syntheticSecretPassword);
        byte[] usernameBytes = Encoding.UTF8.GetBytes(syntheticUsername);

        Assert.False(ContainsSequence(dbBytes, passwordBytes),
            "CRITICAL SECURITY VIOLATION: Plaintext password canary found in physical SQLite database file!");

        Assert.False(ContainsSequence(dbBytes, usernameBytes),
            "CRITICAL SECURITY VIOLATION: Plaintext username canary found in physical SQLite database file!");

        string walPath = $"{_tempDbPath}-wal";
        if (File.Exists(walPath))
        {
            byte[] walBytes = ReadFileShared(walPath);
            Assert.False(ContainsSequence(walBytes, passwordBytes),
                "CRITICAL SECURITY VIOLATION: Plaintext password canary found in SQLite WAL file!");
            Assert.False(ContainsSequence(walBytes, usernameBytes),
                "CRITICAL SECURITY VIOLATION: Plaintext username canary found in SQLite WAL file!");
        }
    }

    private static byte[] ReadFileShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var ms = new MemoryStream();
        fs.CopyTo(ms);
        return ms.ToArray();
    }

    private static bool ContainsSequence(byte[] source, byte[] pattern)
    {
        if (pattern.Length == 0 || source.Length < pattern.Length)
        {
            return false;
        }

        return source.AsSpan().IndexOf(pattern) >= 0;
    }
}
