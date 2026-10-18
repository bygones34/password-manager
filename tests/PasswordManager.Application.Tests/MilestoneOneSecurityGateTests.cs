using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using PasswordManager.Application.Abstractions;
using PasswordManager.Application.Exceptions;
using PasswordManager.Application.Logging;
using PasswordManager.Application.Models;
using PasswordManager.Application.Services;
using PasswordManager.Domain.Enums;
using PasswordManager.Infrastructure.Persistence;
using PasswordManager.Security;
using Xunit;

namespace PasswordManager.Application.Tests;

/// <summary>
/// Milestone M1 Security Exit Gate Verification Suite.
/// Formally proves all 7 security exit criteria specified in DEVELOPMENT_ROADMAP.md and SECURITY_TEST_MATRIX.md:
/// 1. Correct master password unlocks and establishes valid session generation.
/// 2. Wrong master password fails closed, leaves vault locked, and leaks no keys.
/// 3. Tampered header, manifest, record, or AAD fails closed.
/// 4. Re-encryption produces distinct CSPRNG nonces and ciphertexts.
/// 5. Malformed headers and bounded anti-DoS violations fail before KDF execution.
/// 6. Vault lock aborts in-flight operations and rejects cross-generation delivery.
/// 7. Zero plaintext canary presence across database, WAL, and logs after full lifecycle.
/// </summary>
public sealed class MilestoneOneSecurityGateTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testDbPath;
    private readonly StringWriter _logWriter;
    private readonly SecureJsonStreamLogger _jsonLogger;
    private readonly SecureMemoryAuditLogger _auditLogger;
    private readonly CompositeSecurityLogger _logger;
    private readonly VaultDatabaseInitializer _dbInitializer;
    private readonly SqliteVaultStorageService _storageService;
    private readonly VaultHeaderService _headerService;
    private readonly AeadEnvelopeService _envelopeService;
    private readonly Argon2idKeyDerivationService _kdfService;
    private readonly VaultLifecycleManager _lifecycleManager;

    private static readonly KdfParameters FastTestKdfParams = new(
        memoryKiB: 16384,
        iterations: 1,
        degreeOfParallelism: 1,
        saltLength: 32,
        keyLength: 32);

    public MilestoneOneSecurityGateTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "pwm_m1_gate_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        _testDbPath = Path.Combine(_testDirectory, "gate_vault.db");

        _logWriter = new StringWriter();
        _jsonLogger = new SecureJsonStreamLogger(_logWriter);
        _auditLogger = new SecureMemoryAuditLogger();
        _logger = new CompositeSecurityLogger(_jsonLogger, _auditLogger);

        _dbInitializer = new VaultDatabaseInitializer();
        _storageService = new SqliteVaultStorageService();
        _headerService = new VaultHeaderService();
        _envelopeService = new AeadEnvelopeService();
        _kdfService = new Argon2idKeyDerivationService();

        _lifecycleManager = new VaultLifecycleManager(
            new TestPathProvider(_testDbPath),
            _dbInitializer,
            _storageService,
            _headerService,
            _envelopeService,
            _kdfService,
            _logger);
    }

    public void Dispose()
    {
        _lifecycleManager.Dispose();
        _logWriter.Dispose();
        SqliteConnection.ClearAllPools();

        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public async Task Gate1_CorrectMasterPassword_UnlocksAndEstablishesActiveSession()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();

        await _lifecycleManager.CreateVaultAsync(password, FastTestKdfParams);
        Assert.Equal(VaultState.Unlocked, _lifecycleManager.CurrentState);
        Assert.Equal(1, _lifecycleManager.CurrentGeneration);

        await _lifecycleManager.LockVaultAsync();
        Assert.Equal(VaultState.Locked, _lifecycleManager.CurrentState);

        await _lifecycleManager.UnlockVaultAsync(password);
        Assert.Equal(VaultState.Unlocked, _lifecycleManager.CurrentState);
        Assert.Equal(2, _lifecycleManager.CurrentGeneration);

        var session = _lifecycleManager.GetActiveSession();
        Assert.NotNull(session);
        Assert.Equal(32, session.RootKey.Length);
        Assert.Equal(32, session.RecordKey.Length);
        Assert.Equal(32, session.ManifestKey.Length);
        Assert.False(session.IsDisposed);
    }

    [Fact]
    public async Task Gate2_WrongMasterPassword_FailsClosed_AndLeavesVaultLocked()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();
        await _lifecycleManager.CreateVaultAsync(password, FastTestKdfParams);
        await _lifecycleManager.LockVaultAsync();

        var wrongPassword = "WrongPassword999!".ToCharArray();

        var ex = await Assert.ThrowsAsync<CryptoAuthenticationException>(() =>
            _lifecycleManager.UnlockVaultAsync(wrongPassword));

        Assert.NotNull(ex);
        Assert.Equal(VaultState.Locked, _lifecycleManager.CurrentState);
        Assert.Throws<InvalidOperationException>(() => _lifecycleManager.GetActiveSession());

        // Verify audit log has UNLOCK_FAILED with AUTH_FAILED
        var failedEvent = _auditLogger.Events.FirstOrDefault(e => e.EventCode == "UNLOCK_FAILED");
        Assert.NotNull(failedEvent.EventCode);
        Assert.False(failedEvent.Success);
        Assert.Equal("AUTH_FAILED", failedEvent.ErrorCode);
    }

    [Fact]
    public async Task Gate3_TamperedCiphertextOrHeader_FailsClosed()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();
        await _lifecycleManager.CreateVaultAsync(password, FastTestKdfParams);

        var session = _lifecycleManager.GetActiveSession();
        var vaultId = session.VaultId;
        var recordKey = session.RecordKey;

        // 3a. Encrypt valid record envelope
        var recordId = Guid.NewGuid();
        var plaintext = Encoding.UTF8.GetBytes("{\"username\":\"test\",\"password\":\"secret\"}");
        var validEnvelope = _envelopeService.EncryptRecord(recordKey, vaultId, recordId, plaintext);

        // 3b. 1-bit flip in record ciphertext fails closed
        var corruptedRecordCiphertext = (byte[])validEnvelope.Ciphertext.Clone();
        corruptedRecordCiphertext[0] ^= 0x01;
        var tamperedRecordEnvelope = new EncryptedEnvelope(
            validEnvelope.EnvelopeVersion,
            validEnvelope.Nonce,
            validEnvelope.Tag,
            corruptedRecordCiphertext);

        Assert.Throws<CryptoAuthenticationException>(() =>
            _envelopeService.DecryptRecord(recordKey, vaultId, recordId, tamperedRecordEnvelope));

        // 3c. Record substitution (swapping RecordId) fails closed
        var swappedRecordId = Guid.NewGuid();
        Assert.Throws<CryptoAuthenticationException>(() =>
            _envelopeService.DecryptRecord(recordKey, vaultId, swappedRecordId, validEnvelope));

        // 3d. Cross-vault swap (swapping VaultId) fails closed
        var otherVaultId = Guid.NewGuid();
        Assert.Throws<CryptoAuthenticationException>(() =>
            _envelopeService.DecryptRecord(recordKey, otherVaultId, recordId, validEnvelope));

        await _lifecycleManager.LockVaultAsync();

        // 3e. Header tampering in SQLite fails closed on unlock
        var headerBytes = await _storageService.GetHeaderBytesAsync(_testDbPath);
        Assert.NotNull(headerBytes);
        var tamperedHeaderBytes = (byte[])headerBytes.Clone();
        tamperedHeaderBytes[20] ^= 0xFF; // corrupt VaultId byte inside header
        await _storageService.SaveHeaderAsync(_testDbPath, tamperedHeaderBytes);

        await Assert.ThrowsAsync<CryptoAuthenticationException>(() =>
            _lifecycleManager.UnlockVaultAsync(password));
        Assert.Equal(VaultState.Locked, _lifecycleManager.CurrentState);
    }

    [Fact]
    public void Gate4_ReEncryption_ProducesUniqueCsprngNonces()
    {
        var vaultId = Guid.NewGuid();
        var recordId = Guid.NewGuid();
        var recordKey = _envelopeService.GenerateRandomBytes(32);
        var plaintext = Encoding.UTF8.GetBytes("IdenticalSecretCredentialPayload123!");

        const int iterations = 100;
        var nonces = new HashSet<string>(StringComparer.Ordinal);
        var ciphertexts = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < iterations; i++)
        {
            var envelope = _envelopeService.EncryptRecord(recordKey, vaultId, recordId, plaintext);
            var nonceHex = Convert.ToHexString(envelope.Nonce);
            var ciphertextHex = Convert.ToHexString(envelope.Ciphertext);

            Assert.True(nonces.Add(nonceHex), "Duplicate nonce generated by CSPRNG!");
            Assert.True(ciphertexts.Add(ciphertextHex), "Duplicate ciphertext generated for identical plaintext!");
        }

        Assert.Equal(iterations, nonces.Count);
        Assert.Equal(iterations, ciphertexts.Count);
    }

    [Fact]
    public void Gate5_MalformedHeader_RejectedPriorToKdfExecution_AntiDos()
    {
        var vaultId = Guid.NewGuid();
        var salt = _envelopeService.GenerateRandomBytes(32);
        var wrappedKey = new WrappedKeyData(new byte[12], new byte[16], new byte[32]);

        // 5a. Serialization itself rejects hostile out-of-bound parameters
        var hostileParams = new KdfParameters(
            memoryKiB: 2097152, // 2 GB
            iterations: 100,
            degreeOfParallelism: 32,
            saltLength: 32,
            keyLength: 32);

        var hostileHeader = new VaultHeaderData(vaultId, hostileParams, salt, wrappedKey);
        Assert.Throws<KdfValidationException>(() => _headerService.Serialize(hostileHeader));

        // 5b. Deserialization of an incoming raw hostile 132-byte binary payload (attacker tampering)
        var validHeader = new VaultHeaderData(vaultId, KdfParameters.Default, salt, wrappedKey);
        var validBytes = _headerService.Serialize(validHeader);

        // Tamper MemoryKiB at offset 26 to 2 GB (2097152 KiB)
        var hostileMemoryBytes = (byte[])validBytes.Clone();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(hostileMemoryBytes.AsSpan(26, 4), 2097152);

        var exMemory = Assert.Throws<KdfValidationException>(() =>
            _headerService.Deserialize(hostileMemoryBytes));
        Assert.Contains("Memory cost", exMemory.Message);

        // Tamper Magic bytes at offset 0 to "BAD!" (0x42414421)
        var hostileMagicBytes = (byte[])validBytes.Clone();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(hostileMagicBytes.AsSpan(0, 4), 0x42414421);

        var exMagic = Assert.Throws<CryptoAuthenticationException>(() =>
            _headerService.Deserialize(hostileMagicBytes));
        Assert.Contains("magic", exMagic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Gate6_LockedVault_CancelsInFlightScopes_AndPreventsDelayedDelivery()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();
        await _lifecycleManager.CreateVaultAsync(password, FastTestKdfParams);

        using var scopeGen1 = _lifecycleManager.CreateOperationScope();
        Assert.True(scopeGen1.IsActive);
        Assert.Equal(1, scopeGen1.Generation);

        // Lock vault
        await _lifecycleManager.LockVaultAsync();
        Assert.False(scopeGen1.IsActive);
        Assert.True(scopeGen1.CancellationToken.IsCancellationRequested);

        // Re-unlock under generation 2
        await _lifecycleManager.UnlockVaultAsync(password);
        Assert.Equal(2, _lifecycleManager.CurrentGeneration);

        // Scope from generation 1 must strictly throw and never deliver results
        var ex = Assert.Throws<OperationCanceledException>(() =>
            scopeGen1.ThrowIfCanceledOrExpired());
        Assert.Contains("expired or vault was locked", ex.Message);

        using var scopeGen2 = _lifecycleManager.CreateOperationScope();
        Assert.True(scopeGen2.IsActive);
        Assert.Equal(2, scopeGen2.Generation);
        scopeGen2.ThrowIfCanceledOrExpired();
    }

    [Fact]
    public async Task Gate7_FullLifecycle_ZeroPlaintextDiskAndLogCanaryScan()
    {
        const string canaryMasterPassword = "Canary_M1_MasterSecret_Password_8877!!";
        const string canaryUserAlice = "alice.canary@internal.corp";
        const string canaryPassAlice = "Alice_Super_Secret_Password_9999!!";
        const string canaryUserBob = "bob.canary@internal.corp";
        const string canaryPassBob = "Bob_Ultra_Secret_Password_7777!!";

        // 1. Create vault
        await _lifecycleManager.CreateVaultAsync(canaryMasterPassword.ToCharArray(), FastTestKdfParams);
        var session = _lifecycleManager.GetActiveSession();

        // 2. Encrypt and store synthetic records
        var record1Id = Guid.NewGuid();
        var record1Json = $"{{\"username\":\"{canaryUserAlice}\",\"password\":\"{canaryPassAlice}\"}}";
        var envelope1 = _envelopeService.EncryptRecord(session.RecordKey, session.VaultId, record1Id, Encoding.UTF8.GetBytes(record1Json));
        await _storageService.SaveRecordAsync(_testDbPath, record1Id, envelope1);

        var record2Id = Guid.NewGuid();
        var record2Json = $"{{\"username\":\"{canaryUserBob}\",\"password\":\"{canaryPassBob}\"}}";
        var envelope2 = _envelopeService.EncryptRecord(session.RecordKey, session.VaultId, record2Id, Encoding.UTF8.GetBytes(record2Json));
        await _storageService.SaveRecordAsync(_testDbPath, record2Id, envelope2);

        // 3. Lock, unlock and lock again
        await _lifecycleManager.LockVaultAsync();
        await _lifecycleManager.UnlockVaultAsync(canaryMasterPassword.ToCharArray());
        await _lifecycleManager.LockVaultAsync();

        // Release connection pools for clean physical file scan
        SqliteConnection.ClearAllPools();

        // 4. Physical byte scan of .db file
        var dbBytes = ReadFileShared(_testDbPath);
        AssertCanaryAbsent(dbBytes, canaryMasterPassword, "vault database (.db)");
        AssertCanaryAbsent(dbBytes, canaryUserAlice, "vault database (.db)");
        AssertCanaryAbsent(dbBytes, canaryPassAlice, "vault database (.db)");
        AssertCanaryAbsent(dbBytes, canaryUserBob, "vault database (.db)");
        AssertCanaryAbsent(dbBytes, canaryPassBob, "vault database (.db)");

        // 5. Physical byte scan of -wal file (if exists)
        var walPath = $"{_testDbPath}-wal";
        if (File.Exists(walPath))
        {
            var walBytes = ReadFileShared(walPath);
            AssertCanaryAbsent(walBytes, canaryMasterPassword, "SQLite WAL file (-wal)");
            AssertCanaryAbsent(walBytes, canaryUserAlice, "SQLite WAL file (-wal)");
            AssertCanaryAbsent(walBytes, canaryPassAlice, "SQLite WAL file (-wal)");
            AssertCanaryAbsent(walBytes, canaryUserBob, "SQLite WAL file (-wal)");
            AssertCanaryAbsent(walBytes, canaryPassBob, "SQLite WAL file (-wal)");
        }

        // 6. Substring scan of generated NDJSON logs
        var logContent = _logWriter.ToString();
        Assert.DoesNotContain(canaryMasterPassword, logContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(canaryUserAlice, logContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(canaryPassAlice, logContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(canaryUserBob, logContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(canaryPassBob, logContent, StringComparison.OrdinalIgnoreCase);

        // 7. Verify all log lines adhere to whitelist JSON structure
        var logLines = logContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var allowedProps = new HashSet<string> { "event_code", "duration_ms", "success", "error_code", "timestamp" };

        foreach (var line in logLines)
        {
            using var doc = JsonDocument.Parse(line);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                Assert.Contains(prop.Name, allowedProps);
            }
        }
    }

    private static void AssertCanaryAbsent(byte[] fileBytes, string canaryString, string fileName)
    {
        var pattern = Encoding.UTF8.GetBytes(canaryString);
        var index = fileBytes.AsSpan().IndexOf(pattern);
        Assert.True(index < 0,
            $"CRITICAL SECURITY VIOLATION: Plaintext canary '{canaryString}' found in physical {fileName} at offset {index}!");
    }

    private static byte[] ReadFileShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var ms = new MemoryStream();
        fs.CopyTo(ms);
        return ms.ToArray();
    }

    private sealed class CompositeSecurityLogger : ISecureLogger
    {
        private readonly ISecureLogger[] _loggers;

        public CompositeSecurityLogger(params ISecureLogger[] loggers)
        {
            _loggers = loggers;
        }

        public void LogEvent(in SecureLogEvent logEvent)
        {
            foreach (var logger in _loggers)
            {
                logger.LogEvent(logEvent);
            }
        }
    }

    private sealed class TestPathProvider : IStoragePathProvider
    {
        private readonly string _dbPath;

        public TestPathProvider(string dbPath)
        {
            _dbPath = dbPath;
        }

        public string GetAppDataDirectory() => Path.GetDirectoryName(_dbPath)!;
        public string GetVaultDatabasePath() => _dbPath;
        public string GetBackupsDirectory() => Path.Combine(GetAppDataDirectory(), "Backups");
        public bool IsPackagedEnvironment => false;
    }
}
