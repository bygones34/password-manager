using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using PasswordManager.Application.Abstractions;
using PasswordManager.Application.Exceptions;
using PasswordManager.Application.Models;
using PasswordManager.Application.Services;
using PasswordManager.Domain.Enums;
using PasswordManager.Infrastructure.Persistence;
using PasswordManager.Security;
using Xunit;

namespace PasswordManager.Application.Tests;

public sealed class VaultLifecycleManagerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testDbPath;
    private readonly TestStoragePathProvider _pathProvider;
    private readonly VaultDatabaseInitializer _dbInitializer;
    private readonly SqliteVaultStorageService _storageService;
    private readonly VaultHeaderService _headerService;
    private readonly AeadEnvelopeService _envelopeService;
    private readonly Argon2idKeyDerivationService _kdfService;
    private readonly VaultLifecycleManager _manager;

    // Fast bounded test parameters (16 MiB, 1 iteration, 1 lane) to keep test suite snappy
    private static readonly KdfParameters FastTestKdfParams = new(
        memoryKiB: 16384,
        iterations: 1,
        degreeOfParallelism: 1,
        saltLength: 32,
        keyLength: 32);

    public VaultLifecycleManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "pwm_lifecycle_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        _testDbPath = Path.Combine(_testDirectory, "test_vault.db");

        _pathProvider = new TestStoragePathProvider(_testDbPath);
        _dbInitializer = new VaultDatabaseInitializer();
        _storageService = new SqliteVaultStorageService();
        _headerService = new VaultHeaderService();
        _envelopeService = new AeadEnvelopeService();
        _kdfService = new Argon2idKeyDerivationService();

        _manager = new VaultLifecycleManager(
            _pathProvider,
            _dbInitializer,
            _storageService,
            _headerService,
            _envelopeService,
            _kdfService);
    }

    public void Dispose()
    {
        _manager.Dispose();
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
            // Best-effort cleanup in test teardown
        }
    }

    [Fact]
    public async Task InitialState_IsNoVault_AndRefreshReflectsDiskState()
    {
        Assert.Equal(VaultState.NoVault, _manager.CurrentState);
        Assert.Equal(0, _manager.CurrentGeneration);
        Assert.Null(_manager.VaultId);

        var exists = await _manager.RefreshVaultStateAsync();
        Assert.False(exists);
        Assert.Equal(VaultState.NoVault, _manager.CurrentState);
    }

    [Fact]
    public async Task CreateVault_EstablishesUnlockedState_AndRaisesEvents()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();
        var recordedEvents = new List<VaultStateChangedEventArgs>();
        _manager.StateChanged += (_, args) => recordedEvents.Add(args);

        await _manager.CreateVaultAsync(password, FastTestKdfParams);

        Assert.Equal(VaultState.Unlocked, _manager.CurrentState);
        Assert.Equal(1, _manager.CurrentGeneration);
        Assert.NotNull(_manager.VaultId);

        // Verify state events: NoVault -> Unlocking -> Unlocked
        Assert.Equal(2, recordedEvents.Count);
        Assert.Equal(VaultState.NoVault, recordedEvents[0].OldState);
        Assert.Equal(VaultState.Unlocking, recordedEvents[0].NewState);
        Assert.Equal(VaultState.Unlocking, recordedEvents[1].OldState);
        Assert.Equal(VaultState.Unlocked, recordedEvents[1].NewState);
        Assert.Equal(1, recordedEvents[1].Generation);

        // Verify active session
        var session = _manager.GetActiveSession();
        Assert.NotNull(session);
        Assert.Equal(_manager.VaultId, session.VaultId);
        Assert.Equal(32, session.RootKey.Length);
        Assert.Equal(32, session.RecordKey.Length);
        Assert.Equal(32, session.ManifestKey.Length);
    }

    [Fact]
    public async Task CreateVault_Throws_WhenVaultAlreadyExists()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();
        await _manager.CreateVaultAsync(password, FastTestKdfParams);

        // Calling Create again while unlocked
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _manager.CreateVaultAsync(password, FastTestKdfParams));

        // Lock vault and try Create again
        await _manager.LockVaultAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _manager.CreateVaultAsync(password, FastTestKdfParams));
    }

    [Fact]
    public async Task UnlockAndLock_FullLifecycleTransitions_AndAdvancesGeneration()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();
        await _manager.CreateVaultAsync(password, FastTestKdfParams);
        var initialVaultId = _manager.VaultId;

        // 1. Lock vault
        await _manager.LockVaultAsync();
        Assert.Equal(VaultState.Locked, _manager.CurrentState);
        Assert.Throws<InvalidOperationException>(() => _manager.GetActiveSession());

        // 2. Unlock vault
        await _manager.UnlockVaultAsync(password);
        Assert.Equal(VaultState.Unlocked, _manager.CurrentState);
        Assert.Equal(2, _manager.CurrentGeneration);
        Assert.Equal(initialVaultId, _manager.VaultId);

        var session2 = _manager.GetActiveSession();
        Assert.NotNull(session2);
        Assert.Equal(2, session2.SessionGeneration);
    }

    [Fact]
    public async Task Unlock_ThrowsCryptoAuthException_WhenMasterPasswordIsWrong()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();
        await _manager.CreateVaultAsync(password, FastTestKdfParams);
        await _manager.LockVaultAsync();

        var wrongPassword = "WrongPassword999!".ToCharArray();

        var ex = await Assert.ThrowsAsync<CryptoAuthenticationException>(() =>
            _manager.UnlockVaultAsync(wrongPassword));

        Assert.NotNull(ex);
        Assert.Equal(VaultState.Locked, _manager.CurrentState);
        Assert.Throws<InvalidOperationException>(() => _manager.GetActiveSession());
    }

    [Fact]
    public async Task Unlock_ThrowsCryptoAuthException_WhenManifestIsTampered()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();
        await _manager.CreateVaultAsync(password, FastTestKdfParams);
        await _manager.LockVaultAsync();

        // Tamper manifest ciphertext directly in SQLite
        var manifest = await _storageService.GetManifestAsync(_testDbPath);
        Assert.NotNull(manifest);

        var corruptedCiphertext = (byte[])manifest.Ciphertext.Clone();
        corruptedCiphertext[0] ^= 0x01; // flip 1 bit

        var corruptedManifest = new EncryptedEnvelope(
            manifest.EnvelopeVersion,
            manifest.Nonce,
            manifest.Tag,
            corruptedCiphertext);

        await _storageService.SaveManifestAsync(_testDbPath, corruptedManifest);

        // Attempt unlock: Manifest auth tag verification must fail closed
        await Assert.ThrowsAsync<CryptoAuthenticationException>(() =>
            _manager.UnlockVaultAsync(password));

        Assert.Equal(VaultState.Locked, _manager.CurrentState);
    }

    [Fact]
    public async Task OperationScope_CancelsInFlightOperations_WhenVaultLocks_SEC_F02()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();
        await _manager.CreateVaultAsync(password, FastTestKdfParams);

        using var scope = _manager.CreateOperationScope();
        Assert.True(scope.IsActive);
        Assert.False(scope.CancellationToken.IsCancellationRequested);

        // Background task awaiting scope cancellation
        var tcs = new TaskCompletionSource<bool>();
        scope.CancellationToken.Register(() => tcs.SetResult(true));

        // Lock vault
        await _manager.LockVaultAsync();

        // Cancellation must trigger promptly
        var cancelled = await Task.WhenAny(tcs.Task, Task.Delay(2000)) == tcs.Task;
        Assert.True(cancelled, "OperationScope cancellation token was not triggered upon vault lock.");

        Assert.False(scope.IsActive);
        Assert.Throws<OperationCanceledException>(() => scope.ThrowIfCanceledOrExpired());
    }

    [Fact]
    public async Task LockVault_ZeroesAllSessionKeysInMemory_SEC_F01()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();
        await _manager.CreateVaultAsync(password, FastTestKdfParams);

        var session = _manager.GetActiveSession();
        var capturedRootKey = session.RootKey;
        var capturedRecordKey = session.RecordKey;
        var capturedManifestKey = session.ManifestKey;

        // Keys have non-zero entropy while unlocked
        Assert.Contains(capturedRootKey, b => b != 0);
        Assert.Contains(capturedRecordKey, b => b != 0);
        Assert.Contains(capturedManifestKey, b => b != 0);

        await _manager.LockVaultAsync();

        // After lock, all key buffers must be wiped to zero in memory
        Assert.All(capturedRootKey, b => Assert.Equal(0, b));
        Assert.All(capturedRecordKey, b => Assert.Equal(0, b));
        Assert.All(capturedManifestKey, b => Assert.Equal(0, b));
        Assert.True(session.IsDisposed);
    }

    [Fact]
    public async Task DuplicateUnlock_ThrowsInvalidOperationException()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();
        await _manager.CreateVaultAsync(password, FastTestKdfParams);

        // Already unlocked
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _manager.UnlockVaultAsync(password));
    }

    [Fact]
    public async Task ConcurrentUnlock_SerializesExecution_AndSecondCallIsRejected()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();
        await _manager.CreateVaultAsync(password, FastTestKdfParams);
        await _manager.LockVaultAsync();

        // Launch two concurrent unlock tasks
        var task1 = Task.Run(() => _manager.UnlockVaultAsync(password));
        var task2 = Task.Run(() => _manager.UnlockVaultAsync(password));

        var results = await Task.WhenAll(
            task1.ContinueWith(t => t.Exception),
            task2.ContinueWith(t => t.Exception));

        // Exactly one task must succeed (null exception) and one must fail with InvalidOperationException
        var succeeded = results.Count(ex => ex is null);
        var failed = results.Count(ex => ex is not null && ex.InnerExceptions.Any(i => i is InvalidOperationException));

        Assert.Equal(1, succeeded);
        Assert.Equal(1, failed);
        Assert.Equal(VaultState.Unlocked, _manager.CurrentState);
    }

    [Fact]
    public async Task OperationScope_PreventsCrossGenerationResultDelivery_Rule5_8()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();
        await _manager.CreateVaultAsync(password, FastTestKdfParams);

        // Scope created in generation 1
        using var scopeGen1 = _manager.CreateOperationScope();
        Assert.Equal(1, scopeGen1.Generation);
        Assert.True(scopeGen1.IsActive);

        // Lock vault and re-unlock (generation advances to 2)
        await _manager.LockVaultAsync();
        await _manager.UnlockVaultAsync(password);

        Assert.Equal(2, _manager.CurrentGeneration);

        // Scope from generation 1 must not be usable or deliverable to UI
        Assert.False(scopeGen1.IsActive);
        var ex = Assert.Throws<OperationCanceledException>(() => scopeGen1.ThrowIfCanceledOrExpired());
        Assert.Contains("expired or vault was locked", ex.Message);

        // New scope for generation 2 must be valid and independent
        using var scopeGen2 = _manager.CreateOperationScope();
        Assert.Equal(2, scopeGen2.Generation);
        Assert.True(scopeGen2.IsActive);
        scopeGen2.ThrowIfCanceledOrExpired();
    }

    [Fact]
    public async Task LockVault_IsIdempotent_WhenCalledMultipleTimes()
    {
        var password = "CorrectMasterPassword123!".ToCharArray();
        await _manager.CreateVaultAsync(password, FastTestKdfParams);

        // First lock
        await _manager.LockVaultAsync();
        Assert.Equal(VaultState.Locked, _manager.CurrentState);

        // Second and third locks should not throw or change state
        await _manager.LockVaultAsync();
        await _manager.LockVaultAsync();
        Assert.Equal(VaultState.Locked, _manager.CurrentState);
    }

    private sealed class TestStoragePathProvider : IStoragePathProvider
    {
        private readonly string _dbPath;

        public TestStoragePathProvider(string dbPath)
        {
            _dbPath = dbPath;
        }

        public string GetAppDataDirectory() => Path.GetDirectoryName(_dbPath)!;
        public string GetVaultDatabasePath() => _dbPath;
        public string GetBackupsDirectory() => Path.Combine(GetAppDataDirectory(), "Backups");
        public bool IsPackagedEnvironment => false;
    }
}
