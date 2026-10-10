using System.Collections.Concurrent;

namespace PasswordManager.Application.Logging;

/// <summary>
/// Thread-safe in-memory audit log collector that validates and stores sanitized events.
/// Useful for testing, security telemetry inspection, and audit verification.
/// </summary>
public sealed class SecureMemoryAuditLogger : ISecureLogger
{
    private readonly ConcurrentQueue<SecureLogEvent> _events = new();

    public IReadOnlyList<SecureLogEvent> Events => _events.ToArray();

    public void LogEvent(in SecureLogEvent logEvent)
    {
        var sanitized = SecureLogSanitizer.Sanitize(logEvent);
        _events.Enqueue(sanitized);
    }

    public void Clear()
    {
        _events.Clear();
    }
}
