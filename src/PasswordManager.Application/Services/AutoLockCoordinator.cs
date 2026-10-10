using PasswordManager.Application.Abstractions;
using PasswordManager.Application.Logging;
using PasswordManager.Domain.Enums;

namespace PasswordManager.Application.Services;

/// <summary>
/// Orchestrates automated vault locking based on user inactivity, OS workstation lock, and power suspend.
/// Uses a testable TimeProvider for deterministic timer evaluation.
/// </summary>
public sealed class AutoLockCoordinator : IAutoLockCoordinator
{
    private readonly IVaultLifecycleManager _lifecycleManager;
    private readonly ISessionLockListener? _sessionLockListener;
    private readonly TimeProvider _timeProvider;
    private readonly ISecureLogger _logger;

    private readonly object _stateLock = new();
    private readonly ITimer _timer;

    private TimeSpan _inactivityTimeout = TimeSpan.FromMinutes(5);
    private bool _isEnabled = true;
    private DateTimeOffset _lastActivityUtc;
    private bool _isDisposed;

    public TimeSpan InactivityTimeout
    {
        get
        {
            lock (_stateLock)
            {
                return _inactivityTimeout;
            }
        }
        set
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Inactivity timeout must be positive.");
            }

            lock (_stateLock)
            {
                _inactivityTimeout = value;
            }
        }
    }

    public bool IsEnabled
    {
        get
        {
            lock (_stateLock)
            {
                return _isEnabled;
            }
        }
        set
        {
            lock (_stateLock)
            {
                _isEnabled = value;
            }
        }
    }

    public AutoLockCoordinator(
        IVaultLifecycleManager lifecycleManager,
        ISessionLockListener? sessionLockListener = null,
        TimeProvider? timeProvider = null,
        ISecureLogger? logger = null)
    {
        _lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
        _sessionLockListener = sessionLockListener;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullSecureLogger.Instance;

        _lastActivityUtc = _timeProvider.GetUtcNow();

        if (_sessionLockListener != null)
        {
            _sessionLockListener.SystemLockTriggered += OnSystemLockReceived;
        }

        _lifecycleManager.StateChanged += OnVaultStateChanged;

        // Periodic timer checking every 1 second
        _timer = _timeProvider.CreateTimer(OnTimerTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public void RecordUserActivity()
    {
        lock (_stateLock)
        {
            _lastActivityUtc = _timeProvider.GetUtcNow();
        }
    }

    public async Task HandleSystemLockAsync(SystemLockReason reason, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            return;
        }

        if (_lifecycleManager.CurrentState == VaultState.Unlocked)
        {
            var eventCode = reason == SystemLockReason.UserInactivity
                ? "AUTO_LOCK_TRIGGERED"
                : "SYSTEM_LOCK_TRIGGERED";

            await _lifecycleManager.LockVaultAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogEvent(new SecureLogEvent(
                eventCode,
                success: true,
                durationMs: 0,
                errorCode: reason.ToString().ToUpperInvariant(),
                timestamp: _timeProvider.GetUtcNow()));
        }
    }

    private void OnTimerTick(object? state)
    {
        if (_isDisposed)
        {
            return;
        }

        bool shouldLock = false;
        lock (_stateLock)
        {
            if (!_isEnabled || _lifecycleManager.CurrentState != VaultState.Unlocked)
            {
                return;
            }

            var elapsed = _timeProvider.GetUtcNow() - _lastActivityUtc;
            if (elapsed >= _inactivityTimeout)
            {
                shouldLock = true;
            }
        }

        if (shouldLock)
        {
            _ = HandleSystemLockAsync(SystemLockReason.UserInactivity);
        }
    }

    private void OnSystemLockReceived(SystemLockReason reason)
    {
        _ = HandleSystemLockAsync(reason);
    }

    private void OnVaultStateChanged(object? sender, Models.VaultStateChangedEventArgs e)
    {
        if (e.NewState == VaultState.Unlocked)
        {
            RecordUserActivity();
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_sessionLockListener != null)
        {
            _sessionLockListener.SystemLockTriggered -= OnSystemLockReceived;
        }

        _lifecycleManager.StateChanged -= OnVaultStateChanged;
        _timer.Dispose();
    }
}
