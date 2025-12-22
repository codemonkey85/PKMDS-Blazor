namespace Pkmds.Rcl.Services;

public interface IAppService
{
    PKM? EditFormPokemon { get; set; }

    bool IsDrawerOpen { get; set; }

    void ToggleDrawer();

    void ClearSelection();

    void DeletePokemon(int partySlotNumber);

    void DeletePokemon(int boxNumber, int boxSlotNumber);

    string? GetPokemonSpeciesName(ushort speciesId);

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

    IEnumerable<ComboItem> GetMoves();

    ComboItem GetMoveComboItem(int moveId);

    void SavePokemon(PKM? selectedPokemon);

    string GetCleanFileName(PKM pkm);

    void SetSelectedLetsGoPokemon(PKM? pkm, int slotNumber);

    void SetSelectedBoxPokemon(PKM? pkm, int boxNumber, int slotNumber);

    void SetSelectedPartyPokemon(PKM? pkm, int slotNumber);

    string ExportPokemonAsShowdown(PKM? pkm);

    string ExportPartyAsShowdown();

    string GetIdFormatString(bool isSid = false);

    SelectedPokemonType GetSelectedPokemonSlot(out int partySlot, out int boxNumber, out int boxSlot);

    Task ImportMysteryGift(DataMysteryGift gift, out bool isSuccessful, out string resultsMessage);

    Task ImportMysteryGift(byte[] data, string fileExtension, out bool isSuccessful, out string resultsMessage);

    void MovePokemon(int? sourceBoxNumber, int sourceSlotNumber, bool isSourceParty, 
        int? destBoxNumber, int destSlotNumber, bool isDestParty);
}
