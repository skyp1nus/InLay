using AwesomeAssertions;
using InLay.Core;
using Xunit;

namespace InLay.Tests;

public class AppPathsTests
{
    private const string Base = @"C:\Users\test\AppData\Local";

    [Fact]
    public void ResolvePlacesEverythingUnderLocalAppDataInLay()
    {
        AppPathSet paths = AppPaths.Resolve(Base);

        paths.LocalAppDataDirectory.Should().Be(Path.Combine(Base, "InLay"));
        paths.LogsDirectory.Should().Be(Path.Combine(Base, "InLay", "logs"));
        paths.SettingsFilePath.Should().Be(Path.Combine(Base, "InLay", "settings.json"));
    }

    [Fact]
    public void ResolveLogFileIsARollingTemplateUnderTheLogsDirectory()
    {
        AppPathSet paths = AppPaths.Resolve(Base);

        paths.LogFilePath.Should().Be(Path.Combine(paths.LogsDirectory, "inlay-.log"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveRejectsAMissingBase(string? invalidBase)
    {
        Action act = () => AppPaths.Resolve(invalidBase!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LivePathsAgreeWithResolveOverTheCurrentLocalAppData()
    {
        string liveBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        AppPathSet expected = AppPaths.Resolve(liveBase);

        AppPaths.LocalAppDataDirectory.Should().Be(expected.LocalAppDataDirectory);
        AppPaths.LogsDirectory.Should().Be(expected.LogsDirectory);
        AppPaths.LogFilePath.Should().Be(expected.LogFilePath);
        AppPaths.SettingsFilePath.Should().Be(expected.SettingsFilePath);
    }
}
