namespace Pkmds.Rcl;

public interface IAppState
{
    public event Action? OnAppStateChanged;

    string CurrentLanguage { get; set; }

    int CurrentLanguageId { get; set; }

    Task<IEnumerable<ComboItem>> SearchPokemonNames(string searchString);

    ComboItem GetSpeciesComboItem(ushort speciesId);

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

    Task<IEnumerable<ComboItem>> SearchItemNames(string searchString);

    ComboItem GetItemComboItem(int itemId);

    Task<IEnumerable<ComboItem>> SearchAbilityNames(string searchString);

    ComboItem GetAbilityComboItem(int abilityId);

    Task<IEnumerable<ComboItem>> SearchMetLocations(string searchString, bool isEggLocation = false);

    ComboItem GetMetLocationComboItem(int metLocationId, bool isEggLocation = false);
}
