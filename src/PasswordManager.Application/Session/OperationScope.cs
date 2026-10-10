namespace PasswordManager.Application.Session;

/// <summary>
/// Implementation of IOperationScope that monitors session lifetime, session generation,
/// and provides a linked cancellation token that aborts on vault lock or external cancellation.
/// </summary>
public sealed class OperationScope : IOperationScope
{
    private readonly VaultSession _session;
    private readonly Func<long> _getCurrentGeneration;
    private readonly CancellationTokenSource _linkedCts;
    private bool _isDisposed;

    public long Generation { get; }
    public CancellationToken CancellationToken => _linkedCts.Token;

    public bool IsActive =>
        !_isDisposed &&
        !_session.IsDisposed &&
        _getCurrentGeneration() == Generation &&
        !_linkedCts.IsCancellationRequested;

    public OperationScope(
        VaultSession session,
        Func<long> getCurrentGeneration,
        CancellationToken externalToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(getCurrentGeneration);

        _session = session;
        _getCurrentGeneration = getCurrentGeneration;
        Generation = session.SessionGeneration;

        _linkedCts = externalToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(session.CancellationToken, externalToken)
            : CancellationTokenSource.CreateLinkedTokenSource(session.CancellationToken);
    }

    public void ThrowIfCanceledOrExpired()
    {
        if (_isDisposed || _session.IsDisposed || _getCurrentGeneration() != Generation)
        {
            throw new OperationCanceledException("Operation scope expired or vault was locked.");
        }

        _linkedCts.Token.ThrowIfCancellationRequested();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _linkedCts.Dispose();
    }
}
