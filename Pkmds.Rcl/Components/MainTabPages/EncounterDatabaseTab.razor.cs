namespace Pkmds.Rcl.Components.MainTabPages;

public partial class EncounterDatabaseTab : RefreshAwareComponent
{
    private int? encounterGroupValue;
    private int? gameVersionValue;
    private bool hasSearched;
    private bool isGenerating;
    private bool isSearching;
    private bool? isShinyLocked;
    private int? levelMax;
    private int? levelMin;

    // ── Search state ──────────────────────────────────────────────────────

    private List<EncounterSearchResult> results = [];
    private EncounterSearchResult? selectedResult;

    // ── UI backing fields ─────────────────────────────────────────────────

    private ComboItem? speciesItem;

    /// <summary>Callback invoked after "Generate Legal Pokémon" places a Pokémon to jump to the Party / Box tab.</summary>
    [Parameter]
    public EventCallback OnJumpToPartyBox { get; set; }

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
        hasSearched = true;
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
        hasSearched = false;
    }

    private EncounterSearchFilter BuildFilter() =>
        new()
        {
            Species = speciesItem is { Value: > 0 }
                ? (ushort)speciesItem.Value
                : null,
            Version = gameVersionValue.HasValue && gameVersionValue.Value > 0
                ? (GameVersion)gameVersionValue.Value
                : null,
            LevelMin = levelMin.HasValue
                ? (byte)levelMin.Value
                : null,
            LevelMax = levelMax.HasValue
                ? (byte)levelMax.Value
                : null,
            IsShinyLocked = isShinyLocked,
            EncounterGroup = encounterGroupValue.HasValue
                ? (EncounterTypeGroup)encounterGroupValue.Value
                : null
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
        if (selectedResult is null)
        {
            return;
        }

        isGenerating = true;
        StateHasChanged();
        await Task.Yield();

        var pkm = AppService.GeneratePokemonFromEncounter(selectedResult.Encounter);

        isGenerating = false;

        if (pkm is null)
        {
            // Null when no save file is loaded, or when the encounter's PKM format cannot be
            // converted to the format required by the current save file (e.g. a Legends: Arceus
            // encounter attempted against a BDSP save).
            Snackbar.Add(
                "Could not generate Pokémon. The encounter may be incompatible with the current save file format.",
                Severity.Error);
            StateHasChanged();
            return;
        }

        // If no slot is currently selected, place the Pokémon in the first empty box slot.
        // For SAV7b (Let's Go), GetSelectedPokemonSlot returns None when only SelectedBoxSlotNumber
        // is set (box number is null for unified storage). Treat that as a valid selection.
        var slotType = AppService.GetSelectedPokemonSlot(out _, out _, out _);
        var isLetsGoWithSlot = AppState.SaveFile is SAV7b && AppState.SelectedBoxSlotNumber.HasValue;
        if (slotType == SelectedPokemonType.None && !isLetsGoWithSlot && !TrySelectFirstEmptyBoxSlot())
        {
            Snackbar.Add(
                "No empty box slots available. Free up a slot and try again.",
                Severity.Warning);
            StateHasChanged();
            return;
        }

        AppService.EditFormPokemon = pkm;

        // EditFormPokemon clones and recalculates stats; save that instance so party stats
        // and any other normalization done in the setter are persisted to the save file.
        var editedPkm = AppService.EditFormPokemon ?? pkm;
        AppService.SavePokemon(editedPkm);

        // Warn if legality analysis reports issues — the PKM is still placed so the
        // user can inspect and fix it in the editor.
        var la = new LegalityAnalysis(editedPkm);
        if (la.Valid)
        {
            Snackbar.Add($"{selectedResult.SpeciesName} generated successfully.", Severity.Success);
        }
        else
        {
            Snackbar.Add(
                $"{selectedResult.SpeciesName} generated, but legality check flagged issues. " +
                "Review the Pokémon in the editor.",
                Severity.Warning);
        }

        await OnJumpToPartyBox.InvokeAsync();

        StateHasChanged();
    }

    /// <summary>
    ///     Finds the first empty box slot in the save file and selects it via <see cref="IAppService" />.
    ///     Returns <see langword="false" /> when no empty slot is found or no save is loaded.
    /// </summary>
    private bool TrySelectFirstEmptyBoxSlot()
    {
        if (AppState.SaveFile is not { } sav)
        {
            return false;
        }

        for (var box = 0; box < sav.BoxCount; box++)
        {
            for (var slot = 0; slot < sav.BoxSlotCount; slot++)
            {
                if (sav.GetBoxSlotAtIndex(box, slot).Species == 0)
                {
                    if (sav is SAV7b)
                    {
                        // Let's Go uses a flat index across unified storage.
                        AppService.SetSelectedLetsGoPokemon(sav.BlankPKM, box * sav.BoxSlotCount + slot);
                    }
                    else
                    {
                        AppService.SetSelectedBoxPokemon(sav.BlankPKM, box, slot);
                    }

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
        _ => Color.Default
    };
}
