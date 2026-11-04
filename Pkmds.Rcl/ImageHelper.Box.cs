namespace Pkmds.Rcl;

/// <summary>
/// Partial class for box wallpaper sprite filename generation.
/// </summary>
public static partial class ImageHelper
{
    /// <summary>
    /// Gets the sprite filename for a box wallpaper based on wallpaper ID and game version.
    /// The wallpaper ID is 0-based (as stored in the save file); sprite files are 1-based.
    /// </summary>
    /// <param name="wallpaperId">The wallpaper ID (0-based, as stored in the save file).</param>
    /// <param name="gameVersion">The game version enum.</param>
    public static string GetBoxWallpaperSpriteFileName(int wallpaperId, GameVersion gameVersion)
    {
        // Emerald's Friends wallpaper is generated from save-specific pattern/palette data.
        // Pokemon Box wallpapers also have no bundled preview assets.
        if ((gameVersion == GameVersion.E && wallpaperId == 16) || gameVersion == GameVersion.RSBOX)
        {
            return string.Empty;
        }

        var abbreviation = GetGameVersionAbbreviation(gameVersion);
        if (string.IsNullOrEmpty(abbreviation))
        {
            return string.Empty;
        }

        // Some game folders only ship their unique wallpapers; base wallpapers
        // (shared with an earlier game in the same generation) live in a fallback folder.
        abbreviation = GetFolderWithFallback(abbreviation, wallpaperId);
        if (string.IsNullOrEmpty(abbreviation))
        {
            return string.Empty;
        }

        // SV wallpaper 20 (ID 19) ships as two game-specific variants:
        // Naranja Academy (Scarlet) and Uva Academy (Violet).
        if (abbreviation == "sv" && wallpaperId == 19)
        {
            var variant = gameVersion == GameVersion.SL
                ? "n"
                : "u";
            return $"{SpritesRoot}box/sv/box_wp20sv_{variant}.png";
        }

        // Sprite files use 1-based numbering: wallpaper ID 0 → box_wp01, etc.
        var fileIndex = wallpaperId + 1;
        return $"{SpritesRoot}box/{abbreviation}/box_wp{fileIndex:00}{abbreviation}.png";
    }

    /// <summary>
    /// Resolves the actual sprite folder for a given game abbreviation and wallpaper ID.
    /// Games that share base wallpapers with a predecessor only store their unique sprites
    /// in their own folder; earlier IDs fall back to the base game's folder.
    /// </summary>
    private static string GetFolderWithFallback(string abbreviation, int wallpaperId) =>
        abbreviation switch
        {
            // FRLG: IDs 0–11 share RS sprites; IDs 12–15 are unique to FRLG.
            "frlg" when wallpaperId < 12 => "rs",
            // Platinum: IDs 0–15 share DP sprites; IDs 16–23 are unique to Pt.
            "pt" when wallpaperId < 16 => "dp",
            // HGSS: IDs 0–15 share DP sprites; IDs 16–23 are unique to HGSS.
            "hgss" when wallpaperId < 16 => "dp",
            // B2W2: IDs 0–15 share BW sprites; IDs 16–23 are unique to B2W2.
            "b2w2" when wallpaperId < 16 => "bw",
            // ORAS: IDs 0–15 share XY sprites; IDs 16–23 are unique to ORAS.
            "ao" when wallpaperId < 16 => "xy",
            _ => abbreviation
        };

    /// <summary>
    /// Converts a GameVersion enum to its box wallpaper folder abbreviation.
    /// Returns an empty string for games with no wallpaper sprites (Gen 7: SM/USUM, Legends: Arceus/Z-A).
    /// </summary>
    private static string GetGameVersionAbbreviation(GameVersion version) => version switch
    {
        // Gen 2: Gold, Silver, Crystal
        GameVersion.GD or GameVersion.SI => "gs",
        GameVersion.C => "c",
        // Gen 3: Ruby, Sapphire, Emerald
        GameVersion.R or GameVersion.S => "rs",
        GameVersion.E => "e",
        // Gen 3: FireRed, LeafGreen
        GameVersion.FR or GameVersion.LG => "frlg",
        // Gen 4: Diamond, Pearl, Platinum
        GameVersion.D or GameVersion.P => "dp",
        GameVersion.Pt => "pt",
        // Gen 4: HeartGold, SoulSilver
        GameVersion.HG or GameVersion.SS => "hgss",
        // Gen 5: Black, White
        GameVersion.B or GameVersion.W => "bw",
        // Gen 5: Black 2, White 2
        GameVersion.B2 or GameVersion.W2 => "b2w2",
        // Gen 6: X, Y
        GameVersion.X or GameVersion.Y => "xy",
        // Gen 6: Omega Ruby, Alpha Sapphire
        GameVersion.OR or GameVersion.AS => "ao",
        // Gen 7: Sun/Moon, Ultra Sun/Ultra Moon — no wallpaper sprites available
        GameVersion.SN or GameVersion.MN => string.Empty,
        GameVersion.US or GameVersion.UM => string.Empty,
        // Legends: Arceus, Legends: Z-A — supports IBoxDetailWallpaper but no sprite assets exist
        GameVersion.PLA or GameVersion.ZA => string.Empty,
        // Gen 8: Sword, Shield
        GameVersion.SW or GameVersion.SH => "swsh",
        // Gen 8: Brilliant Diamond, Shining Pearl
        GameVersion.BD or GameVersion.SP => "bdsp",
        // Gen 9: Scarlet, Violet
        GameVersion.SL or GameVersion.VL => "sv",
        // Fallback — try to use the version string; will 404 if no folder exists
        _ => version.ToString().ToLowerInvariant()
    };
}
