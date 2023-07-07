namespace Pkmds.Rcl;

public interface IAppState
{
    event Action? OnAppStateChanged;

    event Action? OnBoxStateChanged;

    event Action? OnPartyStateChanged;

    string CurrentLanguage { get; set; }

    int CurrentLanguageId { get; }

    PKM? EditFormPokemon { get; set; }

    IEnumerable<ComboItem> SearchPokemonNames(string searchString);

    ComboItem GetSpeciesComboItem(ushort speciesId);

    SaveFile? SaveFile { get; set; }

    int? SelectedBoxNumber { get; set; }

    int? SelectedBoxSlotNumber { get; set; }

    int? SelectedPartySlotNumber { get; set; }

    bool ShowProgressIndicator { get; set; }

    string FileDisplayName { get; set; }

    void Refresh();

    void ClearSelection();

    string[] NatureStatShortNames { get; }

    string GetStatModifierString(int nature);

    void LoadPokemonStats(PKM? pokemon);

    IEnumerable<ComboItem> SearchItemNames(string searchString);

    ComboItem GetItemComboItem(int itemId);

    IEnumerable<ComboItem> SearchAbilityNames(string searchString);

    ComboItem GetAbilityComboItem(int abilityId);

    IEnumerable<ComboItem> SearchMetLocations(string searchString, bool isEggLocation = false);

    ComboItem GetMetLocationComboItem(int metLocationId, bool isEggLocation = false);

    IEnumerable<ComboItem> SearchMoves(string searchString);

    ComboItem GetMoveComboItem(int moveId);

    bool GetMarking(PKM? pokemon, int index);

    void SetMarking(PKM? pokemon, int index, bool value);

    void ToggleMarking(PKM? pokemon, int index);

    string GetCharacteristic(PKM? pokemon);

    bool IsPurificationVisible { get; }

    bool IsSizeVisible { get; }

    public void SavePokemon(PKM? SelectedPokemon);

    string GetCleanFileName(PKM pkm);

    void SetSelectedBoxPokemon(PKM? pkm, int boxNumber, int slotNumber);

    void SetSelectedPartyPokemon(PKM? pkm, int slotNumber);
}
