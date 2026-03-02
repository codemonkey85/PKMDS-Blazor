namespace Pkmds.Rcl.Components.MainTabPages;

public partial class EncounterDatabaseTab : RefreshAwareComponent
{
    /// <summary>Callback invoked after "Generate Legal Pokémon" places a Pokémon to jump to the Party / Box tab.</summary>
    [Parameter]
    public EventCallback OnJumpToPartyBox { get; set; }

    // ── Search state ──────────────────────────────────────────────────────

    private List<EncounterSearchResult> results = [];
    private bool isSearching;
    private bool isGenerating;
    private EncounterSearchResult? selectedResult;

    // ── UI backing fields ─────────────────────────────────────────────────

    private ComboItem? speciesItem;
    private int? gameVersionValue;
    private int? levelMin;
    private int? levelMax;
    private bool? isShinyLocked;
    private int? encounterGroupValue;

    // ── Computed properties ───────────────────────────────────────────────

    private bool HasResults => results.Count > 0;
    private int ResultCount => results.Count;

    // ── Search ────────────────────────────────────────────────────────────

    private async Task RunSearchAsync()
    {
        if (AppState.SaveFile is null || speciesItem is null)
        {
            return;
        }

        isSearching = true;
        selectedResult = null;
        StateHasChanged();

        // Yield so the spinner renders before the CPU-bound encounter search.
        await Task.Yield();

        var filter = BuildFilter();
        results = AppService.SearchEncounters(filter).ToList();

        isSearching = false;
        StateHasChanged();
    }

    private void ResetFilter()
    {
        speciesItem = null;
        gameVersionValue = null;
        levelMin = null;
        levelMax = null;
        isShinyLocked = null;
        encounterGroupValue = null;
        results = [];
        selectedResult = null;
    }

    private EncounterSearchFilter BuildFilter() =>
        new()
        {
            Species = speciesItem is { Value: > 0 } ? (ushort)speciesItem.Value : null,
            Version = gameVersionValue.HasValue && gameVersionValue.Value > 0
                ? (GameVersion)gameVersionValue.Value
                : null,
            LevelMin = levelMin.HasValue ? (byte)levelMin.Value : null,
            LevelMax = levelMax.HasValue ? (byte)levelMax.Value : null,
            IsShinyLocked = isShinyLocked,
            EncounterGroup = encounterGroupValue.HasValue
                ? (EncounterTypeGroup)encounterGroupValue.Value
                : null,
        };

    // ── Row click ─────────────────────────────────────────────────────────

    private Task OnRowClickAsync(TableRowClickEventArgs<EncounterSearchResult> args)
    {
        selectedResult = args.Item;
        return Task.CompletedTask;
    }

    // ── Generate Legal Pokémon ────────────────────────────────────────────

    private async Task GeneratePokemonAsync()
    {
        if (selectedResult is null) return;

        isGenerating = true;
        StateHasChanged();
        await Task.Yield();

        var pkm = AppService.GeneratePokemonFromEncounter(selectedResult.Encounter);

        isGenerating = false;

        if (pkm is null)
        {
            Snackbar.Add(
                "Could not generate a legal Pokémon for this encounter. Try a different encounter.",
                Severity.Warning);
        }
        else
        {
            // If no slot is currently selected, place the Pokémon in the first empty box slot.
            var slotType = AppService.GetSelectedPokemonSlot(out _, out _, out _);
            if (slotType == SelectedPokemonType.None && !TrySelectFirstEmptyBoxSlot())
            {
                Snackbar.Add(
                    "No empty box slots available. Free up a slot and try again.",
                    Severity.Warning);
                StateHasChanged();
                return;
            }

            AppService.EditFormPokemon = pkm;
            AppService.SavePokemon(pkm);
            Snackbar.Add($"{selectedResult.SpeciesName} generated and placed in the selected slot.", Severity.Success);
            await OnJumpToPartyBox.InvokeAsync();
        }

        StateHasChanged();
    }

    /// <summary>
    /// Finds the first empty box slot in the save file and selects it via <see cref="IAppService"/>.
    /// Returns <see langword="false"/> when no empty slot is found or no save is loaded.
    /// </summary>
    private bool TrySelectFirstEmptyBoxSlot()
    {
        if (AppState.SaveFile is not { } sav) return false;

        for (var box = 0; box < sav.BoxCount; box++)
        {
            for (var slot = 0; slot < sav.BoxSlotCount; slot++)
            {
                if (sav.GetBoxSlotAtIndex(box, slot).Species == 0)
                {
                    AppService.SetSelectedBoxPokemon(sav.BlankPKM, box, slot);
                    return true;
                }
            }
        }

        return false;
    }

    // ── Species autocomplete ──────────────────────────────────────────────

    private async Task<IEnumerable<ComboItem>> SearchSpeciesAsync(string search, CancellationToken ct) =>
        await Task.FromResult(AppService.SearchPokemonNames(search));

    private static string SpeciesItemToString(ComboItem? item) => item?.Text ?? string.Empty;

    // ── Table helpers ─────────────────────────────────────────────────────

    private static string RowStyleFunc(EncounterSearchResult _, int __) => "cursor: pointer;";

    private static Color GetTypeChipColor(string typeName) => typeName switch
    {
        "Wild" => Color.Success,
        "Static" => Color.Primary,
        "Mystery Gift" => Color.Tertiary,
        "Trade" => Color.Warning,
        "Egg" => Color.Info,
        _ => Color.Default,
    };
}
