namespace PasswordManager.Application.Tests.Helpers;

/// <summary>
/// Controllable TimeProvider for deterministic timer and clock testing.
/// </summary>
public sealed class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;
    private readonly List<ManualTimer> _timers = new();
    private readonly object _lock = new();

    public ManualTimeProvider(DateTimeOffset? initialTime = null)
    {
        _utcNow = initialTime ?? new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_lock)
        {
            return _utcNow;
        }
    }

    public void Advance(TimeSpan delta)
    {
        if (delta <= TimeSpan.Zero)
        {
            return;
        }

        List<ManualTimer> timersToTick;
        lock (_lock)
        {
            _utcNow += delta;
            timersToTick = _timers.ToList();
        }

        foreach (var timer in timersToTick)
        {
            timer.CheckAndTick(_utcNow);
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state, dueTime, period);
        lock (_lock)
        {
            _timers.Add(timer);
        }
        return timer;
    }

    private void RemoveTimer(ManualTimer timer)
    {
        lock (_lock)
        {
            _timers.Remove(timer);
        }
    }

    private sealed class ManualTimer : ITimer
    {
        private readonly ManualTimeProvider _provider;
        private readonly TimerCallback _callback;
        private readonly object? _state;
        private TimeSpan _dueTime;
        private TimeSpan _period;
        private DateTimeOffset _nextTick;
        private bool _isDisposed;

        public ManualTimer(ManualTimeProvider provider, TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            _provider = provider;
            _callback = callback;
            _state = state;
            _dueTime = dueTime;
            _period = period;
            _nextTick = provider.GetUtcNow() + dueTime;
        }

        public void CheckAndTick(DateTimeOffset currentUtc)
        {
            if (_isDisposed || _dueTime == Timeout.InfiniteTimeSpan)
            {
                return;
            }

            if (currentUtc >= _nextTick)
            {
                _callback(_state);

                if (_period == Timeout.InfiniteTimeSpan || _period <= TimeSpan.Zero)
                {
                    _dueTime = Timeout.InfiniteTimeSpan;
                }
                else
                {
                    _nextTick = currentUtc + _period;
                }
            }
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            if (_isDisposed)
            {
                return false;
            }

            _dueTime = dueTime;
            _period = period;
            _nextTick = _provider.GetUtcNow() + dueTime;
            return true;
        }

        public void Dispose()
        {
            _isDisposed = true;
            _provider.RemoveTimer(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
