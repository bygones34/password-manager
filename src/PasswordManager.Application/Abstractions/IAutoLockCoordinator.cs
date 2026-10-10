namespace PasswordManager.Application.Abstractions;

/// <summary>
/// Port orchestrating automated vault locking based on user inactivity, OS workstation lock, and power suspend.
/// </summary>
public interface IAutoLockCoordinator : IDisposable
{
    /// <summary>
    /// Gets or sets the duration of user inactivity required before automatically locking the vault.
    /// Default baseline is 5 minutes.
    /// </summary>
    TimeSpan InactivityTimeout { get; set; }

    /// <summary>
    /// Gets or sets whether automatic inactivity locking is active.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Records user interaction (keystroke, mouse click, navigation) to refresh the inactivity timer.
    /// </summary>
    void RecordUserActivity();

    /// <summary>
    /// Handles an immediate system-triggered lock event (OS Lock, Suspend, Logoff, Remote Disconnect).
    /// </summary>
    Task HandleSystemLockAsync(SystemLockReason reason, CancellationToken cancellationToken = default);
}
