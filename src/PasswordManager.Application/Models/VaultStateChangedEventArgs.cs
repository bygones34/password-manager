namespace PasswordManager.Application.Models;

using PasswordManager.Domain.Enums;

/// <summary>
/// Event arguments describing a transition in the vault lifecycle state.
/// </summary>
public sealed class VaultStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// The previous state before transition.
    /// </summary>
    public VaultState OldState { get; }

    /// <summary>
    /// The new state after transition.
    /// </summary>
    public VaultState NewState { get; }

    /// <summary>
    /// The session generation at the time of the transition.
    /// </summary>
    public long Generation { get; }

    public VaultStateChangedEventArgs(VaultState oldState, VaultState newState, long generation)
    {
        OldState = oldState;
        NewState = newState;
        Generation = generation;
    }
}
