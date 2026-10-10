namespace PasswordManager.Application.Logging;

/// <summary>
/// Immutable structured log event conforming strictly to the security whitelist policy.
/// Contains ZERO free-form message strings, user inputs, or exception stack dumps.
/// </summary>
public readonly record struct SecureLogEvent
{
    /// <summary>
    /// Whitelisted security or lifecycle event identifier (e.g., "VAULT_UNLOCKED", "UNLOCK_FAILED").
    /// </summary>
    public string EventCode { get; init; }

    /// <summary>
    /// Operation duration in milliseconds (0 if not timed).
    /// </summary>
    public long DurationMs { get; init; }

    /// <summary>
    /// Indicates whether the operation completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Safe, whitelisted error code string (e.g., "AUTH_FAILED", "TIMEOUT", "NONE").
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// UTC timestamp of the event.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    public SecureLogEvent(
        string eventCode,
        bool success,
        long durationMs = 0,
        string? errorCode = null,
        DateTimeOffset? timestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventCode);

        EventCode = eventCode;
        Success = success;
        DurationMs = durationMs >= 0 ? durationMs : 0;
        ErrorCode = errorCode;
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
    }
}
