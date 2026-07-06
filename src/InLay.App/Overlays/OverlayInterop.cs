using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace InLay.App.Overlays;

/// <summary>
/// Win32 interop for the click-through overlay (docs §4.3), all via CsWin32-generated P/Invokes.
/// Overlays are positioned in physical pixels; DPI is read per monitor.
/// </summary>
internal static class OverlayInterop
{
    private const uint DefaultDpi = 96;

    /// <summary>
    /// Makes the window fully click-through and non-activating by OR-ing the overlay extended styles
    /// into <c>GWL_EXSTYLE</c> (read-modify-write, never overwrite).
    /// </summary>
    public static void MakeClickThrough(nint windowHandle)
    {
        var hwnd = new HWND(windowHandle);
        int current = PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        int updated = unchecked((int)ComposeExStyle((uint)current));
        _ = PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, updated);
    }

    /// <summary>Positions the window in physical pixels without activating it or changing its owner z-order.</summary>
    public static void SetPhysicalBounds(nint windowHandle, int x, int y, int width, int height)
    {
        _ = PInvoke.SetWindowPos(
            new HWND(windowHandle),
            HWND.HWND_TOPMOST,
            x,
            y,
            width,
            height,
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOOWNERZORDER);
    }

    /// <summary>Effective DPI of the monitor nearest the window (96 = 100%). Falls back to 96 on failure.</summary>
    public static uint GetDpiForNearestMonitor(nint windowHandle)
    {
        // GetDpiForMonitor is Windows 8.1+. InLay targets Windows 10/11, so this guard is always true
        // at runtime; it satisfies the platform-compatibility analyzer without raising the TFM baseline.
        if (!OperatingSystem.IsWindowsVersionAtLeast(8, 1))
        {
            return DefaultDpi;
        }

        HMONITOR monitor = PInvoke.MonitorFromWindow(new HWND(windowHandle), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        return PInvoke.GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out uint dpiX, out _).Succeeded
            ? dpiX
            : DefaultDpi;
    }

    /// <summary>Pure OR of the four click-through extended styles onto an existing GWL_EXSTYLE bitmask.</summary>
    internal static uint ComposeExStyle(uint existing) =>
        existing
        | (uint)WINDOW_EX_STYLE.WS_EX_LAYERED
        | (uint)WINDOW_EX_STYLE.WS_EX_TRANSPARENT
        | (uint)WINDOW_EX_STYLE.WS_EX_NOACTIVATE
        | (uint)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW;
}
