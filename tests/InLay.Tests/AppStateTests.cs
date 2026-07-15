using AwesomeAssertions;
using InLay.Core;
using Xunit;

namespace InLay.Tests;

public class AppStateTests
{
    [Fact]
    public void DefaultsToNotPaused()
    {
        new AppState().IsPaused.Should().BeFalse();
    }

    [Fact]
    public void SettingIsPausedRaisesPausedChangedWithTheNewValue()
    {
        var state = new AppState();
        var raised = new List<bool>();
        state.PausedChanged += (_, value) => raised.Add(value);

        state.IsPaused = true;

        state.IsPaused.Should().BeTrue();
        raised.Should().ContainSingle().Which.Should().BeTrue();
    }

    [Fact]
    public void SettingTheSameValueDoesNotRaiseTheEvent()
    {
        var state = new AppState();
        var raisedCount = 0;
        state.PausedChanged += (_, _) => raisedCount++;

        state.IsPaused = false; // already false

        raisedCount.Should().Be(0);
    }

    [Fact]
    public void TogglePauseFlipsTheFlagReturnsItAndRaisesTheEvent()
    {
        var state = new AppState();
        var raised = new List<bool>();
        state.PausedChanged += (_, value) => raised.Add(value);

        bool first = state.TogglePause();
        bool second = state.TogglePause();

        first.Should().BeTrue();
        second.Should().BeFalse();
        state.IsPaused.Should().BeFalse();
        raised.Should().Equal(true, false);
    }
}
