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

public sealed class LogSanitizationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testDbPath;
    private readonly StringWriter _stringWriter;
    private readonly SecureJsonStreamLogger _jsonLogger;
    private readonly SecureMemoryAuditLogger _memoryLogger;
    private readonly CompositeLogger _compositeLogger;
    private readonly VaultLifecycleManager _lifecycleManager;

    private static readonly KdfParameters FastTestKdfParams = new(
        memoryKiB: 16384,
        iterations: 1,
        degreeOfParallelism: 1,
        saltLength: 32,
        keyLength: 32);

    public LogSanitizationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "pwm_log_sanitization_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        _testDbPath = Path.Combine(_testDirectory, "audit_vault.db");

        _stringWriter = new StringWriter();
        _jsonLogger = new SecureJsonStreamLogger(_stringWriter);
        _memoryLogger = new SecureMemoryAuditLogger();
        _compositeLogger = new CompositeLogger(_jsonLogger, _memoryLogger);

        _lifecycleManager = new VaultLifecycleManager(
            new SimpleStoragePathProvider(_testDbPath),
            new VaultDatabaseInitializer(),
            new SqliteVaultStorageService(),
            new VaultHeaderService(),
            new AeadEnvelopeService(),
            new Argon2idKeyDerivationService(),
            _compositeLogger);
    }

    public void Dispose()
    {
        _lifecycleManager.Dispose();
        _stringWriter.Dispose();
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
    public async Task LogStreams_ContainZeroPlaintextMasterPasswordsOrAttempts_SEC_G02()
    {
        const string syntheticMasterPassword = "SyntheticSecretMasterPass_999!";
        const string syntheticWrongAttempt = "MaliciousWrongAttempt_888!";

        // 1. Create vault
        await _lifecycleManager.CreateVaultAsync(syntheticMasterPassword.ToCharArray(), FastTestKdfParams);

        // 2. Lock vault
        await _lifecycleManager.LockVaultAsync();

        // 3. Unlock vault with valid password
        await _lifecycleManager.UnlockVaultAsync(syntheticMasterPassword.ToCharArray());

        // 4. Lock vault again
        await _lifecycleManager.LockVaultAsync();

        // 5. Attempt unlock with wrong password
        await Assert.ThrowsAsync<CryptoAuthenticationException>(() =>
            _lifecycleManager.UnlockVaultAsync(syntheticWrongAttempt.ToCharArray()));

        // Inspect generated NDJSON log output
        var logContent = _stringWriter.ToString();
        Assert.False(string.IsNullOrWhiteSpace(logContent), "Log output should contain recorded events.");

        // Canary search: Verify synthetic passwords are NEVER present in any log stream
        Assert.DoesNotContain(syntheticMasterPassword, logContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(syntheticWrongAttempt, logContent, StringComparison.OrdinalIgnoreCase);

        // Verify strictly whitelisted JSON schema across every line
        var lines = logContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEmpty(lines);

        var allowedProperties = new HashSet<string>
        {
            "event_code",
            "duration_ms",
            "success",
            "error_code",
            "timestamp"
        };

        foreach (var line in lines)
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            Assert.Equal(JsonValueKind.Object, root.ValueKind);

            foreach (var prop in root.EnumerateObject())
            {
                Assert.Contains(prop.Name, allowedProperties);
            }
        }
    }

    [Theory]
    [InlineData("password123!", "SANITIZED_EVENT")]
    [InlineData("SELECT * FROM USERS", "SANITIZED_EVENT")]
    [InlineData("user@example.com", "SANITIZED_EVENT")]
    [InlineData("<script>alert(1)</script>", "SANITIZED_EVENT")]
    public void SecureLogSanitizer_ReplacesNonWhitelistedEventCodes(string hostileInput, string expected)
    {
        var rawEvent = new SecureLogEvent(hostileInput, false);
        var sanitized = SecureLogSanitizer.Sanitize(rawEvent);

        Assert.Equal(expected, sanitized.EventCode);
    }

    [Theory]
    [InlineData("password was: secret123", "SANITIZED_ERROR")]
    [InlineData("admin@corp.internal; rm -rf /", "SANITIZED_ERROR")]
    [InlineData("token: eyJhbGciOi...", "SANITIZED_ERROR")]
    public void SecureLogSanitizer_ReplacesNonWhitelistedErrorCodes(string hostileError, string expected)
    {
        var rawEvent = new SecureLogEvent("VAULT_LOCKED", false, 0, hostileError);
        var sanitized = SecureLogSanitizer.Sanitize(rawEvent);

        Assert.Equal(expected, sanitized.ErrorCode);
    }

    private sealed class CompositeLogger : ISecureLogger
    {
        private readonly ISecureLogger[] _loggers;

        public CompositeLogger(params ISecureLogger[] loggers)
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
