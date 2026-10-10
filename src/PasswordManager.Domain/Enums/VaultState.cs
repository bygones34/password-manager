namespace PasswordManager.Domain.Enums;

/// <summary>
/// States of the vault lifecycle and active session.
/// </summary>
public enum VaultState
{
    /// <summary>
    /// No vault database exists at the designated storage path.
    /// </summary>
    NoVault,

    /// <summary>
    /// Vault exists but is sealed; no symmetric keys or decrypted records exist in memory.
    /// </summary>
    Locked,

    /// <summary>
    /// Vault is currently verifying master password, deriving KEK and unwrapping root key.
    /// </summary>
    Unlocking,

    /// <summary>
    /// Vault is open; active session generation is established and symmetric keys are present in memory.
    /// </summary>
    Unlocked,

    /// <summary>
    /// Vault is actively cancelling pending requests, zeroing keys and clearing memory buffers.
    /// </summary>
    Locking,

    /// <summary>
    /// An unrecoverable error or tampering violation occurred.
    /// </summary>
    Faulted
}
