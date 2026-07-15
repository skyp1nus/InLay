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
    public void ShouldEmitIsFalseForAnUnresolvedLayout(LayoutChangeReason reason)
    {
        LayoutMonitor.ShouldEmit(0, reason, EnUs, LayoutChangeReason.LayoutSwitch).Should().BeFalse();
    }

    [Theory]
    [InlineData(LayoutChangeReason.LayoutSwitch)]
    [InlineData(LayoutChangeReason.FocusRefresh)]
    public void ShouldEmitIsTrueWhenTheLayoutDiffers(LayoutChangeReason reason)
    {
        LayoutMonitor.ShouldEmit(UkUa, reason, EnUs, LayoutChangeReason.LayoutSwitch).Should().BeTrue();
    }

    [Fact]
    public void ShouldEmitDropsARepeatedRefreshForTheSameLayout()
    {
        LayoutMonitor.ShouldEmit(UkUa, LayoutChangeReason.FocusRefresh, UkUa, LayoutChangeReason.FocusRefresh)
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldEmitDropsARefreshAfterASwitchForTheSameLayout()
    {
        // The splash already fired for this layout; a later focus/poll refresh must not re-fire it.
        LayoutMonitor.ShouldEmit(UkUa, LayoutChangeReason.FocusRefresh, UkUa, LayoutChangeReason.LayoutSwitch)
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldEmitDropsARepeatedSwitchForTheSameLayout()
    {
        // Duplicate switch signal for one transition (shell hook + poll) — one splash, not two.
        LayoutMonitor.ShouldEmit(UkUa, LayoutChangeReason.LayoutSwitch, UkUa, LayoutChangeReason.LayoutSwitch)
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldEmitUpgradesARefreshToASwitchForTheSameLayout()
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
    public void ClassifyReasonIsASwitchWhenTheLayoutChangesUnderTheSameWindow()
    {
        // The whole point: an in-place switch is a new LANGID under an unchanged foreground window, so it
        // is detected even when HSHELL_LANGUAGE never fires.
        LayoutMonitor.ClassifyReason(UkUa, WindowA, EnUs, WindowA)
            .Should().Be(LayoutChangeReason.LayoutSwitch);
    }

    [Fact]
    public void ClassifyReasonIsARefreshWhenTheLayoutIsUnchanged()
    {
        LayoutMonitor.ClassifyReason(EnUs, WindowA, EnUs, WindowA)
            .Should().Be(LayoutChangeReason.FocusRefresh);
    }

    [Fact]
    public void ClassifyReasonIsARefreshWhenTheWindowChangedEvenIfTheLayoutDiffers()
    {
        // Focusing a different window that simply has another layout is not a switch the user made.
        LayoutMonitor.ClassifyReason(UkUa, WindowB, EnUs, WindowA)
            .Should().Be(LayoutChangeReason.FocusRefresh);
    }

    [Fact]
    public void ClassifyReasonIsARefreshWhenFocusingAnotherWindowWithTheSameLayout()
    {
        LayoutMonitor.ClassifyReason(EnUs, WindowB, EnUs, WindowA)
            .Should().Be(LayoutChangeReason.FocusRefresh);
    }

    [Fact]
    public void ClassifyReasonIsARefreshForAnUnresolvedLayout()
    {
        LayoutMonitor.ClassifyReason(0, WindowA, EnUs, WindowA)
            .Should().Be(LayoutChangeReason.FocusRefresh);
    }

    [Fact]
    public void ClassifyReasonIsARefreshForTheFirstObservation()
    {
        // No prior baseline: lastHwnd is 0 and a real window handle never equals 0, so the first read is
        // a silent refresh, never a spurious startup switch.
        LayoutMonitor.ClassifyReason(UkUa, WindowA, 0, 0)
            .Should().Be(LayoutChangeReason.FocusRefresh);
    }
}
