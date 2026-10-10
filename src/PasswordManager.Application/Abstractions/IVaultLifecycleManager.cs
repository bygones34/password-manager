using PasswordManager.Application.Models;
using PasswordManager.Application.Session;
using PasswordManager.Domain.Enums;

namespace PasswordManager.Application.Abstractions;

/// <summary>
/// Core port orchestrating vault lifecycle states, session creation, unlocking, locking, and operation scopes.
/// </summary>
public interface IVaultLifecycleManager : IDisposable
{
    /// <summary>
    /// Current state of the vault.
    /// </summary>
    VaultState CurrentState { get; }

    /// <summary>
    /// Current session generation. Incremented monotonically on each successful unlock.
    /// </summary>
    long CurrentGeneration { get; }

    /// <summary>
    /// Active vault ID if initialized or unlocked, or null if no vault.
    /// </summary>
    Guid? VaultId { get; }

    /// <summary>
    /// Event raised when the vault state transitions.
    /// </summary>
    event EventHandler<VaultStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Checks whether a valid vault exists at the storage location and refreshes CurrentState (NoVault vs Locked).
    /// </summary>
    Task<bool> RefreshVaultStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes a new vault with the specified master password and optional KDF parameters.
    /// Automatically unlocks the vault upon successful creation.
    /// </summary>
    Task CreateVaultAsync(
        ReadOnlyMemory<char> masterPassword,
        KdfParameters? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks an existing vault using the provided master password.
    /// Enforces single concurrent unlock, validates KDF bounds, unwraps root key, and establishes active session.
    /// </summary>
    Task UnlockVaultAsync(
        ReadOnlyMemory<char> masterPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Seals and locks the vault.
    /// Cancels pending operation scopes, zeros key memory buffers, and resets active session.
    /// </summary>
    Task LockVaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current active VaultSession. Throws InvalidOperationException if the vault is not unlocked.
    /// </summary>
    VaultSession GetActiveSession();

    /// <summary>
    /// Creates a scoped token for sensitive operations bound to the current session generation.
    /// Throws InvalidOperationException if the vault is not unlocked.
    /// </summary>
    IOperationScope CreateOperationScope(CancellationToken cancellationToken = default);
}
