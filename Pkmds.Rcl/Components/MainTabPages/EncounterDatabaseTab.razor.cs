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
            Version = gameVersionValue is > 0
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
        if (!await EnsureTargetSlotSelectedAsync())
        {
            isGenerating = false;
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

    // ── Slot selection helper ─────────────────────────────────────────────

    /// <summary>
    /// Ensures a target box slot is ready for writing. When a slot is already selected and
    /// occupied, prompts the user to overwrite, use the first available slot, or cancel.
    /// Falls back to the first empty box slot automatically when no slot is selected.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if a slot is ready and the caller should proceed;
    /// <see langword="false" /> if the caller should abort.
    /// </returns>
    private async Task<bool> EnsureTargetSlotSelectedAsync()
    {
        var slotType = AppService.GetSelectedPokemonSlot(out _, out _, out _);
        var isLetsGoWithSlot = AppState.SaveFile is SAV7b && AppState.SelectedBoxSlotNumber.HasValue;
        var hasSelectedSlot = slotType != SelectedPokemonType.None || isLetsGoWithSlot;

        if (hasSelectedSlot)
        {
            // No occupant loaded (null) or an empty slot (Species 0) means there's nothing to
            // overwrite — proceed without prompting. Guarding the null case fixes a
            // NullReferenceException when generating into a slot with no loaded occupant, e.g. the
            // first generate after loading a save with nothing selected (issue #949).
            if (AppService.EditFormPokemon is not { Species: > 0 } occupant)
            {
                return true;
            }

            var occupantName = SafeNameLookup.Species(occupant.Species);
            var confirmed = await DialogService.ShowMessageBoxAsync(
                "Overwrite Pokémon?",
                $"The selected slot contains {occupantName}. Overwrite it?",
                yesText: "Overwrite",
                noText: "Use First Available Slot",
                cancelText: "Cancel");
            switch (confirmed)
            {
                case null:
                    return false;
                case false when !AppService.TrySelectFirstEmptyBoxSlot():
                    Snackbar.Add(
                        "No empty box slots available. Free up a slot and try again.",
                        Severity.Warning);
                    return false;
            }
        }
        else if (!AppService.TrySelectFirstEmptyBoxSlot())
        {
            Snackbar.Add(
                "No empty box slots available. Free up a slot and try again.",
                Severity.Warning);
            return false;
        }

        return true;
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

    private static IEnumerable<int?> GameVersionFilterItems =>
        new int?[] { null }.Concat(GameInfo.FilteredSources.Games.Where(g => g.Value > 0).Select(g => (int?)g.Value));

    private static string GameVersionFilterText(int? value) => value is { } v
        ? GameInfo.FilteredSources.Games.FirstOrDefault(g => g.Value == v)?.Text ?? v.ToString()
        : "All (current format)";

    private static readonly bool?[] ShinyLockFilterItems = [null, true, false];

    private static string ShinyLockFilterText(bool? value) => value switch
    {
        null => "Any",
        true => "Shiny-locked only",
        _ => "Can be shiny"
    };

    private static readonly int?[] EncounterGroupFilterItems =
    [
        null,
        (int?)EncounterTypeGroup.Slot,
        (int?)EncounterTypeGroup.Static,
        (int?)EncounterTypeGroup.Mystery,
        (int?)EncounterTypeGroup.Trade,
        (int?)EncounterTypeGroup.Egg
    ];

    private static string EncounterGroupFilterText(int? value) => value switch
    {
        null => "All types",
        (int)EncounterTypeGroup.Slot => "Wild",
        (int)EncounterTypeGroup.Static => "Static / Gift",
        (int)EncounterTypeGroup.Mystery => "Mystery Gift",
        (int)EncounterTypeGroup.Trade => "Trade",
        (int)EncounterTypeGroup.Egg => "Egg",
        _ => value.ToString() ?? string.Empty
    };
}
