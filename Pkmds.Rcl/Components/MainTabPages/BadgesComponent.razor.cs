namespace Pkmds.Rcl.Components.MainTabPages;

public partial class BadgesComponent : IDisposable
{
    private const int BadgesFlagStart = 124;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    private IReadOnlyList<BadgeInfo> GetBadgeInfos() => AppState.SaveFile switch
    {
        SAV1 or SAV3FRLG => BadgeData.KantoBadges,
        SAV2 or SAV4HGSS => BadgeData.JohtoKantoBadges,
        SAV3RS or SAV3E or SAV6AO => BadgeData.HoennBadges,
        SAV4DP or SAV4Pt or SAV8BS => BadgeData.SinnohBadges,
        SAV5BW => BadgeData.UnivoaBwBadges,
        SAV5B2W2 => BadgeData.UnovaB2W2Badges,
        SAV6XY => BadgeData.KalosBadges,
        SAV8SWSH { Version: GameVersion.SW } => BadgeData.GalarSwordBadges,
        SAV8SWSH => BadgeData.GalarShieldBadges,
        _ => []
    };

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private List<bool> GetSaveFileBadgesValue()
    {
        List<bool> badgeFlags = [];

        if (AppState.SaveFile is not { } saveFile)
        {
            return badgeFlags;
        }

        if (saveFile.Context is EntityContext.Gen8b && saveFile is SAV8BS sav8bs)
        {
            for (var i = 0; i < 8; i++)
            {
                badgeFlags.Add(sav8bs.FlagWork.GetSystemFlag(BadgesFlagStart + i));
            }

            return badgeFlags;
        }

        var badgeFlagInt = 0;
        var badgeTotal = 8;
        switch (saveFile.Context)
        {
            case EntityContext.Gen1 when saveFile is SAV1 sav1:
                badgeFlagInt = sav1.Badges;
                break;

            case EntityContext.Gen2 when saveFile is SAV2 sav2:
                badgeFlagInt = sav2.Badges;
                badgeTotal = 16;
                break;

            case EntityContext.Gen3 when saveFile is SAV3 sav3:
                badgeFlagInt = sav3.Badges;
                break;

            case EntityContext.Gen4 when saveFile is SAV4DP sav4dp:
                badgeFlagInt = sav4dp.Badges;
                break;

            case EntityContext.Gen4 when saveFile is SAV4Pt sav4pt:
                badgeFlagInt = sav4pt.Badges;
                break;

            case EntityContext.Gen4 when saveFile is SAV4HGSS sav4hgss:
                // HGSS stores 16 badges across two bytes: Johto in Badges (bits 0-7),
                // Kanto in Badges16 (read into bits 8-15). Mirrors PKHeX SAV_SimpleTrainer.
                badgeFlagInt = sav4hgss.Badges | (sav4hgss.Badges16 << 8);
                badgeTotal = 16;
                break;

            case EntityContext.Gen5 when saveFile is SAV5BW sav5bw:
                badgeFlagInt = sav5bw.Misc.Badges;
                break;

            case EntityContext.Gen5 when saveFile is SAV5B2W2 sav5b2w2:
                badgeFlagInt = sav5b2w2.Misc.Badges;
                break;

            case EntityContext.Gen6 when saveFile is SAV6XY sav6xy:
                badgeFlagInt = sav6xy.Badges;
                break;

            case EntityContext.Gen6 when saveFile is SAV6AO sav6ao:
                badgeFlagInt = sav6ao.Badges;
                break;

            case EntityContext.Gen7b when saveFile is SAV7b:
                // https://github.com/codemonkey85/PKMDS-Blazor/issues/59
                // LGPE has gym clear system flags s0012–s0019 (FSYS_GYM_CLEAR_ROCK through
                // FSYS_GYM_CLEAR_GROUND) in EventWork7b, but PKHeX exposes no badge UI for
                // this game. Those flags may be gym-defeat state rather than badge possession,
                // and additional flags may be required. Do not use until confirmed.
                //badgeFlagInt = sav7b.Badges;
                break;

            case EntityContext.Gen8 when saveFile is SAV8SWSH sav8swsh:
                badgeFlagInt = sav8swsh.Badges;
                break;

            case EntityContext.Gen9 when saveFile is SAV9SV:
                // https://github.com/codemonkey85/PKMDS-Blazor/issues/60
                // SV has FSYS_YMAP_SCENARIO_GYM_CLEAR_* SCBlock flags per gym, but the YMAP
                // prefix indicates these drive overworld map display state, not badge possession.
                // PKHeX exposes no badge UI for this game and these flags are unused by any
                // PKHeX save logic. Additional flags (or a dedicated badge bitmask) may be
                // required. Do not use until confirmed.
                //badgeFlagInt = sav9sv.Badges;
                break;
        }

        for (var i = 0; i < badgeTotal; i++)
        {
            badgeFlags.Add((badgeFlagInt & 1 << i) != 0);
        }

        return badgeFlags;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private void OnBadgeToggle(int badgeIndex)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        Haptics.Tap();

        switch (saveFile.Context)
        {
            case EntityContext.Gen1 when saveFile is SAV1 sav1:
                sav1.Badges = ToggleBadge(sav1.Badges, badgeIndex);
                break;

            case EntityContext.Gen2 when saveFile is SAV2 sav2:
                sav2.Badges = ToggleBadge(sav2.Badges, badgeIndex);
                break;

            case EntityContext.Gen3 when saveFile is SAV3 sav3:
                sav3.Badges = ToggleBadge(sav3.Badges, badgeIndex);
                break;

            case EntityContext.Gen4 when saveFile is SAV4DP sav4dp:
                sav4dp.Badges = (byte)ToggleBadge(sav4dp.Badges, badgeIndex);
                break;

            case EntityContext.Gen4 when saveFile is SAV4Pt sav4pt:
                sav4pt.Badges = (byte)ToggleBadge(sav4pt.Badges, badgeIndex);
                break;

            case EntityContext.Gen4 when saveFile is SAV4HGSS sav4hgss:
                // HGSS stores 16 badges across two bytes: Johto in Badges, Kanto in
                // Badges16. Toggle on the combined 16-bit value, then split back out.
                var hgssBadges = ToggleBadge(sav4hgss.Badges | (sav4hgss.Badges16 << 8), badgeIndex);
                sav4hgss.Badges = (byte)(hgssBadges & 0xFF);
                sav4hgss.Badges16 = (hgssBadges >> 8) & 0xFF;
                break;

            case EntityContext.Gen5 when saveFile is SAV5BW sav5bw:
                sav5bw.Misc.Badges = ToggleBadge(sav5bw.Misc.Badges, badgeIndex);
                break;

            case EntityContext.Gen5 when saveFile is SAV5B2W2 sav5b2w2:
                sav5b2w2.Misc.Badges = ToggleBadge(sav5b2w2.Misc.Badges, badgeIndex);
                break;

            case EntityContext.Gen6 when saveFile is SAV6XY sav6xy:
                sav6xy.Badges = ToggleBadge(sav6xy.Badges, badgeIndex);
                break;

            case EntityContext.Gen6 when saveFile is SAV6AO sav6ao:
                sav6ao.Badges = ToggleBadge(sav6ao.Badges, badgeIndex);
                break;

            case EntityContext.Gen7b when saveFile is SAV7b:
                // https://github.com/codemonkey85/PKMDS-Blazor/issues/59
                // See GetSaveFileBadgesValue for investigation notes.
                break;

            case EntityContext.Gen8 when saveFile is SAV8SWSH sav8swsh:
                sav8swsh.Badges = ToggleBadge(sav8swsh.Badges, badgeIndex);
                break;

            case EntityContext.Gen8b when saveFile is SAV8BS sav8bs:
                sav8bs.FlagWork.SetSystemFlag(BadgesFlagStart + badgeIndex,
                    !sav8bs.FlagWork.GetSystemFlag(BadgesFlagStart + badgeIndex));
                break;

            case EntityContext.Gen9 when saveFile is SAV9SV:
                // https://github.com/codemonkey85/PKMDS-Blazor/issues/60
                // See GetSaveFileBadgesValue for investigation notes.
                break;
        }

        // No (byte) cast on the mask: Kanto badges (indices 8-15 in GSC/HGSS) need
        // bits 8-15, and (byte)(1 << 8) truncates to 0 — the cause of issue: Kanto
        // badge toggles silently doing nothing. The caller assigns into the correct
        // width (SAV2.Badges is a 16-bit int; HGSS splits the result across two bytes).
        static int ToggleBadge(int badges, int badgeIndex) =>
            badges ^ (1 << badgeIndex);
    }
}
