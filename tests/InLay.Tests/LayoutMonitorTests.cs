using AwesomeAssertions;
using InLay.Core;
using Xunit;

namespace InLay.Tests;

public class LayoutMonitorTests
{
    private const ushort EnUs = 0x0409;
    private const ushort UkUa = 0x0422;

    [Theory]
    [InlineData(LayoutChangeReason.LayoutSwitch)]
    [InlineData(LayoutChangeReason.FocusRefresh)]
    public void ShouldEmit_is_false_for_an_unresolved_layout(LayoutChangeReason reason)
    {
        LayoutMonitor.ShouldEmit(0, reason, EnUs, LayoutChangeReason.LayoutSwitch).Should().BeFalse();
    }

    [Theory]
    [InlineData(LayoutChangeReason.LayoutSwitch)]
    [InlineData(LayoutChangeReason.FocusRefresh)]
    public void ShouldEmit_is_true_when_the_layout_differs(LayoutChangeReason reason)
    {
        LayoutMonitor.ShouldEmit(UkUa, reason, EnUs, LayoutChangeReason.LayoutSwitch).Should().BeTrue();
    }

    [Fact]
    public void ShouldEmit_drops_a_repeated_refresh_for_the_same_layout()
    {
        LayoutMonitor.ShouldEmit(UkUa, LayoutChangeReason.FocusRefresh, UkUa, LayoutChangeReason.FocusRefresh)
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldEmit_drops_a_refresh_after_a_switch_for_the_same_layout()
    {
        // The splash already fired for this layout; a later focus/poll refresh must not re-fire it.
        LayoutMonitor.ShouldEmit(UkUa, LayoutChangeReason.FocusRefresh, UkUa, LayoutChangeReason.LayoutSwitch)
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldEmit_drops_a_repeated_switch_for_the_same_layout()
    {
        // Duplicate switch signal for one transition (shell hook + poll) — one splash, not two.
        LayoutMonitor.ShouldEmit(UkUa, LayoutChangeReason.LayoutSwitch, UkUa, LayoutChangeReason.LayoutSwitch)
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldEmit_upgrades_a_refresh_to_a_switch_for_the_same_layout()
    {
        // The race the reason-aware de-dup exists for: the poll/focus read reports the in-place switch's
        // new HKL as a refresh before HSHELL_LANGUAGE arrives; the real switch must still emit so the
        // splash fires instead of being de-duped away.
        LayoutMonitor.ShouldEmit(UkUa, LayoutChangeReason.LayoutSwitch, UkUa, LayoutChangeReason.FocusRefresh)
            .Should().BeTrue();
    }

    private static readonly nint WindowA = 0x1111;
    private static readonly nint WindowB = 0x2222;

    [Fact]
    public void ClassifyReason_is_a_switch_when_the_layout_changes_under_the_same_window()
    {
        // The whole point: an in-place switch is a new LANGID under an unchanged foreground window, so it
        // is detected even when HSHELL_LANGUAGE never fires.
        LayoutMonitor.ClassifyReason(UkUa, WindowA, EnUs, WindowA)
            .Should().Be(LayoutChangeReason.LayoutSwitch);
    }

    [Fact]
    public void ClassifyReason_is_a_refresh_when_the_layout_is_unchanged()
    {
        LayoutMonitor.ClassifyReason(EnUs, WindowA, EnUs, WindowA)
            .Should().Be(LayoutChangeReason.FocusRefresh);
    }

    [Fact]
    public void ClassifyReason_is_a_refresh_when_the_window_changed_even_if_the_layout_differs()
    {
        // Focusing a different window that simply has another layout is not a switch the user made.
        LayoutMonitor.ClassifyReason(UkUa, WindowB, EnUs, WindowA)
            .Should().Be(LayoutChangeReason.FocusRefresh);
    }

    [Fact]
    public void ClassifyReason_is_a_refresh_when_focusing_another_window_with_the_same_layout()
    {
        LayoutMonitor.ClassifyReason(EnUs, WindowB, EnUs, WindowA)
            .Should().Be(LayoutChangeReason.FocusRefresh);
    }

    [Fact]
    public void ClassifyReason_is_a_refresh_for_an_unresolved_layout()
    {
        LayoutMonitor.ClassifyReason(0, WindowA, EnUs, WindowA)
            .Should().Be(LayoutChangeReason.FocusRefresh);
    }

    [Fact]
    public void ClassifyReason_is_a_refresh_for_the_first_observation()
    {
        // No prior baseline: lastHwnd is 0 and a real window handle never equals 0, so the first read is
        // a silent refresh, never a spurious startup switch.
        LayoutMonitor.ClassifyReason(UkUa, WindowA, 0, 0)
            .Should().Be(LayoutChangeReason.FocusRefresh);
    }
}
