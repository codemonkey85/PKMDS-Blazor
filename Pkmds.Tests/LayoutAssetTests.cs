namespace Pkmds.Tests;

public sealed class LayoutAssetTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [Fact]
    public void ActiveTabPanelOverridesMudBlazorDisplayContents()
    {
        var appCss = File.ReadAllText(Path.Combine(RepoRoot, "Pkmds.Rcl", "wwwroot", "css", "app.css"));

        appCss.Should().Contain(
            ".mud-tabs-panels > .mud-tab-panel:not(.mud-tab).mud-tab-panel-active:not(.mud-tab-panel-hidden)");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Pkmds.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
