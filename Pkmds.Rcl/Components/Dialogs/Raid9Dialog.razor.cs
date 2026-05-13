using System.Globalization;

namespace Pkmds.Rcl.Components.Dialogs;

public partial class Raid9Dialog
{
    [Parameter]
    [EditorRequired]
    public SAV9SV? SaveFile { get; set; }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    // TeraRaidDetail objects wrap Memory<byte> slices of the block —
    // property setters write through to the save data directly.
    private TeraRaidDetail[] _paldeaRaids = [];
    private TeraRaidDetail[] _kitakamiRaids = [];
    private TeraRaidDetail[] _blueberryRaids = [];
    private List<(int Index, SevenStarRaidDetail Raid)> _activeSevenStarRaids = [];

    private int _paldeaIndex;
    private int _kitakamiIndex;
    private int _blueberryIndex;

    // Paldea list seeds (16-hex; DLC lists have no global seeds)
    private string _currentSeedHex = string.Empty;
    private string _tomorrowSeedHex = string.Empty;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        LoadData();
    }

    private void LoadData()
    {
        if (SaveFile is null)
            return;

        _paldeaRaids = SaveFile.RaidPaldea.GetAllRaids();
        _currentSeedHex = SaveFile.RaidPaldea.CurrentSeed.ToString("X16");
        _tomorrowSeedHex = SaveFile.RaidPaldea.TomorrowSeed.ToString("X16");
        _paldeaIndex = 0;

        if (SaveFile.SaveRevision >= 1)
        {
            _kitakamiRaids = SaveFile.RaidKitakami.GetAllRaids();
            _kitakamiIndex = 0;
        }

        if (SaveFile.SaveRevision >= 2)
        {
            _blueberryRaids = SaveFile.RaidBlueberry.GetAllRaids();
            _blueberryIndex = 0;
        }

        _activeSevenStarRaids = SaveFile.RaidSevenStar.GetAllRaids()
            .Select((r, i) => (Index: i, Raid: r))
            .Where(x => x.Raid.Identifier != 0)
            .ToList();
    }

    private void SetCurrentSeed(string hex)
    {
        _currentSeedHex = hex;
        if (SaveFile is not null && ulong.TryParse(hex, NumberStyles.HexNumber, null, out var v))
            SaveFile.RaidPaldea.CurrentSeed = v;
    }

    private void SetTomorrowSeed(string hex)
    {
        _tomorrowSeedHex = hex;
        if (SaveFile is not null && ulong.TryParse(hex, NumberStyles.HexNumber, null, out var v))
            SaveFile.RaidPaldea.TomorrowSeed = v;
    }

    private static void SetRaidSeed(TeraRaidDetail raid, string hex)
    {
        if (uint.TryParse(hex, NumberStyles.HexNumber, null, out var v))
            raid.Seed = v;
    }

    private static void SetSevenStarIdentifier(SevenStarRaidDetail raid, string hex)
    {
        if (uint.TryParse(hex, NumberStyles.HexNumber, null, out var v))
            raid.Identifier = v;
    }

    private void Propagate(RaidSpawnList9 list, int sourceIndex, bool seedToo)
    {
        list.Propagate(sourceIndex, seedToo);
        StateHasChanged();
    }

    private void Save()
    {
        RefreshService.Refresh();
        Haptics.Confirm();
        MudDialog?.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog?.Close(DialogResult.Cancel());
}
