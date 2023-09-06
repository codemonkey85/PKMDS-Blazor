namespace Pkmds.Rcl.Services;

public interface IAppService
{
    string[] NatureStatShortNames { get; }

    PKM? EditFormPokemon { get; set; }

    void ClearSelection();

    void DeletePokemon();

    IEnumerable<ComboItem> SearchPokemonNames(string searchString);

    ComboItem GetSpeciesComboItem(ushort speciesId);

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

    public void SavePokemon(PKM? SelectedPokemon);

    string GetCleanFileName(PKM pkm);

    void SetSelectedBoxPokemon(PKM? pkm, int boxNumber, int slotNumber);

    void SetSelectedPartyPokemon(PKM? pkm, int slotNumber);
}
