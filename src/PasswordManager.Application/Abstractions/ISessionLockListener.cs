namespace PasswordManager.Application.Abstractions;

/// <summary>
/// Reasons causing a system or environment-driven vault lock.
/// </summary>
public enum SystemLockReason
{
    /// <summary>
    /// User remained inactive longer than the configured timeout threshold.
    /// </summary>
    UserInactivity,

    /// <summary>
    /// Windows workstation was locked (Win+L or security desktop).
    /// </summary>
    WorkstationLocked,

    /// <summary>
    /// Windows user logged off.
    /// </summary>
    SessionLogoff,

    /// <summary>
    /// Remote desktop session was disconnected.
    /// </summary>
    RemoteDisconnect,

    /// <summary>
    /// System entered sleep, hibernate or low-power suspend state.
    /// </summary>
    SystemSuspend
}

/// <summary>
/// Port for observing operating system session lock, logoff and power suspend events.
/// </summary>
public interface ISessionLockListener
{
    /// <summary>
    /// Event triggered when the OS or platform signals a session lock, logoff, disconnect, or power suspend.
    /// </summary>
    event Action<SystemLockReason>? SystemLockTriggered;
}
