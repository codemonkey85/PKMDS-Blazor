using System.Globalization;

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
            RestoreSnapshot();
    }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    // TeraRaidDetail / SevenStarRaidDetail wrappers write directly through Memory<byte>
    // slices of the underlying SCBlocks. To make Save / Cancel meaningful, we snapshot the
    // raw block bytes on open and restore them on Cancel.
    private TeraRaidDetail[] _paldeaRaids = [];
    private TeraRaidDetail[] _kitakamiRaids = [];
    private TeraRaidDetail[] _blueberryRaids = [];
    private List<(int Index, SevenStarRaidDetail Raid)> _activeSevenStarRaids = [];

    private byte[]? _paldeaSnapshot;
    private byte[]? _kitakamiSnapshot;
    private byte[]? _blueberrySnapshot;
    private byte[]? _sevenStarCapturedSnapshot;
    private byte[]? _sevenStarDefeatedSnapshot;

    private int _paldeaIndex;
    private int _kitakamiIndex;
    private int _blueberryIndex;

    // Paldea list seeds (16-hex; DLC lists have no global seeds)
    private string _currentSeedHex = string.Empty;
    private string _tomorrowSeedHex = string.Empty;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (ReferenceEquals(loadedSave, SaveFile))
            return;
        if (!committed)
            RestoreSnapshot();
        _paldeaSnapshot = _kitakamiSnapshot = _blueberrySnapshot = null;
        _sevenStarCapturedSnapshot = _sevenStarDefeatedSnapshot = null;
        _paldeaRaids = _kitakamiRaids = _blueberryRaids = [];
        committed = false;
        loadedSave = SaveFile;
        LoadData();
    }

    private void LoadData()
    {
        if (SaveFile is null)
            return;

        _paldeaSnapshot ??= SaveFile.RaidPaldea.Data.ToArray();
        _paldeaRaids = SaveFile.RaidPaldea.GetAllRaids();
        _currentSeedHex = SaveFile.RaidPaldea.CurrentSeed.ToString("X16");
        _tomorrowSeedHex = SaveFile.RaidPaldea.TomorrowSeed.ToString("X16");
        _paldeaIndex = 0;

        if (SaveFile.SaveRevision >= 1)
        {
            _kitakamiSnapshot ??= SaveFile.RaidKitakami.Data.ToArray();
            _kitakamiRaids = SaveFile.RaidKitakami.GetAllRaids();
            _kitakamiIndex = 0;
        }

        if (SaveFile.SaveRevision >= 2)
        {
            _blueberrySnapshot ??= SaveFile.RaidBlueberry.Data.ToArray();
            _blueberryRaids = SaveFile.RaidBlueberry.GetAllRaids();
            _blueberryIndex = 0;
        }

        _sevenStarCapturedSnapshot ??= SaveFile.RaidSevenStar.Captured.Data.ToArray();
        _sevenStarDefeatedSnapshot ??= SaveFile.RaidSevenStar.Defeated.Data.ToArray();
        _activeSevenStarRaids = SaveFile.RaidSevenStar.GetAllRaids()
            .Select((r, i) => (Index: i, Raid: r))
            .Where(x => x.Raid.Identifier != 0)
            .ToList();
    }

    private void SetCurrentSeed(string hex)
    {
        _currentSeedHex = hex;
        if (SaveFile is not null && ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
            SaveFile.RaidPaldea.CurrentSeed = v;
    }

    private void SetTomorrowSeed(string hex)
    {
        _tomorrowSeedHex = hex;
        if (SaveFile is not null && ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
            SaveFile.RaidPaldea.TomorrowSeed = v;
    }

    private static void SetRaidSeed(TeraRaidDetail raid, string hex)
    {
        if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
            raid.Seed = v;
    }

    private static void SetSevenStarIdentifier(SevenStarRaidDetail raid, string hex)
    {
        if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
            raid.Identifier = v;
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
            save.State.Edited = true;
        RefreshService.Refresh();
        Haptics.Confirm();
        MudDialog?.Close(DialogResult.Ok(true));
    }

    private void RestoreSnapshot()
    {
        if (loadedSave is not null)
        {
            _paldeaSnapshot?.CopyTo(loadedSave.RaidPaldea.Data);
            _kitakamiSnapshot?.CopyTo(loadedSave.RaidKitakami.Data);
            _blueberrySnapshot?.CopyTo(loadedSave.RaidBlueberry.Data);
            _sevenStarCapturedSnapshot?.CopyTo(loadedSave.RaidSevenStar.Captured.Data);
            _sevenStarDefeatedSnapshot?.CopyTo(loadedSave.RaidSevenStar.Defeated.Data);
        }
    }

    private void Cancel()
    {
        RestoreSnapshot();
        MudDialog?.Close(DialogResult.Cancel());
    }
}
