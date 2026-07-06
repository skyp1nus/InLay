using AwesomeAssertions;
using InLay.Core;
using Xunit;

namespace InLay.Tests;

public class AppPathsTests
{
    private const string Base = @"C:\Users\test\AppData\Local";

    [Fact]
    public void Resolve_places_everything_under_LocalAppData_InLay()
    {
        AppPathSet paths = AppPaths.Resolve(Base);

        paths.LocalAppDataDirectory.Should().Be(Path.Combine(Base, "InLay"));
        paths.LogsDirectory.Should().Be(Path.Combine(Base, "InLay", "logs"));
        paths.SettingsFilePath.Should().Be(Path.Combine(Base, "InLay", "settings.json"));
    }

    [Fact]
    public void Resolve_log_file_is_a_rolling_template_under_the_logs_directory()
    {
        AppPathSet paths = AppPaths.Resolve(Base);

        paths.LogFilePath.Should().Be(Path.Combine(paths.LogsDirectory, "inlay-.log"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_rejects_a_missing_base(string? invalidBase)
    {
        Action act = () => AppPaths.Resolve(invalidBase!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Live_paths_agree_with_Resolve_over_the_current_LocalAppData()
    {
        string liveBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        AppPathSet expected = AppPaths.Resolve(liveBase);

        AppPaths.LocalAppDataDirectory.Should().Be(expected.LocalAppDataDirectory);
        AppPaths.LogsDirectory.Should().Be(expected.LogsDirectory);
        AppPaths.LogFilePath.Should().Be(expected.LogFilePath);
        AppPaths.SettingsFilePath.Should().Be(expected.SettingsFilePath);
    }
}
