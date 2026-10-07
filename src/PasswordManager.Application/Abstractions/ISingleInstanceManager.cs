using System;

namespace PasswordManager.Application.Abstractions;

/// <summary>
/// Port for coordinating single instance application execution.
/// </summary>
public interface ISingleInstanceManager : IDisposable
{
    /// <summary>
    /// Attempts to acquire the exclusive single-instance lock for the current user session.
    /// </summary>
    /// <returns>True if this instance is the primary instance; false if another instance is already running.</returns>
    bool TryAcquireOwnership();

    /// <summary>
    /// Signals the already running primary instance to activate and bring its window to foreground.
    /// </summary>
    bool SignalExistingInstance();

    /// <summary>
    /// Event triggered when an activation request is received from a secondary instance.
    /// </summary>
    event Action? ActivationRequested;
}
