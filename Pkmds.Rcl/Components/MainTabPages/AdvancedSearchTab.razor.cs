namespace Pkmds.Rcl.Components.MainTabPages;

public partial class AdvancedSearchTab : RefreshAwareComponent
{
    private const string LocalStorageKey = "pkmds.search.filters";
    private readonly Dictionary<int, AbilitySummary?> loadedAbilities = [];
    private readonly Dictionary<int, ItemSummary?> loadedBalls = [];
    private readonly Dictionary<int, ItemSummary?> loadedHeldItems = [];
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
    private AbilitySummary? filterAbilityInfo;

    // ── Filter info state ─────────────────────────────────────────────────

    private ItemSummary? filterBallInfo;
    private ItemSummary? filterHeldItemInfo;
    private int? gender; // null=any, 0=male, 1=female, -1=genderless
    private bool hasSearched;
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

    // Appearance
    private int? type1;
    private int? type2;
    private byte? form;
    private int? teraType;
    private bool? isFavorite;
    private bool? isAlpha;
    private bool? isShadow;
    private bool? canGigantamax;
    private byte? dynamaxLevelMin;

    // Origin
    private ComboItem? metLocationItem;
    private DateTime? metDateMin;
    private DateTime? metDateMax;
    private int? pokerusState;

    // Ribbons
    private readonly List<string> selectedRibbons = [];
    private string? pendingRibbonName;

    // Markings — HashSet of indices (0=Circle..5=Diamond)
    private readonly HashSet<int> selectedMarkings = [];

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
        hasSearched = true;
        StateHasChanged();

        // Load descriptions in the background; StateHasChanged after so popovers populate.
        _ = LoadResultDescriptionsAsync();
    }

    private async Task LoadResultDescriptionsAsync()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        loadedAbilities.Clear();
        loadedHeldItems.Clear();
        loadedBalls.Clear();

        var version = saveFile.Version;

        var distinctAbilityIds = results.Select(r => r.Pokemon.Ability).Distinct().ToList();
        // Item IDs are context-specific (Gen 1/2/3/4/8b/9/9a all differ from the modern table).
        // Resolve the name with the source Pokémon's own context, then dedupe by ID — the
        // cache is keyed by ID and any duplicates within a single save will share a context.
        var distinctHeldItemEntries = results
            .Where(r => r.Pokemon.HeldItem > 0)
            .Select(r => (Id: r.Pokemon.HeldItem, Context: r.Pokemon.Context))
            .GroupBy(t => t.Id)
            .Select(g => g.First())
            .ToList();
        var distinctBallIds = results.Select(r => r.Pokemon.Ball)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var abilityResults = await Task.WhenAll(
            distinctAbilityIds.Select(async id => (id, info: await DescriptionService.GetAbilityInfoAsync(id, version))));
        var heldItemResults = await Task.WhenAll(
            distinctHeldItemEntries.Select(async entry =>
            {
                var items = GameInfo.Strings.GetItemStrings(entry.Context, version);
                return (id: entry.Id, info: await DescriptionService.GetItemInfoAsync(SafeNameLookup.Item(items, entry.Id), version));
            }));
        var ballResults = await Task.WhenAll(
            distinctBallIds.Select(async id =>
            {
                var ballItem = GameInfo.FilteredSources.Balls.FirstOrDefault(b => b.Value == id);
                var info = ballItem is not null
                    ? await DescriptionService.GetItemInfoAsync(ballItem.Text, version)
                    : null;
                return (id, info);
            }));

        foreach (var (id, info) in abilityResults)
        {
            loadedAbilities[id] = info;
        }

        foreach (var (id, info) in heldItemResults)
        {
            loadedHeldItems[id] = info;
        }

        foreach (var (id, info) in ballResults)
        {
            loadedBalls[id] = info;
        }

        await InvokeAsync(StateHasChanged);
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
        filterBallInfo = null;
        filterAbilityInfo = null;
        filterHeldItemInfo = null;
        originGame = null;
        hiddenPowerType = null;
        anyMoveItems = [];
        allMoveItems = [];
        type1 = null;
        type2 = null;
        form = null;
        teraType = null;
        isFavorite = null;
        isAlpha = null;
        isShadow = null;
        canGigantamax = null;
        dynamaxLevelMin = null;
        metLocationItem = null;
        metDateMin = null;
        metDateMax = null;
        pokerusState = null;
        selectedRibbons.Clear();
        pendingRibbonName = null;
        selectedMarkings.Clear();
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
            OriginGame = originGame is > 0
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
            HiddenPowerType = hiddenPowerType,
            Form = form,
            Type1 = type1,
            Type2 = type2,
            TeraType = teraType,
            IsFavorite = isFavorite,
            IsAlpha = isAlpha,
            IsShadow = isShadow,
            CanGigantamax = canGigantamax,
            DynamaxLevelMin = dynamaxLevelMin,
            MetLocation = metLocationItem is { Value: > 0 }
                ? metLocationItem.Value
                : null,
            MetDateMin = metDateMin.HasValue
                ? DateOnly.FromDateTime(metDateMin.Value)
                : null,
            MetDateMax = metDateMax.HasValue
                ? DateOnly.FromDateTime(metDateMax.Value)
                : null,
            PokerusState = pokerusState,
            RequiredRibbons = selectedRibbons.ToList(),
            RequiredMarkings = selectedMarkings.ToList()
        };

    // ── Row navigation ────────────────────────────────────────────────────

    private async Task JumpToResultAsync(AdvancedSearchResult row)
    {
        if (!await UnsavedChangesGuard.ConfirmAsync(
                AppService,
                DialogService,
                "This Pokémon has unsaved changes. Save them to the slot before jumping to the search result?",
                snackbar: Snackbar))
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

    private Task<IEnumerable<ComboItem>> SearchAbilitiesAsync(string search, CancellationToken ct)
    {
        if (AppState.SaveFile is null)
        {
            return Task.FromResult(Enumerable.Empty<ComboItem>());
        }

        var source = GameInfo.FilteredSources.Abilities
            .DistinctBy(a => a.Value);

        IEnumerable<ComboItem> results = string.IsNullOrWhiteSpace(search)
            ? source.OrderBy(a => a.Text).Take(30)
            : source
                .Where(a => a.Text.Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Text);

        return Task.FromResult(results);
    }

    private async Task OnBallFilterChanged(int? value)
    {
        ball = value;
        if (value is > 0 && AppState.SaveFile is { } sf)
        {
            var ballItem = GameInfo.FilteredSources.Balls.FirstOrDefault(b => b.Value == value);
            filterBallInfo = ballItem is not null
                ? await DescriptionService.GetItemInfoAsync(ballItem.Text, sf.Version)
                : null;
        }
        else
        {
            filterBallInfo = null;
        }
    }

    private async Task OnAbilitySelected(ComboItem? item)
    {
        abilityItem = item;
        abilityId = item is { Value: > 0 }
            ? item.Value
            : null;
        if (abilityId.HasValue && AppState.SaveFile is { } sf)
        {
            filterAbilityInfo = await DescriptionService.GetAbilityInfoAsync(abilityId.Value, sf.Version);
        }
        else
        {
            filterAbilityInfo = null;
        }
    }

    // ── Form / Tera / Flags / Origin helpers ──────────────────────────────

    private IReadOnlyList<string> GetFormList()
    {
        if (AppState.SaveFile is not { } sf || speciesItem is not { Value: > 0 } sp)
        {
            return [];
        }

        var forms = FormConverter.GetFormList(
            (ushort)sp.Value,
            GameInfo.Strings.types,
            GameInfo.Strings.forms,
            GameInfo.GenderSymbolUnicode,
            sf.Context);
        return forms.Any(f => !string.IsNullOrEmpty(f))
            ? forms
            : [];
    }

    private bool SaveSupportsTeraType() =>
        AppState.SaveFile?.BlankPKM is ITeraType;

    private bool SaveSupportsFavorite() =>
        AppState.SaveFile?.BlankPKM is IFavorite;

    private bool SaveSupportsAlpha() =>
        AppState.SaveFile?.BlankPKM is IAlpha;

    private bool SaveSupportsShadow() =>
        AppState.SaveFile?.BlankPKM is IShadowCapture;

    private bool SaveSupportsGigantamax() =>
        AppState.SaveFile?.BlankPKM is IGigantamax;

    private bool SaveSupportsDynamaxLevel() =>
        AppState.SaveFile?.BlankPKM is IDynamaxLevel;

    private async Task<IEnumerable<ComboItem>> SearchMetLocationsAsync(string search, CancellationToken ct) =>
        AppState.SaveFile is not { } sf
            ? []
            : await Task.FromResult(AppService.SearchMetLocations(search, sf.Version, sf.Context));

    private IReadOnlyList<string> GetAvailableRibbonNames()
    {
        if (AppState.SaveFile is not { } sf)
        {
            return [];
        }

        return [.. RibbonHelper.GetAllRibbonInfo(sf.BlankPKM)
            .Select(r => r.Name)
            .Where(n => !selectedRibbons.Contains(n))
            .Distinct()
            .OrderBy(RibbonHelper.GetRibbonDisplayName)];
    }

    private async Task<IEnumerable<string>> SearchRibbonNamesAsync(string search, CancellationToken ct)
    {
        var names = GetAvailableRibbonNames();
        if (string.IsNullOrWhiteSpace(search))
        {
            return await Task.FromResult(names.Take(30).ToList());
        }

        return await Task.FromResult(names
            .Where(n => RibbonHelper.GetRibbonDisplayName(n)
                .Contains(search, StringComparison.OrdinalIgnoreCase))
            .Take(30)
            .ToList());
    }

    private void AddRibbon(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || selectedRibbons.Contains(name))
        {
            return;
        }

        selectedRibbons.Add(name);
        pendingRibbonName = null;
    }

    private void RemoveRibbon(string name) => selectedRibbons.Remove(name);

    private static readonly (int Index, string Symbol, string Label)[] AllMarkingShapes =
    [
        ((int)MarkingsHelper.Markings.Circle, MarkingsHelper.Circle, "Circle"),
        ((int)MarkingsHelper.Markings.Triangle, MarkingsHelper.Triangle, "Triangle"),
        ((int)MarkingsHelper.Markings.Square, MarkingsHelper.Square, "Square"),
        ((int)MarkingsHelper.Markings.Heart, MarkingsHelper.Heart, "Heart"),
        ((int)MarkingsHelper.Markings.Star, MarkingsHelper.Star, "Star"),
        ((int)MarkingsHelper.Markings.Diamond, MarkingsHelper.Diamond, "Diamond"),
    ];

    private IEnumerable<(int Index, string Symbol, string Label)> GetAvailableMarkingShapes()
    {
        // Gen 3 only has 4 markings (no Star/Diamond). Gate the UI on the save's
        // blank PKM so users can't pick indices that will never match.
        if (AppState.SaveFile?.BlankPKM is not IAppliedMarkings marks)
        {
            return [];
        }

        return AllMarkingShapes.Where(s => s.Index < marks.MarkingCount);
    }

    private void ToggleMarking(int index)
    {
        Haptics.Tap();
        if (!selectedMarkings.Add(index))
        {
            selectedMarkings.Remove(index);
        }
    }

    private async Task OnHeldItemFilterChanged(ComboItem? v)
    {
        heldItemId = v is { Value: > 0 }
            ? v.Value
            : null;
        if (heldItemId.HasValue && AppState.SaveFile is { } sf)
        {
            // The filter dropdown sources from FilteredSources.Items, whose Value is in the
            // save's context-specific item ID space — look the name up in the same space.
            var items = GameInfo.Strings.GetItemStrings(sf.Context, sf.Version);
            var name = heldItemId.Value < items.Length
                ? items[heldItemId.Value]
                : null;
            filterHeldItemInfo = name is not null
                ? await DescriptionService.GetItemInfoAsync(name, sf.Version)
                : null;
        }
        else
        {
            filterHeldItemInfo = null;
        }
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
        if (name is null || !savedFilters.Remove(name))
        {
            return;
        }

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
        nature = f.Nature;
        isLegal = f.IsLegal;
        levelMin = f.LevelMin;
        levelMax = f.LevelMax;
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
        form = f.Form;
        type1 = f.Type1;
        type2 = f.Type2;
        teraType = f.TeraType;
        isFavorite = f.IsFavorite;
        isAlpha = f.IsAlpha;
        isShadow = f.IsShadow;
        canGigantamax = f.CanGigantamax;
        dynamaxLevelMin = f.DynamaxLevelMin;
        metLocationItem = null;
        if (f.MetLocation is > 0 && AppState.SaveFile is { } sf)
        {
            metLocationItem = AppService.GetMetLocationComboItem(
                (ushort)f.MetLocation.Value, sf.Version, sf.Context);
        }
        metDateMin = f.MetDateMin?.ToDateTime(TimeOnly.MinValue);
        metDateMax = f.MetDateMax?.ToDateTime(TimeOnly.MinValue);
        pokerusState = f.PokerusState;
        selectedRibbons.Clear();
        selectedRibbons.AddRange(f.RequiredRibbons);
        pendingRibbonName = null;
        selectedMarkings.Clear();
        foreach (var markingIndex in f.RequiredMarkings)
        {
            selectedMarkings.Add(markingIndex);
        }
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

    private static readonly bool?[] BoolFilterItems = [null, true, false];

    private static string BoolFilterText(bool? value, string trueLabel, string falseLabel) => value switch
    {
        null => "Any",
        true => trueLabel,
        _ => falseLabel
    };

    private static IEnumerable<int?> TypeFilterItems =>
        new int?[] { null }.Concat(Enumerable.Range(0, 18).Select(i => (int?)i));

    private static string TypeFilterText(int? value) =>
        value is { } v ? GameInfo.Strings.Types[v] : "Any";

    private static IEnumerable<int?> TeraTypeFilterItems =>
        TypeFilterItems.Append((int?)TeraTypeUtil.Stellar);

    private static string TeraTypeFilterText(int? value) => value switch
    {
        null => "Any",
        TeraTypeUtil.Stellar => GameInfo.Strings.Types[TeraTypeUtil.StellarTypeDisplayStringIndex],
        var v => GameInfo.Strings.Types[v.Value]
    };

    private static IEnumerable<int?> GenderFilterItems => [null, 0, 1, -1];

    private static string GenderFilterText(int? value) => value switch
    {
        null => "Any",
        0 => "Male",
        1 => "Female",
        _ => "Genderless"
    };

    private static IEnumerable<int?> NatureFilterItems =>
        new int?[] { null }.Concat(GameInfo.FilteredSources.Natures.Select(n => (int?)n.Value));

    private static string NatureFilterText(int? value) => value is { } v
        ? GameInfo.FilteredSources.Natures.FirstOrDefault(n => n.Value == v)?.Text ?? v.ToString()
        : "Any";

    private static IEnumerable<int?> BallFilterItems =>
        new int?[] { null }.Concat(GameInfo.FilteredSources.Balls.Select(b => (int?)b.Value));

    private static string BallFilterText(int? value) => value is { } v
        ? GameInfo.FilteredSources.Balls.FirstOrDefault(b => b.Value == v)?.Text ?? v.ToString()
        : "Any";

    private static IEnumerable<int?> OriginGameFilterItems =>
        new int?[] { null }.Concat(GameInfo.FilteredSources.Games.Where(g => g.Value > 0).Select(g => (int?)g.Value));

    private static string OriginGameFilterText(int? value) => value is { } v
        ? GameInfo.FilteredSources.Games.FirstOrDefault(g => g.Value == v)?.Text ?? v.ToString()
        : "Any";

    private IEnumerable<int?> LanguageFilterItems =>
        new int?[] { null }.Concat(
            GameInfo.LanguageDataSource(AppState.SaveFile?.Generation ?? 3, AppState.SaveFile?.Context ?? EntityContext.Gen3)
                .Select(l => (int?)l.Value));

    private string LanguageFilterText(int? value) => value is { } v
        ? GameInfo.LanguageDataSource(AppState.SaveFile?.Generation ?? 3, AppState.SaveFile?.Context ?? EntityContext.Gen3)
              .FirstOrDefault(l => l.Value == v)?.Text ?? v.ToString()
        : "Any";

    private static IEnumerable<int?> HiddenPowerFilterItems =>
        new int?[] { null }.Concat(Enumerable.Range(0, HiddenPower.TypeCount).Select(i => (int?)i));

    private static string HiddenPowerFilterText(int? value) =>
        value is { } v ? GameInfo.Strings.Types[v + 1] : "Any";

    private static IEnumerable<int?> PokerusFilterItems => [null, 0, 1, 2];

    private static string PokerusFilterText(int? value) => value switch
    {
        null => "Any",
        0 => "Never infected",
        1 => "Currently infected",
        _ => "Cured"
    };
}
