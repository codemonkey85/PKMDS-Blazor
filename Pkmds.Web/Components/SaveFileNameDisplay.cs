using static PKHeX.Core.GameVersion;

namespace Pkmds.Web.Components;

public static class SaveFileNameDisplay
{
    public static string SaveFileNameDisplayString(IAppState appState, IAppService appService, bool isPageTitle = false)
    {
        const string baseTitle = "PKMDS Save Editor";

        if (appState.SaveFile is not { } saveFile)
        {
            return baseTitle;
        }

        var sbTitle = new StringBuilder(isPageTitle ? baseTitle : string.Empty);
        if (isPageTitle)
        {
            sbTitle.Append(" - ");
        }

        sbTitle.Append($"{saveFile.OT} ");

        if (saveFile.Context is not EntityContext.Gen1)
        {
            var genderDisplay = saveFile.Gender == (byte)Gender.Male ? Constants.MaleGenderUnicode : Constants.FemaleGenderUnicode;
            sbTitle.Append($"{genderDisplay} ");
        }

        sbTitle.Append($"({saveFile.DisplayTID.ToString(appService.GetIdFormatString())}, ");

        sbTitle.Append($"{FriendlyGameName(saveFile.Version)}, ");

        sbTitle.Append($"{saveFile.PlayTimeString})");

        return sbTitle.ToString();
    }

    private static string FriendlyGameName(GameVersion gameVersion) => gameVersion switch
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
        _ => gameVersion.ToString(),
    };
}
