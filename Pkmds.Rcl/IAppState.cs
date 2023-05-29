namespace Pkmds.Rcl;

public interface IAppState
{
    event Action? OnAppStateChanged;

    string CurrentLanguage { get; set; }

    int CurrentLanguageId { get; }

    IEnumerable<ComboItem> SearchPokemonNames(string searchString);

    ComboItem GetSpeciesComboItem(ushort speciesId);

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

    IEnumerable<ComboItem> SearchItemNames(string searchString);

    ComboItem GetItemComboItem(int itemId);

    IEnumerable<ComboItem> SearchAbilityNames(string searchString);

    ComboItem GetAbilityComboItem(int abilityId);

    IEnumerable<ComboItem> SearchMetLocations(string searchString, bool isEggLocation = false);

    ComboItem GetMetLocationComboItem(int metLocationId, bool isEggLocation = false);

    IEnumerable<ComboItem> SearchMoves(string searchString);

    ComboItem GetMoveComboItem(int moveId);

    bool GetMarking(int index);

    void SetMarking(int index, bool value);

    void ToggleMarking(int index);

    string GetCharacteristic();

    bool IsPurificationVisible { get; }

    bool IsSizeVisible { get; }
}
