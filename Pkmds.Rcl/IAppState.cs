namespace Pkmds.Rcl;

public interface IAppState
{
    public event Action? OnAppStateChanged;

    string CurrentLanguage { get; set; }

    int CurrentLanguageId { get; set; }

    Task<IEnumerable<ushort>> SearchPokemonNames(string searchString);

    string ConvertSpeciesToName(ushort species);

    string[] GenderForms { get; }

    SaveFile? SaveFile { get; set; }

    PKM? SelectedPokemon { get; set; }

    int? SelectedBoxSlot { get; set; }

    bool ShowProgressIndicator { get; set; }

    string FileDisplayName { get; set; }

    void Refresh();

    void ClearSelection();

    string[] NatureStatShortNames { get; }

    string GetStatModifierString(int nature);

    void LoadPokemonStats();

    Task<IEnumerable<int>> SearchItemNames(string searchString);

    string ConvertItemToName(int item);

    Task<IEnumerable<int>> SearchAbilityNames(string searchString);

    string ConvertAbilityToName(int ability);
}
