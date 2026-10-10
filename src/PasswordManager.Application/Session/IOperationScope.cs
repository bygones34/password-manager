namespace PasswordManager.Application.Session;

/// <summary>
/// Represents a bounded scope for a sensitive operation (decrypt, reveal, fill)
/// tied to a specific vault session generation and subject to immediate cancellation on vault lock.
/// </summary>
public interface IOperationScope : IDisposable
{
    /// <summary>
    /// Monotonically increasing generation of the session this scope was created for.
    /// </summary>
    long Generation { get; }

    /// <summary>
    /// Cancellation token that triggers immediately if either the operation token is cancelled
    /// or the vault session is locked / disposed.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Indicates whether the scope is still active and valid (not disposed, session not locked, generation matching).
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Verifies that the scope is active and the session generation matches the current active session.
    /// Throws OperationCanceledException if expired or locked.
    /// </summary>
    void ThrowIfCanceledOrExpired();
}
