namespace Pkmds.Web.Components.MainTabPages;

public partial class BadgesComponent : IDisposable
{
    private const int BadgesFlagStart = 124;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

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
                badgeFlagInt = sav4hgss.Badges;
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
                // TODO: Figure out why this isn't available
                //badgeFlagInt = sav7b.Badges;
                break;

            case EntityContext.Gen8 when saveFile is SAV8SWSH sav8swsh:
                badgeFlagInt = sav8swsh.Badges;
                break;

            case EntityContext.Gen9 when saveFile is SAV9SV:
                // TODO: Figure out why this isn't available
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
                sav4dp.Badges = ToggleBadge(sav4dp.Badges, badgeIndex);
                break;

            case EntityContext.Gen4 when saveFile is SAV4Pt sav4pt:
                sav4pt.Badges = ToggleBadge(sav4pt.Badges, badgeIndex);
                break;

            case EntityContext.Gen4 when saveFile is SAV4HGSS sav4hgss:
                sav4hgss.Badges = ToggleBadge(sav4hgss.Badges, badgeIndex);
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
                // TODO: Figure out why this isn't available
                //sav7b.Badges = ToggleBadge(sav7b.Badges, badgeIndex);
                break;

            case EntityContext.Gen8 when saveFile is SAV8SWSH sav8swsh:
                sav8swsh.Badges = ToggleBadge(sav8swsh.Badges, badgeIndex);
                break;

            case EntityContext.Gen8b when saveFile is SAV8BS sav8bs:
                sav8bs.FlagWork.SetSystemFlag(BadgesFlagStart + badgeIndex,
                    !sav8bs.FlagWork.GetSystemFlag(BadgesFlagStart + badgeIndex));
                break;

            case EntityContext.Gen9 when saveFile is SAV9SV:
                // TODO: Figure out why this isn't available
                //sav9sv.Badges = ToggleBadge(sav9sv.Badges, badgeIndex);
                break;
        }

        static int ToggleBadge(int badges, int badgeIndex) =>
            badges ^ (byte)(1 << badgeIndex);
    }
}
