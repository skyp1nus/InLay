using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using InLay.Core;

namespace InLay.App.Overlays;

/// <summary>
/// FullScreenSplash indicator (docs §4.4): a large translucent pill centered on the primary monitor
/// that fades in on a layout switch, holds, then fades out. It needs no caret, so it is useful even
/// before caret tracking exists. During rapid switching it stays visible and only swaps the label — the
/// fade always eases from the current opacity, so it never restarts from zero (no flicker).
/// </summary>
internal sealed class FullScreenSplash : OverlayWindow
{
    private const double PillWidthDip = 240;
    private const double PillHeightDip = 150;

    private static readonly Duration FadeInDuration = new(TimeSpan.FromMilliseconds(130));
    private static readonly Duration FadeOutDuration = new(TimeSpan.FromMilliseconds(280));

    private readonly TextBlock _label;
    private readonly Border _pill;
    private readonly DoubleAnimation _fadeIn;
    private readonly DoubleAnimation _fadeOut;
    private readonly DispatcherTimer _holdTimer;

    public FullScreenSplash()
    {
        _label = new TextBlock
        {
            FontSize = 64,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        _pill = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0xE0, 0x1F, 0x1F, 0x1F)),
            CornerRadius = new CornerRadius(28),
            Opacity = 0,
            Child = _label,
        };

        Content = _pill;

        // No From: the fade always starts from the pill's current opacity, so a switch mid-fade eases
        // smoothly instead of snapping back to zero.
        _fadeIn = new DoubleAnimation
        {
            To = 1.0,
            Duration = FadeInDuration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };

        _fadeOut = new DoubleAnimation
        {
            To = 0.0,
            Duration = FadeOutDuration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
        };
        _fadeOut.Completed += OnFadeOutCompleted;

        _holdTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        _holdTimer.Tick += OnHoldElapsed;
    }

    /// <summary>Shows the splash for <paramref name="layout"/>: fades to full opacity, then holds before fading out.</summary>
    public void Show(LayoutInfo layout)
    {
        _label.Text = layout.Label;

        if (!IsVisible)
        {
            PositionOnPrimaryMonitor();
            base.Show();
        }

        _pill.BeginAnimation(UIElement.OpacityProperty, _fadeIn);
        _holdTimer.Stop();
        _holdTimer.Start(); // (re)start the hold; rapid switches keep the pill up and just swap the label
    }

    private void OnHoldElapsed(object? sender, EventArgs e)
    {
        _holdTimer.Stop();
        _pill.BeginAnimation(UIElement.OpacityProperty, _fadeOut);
    }

    private void OnFadeOutCompleted(object? sender, EventArgs e) => Hide();

    private void PositionOnPrimaryMonitor()
    {
        double scale = GetCurrentDpi() / 96.0;
        int width = (int)(PillWidthDip * scale);
        int height = (int)(PillHeightDip * scale);

        if (TryGetPrimaryMonitorBounds(out int monitorX, out int monitorY, out int monitorWidth, out int monitorHeight))
        {
            int x = monitorX + ((monitorWidth - width) / 2);
            int y = monitorY + ((monitorHeight - height) / 2);
            SetPhysicalBounds(x, y, width, height);
        }
    }
}
