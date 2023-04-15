namespace Pkmds.Rcl;

public record AppState : IAppState
{
    public AppState()
    {
        GameStrings = GameInfo.GetStrings(CurrentLanguageId);
    }

    public int CurrentLanguageId { get; set; } = (int)LanguageID.English;

    public GameStrings? GameStrings { get; set; }

    public string[] GenderForms => new[] { string.Empty, "F", string.Empty };

    public event Action? OnAppStateChanged;

    public SaveFile? SaveFile { get; set; }

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
    public async Task<IEnumerable<ushort>> SearchPokemonNames(
        EntityContext generationContext, string searchString) =>
        SpeciesName.SpeciesLang[CurrentLanguageId]
            .Skip(1) // Skip 'Egg'
            .Take(SaveFile?.MaxSpeciesID ?? (ushort)Species.MAX_COUNT)
            .Where(speciesName => speciesName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .Order()
            .Select(speciesName => (ushort)SpeciesName.GetSpeciesID(speciesName, CurrentLanguageId));
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    public string ConvertSpeciesToName(ushort species) =>
        SpeciesName.GetSpeciesName(species, CurrentLanguageId);

    public void Refresh() => OnAppStateChanged?.Invoke();

    public void ClearSelection()
    {
        SelectedPokemon = null;
        SelectedBoxSlot = null;
        Refresh();
    }

    public string[] NatureStatShortNames => new[] { "Atk", "Def", "Spe", "SpA", "SpD" };

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
}
