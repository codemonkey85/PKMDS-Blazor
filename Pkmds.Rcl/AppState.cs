namespace Pkmds.Rcl;

public record AppState : IAppState
{
    public AppState()
    {
        LocalizeUtil.InitializeStrings(CurrentLanguage, SaveFile);
    }

    public string CurrentLanguage
    {
        get => currentLanguage;
        set
        {
            currentLanguage = value;
            LocalizeUtil.InitializeStrings(CurrentLanguage, SaveFile);
        }
    }

    public int CurrentLanguageId => SaveFile?.Language ?? (int)LanguageID.English;

    public event Action? OnAppStateChanged;

    public SaveFile? SaveFile
    {
        get => saveFile;
        set
        {
            saveFile = value;
            LocalizeUtil.InitializeStrings(CurrentLanguage, SaveFile);
        }
    }

    private string currentLanguage = GameLanguage.DefaultLanguage;
    private SaveFile? saveFile;
    private PKM? _selectedPokemon;

    public PKM? SelectedPokemon
    {
        get => _selectedPokemon;
        set
        {
            _selectedPokemon = value;
            LoadPokemonStats();
        }
    }

    public int? SelectedBoxSlot { get; set; }

    public bool ShowProgressIndicator { get; set; }

    public string FileDisplayName { get; set; } = string.Empty;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<IEnumerable<ComboItem>> SearchPokemonNames(string searchString) => SaveFile is null || searchString is not { Length: > 0 }
        ? Enumerable.Empty<ComboItem>()
        : GameInfo.FilteredSources.Species
            .Where(species => species.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(species => species.Text);
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    public ComboItem GetSpeciesComboItem(ushort speciesId) => GameInfo.FilteredSources.Species
        .FirstOrDefault(species => species.Value == speciesId) ?? default!;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<IEnumerable<ComboItem>> SearchItemNames(string searchString) => SaveFile is null || searchString is not { Length: > 0 }
        ? Enumerable.Empty<ComboItem>()
        : GameInfo.FilteredSources.Items
            .Where(item => item.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Text);
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    public ComboItem GetItemComboItem(int itemId) => GameInfo.FilteredSources.Items
        .FirstOrDefault(item => item.Value == itemId) ?? default!;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<IEnumerable<ComboItem>> SearchAbilityNames(string searchString) => SaveFile is null || searchString is not { Length: > 0 }
        ? Enumerable.Empty<ComboItem>()
        : GameInfo.FilteredSources.Abilities
            .Where(ability => ability.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(ability => ability.Text);
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    public ComboItem GetAbilityComboItem(int abilityId) => GameInfo.FilteredSources.Abilities
        .FirstOrDefault(ability => ability.Value == abilityId) ?? default!;

    public void Refresh() => OnAppStateChanged?.Invoke();

    public void ClearSelection()
    {
        SelectedPokemon = null;
        SelectedBoxSlot = null;
        Refresh();
    }

    public string[] NatureStatShortNames => new[] { "Atk", "Def", "Spe", "SpA", "SpD" };

    public bool IsPurificationVisible => false;

    public bool IsSizeVisible => false;

    public string GetStatModifierString(int nature)
    {
        var (up, down) = NatureAmp.GetNatureModification(nature);
        return up == down ? string.Empty : $"({NatureStatShortNames[up]} ↑, {NatureStatShortNames[down]} ↓)";
    }

    public void LoadPokemonStats()
    {
        if (SaveFile is null || _selectedPokemon is null)
        {
            return;
        }

        var pt = SaveFile.Personal;
        var pi = pt.GetFormEntry(_selectedPokemon.Species, _selectedPokemon.Form);
        Span<ushort> stats = stackalloc ushort[6];
        _selectedPokemon.LoadStats(pi, stats);
        _selectedPokemon.SetStats(stats);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<IEnumerable<ComboItem>> SearchMetLocations(string searchString, bool isEggLocation = false) => SaveFile is null || searchString is not { Length: > 0 }
        ? Enumerable.Empty<ComboItem>()
        : GameInfo.GetLocationList(SaveFile.Version, SaveFile.Context, isEggLocation)
            .Where(ability => ability.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(ability => ability.Text);
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    public ComboItem GetMetLocationComboItem(int metLocationId, bool isEggLocation = false) => SaveFile is null
        ? default!
        : GameInfo.GetLocationList(SaveFile.Version, SaveFile.Context, isEggLocation)
            .FirstOrDefault(metLocation => metLocation.Value == metLocationId) ?? default!;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<IEnumerable<ComboItem>> SearchMoves(string searchString) => SaveFile is null || searchString is not { Length: > 0 }
        ? Enumerable.Empty<ComboItem>()
        : GameInfo.FilteredSources.Moves
            .Where(move => move.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(move => move.Text);
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    public ComboItem GetMoveComboItem(int moveId) => GameInfo.FilteredSources.Moves
        .FirstOrDefault(metLocation => metLocation.Value == moveId) ?? default!;

    public bool GetMarking(int index) =>
        SelectedPokemon is not null && index <= SelectedPokemon.MarkingCount - 1 && SelectedPokemon.GetMarking(index) == 1;

    public void SetMarking(int index, bool value) =>
        SelectedPokemon?.SetMarking(index, value ? 1 : 0);

    public void ToggleMarking(int index) =>
        SelectedPokemon?.ToggleMarking(index);

    public string GetCharacteristic() =>
        SelectedPokemon?.Characteristic is int characteristicIndex &&
        characteristicIndex > -1 &&
        GameInfo.Strings.characteristics is { Length: > 0 } characteristics &&
        characteristicIndex < characteristics.Length
            ? characteristics[characteristicIndex]
            : string.Empty;
}
