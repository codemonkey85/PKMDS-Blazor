namespace Pkmds.Rcl.Components.MainTabPages.Trainer;

public partial class TrainerInfoSav9SvSection
{
    [Parameter]
    [EditorRequired]
    public SAV9SV SaveFile { get; set; } = null!;

    private static double GetSvRotation(SAV9SV sv) =>
        Math.Atan2(sv.RZ, sv.RW) * 360.0 / Math.PI;

    private static void SetSvRotation(SAV9SV sv, double angle)
    {
        var rad = angle * Math.PI / 360.0;
        sv.SetPlayerRotation(0, (float)Math.Sin(rad), 0, (float)Math.Cos(rad));
    }

    private static string GetThrowStyleName(ThrowStyle9 style) => style switch
    {
        ThrowStyle9.OriginalStyle => "Original style",
        ThrowStyle9.LeftHandedStyle => "Left-handed style",
        ThrowStyle9.ElegantStyle => "Elegant style",
        ThrowStyle9.ReverentStyle => "Reverent style",
        ThrowStyle9.NinjaStyle => "Ninja style",
        ThrowStyle9.DaintyStyle => "Dainty style",
        ThrowStyle9.TwirlingStyle => "Twirling style",
        ThrowStyle9.SmugStyle => "Smug style",
        ThrowStyle9.GalarianStarStyle => "Galarian Star style",
        _ => style.ToString(),
    };

    // Block names for the SV "Unlock All Fly Locations" action — covers base game,
    // Kitakami (SU1), and Blueberry Academy (SU2) fly points + their Pokémon Centers.
    private static readonly string[] SvFlyLocationBlockNames =
    [
        "FSYS_YMAP_FLY_01", "FSYS_YMAP_FLY_02", "FSYS_YMAP_FLY_03", "FSYS_YMAP_FLY_04",
        "FSYS_YMAP_FLY_05", "FSYS_YMAP_FLY_06", "FSYS_YMAP_FLY_07", "FSYS_YMAP_FLY_08",
        "FSYS_YMAP_FLY_09", "FSYS_YMAP_FLY_10", "FSYS_YMAP_FLY_11", "FSYS_YMAP_FLY_12",
        "FSYS_YMAP_FLY_13", "FSYS_YMAP_FLY_14", "FSYS_YMAP_FLY_15", "FSYS_YMAP_FLY_16",
        "FSYS_YMAP_FLY_17", "FSYS_YMAP_FLY_18", "FSYS_YMAP_FLY_19", "FSYS_YMAP_FLY_20",
        "FSYS_YMAP_FLY_21", "FSYS_YMAP_FLY_22", "FSYS_YMAP_FLY_23", "FSYS_YMAP_FLY_24",
        "FSYS_YMAP_FLY_25", "FSYS_YMAP_FLY_26", "FSYS_YMAP_FLY_27", "FSYS_YMAP_FLY_28",
        "FSYS_YMAP_FLY_29", "FSYS_YMAP_FLY_30", "FSYS_YMAP_FLY_31", "FSYS_YMAP_FLY_32",
        "FSYS_YMAP_FLY_33", "FSYS_YMAP_FLY_34", "FSYS_YMAP_FLY_35",
        "FSYS_YMAP_FLY_MAGATAMA", "FSYS_YMAP_FLY_MOKKAN", "FSYS_YMAP_FLY_TSURUGI", "FSYS_YMAP_FLY_UTSUWA",
        "FSYS_YMAP_POKECEN_02", "FSYS_YMAP_POKECEN_03", "FSYS_YMAP_POKECEN_04", "FSYS_YMAP_POKECEN_05",
        "FSYS_YMAP_POKECEN_06", "FSYS_YMAP_POKECEN_07", "FSYS_YMAP_POKECEN_08", "FSYS_YMAP_POKECEN_09",
        "FSYS_YMAP_POKECEN_10", "FSYS_YMAP_POKECEN_11", "FSYS_YMAP_POKECEN_12", "FSYS_YMAP_POKECEN_13",
        "FSYS_YMAP_POKECEN_14", "FSYS_YMAP_POKECEN_15", "FSYS_YMAP_POKECEN_16", "FSYS_YMAP_POKECEN_17",
        "FSYS_YMAP_POKECEN_18", "FSYS_YMAP_POKECEN_19", "FSYS_YMAP_POKECEN_20", "FSYS_YMAP_POKECEN_21",
        "FSYS_YMAP_POKECEN_22", "FSYS_YMAP_POKECEN_23", "FSYS_YMAP_POKECEN_24", "FSYS_YMAP_POKECEN_25",
        "FSYS_YMAP_POKECEN_26", "FSYS_YMAP_POKECEN_27", "FSYS_YMAP_POKECEN_28", "FSYS_YMAP_POKECEN_29",
        "FSYS_YMAP_POKECEN_30", "FSYS_YMAP_POKECEN_31", "FSYS_YMAP_POKECEN_32", "FSYS_YMAP_POKECEN_33",
        "FSYS_YMAP_POKECEN_34", "FSYS_YMAP_POKECEN_35",
        "FSYS_YMAP_MAGATAMA", "FSYS_YMAP_MOKKAN", "FSYS_YMAP_TSURUGI", "FSYS_YMAP_UTSUWA",
        "FSYS_YMAP_SU1MAP_CHANGE",
        "FSYS_YMAP_FLY_SU1_AREA10", "FSYS_YMAP_FLY_SU1_BUSSTOP", "FSYS_YMAP_FLY_SU1_CENTER01",
        "FSYS_YMAP_FLY_SU1_PLAZA",
        "FSYS_YMAP_FLY_SU1_SPOT01", "FSYS_YMAP_FLY_SU1_SPOT02", "FSYS_YMAP_FLY_SU1_SPOT03",
        "FSYS_YMAP_FLY_SU1_SPOT04", "FSYS_YMAP_FLY_SU1_SPOT05", "FSYS_YMAP_FLY_SU1_SPOT06",
        "FSYS_YMAP_S2_MAPCHANGE_ENABLE",
        "FSYS_YMAP_FLY_SU2_DRAGON", "FSYS_YMAP_FLY_SU2_ENTRANCE", "FSYS_YMAP_FLY_SU2_FAIRY",
        "FSYS_YMAP_FLY_SU2_HAGANE", "FSYS_YMAP_FLY_SU2_HONOO",
        "FSYS_YMAP_FLY_SU2_SPOT01", "FSYS_YMAP_FLY_SU2_SPOT02", "FSYS_YMAP_FLY_SU2_SPOT03",
        "FSYS_YMAP_FLY_SU2_SPOT04", "FSYS_YMAP_FLY_SU2_SPOT05", "FSYS_YMAP_FLY_SU2_SPOT06",
        "FSYS_YMAP_FLY_SU2_SPOT07", "FSYS_YMAP_FLY_SU2_SPOT08", "FSYS_YMAP_FLY_SU2_SPOT09",
        "FSYS_YMAP_FLY_SU2_SPOT10", "FSYS_YMAP_FLY_SU2_SPOT11",
        "FSYS_YMAP_POKECEN_SU02",
    ];

    private void UnlockSvFlyLocations()
    {
        var accessor = SaveFile.Blocks;
        foreach (var name in SvFlyLocationBlockNames)
        {
            if (accessor.TryGetBlock(name, out var block))
                block.ChangeBooleanType(SCTypeCode.Bool2);
        }
        Snackbar.Add("Unlocked all fly locations.", Severity.Success);
    }

    private void UnlockSvBikeUpgrades()
    {
        var accessor = SaveFile.Blocks;
        accessor.GetBlock("FSYS_RIDE_DASH_ENABLE").ChangeBooleanType(SCTypeCode.Bool2);
        accessor.GetBlock("FSYS_RIDE_SWIM_ENABLE").ChangeBooleanType(SCTypeCode.Bool2);
        accessor.GetBlock("FSYS_RIDE_HIJUMP_ENABLE").ChangeBooleanType(SCTypeCode.Bool2);
        accessor.GetBlock("FSYS_RIDE_GLIDE_ENABLE").ChangeBooleanType(SCTypeCode.Bool2);
        accessor.GetBlock("FSYS_RIDE_CLIMB_ENABLE").ChangeBooleanType(SCTypeCode.Bool2);
        if (accessor.TryGetBlock("FSYS_RIDE_FLIGHT_ENABLE", out var fly))
            fly.ChangeBooleanType(SCTypeCode.Bool2);
        Snackbar.Add("Unlocked all ride upgrades.", Severity.Success);
    }

    private async Task OpenFashionDialogAsync()
    {
        var parameters = new DialogParameters<Fashion9Dialog>
        {
            { x => x.SaveFile, SaveFile },
        };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Large);
        await DialogService.ShowAsync<Fashion9Dialog>("Fashion Editor", parameters, options);
    }

    private async Task OpenRaidDialogAsync()
    {
        var parameters = new DialogParameters<Raid9Dialog>
        {
            { x => x.SaveFile, SaveFile },
        };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Large);
        await DialogService.ShowAsync<Raid9Dialog>("Tera Raid Editor", parameters, options);
    }

    // SV-specific currencies — kept in this section since they only apply to SAV9SV.
    // Blueberry Points are post-DLC 2 only.
    private uint LeaguePoints
    {
        get => SaveFile.LeaguePoints;
        set => SaveFile.LeaguePoints = value;
    }

    private uint BlueberryPoints
    {
        get => SaveFile.BlueberryPoints;
        set => SaveFile.BlueberryPoints = value;
    }
}
