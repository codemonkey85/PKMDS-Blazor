namespace Pkmds.Tests;

public sealed class LayoutAssetTests
{
    [Fact]
    public void ActiveTabPanelOverridesMudBlazorDisplayContents()
    {
        var appCss = RepoFileTestHelper.ReadAllText("Pkmds.Rcl", "wwwroot", "css", "app.css");

        appCss.Should().Contain(
            ".mud-tabs-panels > .mud-tab-panel:not(.mud-tab).mud-tab-panel-active:not(.mud-tab-panel-hidden)");
    }
}
