namespace Pkmds.Rcl.Components.MainTabPages;

public partial class AdvancedSearchTab : RefreshAwareComponent
{
    private const string LocalStorageKey = "pkmds.search.filters";
    private int? abilityId;

    // ── Ability autocomplete ──────────────────────────────────────────────

    private ComboItem? abilityItem;
    private MudAutocomplete<ComboItem>? allMoveAutoRef;
    private List<ComboItem> allMoveItems = [];

    // ── Move chips ────────────────────────────────────────────────────────

    private MudAutocomplete<ComboItem>? anyMoveAutoRef;

    // Moves
    private List<ComboItem> anyMoveItems = [];
    private int? ball;
    private int? gender; // null=any, 0=male, 1=female, -1=genderless
    private int? heldItemId;
    private int? hiddenPowerType;
    private int? hpEvMin, atkEvMin, defEvMin, spaEvMin, spdEvMin, speEvMin;
    private int? hpIvMin, atkIvMin, defIvMin, spaIvMin, spdIvMin, speIvMin;
    private bool? isEgg;
    private bool? isLegal;

    private bool isSearching;

    // Basic
    private bool? isShiny;
    private int? languageId;

    private int? levelMax;

    // Stats
    private int? levelMin;
    private int? nature; // null=any, 0-24

    private int? originGame;

    // Trainer
    private string otName = string.Empty;

    // ── Search state ──────────────────────────────────────────────────────

    private List<AdvancedSearchResult> results = [];

    // Saved filters
    private Dictionary<string, AdvancedSearchFilter> savedFilters = [];
    private string saveFilterName = string.Empty;
    private HashSet<AdvancedSearchResult> selectedRows = [];
    private string? selectedSavedFilter;

    // ── UI backing fields ─────────────────────────────────────────────────
    // Species
    private ComboItem? speciesItem;
    private uint? trainerId;

    /// <summary>Callback invoked after a result row is clicked to jump to the Party / Box tab.</summary>
    [Parameter]
    public EventCallback OnJumpToPartyBox { get; set; }

    // ── Computed props ────────────────────────────────────────────────────

    private bool HasResults => results.Count > 0;
    private bool HasSelection => selectedRows.Count > 0;
    private int ResultCount => results.Count;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadSavedFiltersAsync();
    }

    // ── Search ────────────────────────────────────────────────────────────

    private async Task RunSearchAsync()
    {
        if (AppState.SaveFile is null)
        {
            return;
        }

        isSearching = true;
        selectedRows = [];
        StateHasChanged();

        // Yield so the spinner renders before the CPU-bound sweep.
        await Task.Yield();

        var filter = BuildFilter();
        results = AppService.SearchPokemon(filter).ToList();

        isSearching = false;
        StateHasChanged();
    }

    private void ResetFilter()
    {
        speciesItem = null;
        isShiny = null;
        isEgg = null;
        gender = null;
        nature = null;
        isLegal = null;
        levelMin = null;
        levelMax = null;
        hpIvMin = atkIvMin = defIvMin = spaIvMin = spdIvMin = speIvMin = null;
        hpEvMin = atkEvMin = defEvMin = spaEvMin = spdEvMin = speEvMin = null;
        otName = string.Empty;
        trainerId = null;
        languageId = null;
        ball = null;
        abilityId = null;
        heldItemId = null;
        originGame = null;
        hiddenPowerType = null;
        anyMoveItems = [];
        allMoveItems = [];
        results = [];
        selectedRows = [];
    }

    private AdvancedSearchFilter BuildFilter() =>
        new()
        {
            Species = speciesItem is { Value: > 0 }
                ? (ushort)speciesItem.Value
                : null,
            IsShiny = isShiny,
            IsEgg = isEgg,
            Gender = gender,
            Nature = nature.HasValue
                ? (byte)nature.Value
                : null,
            Ability = abilityId,
            HeldItem = heldItemId,
            Ball = ball,
            OriginGame = originGame.HasValue && originGame.Value > 0
                ? (GameVersion)originGame.Value
                : null,
            IsLegal = isLegal,
            OriginalTrainerName = string.IsNullOrWhiteSpace(otName)
                ? null
                : otName.Trim(),
            TrainerId = trainerId,
            LanguageId = languageId,
            LevelMin = levelMin.HasValue
                ? (byte)levelMin.Value
                : null,
            LevelMax = levelMax.HasValue
                ? (byte)levelMax.Value
                : null,
            HpIvMin = hpIvMin,
            AtkIvMin = atkIvMin,
            DefIvMin = defIvMin,
            SpaIvMin = spaIvMin,
            SpdIvMin = spdIvMin,
            SpeIvMin = speIvMin,
            HpEvMin = hpEvMin,
            AtkEvMin = atkEvMin,
            DefEvMin = defEvMin,
            SpaEvMin = spaEvMin,
            SpdEvMin = spdEvMin,
            SpeEvMin = speEvMin,
            AnyMoves = anyMoveItems.Select(m => (ushort)m.Value).ToList(),
            AllMoves = allMoveItems.Select(m => (ushort)m.Value).ToList(),
            HiddenPowerType = hiddenPowerType
        };

    // ── Row click ─────────────────────────────────────────────────────────

    private async Task OnRowClickAsync(TableRowClickEventArgs<AdvancedSearchResult> args)
    {
        if (args.Item is not { } row)
        {
            return;
        }

        if (row.IsParty)
        {
            AppService.SetSelectedPartyPokemon(row.Pokemon, row.SlotNumber);
        }
        else if (AppState.SaveFile is SAV7b lgsave)
        {
            // Let's Go: convert box+slot to flat index.
            var flatSlot = row.BoxNumber * lgsave.BoxSlotCount + row.SlotNumber;
            AppService.SetSelectedLetsGoPokemon(row.Pokemon, flatSlot);
        }
        else
        {
            AppService.SetSelectedBoxPokemon(row.Pokemon, row.BoxNumber, row.SlotNumber);
        }

        await OnJumpToPartyBox.InvokeAsync();
    }

    private async Task<IEnumerable<ComboItem>> SearchMovesAsync(string search, CancellationToken ct) =>
        await Task.FromResult(AppService.SearchMoves(search));

    private async Task AddAnyMove(ComboItem? item)
    {
        if (item is not { Value: > 0 } || anyMoveItems.Any(m => m.Value == item.Value))
        {
            return;
        }

        anyMoveItems = [.. anyMoveItems, item];
        if (anyMoveAutoRef is not null)
        {
            await anyMoveAutoRef.ClearAsync();
        }
    }

    private void RemoveAnyMove(ComboItem item) =>
        anyMoveItems = anyMoveItems.Where(m => m.Value != item.Value).ToList();

    private async Task AddAllMove(ComboItem? item)
    {
        if (item is not { Value: > 0 } || allMoveItems.Any(m => m.Value == item.Value))
        {
            return;
        }

        allMoveItems = [.. allMoveItems, item];
        if (allMoveAutoRef is not null)
        {
            await allMoveAutoRef.ClearAsync();
        }
    }

    private void RemoveAllMove(ComboItem item) =>
        allMoveItems = allMoveItems.Where(m => m.Value != item.Value).ToList();

    // ── Species autocomplete ──────────────────────────────────────────────

    private async Task<IEnumerable<ComboItem>> SearchSpeciesAsync(string search, CancellationToken ct) =>
        await Task.FromResult(AppService.SearchPokemonNames(search));

    private static string SpeciesItemToString(ComboItem? item) => item?.Text ?? string.Empty;

    // ── Item autocomplete ─────────────────────────────────────────────────

    private async Task<IEnumerable<ComboItem>> SearchItemsAsync(string search, CancellationToken ct) =>
        await Task.FromResult(AppService.SearchItemNames(search));

    private static string ItemToString(ComboItem? item) => item?.Text ?? string.Empty;

    private async Task<IEnumerable<ComboItem>> SearchAbilitiesAsync(string search, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(search) || AppState.SaveFile is null)
        {
            return [];
        }

        return await Task.FromResult(
            GameInfo.FilteredSources.Abilities
                .Where(a => a.Text.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Take(20)
        );
    }

    private void OnAbilitySelected(ComboItem? item)
    {
        abilityItem = item;
        abilityId = item is { Value: > 0 }
            ? item.Value
            : null;
    }

    // ── Batch operations ──────────────────────────────────────────────────

    private async Task CopyShowdownAsync()
    {
        var sb = new StringBuilder();
        foreach (var row in selectedRows)
        {
            sb.AppendLine(AppService.ExportPokemonAsShowdown(row.Pokemon));
            sb.AppendLine();
        }

        var text = sb.ToString().Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        try
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
            Snackbar.Add("Copied Showdown text to clipboard.", Severity.Success);
        }
        catch
        {
            Snackbar.Add("Clipboard unavailable — check browser permissions.", Severity.Warning);
        }
    }

    // ── Saved filters (localStorage) ──────────────────────────────────────

    private async Task LoadSavedFiltersAsync()
    {
        try
        {
            var json = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", LocalStorageKey);
            if (json is not null)
            {
                savedFilters = JsonSerializer.Deserialize<Dictionary<string, AdvancedSearchFilter>>(json) ?? [];
            }
        }
        catch
        {
            // Ignore localStorage failures (e.g., private browsing mode).
        }
    }

    private async Task SaveCurrentFilterAsync()
    {
        var name = saveFilterName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Snackbar.Add("Enter a name before saving.", Severity.Warning);
            return;
        }

        savedFilters[name] = BuildFilter();
        await PersistFiltersAsync();
        Snackbar.Add($"Filter \"{name}\" saved.", Severity.Success);
    }

    private async Task DeleteSavedFilterAsync()
    {
        var name = selectedSavedFilter;
        if (name is null || !savedFilters.ContainsKey(name))
        {
            return;
        }

        savedFilters.Remove(name);
        selectedSavedFilter = null;
        await PersistFiltersAsync();
        Snackbar.Add($"Filter \"{name}\" deleted.", Severity.Success);
    }

    private void LoadSavedFilter(string? name)
    {
        selectedSavedFilter = name;
        if (name is null || !savedFilters.TryGetValue(name, out var f))
        {
            return;
        }

        ApplyFilterToUi(f);
    }

    private void ApplyFilterToUi(AdvancedSearchFilter f)
    {
        speciesItem = f.Species.HasValue
            ? AppService.GetSpeciesComboItem(f.Species.Value)
            : null;
        isShiny = f.IsShiny;
        isEgg = f.IsEgg;
        gender = f.Gender;
        nature = f.Nature.HasValue
            ? f.Nature.Value
            : null;
        isLegal = f.IsLegal;
        levelMin = f.LevelMin.HasValue
            ? f.LevelMin.Value
            : null;
        levelMax = f.LevelMax.HasValue
            ? f.LevelMax.Value
            : null;
        hpIvMin = f.HpIvMin;
        atkIvMin = f.AtkIvMin;
        defIvMin = f.DefIvMin;
        spaIvMin = f.SpaIvMin;
        spdIvMin = f.SpdIvMin;
        speIvMin = f.SpeIvMin;
        hpEvMin = f.HpEvMin;
        atkEvMin = f.AtkEvMin;
        defEvMin = f.DefEvMin;
        spaEvMin = f.SpaEvMin;
        spdEvMin = f.SpdEvMin;
        speEvMin = f.SpeEvMin;
        otName = f.OriginalTrainerName ?? string.Empty;
        trainerId = f.TrainerId;
        languageId = f.LanguageId;
        ball = f.Ball;
        abilityId = f.Ability;
        abilityItem = null;
        heldItemId = f.HeldItem;
        originGame = f.OriginGame.HasValue
            ? (int)f.OriginGame.Value
            : null;
        hiddenPowerType = f.HiddenPowerType;
        anyMoveItems = f.AnyMoves
            .Select(id => AppService.GetMoveComboItem(id))
            .Where(c => c is { Value: > 0 })
            .ToList();
        allMoveItems = f.AllMoves
            .Select(id => AppService.GetMoveComboItem(id))
            .Where(c => c is { Value: > 0 })
            .ToList();
        results = [];
        selectedRows = [];
    }

    private async Task PersistFiltersAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(savedFilters);
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, json);
        }
        catch
        {
            // Ignore localStorage failures.
        }
    }

    // ── Helper: table row style ───────────────────────────────────────────

    private static string RowStyleFunc(AdvancedSearchResult _, int __) => "cursor: pointer;";
}
