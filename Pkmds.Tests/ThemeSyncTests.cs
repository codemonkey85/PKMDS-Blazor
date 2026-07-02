using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using MudBlazor.Utilities;
using Pkmds.Rcl.Theming;

namespace Pkmds.Tests;

/// <summary>
/// Guards the colors that AppTheme shares with static host assets which cannot reference C#
/// (index.html, manifest.webmanifest, the pre-boot splash styles in app.css, and
/// BrowserNotSupported.html). AppTheme.Colors is the single source of truth; when a palette
/// color changes there, these tests fail until the static files are updated to match.
/// </summary>
public class ThemeSyncTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static readonly HashSet<string> AllThemeColors = typeof(AppTheme.Colors)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral)
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Pkmds.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Could not locate the repo root (no Pkmds.slnx found above the test bin directory).");
    }

    private static string ReadRepoFile(params string[] pathSegments) =>
        File.ReadAllText(Path.Combine([RepoRoot, .. pathSegments]));

    [Fact]
    public void IndexHtml_ThemeColorMeta_IsSingleAndMatchesAppTheme()
    {
        var html = ReadRepoFile("Pkmds.Web", "wwwroot", "index.html");

        var metas = Regex.Matches(html, """<meta[^>]*name="theme-color"[^>]*>""");

        // A single mode-independent tag is only valid because AppbarBackground is identical
        // in both palettes — see AppbarBackground_IsIdenticalInBothPalettes below.
        metas.Should().ContainSingle();

        var content = Regex.Match(metas[0].Value, """content="([^"]+)""").Groups[1].Value;
        content.Should().BeEquivalentTo(AppTheme.PwaThemeColor);
    }

    [Fact]
    public void IndexHtml_BrandAccents_MatchLightPrimary()
    {
        var html = ReadRepoFile("Pkmds.Web", "wwwroot", "index.html");

        var maskIcon = Regex.Match(html, """<link[^>]*rel="mask-icon"[^>]*color="([^"]+)""");
        maskIcon.Success.Should().BeTrue("index.html should declare a mask-icon color");
        maskIcon.Groups[1].Value.Should().BeEquivalentTo(AppTheme.Colors.Blue800);

        var tileColor = Regex.Match(html, """<meta[^>]*name="msapplication-TileColor"[^>]*content="([^"]+)""");
        tileColor.Success.Should().BeTrue("index.html should declare msapplication-TileColor");
        tileColor.Groups[1].Value.Should().BeEquivalentTo(AppTheme.Colors.Blue800);
    }

    [Fact]
    public void Manifest_ThemeAndBackgroundColors_MatchAppTheme()
    {
        var manifest = ReadRepoFile("Pkmds.Web", "wwwroot", "manifest.webmanifest");

        using var json = JsonDocument.Parse(manifest);
        json.RootElement.GetProperty("theme_color").GetString()
            .Should().BeEquivalentTo(AppTheme.PwaThemeColor);
        json.RootElement.GetProperty("background_color").GetString()
            .Should().BeEquivalentTo(AppTheme.PwaBackgroundColor);
    }

    [Fact]
    public void AppbarBackground_IsIdenticalInBothPalettes_AndMatchesPwaThemeColor()
    {
        // The static theme-color meta tag and manifest theme_color carry one value for both
        // light and dark. If the appbar colors ever diverge, this test fails as a signal that
        // the theme-color plumbing (index.html, manifest, setAppTheme interop) must become
        // mode-aware at the same time.
        var expected = new MudColor(AppTheme.PwaThemeColor);
        AppTheme.Default.PaletteLight.AppbarBackground.Should().Be(expected);
        AppTheme.Default.PaletteDark.AppbarBackground.Should().Be(expected);
    }

    [Fact]
    public void SplashCss_UsesOnlyAppThemeColors()
    {
        var css = ReadRepoFile("Pkmds.Rcl", "wwwroot", "css", "app.css");

        var begin = css.IndexOf("/* begin pkmds-splash", StringComparison.Ordinal);
        var end = css.IndexOf("/* end pkmds-splash", StringComparison.Ordinal);
        begin.Should().BeGreaterThan(-1, "app.css should contain the 'begin pkmds-splash' marker comment");
        end.Should().BeGreaterThan(begin, "app.css should contain the 'end pkmds-splash' marker comment after the begin marker");

        var splashSection = css[begin..end];
        var hexColors = Regex.Matches(splashSection, "#[0-9A-Fa-f]{6}(?![0-9A-Fa-f])")
            .Select(m => m.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        hexColors.Should().NotBeEmpty()
            .And.AllSatisfy(hex => AllThemeColors.Should().Contain(hex,
                $"splash color {hex} in app.css should be one of the AppTheme.Colors constants"));
    }

    [Fact]
    public void BrowserNotSupportedPage_LinkColors_MatchPalettePrimaries()
    {
        // The page has its own standalone grays (it renders without any app CSS), but its
        // link colors mirror the palette primaries and should follow them if they change.
        var html = ReadRepoFile("Pkmds.Web", "wwwroot", "BrowserNotSupported.html");

        html.Should().ContainEquivalentOf(AppTheme.Colors.Blue800,
            "BrowserNotSupported.html light link color should match the light palette Primary");
        html.Should().ContainEquivalentOf(AppTheme.Colors.Blue400,
            "BrowserNotSupported.html dark link color should match the dark palette Primary");
    }
}
