using static PKHeX.Core.GameVersion;

namespace Pkmds.Rcl.Components;

/// <summary>
/// Utility class for generating display names for save files and game versions.
/// </summary>
public static class SaveFileNameDisplay
{
    /// <summary>
    /// Generates a formatted display string for the current save file.
    /// Includes trainer name, gender, TID, game version, and playtime.
    /// </summary>
    /// <param name="appState">The application state containing the save file.</param>
    /// <param name="appService">The application service for formatting IDs.</param>
    /// <param name="isPageTitle">Whether this is for a page title (includes app name prefix).</param>
    /// <returns>A formatted string representing the save file information.</returns>
    public static string SaveFileNameDisplayString(IAppState appState, IAppService appService, bool isPageTitle = false)
    {
        if (appState.SaveFile is not { } saveFile)
        {
            return Constants.AppTitle;
        }

        var sbTitle = new StringBuilder(isPageTitle
            ? Constants.AppShortTitle
            : string.Empty);
        if (isPageTitle)
        {
            sbTitle.Append(" - ");
        }

        // Trainer name
        sbTitle.Append($"{saveFile.OT} ");

        // Gender symbol (not available in Gen 1)
        if (saveFile.Context is not EntityContext.Gen1)
        {
            var genderDisplay = saveFile.Gender == (byte)Gender.Male
                ? Constants.MaleGenderUnicode
                : Constants.FemaleGenderUnicode;
            sbTitle.Append($"{genderDisplay} ");
        }

        // Trainer ID
        sbTitle.Append($"({saveFile.DisplayTID.ToString(appService.GetIdFormatString())}, ");

        // Game name
        sbTitle.Append($"{FriendlyGameName(saveFile.Version)}, ");

        // Playtime
        sbTitle.Append($"{saveFile.PlayTimeString})");

        return sbTitle.ToString();
    }

    /// <summary>
    /// Converts a GameVersion enum value to a user-friendly game name.
    /// </summary>
    /// <param name="gameVersion">The game version to convert.</param>
    /// <returns>A human-readable game name.</returns>
    public static string FriendlyGameName(GameVersion gameVersion) => gameVersion switch
    {
        Invalid => "Invalid",
        S => "Sapphire",
        R => "Ruby",
        E => "Emerald",
        FR => "FireRed",
        LG => "LeafGreen",
        CXD => "Colosseum / XD",
        D => "Diamond",
        P => "Pearl",
        Pt => "Platinum",
        HG => "HeartGold",
        SS => "SoulSilver",
        W => "White",
        B => "Black",
        W2 => "White 2",
        B2 => "Black 2",
        X => "X",
        Y => "Y",
        AS => "Alpha Sapphire",
        OR => "Omega Ruby",
        SN => "Sun",
        MN => "Moon",
        US => "Ultra Sun",
        UM => "Ultra Moon",
        GO => "GO",
        RD => "Red",
        GN => "Green",
        BU => "Blue",
        YW => "Yellow",
        GD => "Gold",
        SI => "Silver",
        C => "Crystal",
        GP => "Let's Go Pikachu",
        GE => "Let's Go Eevee",
        SW => "Sword",
        SH => "Shield",
        PLA => "Legends: Arceus",
        BD => "Brilliant Diamond",
        SP => "Shining Pearl",
        SL => "Scarlet",
        VL => "Violet",
        StadiumJ => "Stadium (J)",
        Stadium => "Stadium",
        Stadium2 => "Stadium 2",
        _ => gameVersion.ToString()
    };
}
