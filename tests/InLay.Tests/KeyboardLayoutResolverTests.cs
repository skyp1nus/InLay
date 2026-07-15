using AwesomeAssertions;
using InLay.Core;
using Xunit;

namespace InLay.Tests;

public class KeyboardLayoutResolverTests
{
    [Theory]
    [InlineData(0x0409L, (ushort)0x0409)]           // bare LANGID (en-US)
    [InlineData(0x04090409L, (ushort)0x0409)]       // typical HKL: device id in the high word, LANGID low
    [InlineData(0x04220422L, (ushort)0x0422)]       // uk-UA
    [InlineData(0xF0140804L, (ushort)0x0804)]       // high bit set (custom/IME HKL) — still the low word
    public void LangIdFromHklTakesTheLowWord(long hkl, ushort expected)
    {
        KeyboardLayoutResolver.LangIdFromHkl((nint)hkl).Should().Be(expected);
    }

    [Theory]
    [InlineData("uk-UA", "UK")]
    [InlineData("en-US", "EN")]
    [InlineData("en", "EN")]
    [InlineData("de-DE", "DE")]
    public void LabelFromLocaleNameUppercasesThePrimarySubtag(string localeName, string expected)
    {
        KeyboardLayoutResolver.LabelFromLocaleName(localeName).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LabelFromLocaleNameFallsBackWhenTheNameIsMissing(string? localeName)
    {
        KeyboardLayoutResolver.LabelFromLocaleName(localeName!).Should().Be("??");
    }
}
