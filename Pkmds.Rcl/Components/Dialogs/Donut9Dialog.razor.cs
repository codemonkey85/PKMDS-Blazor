namespace Pkmds.Rcl.Components.Dialogs;

public partial class Donut9Dialog
{
    [Parameter]
    [EditorRequired]
    public SAV9ZA? SaveFile { get; set; }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    // Raw slot indices (0–998) for slots where MillisecondsSince1970 != 0
    private List<int> _activeIndices = [];

    // Currently-displayed slot (0–998, may be empty if navigated there manually)
    private int _selectedRawIndex;

    // Berry options built once per save load (includes item names)
    private List<(ushort Item, string Label)> _berryOptions = [];

    // Donut9a wrappers (and bulk operations on DonutPocket9a) write directly through
    // Memory<byte> slices of the underlying SCBlock. To make Save / Cancel meaningful,
    // we snapshot the raw block bytes on open and restore them on Cancel.
    private byte[]? _donutsSnapshot;

    // Flavor perk options — constant across all saves
    private static IReadOnlyList<(ulong Hash, string Name)> FlavorOptions { get; } =
        [(0, "None"), .. DonutInfo.Flavors];

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        LoadData();
    }

    private void LoadData()
    {
        if (SaveFile is null)
            return;

        _donutsSnapshot ??= SaveFile.Donuts.Data.ToArray();

        _activeIndices.Clear();
        var donuts = SaveFile.Donuts;
        for (var i = 0; i < DonutPocket9a.MaxCount; i++)
        {
            if (donuts.GetDonut(i).MillisecondsSince1970 != 0)
                _activeIndices.Add(i);
        }

        if (!_activeIndices.Contains(_selectedRawIndex))
            _selectedRawIndex = _activeIndices.Count > 0 ? _activeIndices[0] : 0;

        // Build berry option list with item names
        var itemList = GameInfo.Strings.itemlist;
        _berryOptions = [(0, "None"), ..
            DonutInfo.Berries.Select(b =>
            {
                var name = b.Item < itemList.Length ? itemList[b.Item] : b.Item.ToString();
                return (b.Item, $"{name} ({b.Item})");
            })];
    }

    // Prev/Next must work even when the user manually selected an inactive slot
    // (via the Slot numeric field). In that case _activeIndices.IndexOf returns -1,
    // so we snap to the nearest occupied slot in the requested direction instead.
    // _activeIndices is built in ascending order, so a linear scan is sufficient.
    private int FindPrevActiveIndex()
    {
        var idx = _activeIndices.IndexOf(_selectedRawIndex);
        if (idx > 0)
            return idx - 1;
        if (idx == 0)
            return -1;
        for (var i = _activeIndices.Count - 1; i >= 0; i--)
            if (_activeIndices[i] < _selectedRawIndex)
                return i;
        return -1;
    }

    private int FindNextActiveIndex()
    {
        var idx = _activeIndices.IndexOf(_selectedRawIndex);
        if (idx >= 0 && idx < _activeIndices.Count - 1)
            return idx + 1;
        if (idx == _activeIndices.Count - 1)
            return -1;
        for (var i = 0; i < _activeIndices.Count; i++)
            if (_activeIndices[i] > _selectedRawIndex)
                return i;
        return -1;
    }

    private bool CanGoPrev => FindPrevActiveIndex() >= 0;
    private bool CanGoNext => FindNextActiveIndex() >= 0;

    private void GotoPrev()
    {
        var i = FindPrevActiveIndex();
        if (i >= 0)
            _selectedRawIndex = _activeIndices[i];
    }

    private void GotoNext()
    {
        var i = FindNextActiveIndex();
        if (i >= 0)
            _selectedRawIndex = _activeIndices[i];
    }

    // Returns the donut at the currently selected raw slot.
    // Donut9a wraps Memory<byte> — property setters write through to the save block.
    private Donut9a? CurrentDonut =>
        SaveFile is not null && _selectedRawIndex < DonutPocket9a.MaxCount
            ? SaveFile.Donuts.GetDonut(_selectedRawIndex)
            : null;

    private bool IsCurrentSlotOccupied =>
        CurrentDonut is { } d && d.MillisecondsSince1970 != 0;

    private static string FormatTimestamp(ulong ms) =>
        ms == 0 ? "—" : Donut9a.Epoch.AddMilliseconds(ms).ToString("yyyy-MM-dd HH:mm:ss");

    private static ushort GetBerry(Donut9a d, int slot) => slot switch
    {
        0 => d.Berry1,
        1 => d.Berry2,
        2 => d.Berry3,
        3 => d.Berry4,
        4 => d.Berry5,
        5 => d.Berry6,
        6 => d.Berry7,
        _ => d.Berry8,
    };

    private static void SetBerry(Donut9a d, int slot, ushort value)
    {
        switch (slot)
        {
            case 0: d.Berry1 = value; break;
            case 1: d.Berry2 = value; break;
            case 2: d.Berry3 = value; break;
            case 3: d.Berry4 = value; break;
            case 4: d.Berry5 = value; break;
            case 5: d.Berry6 = value; break;
            case 6: d.Berry7 = value; break;
            default: d.Berry8 = value; break;
        }
    }

    private static ulong GetFlavor(Donut9a d, int slot) => slot switch
    {
        0 => d.Flavor0,
        1 => d.Flavor1,
        _ => d.Flavor2,
    };

    private static void SetFlavor(Donut9a d, int slot, ulong value)
    {
        switch (slot)
        {
            case 0: d.Flavor0 = value; break;
            case 1: d.Flavor1 = value; break;
            default: d.Flavor2 = value; break;
        }
    }

    private void Recalculate()
    {
        if (CurrentDonut is not { } d)
            return;
        DonutInfo.RecalculateDonutStats(d);
        StateHasChanged();
    }

    private void SetAllShinyTemplate()
    {
        SaveFile?.Donuts.SetAllAsShinyTemplate();
        LoadData();
        StateHasChanged();
    }

    private void SetAllRandomLv3()
    {
        SaveFile?.Donuts.SetAllRandomLv3();
        LoadData();
        StateHasChanged();
    }

    private void CloneSelected()
    {
        if (SaveFile is null || _activeIndices.Count == 0)
            return;
        SaveFile.Donuts.CloneAllFromIndex(_selectedRawIndex);
        LoadData();
        StateHasChanged();
    }

    private void Save()
    {
        RefreshService.Refresh();
        Haptics.Confirm();
        MudDialog?.Close(DialogResult.Ok(true));
    }

    private void Cancel()
    {
        if (SaveFile is not null && _donutsSnapshot is not null)
        {
            _donutsSnapshot.CopyTo(SaveFile.Donuts.Data);
            RefreshService.Refresh();
        }
        MudDialog?.Close(DialogResult.Cancel());
    }
}
