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

    public PKM? EditFormPokemon
    {
        get => editFormPokemon;
        set
        {
            editFormPokemon = value?.Clone();
            LoadPokemonStats(editFormPokemon);
        }
    }

    public event Action? OnAppStateChanged;
    public event Action? OnBoxStateChanged;
    public event Action? OnPartyStateChanged;

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
    private PKM? editFormPokemon;

    public int? SelectedBoxNumber { get; set; }

    public int? SelectedBoxSlotNumber { get; set; }

    public int? SelectedPartySlotNumber { get; set; }

    public bool ShowProgressIndicator { get; set; }

    public string FileDisplayName { get; set; } = string.Empty;

    public IEnumerable<ComboItem> SearchPokemonNames(string searchString) => SaveFile is null || searchString is not { Length: > 0 }
        ? Enumerable.Empty<ComboItem>()
        : GameInfo.FilteredSources.Species
            .Where(species => species.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(species => species.Text);

    public ComboItem GetSpeciesComboItem(ushort speciesId) => GameInfo.FilteredSources.Species
        .FirstOrDefault(species => species.Value == speciesId) ?? default!;

    public IEnumerable<ComboItem> SearchItemNames(string searchString) => SaveFile is null || searchString is not { Length: > 0 }
        ? Enumerable.Empty<ComboItem>()
        : GameInfo.FilteredSources.Items
            .Where(item => item.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Text);

    public ComboItem GetItemComboItem(int itemId) => GameInfo.FilteredSources.Items
        .FirstOrDefault(item => item.Value == itemId) ?? default!;

    public IEnumerable<ComboItem> SearchAbilityNames(string searchString) => SaveFile is null || searchString is not { Length: > 0 }
        ? Enumerable.Empty<ComboItem>()
        : GameInfo.FilteredSources.Abilities
            .Where(ability => ability.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(ability => ability.Text);

    public ComboItem GetAbilityComboItem(int abilityId) => GameInfo.FilteredSources.Abilities
        .FirstOrDefault(ability => ability.Value == abilityId) ?? default!;

    public void Refresh() => OnAppStateChanged?.Invoke();

    public void ClearSelection()
    {
        SelectedBoxNumber = null;
        SelectedBoxSlotNumber = null;
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

    public void LoadPokemonStats(PKM? pokemon)
    {
        if (SaveFile is null || pokemon is null)
        {
            return;
        }

        var pt = SaveFile.Personal;
        var pi = pt.GetFormEntry(pokemon.Species, pokemon.Form);
        Span<ushort> stats = stackalloc ushort[6];
        pokemon.LoadStats(pi, stats);
        pokemon.SetStats(stats);
    }

    public IEnumerable<ComboItem> SearchMetLocations(string searchString, bool isEggLocation = false) => SaveFile is null || searchString is not { Length: > 0 }
        ? Enumerable.Empty<ComboItem>()
        : GameInfo.GetLocationList(SaveFile.Version, SaveFile.Context, isEggLocation)
            .Where(ability => ability.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(ability => ability.Text);

    public ComboItem GetMetLocationComboItem(int metLocationId, bool isEggLocation = false) => SaveFile is null
        ? default!
        : GameInfo.GetLocationList(SaveFile.Version, SaveFile.Context, isEggLocation)
            .FirstOrDefault(metLocation => metLocation.Value == metLocationId) ?? default!;

    public IEnumerable<ComboItem> SearchMoves(string searchString) => SaveFile is null || searchString is not { Length: > 0 }
        ? Enumerable.Empty<ComboItem>()
        : GameInfo.FilteredSources.Moves
            .Where(move => move.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(move => move.Text);

    public ComboItem GetMoveComboItem(int moveId) => GameInfo.FilteredSources.Moves
        .FirstOrDefault(metLocation => metLocation.Value == moveId) ?? default!;

    public bool GetMarking(PKM? pokemon, int index) =>
        pokemon is not null && index <= pokemon.MarkingCount - 1 && pokemon.GetMarking(index) == 1;

    public void SetMarking(PKM? pokemon, int index, bool value) =>
        pokemon?.SetMarking(index, value ? 1 : 0);

    public void ToggleMarking(PKM? pokemon, int index) =>
        pokemon?.ToggleMarking(index);

    public string GetCharacteristic(PKM? pokemon) =>
        pokemon?.Characteristic is int characteristicIndex &&
        characteristicIndex > -1 &&
        GameInfo.Strings.characteristics is { Length: > 0 } characteristics &&
        characteristicIndex < characteristics.Length
            ? characteristics[characteristicIndex]
            : string.Empty;

    public void SavePokemon(PKM? pokemon)
    {
        if (SaveFile is null || pokemon is null)
        {
            return;
        }

        if (SelectedPartySlotNumber is not null)
        {
            SaveFile.SetPartySlotAtIndex(pokemon, SelectedPartySlotNumber.Value);
            OnPartyStateChanged?.Invoke();
        }
        else if (SelectedBoxNumber is not null && SelectedBoxSlotNumber is not null)
        {
            SaveFile.SetBoxSlotAtIndex(pokemon, SelectedBoxNumber.Value, SelectedBoxSlotNumber.Value);
            OnBoxStateChanged?.Invoke();
        }
    }

    private const string defaultFileName = "pkm.bin";

    public string GetCleanFileName(PKM pkm) => pkm.Context switch
    {
        EntityContext.SplitInvalid or EntityContext.MaxInvalid => defaultFileName,
        EntityContext.Gen1 or EntityContext.Gen2 => pkm switch
        {
            PK1 pk1 => $"{GameInfo.GetStrings("en").Species[pk1.Species]}_{pk1.DV16}.{pk1.Extension}",
            PK2 pk2 => $"{GameInfo.GetStrings("en").Species[pk2.Species]}_{pk2.DV16}.{pk2.Extension}",
            _ => defaultFileName,
        },
        _ => $"{GameInfo.GetStrings("en").Species[pkm.Species]}_{pkm.PID:X}.{pkm.Extension}",
    };

    public void SetSelectedBoxPokemon(PKM? pkm, int boxNumber, int slotNumber)
    {
        SelectedPartySlotNumber = null;

        if (pkm is not { Species: > 0 })
        {
            SelectedBoxNumber = null;
            SelectedBoxSlotNumber = null;
            EditFormPokemon = null;
        }
        else
        {
            SelectedBoxNumber = boxNumber;
            SelectedBoxSlotNumber = slotNumber;
            EditFormPokemon = pkm;
        }
        Refresh();
    }

    public void SetSelectedPartyPokemon(PKM? pkm, int slotNumber)
    {
        SelectedBoxNumber = null;
        SelectedBoxSlotNumber = null;

        if (pkm is not { Species: > 0 })
        {
            SelectedPartySlotNumber = null;
            EditFormPokemon = null;
        }
        else
        {
            SelectedPartySlotNumber = slotNumber;
            EditFormPokemon = pkm;
        }
        Refresh();
    }
}
