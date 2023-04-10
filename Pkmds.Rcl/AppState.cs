namespace Pkmds.Rcl;

public record AppState : IAppState
{
    private static readonly string[] SpeciesExcludes = new string[]
    {
        "Egg",
    };

    public AppState()
    {
        GameStrings = GameInfo.GetStrings("en-US");

        if (SpeciesNameDictionary.Count > 0)
        {
            return;
        }

        foreach (Species species in Enum.GetValues(typeof(Species)))
        {
            var name = SpeciesName.GetSpeciesName((ushort)species, (int)LanguageID.English);
            if (SpeciesExcludes.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            SpeciesNameDictionary.Add(species, name);
        }
    }

    public Dictionary<Species, string> SpeciesNameDictionary { get; } = new();

    public GameStrings? GameStrings { get; set; }

    public string[] GenderForms => new[] { string.Empty, "F", string.Empty };

    public event Action? OnAppStateChanged;

    public SaveFile? SaveFile { get; set; }

    public PKM? SelectedPokemon { get; set; }

    public int? SelectedBoxSlot { get; set; }

    public bool ShowProgressIndicator { get; set; }

    public string FileDisplayName { get; set; } = string.Empty;

    public void Refresh() => OnAppStateChanged?.Invoke();

    public void ClearSelection()
    {
        SelectedPokemon = null;
        SelectedBoxSlot = null;
        Refresh();
    }
}
