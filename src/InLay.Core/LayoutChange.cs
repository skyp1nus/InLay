namespace InLay.Core;

/// <summary>
/// Why <see cref="LayoutMonitor.LayoutChanged"/> fired, so indicators can react selectively (docs §4.4):
/// a transient splash should appear only for a genuine switch, while a persistent HUD tracks every read.
/// </summary>
public enum LayoutChangeReason
{
    /// <summary>The keyboard layout was switched in place — a real language change the user made.</summary>
    LayoutSwitch,

    /// <summary>
    /// The layout was re-read without a real switch: after a focus/foreground change, on startup, on
    /// resume, or by the fallback poll. Persistent indicators refresh; transient ones should stay quiet.
    /// </summary>
    FocusRefresh,
}

/// <summary>
/// Payload for <see cref="LayoutMonitor.LayoutChanged"/>: the resolved layout plus why it fired. Named to
/// avoid the <c>EventArgs</c> suffix (CA1711) since it is a plain record, matching the engine's other
/// <see cref="EventHandler{T}"/> payloads rather than deriving from <see cref="EventArgs"/>.
/// </summary>
/// <param name="Layout">The resolved active keyboard layout.</param>
/// <param name="Reason">Why the change was reported — a real in-place switch versus a refresh.</param>
public sealed record LayoutChange(LayoutInfo Layout, LayoutChangeReason Reason);
