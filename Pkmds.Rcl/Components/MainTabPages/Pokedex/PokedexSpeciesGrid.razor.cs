namespace Pkmds.Rcl.Components.MainTabPages.Pokedex;

public partial class PokedexSpeciesGrid
{
    private bool hasDetailedEditor;
    private int lastRefreshToken = -1;

    // Tracks the last seen values to avoid a full BuildRows() call on every re-render.
    private SaveFile? lastSaveFile;
    private IReadOnlyList<RegionalDexDefinition> regionalDexDefinitions = [];
    private List<PokedexGridRow> rows = [];

    // Pre-filtered view of rows passed directly to MudDataGrid Items so the
    // virtualizer always has a correct, stable item count.  Rebuilt whenever
    // rows changes or any filter criterion changes.  This avoids the
    // QuickFilter delegate-recreation problem where MudDataGrid invalidates its
    // internal cache every render cycle and the Virtualize component resets the
    // scroll position back to 0.
    private List<PokedexGridRow> filteredRows = [];

    private string searchText = string.Empty;
    private int selectedRegionalDexFilter = -1; // -1 = All
    private DexStatusFilter selectedStatusFilter = DexStatusFilter.All;

    // Incremented by PokedexTab after each bulk operation (Fill / Seen All / Clear).
    // Giving the grid a changing parameter ensures Blazor re-renders the child and
    // calls OnParametersSet, which rebuilds the row list to reflect the new state.
    [Parameter]
    public int RefreshToken { get; set; }

    // Invoked after any per-species Seen/Caught toggle so the parent (PokedexTab)
    // can refresh its summary counts and progress bars without a full grid rebuild.
    [Parameter]
    public EventCallback OnSpeciesChanged { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var saveFile = AppState.SaveFile;
        // Only rebuild from the save file when the save file itself changes or when a
        // bulk operation increments RefreshToken.  Individual Seen/Caught toggles are
        // handled in-place by UpdateRowFromSave so the virtualizer's Items reference
        // changes without a full list rebuild.
        if (ReferenceEquals(saveFile, lastSaveFile) && RefreshToken == lastRefreshToken)
        {
            return;
        }

        lastSaveFile = saveFile;
        lastRefreshToken = RefreshToken;
        BuildRows();
    }

    // Materializes one PokedexGridRow per species that is registered in this game's
    // Pokédex.  Uses the same per-game filter as PokedexTab.GetDexTotalCount so the
    // grid does not show species that the game never tracks (e.g. non-Galar species
    // in SWSH, non-Hisui species in LA).
    private void BuildRows()
    {
        if (AppState.SaveFile is not { HasPokeDex: true } saveFile)
        {
            rows = [];
            filteredRows = [];
            return;
        }

        var speciesNames = GameInfo.Strings.Species;
        var pokedexGridRows = new List<PokedexGridRow>();

        for (ushort i = 1; i <= saveFile.MaxSpeciesID; i++)
        {
            if (!IsSpeciesInDex(saveFile, i))
            {
                continue;
            }

            var name = i < speciesNames.Count
                ? speciesNames[i]
                : i.ToString(CultureInfo.InvariantCulture);

            var regionalIds = PokedexHelpers.GetRegionalIds(saveFile, i);
            pokedexGridRows.Add(new PokedexGridRow(i, name, regionalIds, saveFile.GetSeen(i), saveFile.GetCaught(i)));
        }

        rows = pokedexGridRows;
        regionalDexDefinitions = PokedexHelpers.GetRegionalDexDefinitions(saveFile);
        selectedRegionalDexFilter = -1;
        hasDetailedEditor = saveFile is SAV4 or SAV5 or SAV6XY or SAV6AO or SAV7 or SAV7b
            or SAV8SWSH or SAV8LA or SAV8BS or SAV9SV or SAV9ZA;
        selectedStatusFilter = DexStatusFilter.All;
        ApplyFilter();
    }

    // Rebuilds filteredRows from the full rows list using the current filter
    // state.  Always call this after changing rows, searchText,
    // selectedStatusFilter, or selectedRegionalDexFilter.
    private void ApplyFilter() => filteredRows = [.. rows.Where(FilterRow)];

    private void SetSearchText(string value)
    {
        searchText = value;
        ApplyFilter();
    }

    private void SetStatusFilter(DexStatusFilter filter)
    {
        selectedStatusFilter = filter;
        ApplyFilter();
    }

    private void SetRegionalDexFilter(int filter)
    {
        selectedRegionalDexFilter = filter;
        ApplyFilter();
    }

    // Delegates to the shared PokedexHelpers.IsSpeciesInDex so the grid and the
    // PokedexTab header counts always filter against the same species set.
    private static int RegionalIdForSort(PokedexGridRow row, int colIdx)
    {
        if (colIdx >= row.RegionalIds.Count)
        {
            return int.MaxValue;
        }

        var id = row.RegionalIds[colIdx];
        return id == 0
            ? int.MaxValue
            : id;
    }

    private static bool IsSpeciesInDex(SaveFile saveFile, ushort species) =>
        PokedexHelpers.IsSpeciesInDex(saveFile, species);

    // Returns true when the row should be visible given the current search text and
    // status filter.  Name/ID matching runs first; status filter is applied after.
    private bool FilterRow(PokedexGridRow row)
    {
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.Trim();
            if (ushort.TryParse(search, out var id))
            {
                if (row.SpeciesId != id)
                {
                    return false;
                }
            }
            else if (!row.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (selectedRegionalDexFilter >= 0 && selectedRegionalDexFilter < row.RegionalIds.Count
                                           && row.RegionalIds[selectedRegionalDexFilter] == 0)
        {
            return false;
        }

        return selectedStatusFilter switch
        {
            DexStatusFilter.Seen => row.IsSeen,
            DexStatusFilter.Caught => row.IsCaught,
            DexStatusFilter.Unseen => !row.IsSeen,
            DexStatusFilter.SeenNotCaught => row is { IsSeen: true, IsCaught: false },
            _ => true
        };
    }

    private async Task OnSeenChanged(PokedexGridRow row, bool value)
    {
        if (AppState.SaveFile is not { HasPokeDex: true } saveFile)
        {
            return;
        }

        if (saveFile is SAV9SV sv)
        {
            // PKHeX bug: SaveFile.SetSeen is a virtual no-op; SAV9SV never overrides it.
            // Must write through the Zukan API directly for both dex block modes.
            if (sv.Zukan.GetRevision() == 0)
            {
                // Paldea block (pre-DLC saves). SetSeen(false) lands on state 1
                // ("known but not seen") to match PKHeX semantics — not state 0.
                var entry = sv.Zukan.DexPaldea.Get(row.SpeciesId);
                entry.SetSeen(value);
            }
            else
            {
                // Kitakami block (post-2.0.1 saves with DLC; stores all species).
                var entry = sv.Zukan.DexKitakami.Get(row.SpeciesId);
                if (value)
                {
                    entry.SetSeenForm(0, true);
                }
                else
                {
                    entry.ClearSeen(0);
                }
            }
        }
        else
        {
            saveFile.SetSeen(row.SpeciesId, value);
        }

        // Re-read the actual stored state to keep the row in sync with the save file.
        UpdateRowFromSave(row, saveFile);

        // Notify the parent so it can refresh its summary counts and progress bars.
        await OnSpeciesChanged.InvokeAsync();
    }

    private async Task OnCaughtChanged(PokedexGridRow row, bool value)
    {
        if (AppState.SaveFile is not { HasPokeDex: true } saveFile)
        {
            return;
        }

        if (saveFile is SAV9SV sv)
        {
            // PKHeX bug: SaveFile.SetCaught is a virtual no-op; SAV9SV never overrides it.
            // Must write through the Zukan API directly for both dex block modes.
            if (sv.Zukan.GetRevision() == 0)
            {
                // Paldea block (pre-DLC saves).
                var entry = sv.Zukan.DexPaldea.Get(row.SpeciesId);
                entry.SetCaught(value);
            }
            else
            {
                // Kitakami block (post-2.0.1 saves with DLC; stores all species).
                var entry = sv.Zukan.DexKitakami.Get(row.SpeciesId);
                if (value)
                {
                    // A caught species must also be seen.
                    entry.SetSeenForm(0, true);
                    entry.SetObtainedForm(0, true);
                }
                else
                {
                    // Clear caught but keep seen.
                    entry.SetObtainedForm(0, false);
                }
            }
        }
        else
        {
            saveFile.SetCaught(row.SpeciesId, value);
        }

        // Re-read the actual stored state to keep the row in sync with the save file.
        UpdateRowFromSave(row, saveFile);

        // Notify the parent so it can refresh its summary counts and progress bars.
        await OnSpeciesChanged.InvokeAsync();
    }

    private void UpdateRowFromSave(PokedexGridRow row, SaveFile saveFile)
    {
        var idx = rows.FindIndex(r => r.SpeciesId == row.SpeciesId);
        if (idx < 0)
        {
            return;
        }

        rows = new List<PokedexGridRow>(rows)
        {
            [idx] = row with
            {
                IsSeen = saveFile.GetSeen(row.SpeciesId),
                IsCaught = saveFile.GetCaught(row.SpeciesId),
            },
        };

        // Rebuild filteredRows so MudDataGrid's virtualizer detects the Items
        // reference change and re-renders visible rows with updated Seen/Caught
        // state.  Also handles the case where a status-filter is active and the
        // row's visibility changes (e.g. unchecking Seen while "Seen" filter is on).
        ApplyFilter();
    }

    private async Task OpenDetails(PokedexGridRow row)
    {
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Medium);
        var result = await DialogService.ShowAsync<PokedexSpeciesDialog>(
            string.Empty,
            new DialogParameters<PokedexSpeciesDialog> { { x => x.SpeciesId, row.SpeciesId } },
            options);

        await result.Result;

        // Refresh the row so Seen/Caught columns reflect any changes made in the dialog.
        if (AppState.SaveFile is { HasPokeDex: true } saveFile)
        {
            UpdateRowFromSave(row, saveFile);
        }
    }
}
