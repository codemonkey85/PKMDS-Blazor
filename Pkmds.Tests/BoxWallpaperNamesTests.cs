using Bunit;
using MudBlazor;
using MudBlazor.Extensions;
using Pkmds.Rcl.Components.Dialogs;

namespace Pkmds.Tests;

public sealed class BoxWallpaperNamesTests : IDisposable
{
    private readonly GameStrings originalStrings = GameInfo.Strings;

    public BoxWallpaperNamesTests() => GameInfo.Strings = GameInfo.GetStrings("en");

    public void Dispose() => GameInfo.Strings = originalStrings;

    [Theory]
    [InlineData(GameVersion.R, 12, "Polka-Dot|Pokémon Center|Machine|Plain")]
    [InlineData(GameVersion.E, 12, "Polka-Dot|Pokémon Center|Machine|Simple|Friends")]
    [InlineData(GameVersion.FR, 12, "Stars|Pokémon Center|Tiles|Simple")]
    [InlineData(GameVersion.RSBOX, 12, "Polka-Dot|Pokémon Center|Machine|Plain|Flower|Tiles|Carpet|Ruin|My Wallpaper")]
    [InlineData(GameVersion.D, 16, "Space|Backyard|Nostalgic|Torchic|Trio|Pikapika|Legend|Team Galactic")]
    [InlineData(GameVersion.Pt, 16, "Distortion|Contest|Nostalgic|Croagunk|Trio|Pikapika|Legend|Team Galactic")]
    [InlineData(GameVersion.HG, 16, "Heart|Soul|Big Brother|Pokéathlon|Trio|Spiky Pika|Kimono Girl|Revival")]
    [InlineData(GameVersion.B, 16, "Reshiram|Zekrom|Monochrome|Team Plasma|Munna|Zoroark|Subway|Musical")]
    [InlineData(GameVersion.B2, 16, "Monochrome|Team Plasma|Movie|PWT|Kyurem 1|Kyurem 2|Reshiram|Zekrom")]
    [InlineData(GameVersion.BD, 16, "Space|Backyard|Nostalgic|Torchic|Trio|Pikapika|Legend|Team Galactic|Distortion (Platinum)|Contest (Platinum)|Nostalgic (Platinum)|Croagunk (Platinum)|Trio (Platinum)|Pikapika (Platinum)|Legend (Platinum)|Team Galactic (Platinum)")]
    public void GetNames_MapsGameSpecificIds(GameVersion version, int commonCount, string expectedSpecialNames)
    {
        var save = version == GameVersion.RSBOX
            ? new SAV3RSBox()
            : BlankSaveFile.Get(version, "TEST", LanguageID.English);

        var names = BoxWallpaperNames.GetNames(save);

        names.Take(commonCount).Should().Equal(GameInfo.Strings.wallpapernames.Take(commonCount));
        names.Skip(commonCount).Should().Equal(expectedSpecialNames.Split('|'));
    }

    [Theory]
    [InlineData(GameVersion.X, 24)]
    [InlineData(GameVersion.OR, 24)]
    [InlineData(GameVersion.SN, 16)]
    [InlineData(GameVersion.US, 16)]
    [InlineData(GameVersion.SW, 19)]
    [InlineData(GameVersion.SL, 20)]
    public void GetNames_PreservesOtherGenerationBoundaries(GameVersion version, int count)
    {
        var names = BoxWallpaperNames.GetNames(BlankSaveFile.Get(version, "TEST", LanguageID.English));

        names.Should().HaveCount(count);
        names[^1].Should().Be(version switch
        {
            GameVersion.SW => "Wallpaper 19",
            GameVersion.SL => "Wallpaper 20",
            _ => GameInfo.Strings.wallpapernames[count - 1]
        });
    }

    [Fact]
    public void GetNames_WithoutWallpaperSupport_ReturnsNoOptions() =>
        BoxWallpaperNames.GetNames(new SAV1()).Should().BeEmpty();

    [Fact]
    public void GetNames_LocalizesPlatinumSuffixAndPlaceholders()
    {
        GameInfo.Strings = GameInfo.GetStrings("de");

        BoxWallpaperNames.GetNames(new SAV8BS())[24].Should().Be(
            GameInfo.Strings.wallpapernames[42] + " (Platin)");
        BoxWallpaperNames.GetNames(new SAV8SWSH())[^1].Should().Be("Hintergrund 19");
    }

    [Theory]
    [InlineData(GameVersion.E, 16)]
    [InlineData(GameVersion.RSBOX, 20)]
    public void CustomWallpapers_WithoutBundledAssets_HaveNoPreview(GameVersion version, int id) =>
        ImageHelper.GetBoxWallpaperSpriteFileName(id, version).Should().BeEmpty();

    [Theory]
    [InlineData(GameVersion.FR, 11, "rs/box_wp12rs.png")]
    [InlineData(GameVersion.FR, 12, "frlg/box_wp13frlg.png")]
    [InlineData(GameVersion.E, 15, "e/box_wp16e.png")]
    [InlineData(GameVersion.Pt, 15, "dp/box_wp16dp.png")]
    [InlineData(GameVersion.Pt, 16, "pt/box_wp17pt.png")]
    [InlineData(GameVersion.HG, 23, "hgss/box_wp24hgss.png")]
    [InlineData(GameVersion.B2, 15, "bw/box_wp16bw.png")]
    [InlineData(GameVersion.B2, 16, "b2w2/box_wp17b2w2.png")]
    [InlineData(GameVersion.BD, 23, "bdsp/box_wp24bdsp.png")]
    [InlineData(GameVersion.BD, 24, "bdsp/box_wp25bdsp.png")]
    [InlineData(GameVersion.BD, 31, "bdsp/box_wp32bdsp.png")]
    public void Preview_UsesSavedIdAndExistingAsset(GameVersion version, int id, string expectedPath)
    {
        ImageHelper.GetBoxWallpaperSpriteFileName(id, version).Should().Be(
            "_content/Pkmds.Rcl/sprites/box/" + expectedPath);
        RepoFileTestHelper.ReadAllBytes(["Pkmds.Rcl", "wwwroot", "sprites", "box", .. expectedPath.Split('/')])
            .Should().NotBeEmpty();
    }

    [Fact]
    public async Task Dialog_PreservesExistingIdsAndWritesSelectedIdWithMatchingPreview()
    {
        var save = new SAV8BS();
        save.SetBoxWallpaper(0, 24);
        save.SetBoxWallpaper(1, 31);
        var state = new TestAppState { SaveFile = save };
        var refresh = new TestRefreshService();
        var appService = new AppService(state, refresh, new LegalizationService(state));
        await using var ctx = BunitTestHelpers.CreateBunitContext(state, refresh, appService);
        var popovers = ctx.Render<MudPopoverProvider>();
        var provider = ctx.Render<MudDialogProvider>();
        var dialogs = ctx.Services.GetService(typeof(IDialogService)) as IDialogService;
        await provider.InvokeAsync(() => dialogs!.ShowAsync<BoxLayoutDialog>("Box Layout"));
        provider.WaitForAssertion(() => provider.FindComponent<BoxLayoutDialog>().Should().NotBeNull());
        var cut = provider.FindComponent<BoxLayoutDialog>();
        var select = cut.FindComponents<MudSelect<int>>().Single(c => c.Instance.Label == "Wallpaper");

        select.Instance.GetState(x => x.Value).Should().Be(24);
        select.Instance.GetState(x => x.Text).Should().Be("Distortion (Platinum)");
        cut.Find("img[alt='Wallpaper preview']").GetAttribute("src").Should().EndWith("box_wp25bdsp.png");
        save.GetBoxWallpaper(0).Should().Be(24);
        save.GetBoxWallpaper(1).Should().Be(31);

        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(31));

        save.GetBoxWallpaper(0).Should().Be(31);
        save.GetBoxWallpaper(1).Should().Be(31);
        cut.Find("img[alt='Wallpaper preview']").GetAttribute("src").Should().EndWith("box_wp32bdsp.png");

        await select.InvokeAsync(() => select.Instance.OpenMenu());

        popovers.WaitForAssertion(() =>
        {
            var thumbnails = popovers.FindAll("img[alt='']");
            thumbnails.Should().HaveCount(32);
            thumbnails[24].GetAttribute("src").Should().EndWith("box_wp25bdsp.png");
            thumbnails[31].GetAttribute("src").Should().EndWith("box_wp32bdsp.png");
        });
    }
}
