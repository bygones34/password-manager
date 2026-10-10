namespace PasswordManager.Application.Logging;

/// <summary>
/// No-op implementation of ISecureLogger.
/// </summary>
public sealed class NullSecureLogger : ISecureLogger
{
    public static readonly NullSecureLogger Instance = new();

    private NullSecureLogger()
    {
    }

    public void LogEvent(in SecureLogEvent logEvent)
    {
        // No-op
    }
}
