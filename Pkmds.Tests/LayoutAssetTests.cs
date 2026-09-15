namespace Pkmds.Tests;

public sealed class LayoutAssetTests
{
    [Fact]
    public void IosPwaUsesOpaqueStatusBarWithoutProgressiveBlurOptIn()
    {
        var index = RepoFileTestHelper.ReadAllText("Pkmds.Web", "wwwroot", "index.html");

        index.Should().Contain("apple-mobile-web-app-status-bar-style\" content=\"black\"");
        index.Should().NotContain("content=\"black-translucent\"");
    }

    [Fact]
    public void ActiveTabPanelOverridesPreserveSpecializedFlexLayouts()
    {
        var appCss = RepoFileTestHelper.ReadAllText("Pkmds.Rcl", "wwwroot", "css", "app.css");

        appCss.Should().Contain(
            ".mud-tabs-panels > .mud-tab-panel:not(.mud-tab).mud-tab-panel-active:not(.mud-tab-panel-hidden)");
        appCss.Should().Contain(
            ".save-file-outer-tabs > .mud-tabs-panels > .mud-tab-panel:not(.mud-tab).mud-tab-panel-active:not(.mud-tab-panel-hidden):has(.bank-tab-content)");
        appCss.Should().Contain(
            ".save-file-outer-tabs > .mud-tabs-panels > .mud-tab-panel:not(.mud-tab).mud-tab-panel-active:not(.mud-tab-panel-hidden):has(.pokedex-tab-content)");
    }
}
