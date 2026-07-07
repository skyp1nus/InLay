using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using InLay.Core;

namespace InLay.App.Overlays;

/// <summary>
/// CornerHud indicator (docs §4.4): a small persistent pill anchored to the bottom-right of the primary
/// monitor whose label updates on every layout switch. For users who want the current layout visible at
/// all times. Built on the click-through OverlayWindow; hidden via <see cref="Window.Hide"/>.
/// </summary>
internal sealed class CornerHud : OverlayWindow
{
    private const double PillWidthDip = 72;
    private const double PillHeightDip = 40;
    private const double MarginDip = 24;

    private readonly TextBlock _label;

    public CornerHud()
    {
        _label = new TextBlock
        {
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        Content = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0xE0, 0x1F, 0x1F, 0x1F)),
            CornerRadius = new CornerRadius(PillHeightDip / 2),
            Child = _label,
        };
    }

    /// <summary>Updates the pill to <paramref name="layout"/> and shows it (persistent, fixed corner).</summary>
    public void Show(LayoutInfo layout)
    {
        _label.Text = layout.Label;

        // The pill is a fixed size anchored to a fixed corner, so its bounds only need computing on the
        // first show; rapid switches then just swap the label without redundant Win32 positioning calls.
        if (!IsVisible)
        {
            PositionBottomRight();
            base.Show();
        }
    }

    private void PositionBottomRight()
    {
        double scale = GetCurrentDpi() / 96.0;
        int width = (int)(PillWidthDip * scale);
        int height = (int)(PillHeightDip * scale);
        int margin = (int)(MarginDip * scale);

        if (TryGetPrimaryMonitorBounds(out int monitorX, out int monitorY, out int monitorWidth, out int monitorHeight))
        {
            int x = monitorX + monitorWidth - width - margin;
            int y = monitorY + monitorHeight - height - margin;
            SetPhysicalBounds(x, y, width, height);
        }
    }
}
