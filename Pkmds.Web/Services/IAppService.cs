namespace Pkmds.Web.Services;

public interface IAppService
{
    PKM? EditFormPokemon { get; set; }

    bool IsDrawerOpen { get; set; }

    void ToggleDrawer();

    void ClearSelection();

    void DeletePokemon(int partySlotNumber);

    void DeletePokemon(int boxNumber, int boxSlotNumber);

    string GetPokemonSpeciesName(ushort speciesId);

    IEnumerable<ComboItem> SearchPokemonNames(string searchString);

    ComboItem GetSpeciesComboItem(ushort speciesId);

    string GetStatModifierString(Nature nature);

    void LoadPokemonStats(PKM? pokemon);

    IEnumerable<ComboItem> SearchItemNames(string searchString);

    ComboItem GetItemComboItem(int itemId);

    ComboItem GetAbilityComboItem(int abilityId);

    IEnumerable<ComboItem> SearchAbilityNames(string searchString);

    IEnumerable<ComboItem> SearchMetLocations(string searchString, GameVersion gameVersion, EntityContext entityContext,
        bool isEggLocation = false);

    ComboItem GetMetLocationComboItem(ushort metLocationId, GameVersion gameVersion, EntityContext entityContext,
        bool isEggLocation = false);

    IEnumerable<ComboItem> SearchMoves(string searchString);

    ComboItem GetMoveComboItem(int moveId);

    public void SavePokemon(PKM? selectedPokemon);

    string GetCleanFileName(PKM pkm);

    void SetSelectedLetsGoPokemon(PKM? pkm, int slotNumber);

    void SetSelectedBoxPokemon(PKM? pkm, int boxNumber, int slotNumber);

    void SetSelectedPartyPokemon(PKM? pkm, int slotNumber);

    string ExportPokemonAsShowdown(PKM? pkm);

    string ExportPartyAsShowdown();

    string GetIdFormatString(bool isSid = false);
}
