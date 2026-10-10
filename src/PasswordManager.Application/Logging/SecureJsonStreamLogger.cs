using System.Text.Json;

namespace PasswordManager.Application.Logging;

/// <summary>
/// Streams sanitized security events to a TextWriter in NDJSON (newline-delimited JSON) format.
/// Strictly restricts output to whitelisted fields.
/// </summary>
public sealed class SecureJsonStreamLogger : ISecureLogger
{
    private readonly TextWriter _writer;
    private readonly object _lock = new();

    public SecureJsonStreamLogger(TextWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public void LogEvent(in SecureLogEvent logEvent)
    {
        var sanitized = SecureLogSanitizer.Sanitize(logEvent);

        lock (_lock)
        {
            var doc = BuildJsonDocument(sanitized);
            var jsonString = JsonSerializer.Serialize(doc);
            _writer.WriteLine(jsonString);
            _writer.Flush();
        }
    }

    private static object BuildJsonDocument(in SecureLogEvent ev)
    {
        return new
        {
            event_code = ev.EventCode,
            duration_ms = ev.DurationMs,
            success = ev.Success,
            error_code = ev.ErrorCode,
            timestamp = ev.Timestamp.ToString("O")
        };
    }
}
