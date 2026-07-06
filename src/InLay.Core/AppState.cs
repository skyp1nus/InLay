namespace InLay.Core;

/// <summary>
/// Shared runtime state for the application. In M0 this is only the paused flag; it is the single
/// source of truth that later milestones' engine (LayoutMonitor) subscribes to via
/// <see cref="PausedChanged"/> to stop and resume indicator output. Kept UI-free so it lives in Core.
/// </summary>
public sealed class AppState
{
    private bool _isPaused;

    /// <summary>Raised when <see cref="IsPaused"/> changes; the event argument is the new value.</summary>
    public event EventHandler<bool>? PausedChanged;

    /// <summary>Whether indicator output is currently paused. Setting the same value is a no-op.</summary>
    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (_isPaused == value)
            {
                return;
            }

            _isPaused = value;
            PausedChanged?.Invoke(this, value);
        }
    }

    /// <summary>Flips <see cref="IsPaused"/> and returns the resulting value.</summary>
    public bool TogglePause()
    {
        IsPaused = !IsPaused;
        return IsPaused;
    }
}
