namespace PasswordManager.Application.Logging;

/// <summary>
/// Security logging port enforcing whitelist-only logging.
/// Rejects free-form message dumps, entity destructuring, and un-sanitized exception payloads.
/// </summary>
public interface ISecureLogger
{
    /// <summary>
    /// Logs a structured security event adhering to the strict whitelist policy.
    /// </summary>
    void LogEvent(in SecureLogEvent logEvent);
}
