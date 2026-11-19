namespace Pkmds.Rcl.Components.Dialogs;

public partial class Raid9Dialog : IDisposable
{
    [Parameter]
    [EditorRequired]
    public SAV9SV? SaveFile { get; set; }

    private SAV9SV? loadedSave;
    private bool committed;

    public void Dispose()
    {
        if (!committed)
        {
            RestoreSnapshot();
        }
    }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    // TeraRaidDetail / SevenStarRaidDetail wrappers write directly through Memory<byte>
    // slices of the underlying SCBlocks. To make Save / Cancel meaningful, we snapshot the
    // raw block bytes on open and restore them on Cancel.
    private TeraRaidDetail[] paldeaRaids = [];
    private TeraRaidDetail[] kitakamiRaids = [];
    private TeraRaidDetail[] blueberryRaids = [];
    private List<(int Index, SevenStarRaidDetail Raid)> activeSevenStarRaids = [];

    private byte[]? paldeaSnapshot;
    private byte[]? kitakamiSnapshot;
    private byte[]? blueberrySnapshot;
    private byte[]? sevenStarCapturedSnapshot;
    private byte[]? sevenStarDefeatedSnapshot;

    private int paldeaIndex;
    private int kitakamiIndex;
    private int blueberryIndex;

    // Paldea list seeds (16-hex; DLC lists have no global seeds)
    private string currentSeedHex = string.Empty;
    private string tomorrowSeedHex = string.Empty;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (ReferenceEquals(loadedSave, SaveFile))
        {
            return;
        }

        if (!committed)
        {
            RestoreSnapshot();
        }

        paldeaSnapshot = kitakamiSnapshot = blueberrySnapshot = null;
        sevenStarCapturedSnapshot = sevenStarDefeatedSnapshot = null;
        paldeaRaids = kitakamiRaids = blueberryRaids = [];
        committed = false;
        loadedSave = SaveFile;
        LoadData();
    }

    private void LoadData()
    {
        if (SaveFile is null)
        {
            return;
        }

        paldeaSnapshot ??= SaveFile.RaidPaldea.Data.ToArray();
        paldeaRaids = SaveFile.RaidPaldea.GetAllRaids();
        currentSeedHex = SaveFile.RaidPaldea.CurrentSeed.ToString("X16");
        tomorrowSeedHex = SaveFile.RaidPaldea.TomorrowSeed.ToString("X16");
        paldeaIndex = 0;

        if (SaveFile.SaveRevision >= 1)
        {
            kitakamiSnapshot ??= SaveFile.RaidKitakami.Data.ToArray();
            kitakamiRaids = SaveFile.RaidKitakami.GetAllRaids();
            kitakamiIndex = 0;
        }

        if (SaveFile.SaveRevision >= 2)
        {
            blueberrySnapshot ??= SaveFile.RaidBlueberry.Data.ToArray();
            blueberryRaids = SaveFile.RaidBlueberry.GetAllRaids();
            blueberryIndex = 0;
        }

        sevenStarCapturedSnapshot ??= SaveFile.RaidSevenStar.Captured.Data.ToArray();
        sevenStarDefeatedSnapshot ??= SaveFile.RaidSevenStar.Defeated.Data.ToArray();
        activeSevenStarRaids = SaveFile.RaidSevenStar.GetAllRaids()
            .Select((r, i) => (Index: i, Raid: r))
            .Where(x => x.Raid.Identifier != 0)
            .ToList();
    }

    private void SetCurrentSeed(string hex)
    {
        currentSeedHex = hex;
        if (SaveFile is not null && ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
        {
            SaveFile.RaidPaldea.CurrentSeed = v;
        }
    }

    private void SetTomorrowSeed(string hex)
    {
        tomorrowSeedHex = hex;
        if (SaveFile is not null && ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
        {
            SaveFile.RaidPaldea.TomorrowSeed = v;
        }
    }

    private static void SetRaidSeed(TeraRaidDetail raid, string hex)
    {
        if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
        {
            raid.Seed = v;
        }
    }

    private static void SetSevenStarIdentifier(SevenStarRaidDetail raid, string hex)
    {
        if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
        {
            raid.Identifier = v;
        }
    }

    private void Propagate(RaidSpawnList9 list, int sourceIndex, bool seedToo)
    {
        list.Propagate(sourceIndex, seedToo);
        StateHasChanged();
    }

    private void Save()
    {
        committed = true;
        if (SaveFile is { } save)
        {
            save.State.Edited = true;
        }

        RefreshService.Refresh();
        Haptics.Confirm();
        MudDialog?.Close(DialogResult.Ok(true));
    }

    private void RestoreSnapshot()
    {
        if (loadedSave is null)
        {
            return;
        }

        paldeaSnapshot?.CopyTo(loadedSave.RaidPaldea.Data);
        kitakamiSnapshot?.CopyTo(loadedSave.RaidKitakami.Data);
        blueberrySnapshot?.CopyTo(loadedSave.RaidBlueberry.Data);
        sevenStarCapturedSnapshot?.CopyTo(loadedSave.RaidSevenStar.Captured.Data);
        sevenStarDefeatedSnapshot?.CopyTo(loadedSave.RaidSevenStar.Defeated.Data);
    }

    private void Cancel()
    {
        RestoreSnapshot();
        MudDialog?.Close(DialogResult.Cancel());
    }
}
