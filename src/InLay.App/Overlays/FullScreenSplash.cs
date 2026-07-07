using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using InLay.Core;

namespace InLay.App.Overlays;

/// <summary>
/// FullScreenSplash indicator (docs §4.4): a large translucent pill centered on the primary monitor
/// that fades in and out on each layout switch. It needs no caret, so it is useful even before caret
/// tracking exists. Purely visual feedback — the click-through base means it never takes focus.
/// </summary>
internal sealed class FullScreenSplash : OverlayWindow
{
    private const double PillWidthDip = 240;
    private const double PillHeightDip = 150;

    private readonly TextBlock _label;
    private readonly Border _pill;
    private readonly Storyboard _fade;

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

        _fade = BuildFadeStoryboard(_pill);
        _fade.Completed += OnFadeCompleted;
    }

    /// <summary>Positions the splash for <paramref name="layout"/> and plays the fade in/out.</summary>
    public void Show(LayoutInfo layout)
    {
        _label.Text = layout.Label;
        PositionOnPrimaryMonitor();

        if (!IsVisible)
        {
            base.Show();
        }

        _fade.Begin();
    }

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

    private void OnFadeCompleted(object? sender, EventArgs e) => Hide();

    private static Storyboard BuildFadeStoryboard(UIElement target)
    {
        // Fade in (0→1) over 180 ms, hold, then fade out (1→0), ending at ~720 ms total (docs §4.4).
        var animation = new DoubleAnimationUsingKeyFrames();
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180))));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(520))));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(720))));

        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));

        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        return storyboard;
    }
}
