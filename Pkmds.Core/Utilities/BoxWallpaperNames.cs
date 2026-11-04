using static PKHeX.Core.LanguageID;
using static Pkmds.Core.Utilities.BoxWallpaperNames.WallpaperName;

namespace Pkmds.Core.Utilities;

/// <summary>Localized wallpaper labels indexed by the wallpaper IDs stored in each save format.</summary>
public static class BoxWallpaperNames
{
    /// <summary>Gets the available labels without changing any saved wallpaper IDs.</summary>
    public static string[] GetNames(SaveFile save)
    {
        if (save is not IBoxDetailWallpaper)
        {
            return [];
        }

        var names = GameInfo.Strings.wallpapernames;
        var result = new List<string>();

        void AddNames(ReadOnlySpan<WallpaperName> indexes, string suffix = "")
        {
            foreach (var index in indexes)
            {
                result.Add(names[(int)index] + suffix);
            }
        }

        switch (save.Generation)
        {
            case 3:
                result.AddRange(names.AsSpan(0, 12));
                AddNames(save switch
                {
                    SAV3RS => [PolkaDot, PokemonCenter, Machine3, Plain],
                    SAV3E => [PolkaDot, PokemonCenter, Machine3, Simple, Friends],
                    SAV3FRLG => [Stars, PokemonCenter, Tiles, Simple],
                    SAV3RSBox => [PolkaDot, PokemonCenter, Machine3, Plain, Flower, Tiles, Carpet, Ruin, MyWallpaper],
                    _ => []
                });
                break;
            case 4 or 5:
                result.AddRange(names.AsSpan(0, 16));
                AddNames(save switch
                {
                    SAV4DP => [Space, Backyard, Nostalgic, Torchic, Trio, Pikapika, Legend, TeamGalactic],
                    SAV4Pt => [Distortion, Contest, Nostalgic, Croagunk, Trio, Pikapika, Legend, TeamGalactic],
                    SAV4HGSS => [Heart, Soul, BigBrother, Pokeathlon, Trio, SpikyPika, KimonoGirl, Revival],
                    SAV5BW => [Reshiram, Zekrom, Monochrome, TeamPlasma, Munna, Zoroark, Subway, Musical],
                    SAV5B2W2 => [Monochrome, TeamPlasma, Movie, PWT, Kyurem1, Kyurem2, Reshiram, Zekrom],
                    _ => []
                });
                break;
            case 6:
                result.AddRange(names.AsSpan(0, 24));
                break;
            case 7:
                result.AddRange(names.AsSpan(0, 16));
                break;
            case 8 when save is SAV8BS:
                result.AddRange(names.AsSpan(0, 16));
                AddNames([Space, Backyard, Nostalgic, Torchic, Trio, Pikapika, Legend, TeamGalactic]);
                AddNames([Distortion, Contest, Nostalgic, Croagunk, Trio, Pikapika, Legend, TeamGalactic], GetPlatinumSuffix());
                break;
            case 8:
                AddPlaceholders(19);
                break;
            case 9:
                AddPlaceholders(20);
                break;
        }

        return [.. result];

        void AddPlaceholders(int count)
        {
            var prefix = GetWallpaperPrefix();
            for (var i = 1; i <= count; i++)
            {
                result.Add($"{prefix}{i}");
            }
        }
    }

    private static string GetPlatinumSuffix() => GameInfo.Strings.Language switch
    {
        Japanese => "Ｐｔ",
        German => " (Platin)",
        French => " (Platine)",
        Italian or Spanish or SpanishL => " (Platino)",
        Korean => " Pt",
        ChineseS or ChineseT => "Ｐｔ",
        _ => " (Platinum)"
    };

    private static string GetWallpaperPrefix() => GameInfo.Strings.Language switch
    {
        Japanese => "かべがみ",
        German => "Hintergrund ",
        French => "Thème ",
        Italian => "Sfondo ",
        Spanish or SpanishL => "Fondo ",
        Korean => "벽지",
        ChineseS => "壁纸",
        ChineseT => "壁紙",
        _ => "Wallpaper "
    };

    // Resource indexes, NOT saved wallpaper IDs. Keep aligned with PKHeX 26.10.18's
    // WinForms SAV_BoxLayout.LoadWallpapers / WallpaperName and text_wallpaper resources.
    internal enum WallpaperName : byte
    {
        Forest, City, Desert, Savanna,
        Crag, Volcano, Snow, Cave,
        Beach, Seafloor, River, Sky,
        PokemonCenter, Machine, Checks, Simple,
        Special1, Special2, Special3, Special4,
        Special5, Special6, Special7, Special8,
        PolkaDot, Machine3, Plain, Friends,
        Stars, Tiles, Flower, Carpet, Ruin, MyWallpaper,
        Space, Backyard, Nostalgic, Torchic,
        Trio, Pikapika, Legend, TeamGalactic,
        Distortion, Contest, Croagunk,
        Heart, Soul, BigBrother, Pokeathlon,
        SpikyPika, KimonoGirl, Revival,
        Reshiram, Zekrom, Monochrome, TeamPlasma,
        Munna, Zoroark, Subway, Musical,
        Movie, PWT, Kyurem1, Kyurem2
    }
}
