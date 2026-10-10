using System.Text.RegularExpressions;

namespace PasswordManager.Application.Logging;

/// <summary>
/// Enforces strict validation and sanitization on security log events to guarantee
/// no sensitive data or attacker-controlled strings ever enter log streams.
/// </summary>
public static partial class SecureLogSanitizer
{
    private static readonly HashSet<string> WhitelistedEventCodes = new(StringComparer.Ordinal)
    {
        "VAULT_CREATED",
        "VAULT_UNLOCKED",
        "VAULT_LOCKED",
        "UNLOCK_FAILED",
        "AUTO_LOCK_TRIGGERED",
        "SYSTEM_LOCK_TRIGGERED",
        "DB_INITIALIZED",
        "RECORD_ENCRYPTED",
        "RECORD_DECRYPTED",
        "RECORD_SAVED",
        "RECORD_DELETED",
        "MANIFEST_ENCRYPTED",
        "MANIFEST_DECRYPTED",
        "MANIFEST_SAVED"
    };

    private static readonly HashSet<string> WhitelistedErrorCodes = new(StringComparer.Ordinal)
    {
        "NONE",
        "AUTH_FAILED",
        "USER_INACTIVITY",
        "WORKSTATION_LOCKED",
        "SESSION_LOGOFF",
        "REMOTE_DISCONNECT",
        "SYSTEM_SUSPEND",
        "TIMEOUT",
        "VALIDATION_FAILED",
        "NOT_FOUND",
        "IO_ERROR"
    };

    [GeneratedRegex("^[A-Z0-9_]{3,64}$", RegexOptions.Compiled)]
    private static partial Regex SafeCodePattern();

    /// <summary>
    /// Validates and sanitizes a SecureLogEvent against the strict security whitelist.
    /// Returns a safe event guaranteed to contain only approved codes.
    /// </summary>
    public static SecureLogEvent Sanitize(in SecureLogEvent source)
    {
        var rawEvent = source.EventCode?.Trim().ToUpperInvariant() ?? "UNKNOWN_EVENT";
        var safeEvent = WhitelistedEventCodes.Contains(rawEvent) || SafeCodePattern().IsMatch(rawEvent)
            ? rawEvent
            : "SANITIZED_EVENT";

        string? safeError = null;
        if (!string.IsNullOrWhiteSpace(source.ErrorCode))
        {
            var rawError = source.ErrorCode.Trim().ToUpperInvariant();
            safeError = WhitelistedErrorCodes.Contains(rawError) || SafeCodePattern().IsMatch(rawError)
                ? rawError
                : "SANITIZED_ERROR";
        }

        return new SecureLogEvent(
            safeEvent,
            source.Success,
            source.DurationMs,
            safeError,
            source.Timestamp);
    }
}
