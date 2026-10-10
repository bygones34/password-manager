using Microsoft.Data.Sqlite;
using PasswordManager.Application.Abstractions;
using PasswordManager.Application.Logging;
using PasswordManager.Application.Models;
using PasswordManager.Application.Services;
using PasswordManager.Application.Tests.Helpers;
using PasswordManager.Domain.Enums;
using PasswordManager.Infrastructure.Persistence;
using PasswordManager.Security;
using Xunit;

namespace PasswordManager.Application.Tests;

public sealed class AutoLockCoordinatorTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testDbPath;
    private readonly VaultLifecycleManager _lifecycleManager;
    private readonly ManualTimeProvider _timeProvider;
    private readonly SecureMemoryAuditLogger _logger;
    private readonly AutoLockCoordinator _coordinator;

    private static readonly KdfParameters FastTestKdfParams = new(
        memoryKiB: 16384,
        iterations: 1,
        degreeOfParallelism: 1,
        saltLength: 32,
        keyLength: 32);

    public AutoLockCoordinatorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "pwm_autolock_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        _testDbPath = Path.Combine(_testDirectory, "autolock_vault.db");

        _timeProvider = new ManualTimeProvider();
        _logger = new SecureMemoryAuditLogger();

        _lifecycleManager = new VaultLifecycleManager(
            new SimpleStoragePathProvider(_testDbPath),
            new VaultDatabaseInitializer(),
            new SqliteVaultStorageService(),
            new VaultHeaderService(),
            new AeadEnvelopeService(),
            new Argon2idKeyDerivationService(),
            _logger);

        _coordinator = new AutoLockCoordinator(
            _lifecycleManager,
            sessionLockListener: null,
            _timeProvider,
            _logger);
    }

    public void Dispose()
    {
        _coordinator.Dispose();
        _lifecycleManager.Dispose();
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
    public void DefaultTimeout_IsFiveMinutes()
    {
        Assert.Equal(TimeSpan.FromMinutes(5), _coordinator.InactivityTimeout);
        Assert.True(_coordinator.IsEnabled);
    }

    [Fact]
    public async Task Inactivity_LocksVault_AfterFiveMinutes()
    {
        var password = "TestPassword123!".ToCharArray();
        await _lifecycleManager.CreateVaultAsync(password, FastTestKdfParams);
        Assert.Equal(VaultState.Unlocked, _lifecycleManager.CurrentState);

        // Advance 4 minutes: still unlocked
        _timeProvider.Advance(TimeSpan.FromMinutes(4));
        Assert.Equal(VaultState.Unlocked, _lifecycleManager.CurrentState);

        // Advance 1 more minute (total 5 minutes): triggers auto-lock
        _timeProvider.Advance(TimeSpan.FromMinutes(1));

        // Allow any async task on timer callback to settle
        await Task.Delay(100);

        Assert.Equal(VaultState.Locked, _lifecycleManager.CurrentState);

        // Verify audit log has AUTO_LOCK_TRIGGERED event
        var lockEvent = _logger.Events.FirstOrDefault(e => e.EventCode == "AUTO_LOCK_TRIGGERED");
        Assert.NotNull(lockEvent.EventCode);
        Assert.True(lockEvent.Success);
        Assert.Equal("USERINACTIVITY", lockEvent.ErrorCode);
    }

    [Fact]
    public async Task RecordUserActivity_ResetsInactivityTimer()
    {
        var password = "TestPassword123!".ToCharArray();
        await _lifecycleManager.CreateVaultAsync(password, FastTestKdfParams);
        Assert.Equal(VaultState.Unlocked, _lifecycleManager.CurrentState);

        // Advance 4 minutes
        _timeProvider.Advance(TimeSpan.FromMinutes(4));
        Assert.Equal(VaultState.Unlocked, _lifecycleManager.CurrentState);

        // User interacts (mouse click / key press)
        _coordinator.RecordUserActivity();

        // Advance another 3 minutes (total 7 minutes since unlock, but only 3 minutes since activity)
        _timeProvider.Advance(TimeSpan.FromMinutes(3));
        Assert.Equal(VaultState.Unlocked, _lifecycleManager.CurrentState);

        // Advance 2 more minutes (5 minutes since last activity)
        _timeProvider.Advance(TimeSpan.FromMinutes(2));
        await Task.Delay(100);

        Assert.Equal(VaultState.Locked, _lifecycleManager.CurrentState);
    }

    [Fact]
    public async Task DisabledAutoLock_DoesNotLockVault()
    {
        var password = "TestPassword123!".ToCharArray();
        await _lifecycleManager.CreateVaultAsync(password, FastTestKdfParams);
        Assert.Equal(VaultState.Unlocked, _lifecycleManager.CurrentState);

        _coordinator.IsEnabled = false;

        // Advance 10 minutes
        _timeProvider.Advance(TimeSpan.FromMinutes(10));
        await Task.Delay(100);

        Assert.Equal(VaultState.Unlocked, _lifecycleManager.CurrentState);
    }

    [Fact]
    public async Task WorkstationLocked_ImmediatelyLocksVault()
    {
        var password = "TestPassword123!".ToCharArray();
        await _lifecycleManager.CreateVaultAsync(password, FastTestKdfParams);
        Assert.Equal(VaultState.Unlocked, _lifecycleManager.CurrentState);

        await _coordinator.HandleSystemLockAsync(SystemLockReason.WorkstationLocked);

        Assert.Equal(VaultState.Locked, _lifecycleManager.CurrentState);

        var lockEvent = _logger.Events.FirstOrDefault(e => e.EventCode == "SYSTEM_LOCK_TRIGGERED");
        Assert.NotNull(lockEvent.EventCode);
        Assert.Equal("WORKSTATIONLOCKED", lockEvent.ErrorCode);
    }

    [Fact]
    public async Task SystemSuspend_ImmediatelyLocksVault()
    {
        var password = "TestPassword123!".ToCharArray();
        await _lifecycleManager.CreateVaultAsync(password, FastTestKdfParams);
        Assert.Equal(VaultState.Unlocked, _lifecycleManager.CurrentState);

        await _coordinator.HandleSystemLockAsync(SystemLockReason.SystemSuspend);

        Assert.Equal(VaultState.Locked, _lifecycleManager.CurrentState);

        var lockEvent = _logger.Events.FirstOrDefault(e => e.EventCode == "SYSTEM_LOCK_TRIGGERED");
        Assert.NotNull(lockEvent.EventCode);
        Assert.Equal("SYSTEMSUSPEND", lockEvent.ErrorCode);
    }

    [Fact]
    public async Task CustomTimeout_IsRespected()
    {
        var password = "TestPassword123!".ToCharArray();
        await _lifecycleManager.CreateVaultAsync(password, FastTestKdfParams);

        _coordinator.InactivityTimeout = TimeSpan.FromMinutes(2);

        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(VaultState.Unlocked, _lifecycleManager.CurrentState);

        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        await Task.Delay(100);

        Assert.Equal(VaultState.Locked, _lifecycleManager.CurrentState);
    }

    private sealed class SimpleStoragePathProvider : IStoragePathProvider
    {
        private readonly string _dbPath;

        public SimpleStoragePathProvider(string dbPath)
        {
            _dbPath = dbPath;
        }

        public string GetAppDataDirectory() => Path.GetDirectoryName(_dbPath)!;
        public string GetVaultDatabasePath() => _dbPath;
        public string GetBackupsDirectory() => Path.Combine(GetAppDataDirectory(), "Backups");
        public bool IsPackagedEnvironment => false;
    }
}
