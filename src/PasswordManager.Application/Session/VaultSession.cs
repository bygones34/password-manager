using System.Security.Cryptography;

namespace PasswordManager.Application.Session;

/// <summary>
/// Represents an active, unlocked in-memory vault session.
/// Holds decrypted symmetric keys and the session cancellation source.
/// Implements deterministic key clearing upon disposal.
/// </summary>
public sealed class VaultSession : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly CancellationToken _sessionToken;
    private bool _isDisposed;

    /// <summary>
    /// Unique identifier of the currently unlocked vault.
    /// </summary>
    public Guid VaultId { get; }

    /// <summary>
    /// Monotonically increasing generation number for this unlock session.
    /// </summary>
    public long SessionGeneration { get; }

    /// <summary>
    /// In-memory 256-bit Root Key. Wiped on session disposal.
    /// </summary>
    public byte[] RootKey { get; }

    /// <summary>
    /// In-memory 256-bit Record Key derived via HKDF-SHA-256. Wiped on session disposal.
    /// </summary>
    public byte[] RecordKey { get; }

    /// <summary>
    /// In-memory 256-bit Manifest Key derived via HKDF-SHA-256. Wiped on session disposal.
    /// </summary>
    public byte[] ManifestKey { get; }

    /// <summary>
    /// Cancellation token triggered immediately when the session is locked or terminated.
    /// </summary>
    public CancellationToken CancellationToken => _sessionToken;

    /// <summary>
    /// Indicates whether the session has been disposed/sealed.
    /// </summary>
    public bool IsDisposed => _isDisposed;

    public VaultSession(
        Guid vaultId,
        long sessionGeneration,
        byte[] rootKey,
        byte[] recordKey,
        byte[] manifestKey)
    {
        ArgumentNullException.ThrowIfNull(rootKey);
        ArgumentNullException.ThrowIfNull(recordKey);
        ArgumentNullException.ThrowIfNull(manifestKey);

        if (rootKey.Length != 32 || recordKey.Length != 32 || manifestKey.Length != 32)
        {
            throw new ArgumentException("Session keys must each be exactly 32 bytes (256-bit).");
        }

        VaultId = vaultId;
        SessionGeneration = sessionGeneration;
        _sessionToken = _cts.Token;
        RootKey = (byte[])rootKey.Clone();
        RecordKey = (byte[])recordKey.Clone();
        ManifestKey = (byte[])manifestKey.Clone();
    }

    /// <summary>
    /// Throws ObjectDisposedException if this session has been locked or disposed.
    /// </summary>
    public void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(VaultSession), "The vault session has been disposed or locked.");
        }
    }

    /// <summary>
    /// Cancels all in-flight operations bound to this session and deterministically
    /// zeroes all symmetric key byte buffers in memory using CryptographicOperations.ZeroMemory.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        try
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            _cts.Dispose();
        }

        // Deterministic zeroing of all key memory buffers
        CryptographicOperations.ZeroMemory(RootKey);
        CryptographicOperations.ZeroMemory(RecordKey);
        CryptographicOperations.ZeroMemory(ManifestKey);
    }
}
