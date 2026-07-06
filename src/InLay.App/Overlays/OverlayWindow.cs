using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace InLay.App.Overlays;

/// <summary>
/// Base class for every indicator overlay (docs §4.3): a borderless, transparent, always-on-top,
/// non-activating, click-through window that never appears in the taskbar or Alt-Tab. Positioning is
/// done in physical pixels via <see cref="SetPhysicalBounds"/>. Concrete overlays (caret badge,
/// splash, corner HUD, border glow) arrive in later milestones; this class carries no visuals itself.
/// </summary>
internal abstract class OverlayWindow : Window
{
    protected OverlayWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowActivated = false;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
    }

    private nint Handle => new WindowInteropHelper(this).EnsureHandle();

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // The HWND exists now; apply the click-through / non-activating extended styles.
        OverlayInterop.MakeClickThrough(Handle);
    }

    /// <summary>Positions the overlay in physical pixels (Win32 coordinates), per docs §4.3.</summary>
    public void SetPhysicalBounds(int x, int y, int width, int height) =>
        OverlayInterop.SetPhysicalBounds(Handle, x, y, width, height);

    /// <summary>Effective DPI (96 = 100%) of the monitor the overlay is currently on.</summary>
    public uint GetCurrentDpi() => OverlayInterop.GetDpiForNearestMonitor(Handle);
}
