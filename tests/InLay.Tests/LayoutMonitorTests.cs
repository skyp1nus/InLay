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
}
