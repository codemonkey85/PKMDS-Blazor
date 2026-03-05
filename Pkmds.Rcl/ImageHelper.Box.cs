namespace Pkmds.Rcl;

/// <summary>
/// Partial class for box wallpaper sprite filename generation.
/// </summary>
public static partial class ImageHelper
{
    /// <summary>
    /// Gets the sprite filename for a box wallpaper based on wallpaper ID and game version.
    /// The wallpaper ID is typically retrieved from the save file's box data.
    /// </summary>
    /// <param name="wallpaperId">The wallpaper ID (typically 0-23 or similar depending on game).</param>
    /// <param name="gameVersion">The game version enum.</param>
    public static string GetBoxWallpaperSpriteFileName(int wallpaperId, GameVersion gameVersion)
    {
        var abbreviation = GetGameVersionAbbreviation(gameVersion);
        return GetBoxWallpaperSpriteFileName(wallpaperId, abbreviation);
    }

    /// <summary>
    /// Gets the sprite filename for a box wallpaper based on wallpaper ID and game version abbreviation.
    /// The wallpaper ID is typically retrieved from the save file's box data.
    /// </summary>
    /// <param name="wallpaperId">The wallpaper ID (typically 0-23 or similar depending on game).</param>
    /// <param name="gameAbbreviation">
    /// The game abbreviation (e.g., "ao" for Omega Ruby/Alpha Sapphire, "swsh" for
    /// Sword/Shield, etc.).
    /// </param>
    private static string GetBoxWallpaperSpriteFileName(int wallpaperId, string? gameAbbreviation)
    {
        if (string.IsNullOrEmpty(gameAbbreviation))
        {
            return string.Empty;
        }

        var gameCode = gameAbbreviation.ToLowerInvariant();
        return $"{SpritesRoot}box/{gameCode}/box_wp{wallpaperId:00}{gameCode}.png";
    }

    /// <summary>
    /// Converts a GameVersion enum to its box wallpaper folder abbreviation.
    /// </summary>
    private static string GetGameVersionAbbreviation(GameVersion version) => version switch
    {
        // Gen 2: Gold, Silver
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
        // Gen 7: Sun, Moon
        GameVersion.SN or GameVersion.MN => "ao",
        // Gen 7: Ultra Sun, Ultra Moon
        GameVersion.US or GameVersion.UM => "ao",
        // Gen 8: Sword, Shield
        GameVersion.SW or GameVersion.SH => "swsh",
        // Gen 8: Brilliant Diamond, Shining Pearl
        GameVersion.BD or GameVersion.SP => "bdsp",
        // Gen 9: Scarlet, Violet
        GameVersion.SL or GameVersion.VL => "sv",
        // Fallback - try to use the version string
        _ => version.ToString().ToLowerInvariant()
    };
}
