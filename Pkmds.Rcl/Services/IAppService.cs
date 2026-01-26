namespace Pkmds.Rcl.Services;

/// <summary>
/// Provides core application services for managing Pokémon data, UI state, and game information.
/// This is the main service for interacting with the save file and Pokémon data.
/// </summary>
public interface IAppService
{
    /// <summary>
    /// Gets or sets the Pokémon currently being edited in the edit form.
    /// This is a clone of the selected Pokémon to allow for canceling edits.
    /// </summary>
    PKM? EditFormPokemon { get; set; }

    /// <summary>
    /// Gets or sets whether the navigation drawer is currently open.
    /// </summary>
    bool IsDrawerOpen { get; set; }

    /// <summary>
    /// Toggles the navigation drawer between open and closed states.
    /// </summary>
    void ToggleDrawer();

    /// <summary>
    /// Clears the current selection (both box and party slots) and resets the edit form.
    /// </summary>
    void ClearSelection();

    /// <summary>
    /// Deletes a Pokémon from the party at the specified slot.
    /// Ensures at least one battle-ready Pokémon remains in the party.
    /// </summary>
    /// <param name="partySlotNumber">The 0-based party slot index (0-5).</param>
    void DeletePokemon(int partySlotNumber);

    /// <summary>
    /// Deletes a Pokémon from a box by replacing it with a blank Pokémon.
    /// </summary>
    /// <param name="boxNumber">The 0-based box number.</param>
    /// <param name="boxSlotNumber">The 0-based slot number within the box.</param>
    void DeletePokemon(int boxNumber, int boxSlotNumber);

    /// <summary>
    /// Gets the display name for a Pokémon species in the current language.
    /// </summary>
    /// <param name="speciesId">The species ID to look up.</param>
    /// <returns>The localized species name.</returns>
    string? GetPokemonSpeciesName(ushort speciesId);

    /// <summary>
    /// Searches for Pokémon species names matching the search string.
    /// </summary>
    /// <param name="searchString">The search term to filter species names.</param>
    /// <returns>A collection of matching species as combo items.</returns>
    IEnumerable<ComboItem> SearchPokemonNames(string searchString);

    /// <summary>
    /// Gets the combo item representation for a specific species.
    /// </summary>
    /// <param name="speciesId">The species ID to look up.</param>
    /// <returns>A combo item containing the species name and ID.</returns>
    ComboItem GetSpeciesComboItem(ushort speciesId);

    /// <summary>
    /// Gets a human-readable string describing a nature's stat modifiers.
    /// </summary>
    /// <param name="nature">The nature to describe.</param>
    /// <returns>A string like "(Atk ↑, Def ↓)" or "(neutral)" for neutral natures.</returns>
    string GetStatModifierString(Nature nature);

    /// <summary>
    /// Calculates and loads the stats for a Pokémon based on its species, form, IVs, EVs, and level.
    /// </summary>
    /// <param name="pokemon">The Pokémon to load stats for.</param>
    void LoadPokemonStats(PKM? pokemon);

    /// <summary>
    /// Searches for items matching the search string.
    /// </summary>
    /// <param name="searchString">The search term to filter item names.</param>
    /// <returns>A collection of matching items as combo items.</returns>
    IEnumerable<ComboItem> SearchItemNames(string searchString);

    /// <summary>
    /// Gets the combo item representation for a specific item.
    /// </summary>
    /// <param name="itemId">The item ID to look up.</param>
    /// <returns>A combo item containing the item name and ID.</returns>
    ComboItem GetItemComboItem(int itemId);

    /// <summary>
    /// Gets the combo item representation for a specific ability.
    /// </summary>
    /// <param name="abilityId">The ability ID to look up.</param>
    /// <returns>A combo item containing the ability name and ID.</returns>
    ComboItem GetAbilityComboItem(int abilityId);

    /// <summary>
    /// Searches for abilities matching the search string.
    /// </summary>
    /// <param name="searchString">The search term to filter ability names.</param>
    /// <returns>A collection of matching abilities as combo items.</returns>
    IEnumerable<ComboItem> SearchAbilityNames(string searchString);

    /// <summary>
    /// Searches for met locations matching the search string for a specific game version and context.
    /// </summary>
    /// <param name="searchString">The search term to filter location names.</param>
    /// <param name="gameVersion">The game version to get locations for.</param>
    /// <param name="entityContext">The generation/context for the locations.</param>
    /// <param name="isEggLocation">Whether to search egg locations instead of met locations.</param>
    /// <returns>A collection of matching locations as combo items.</returns>
    IEnumerable<ComboItem> SearchMetLocations(string searchString, GameVersion gameVersion, EntityContext entityContext,
        bool isEggLocation = false);

    /// <summary>
    /// Gets the combo item representation for a specific met location.
    /// </summary>
    /// <param name="metLocationId">The location ID to look up.</param>
    /// <param name="gameVersion">The game version for the location.</param>
    /// <param name="entityContext">The generation/context for the location.</param>
    /// <param name="isEggLocation">Whether this is an egg location instead of a met location.</param>
    /// <returns>A combo item containing the location name and ID.</returns>
    ComboItem GetMetLocationComboItem(ushort metLocationId, GameVersion gameVersion, EntityContext entityContext,
        bool isEggLocation = false);

    /// <summary>
    /// Searches for moves matching the search string.
    /// </summary>
    /// <param name="searchString">The search term to filter move names.</param>
    /// <returns>A collection of matching moves as combo items.</returns>
    IEnumerable<ComboItem> SearchMoves(string searchString);

    /// <summary>
    /// Gets all available moves for the current game context.
    /// </summary>
    /// <returns>A collection of all moves as combo items.</returns>
    IEnumerable<ComboItem> GetMoves();

    /// <summary>
    /// Gets the combo item representation for a specific move.
    /// </summary>
    /// <param name="moveId">The move ID to look up.</param>
    /// <returns>A combo item containing the move name and ID.</returns>
    ComboItem GetMoveComboItem(int moveId);

    /// <summary>
    /// Saves the edited Pokémon back to its original slot in the save file.
    /// </summary>
    /// <param name="selectedPokemon">The Pokémon to save.</param>
    void SavePokemon(PKM? selectedPokemon);

    /// <summary>
    /// Generates a clean, standardized filename for exporting a Pokémon.
    /// Format varies by generation: Gen 1/2 use DV values, Gen 3+ use PID.
    /// </summary>
    /// <param name="pkm">The Pokémon to generate a filename for.</param>
    /// <returns>A clean filename suitable for file export.</returns>
    string GetCleanFileName(PKM pkm);

    /// <summary>
    /// Sets the selected Pokémon from Let's Go Pikachu/Eevee storage.
    /// </summary>
    /// <param name="pkm">The Pokémon to select.</param>
    /// <param name="slotNumber">The slot number in Let's Go storage.</param>
    void SetSelectedLetsGoPokemon(PKM? pkm, int slotNumber);

    /// <summary>
    /// Sets the selected Pokémon from a box slot.
    /// </summary>
    /// <param name="pkm">The Pokémon to select.</param>
    /// <param name="boxNumber">The box number (0-based).</param>
    /// <param name="slotNumber">The slot number within the box (0-based).</param>
    void SetSelectedBoxPokemon(PKM? pkm, int boxNumber, int slotNumber);

    /// <summary>
    /// Sets the selected Pokémon from a party slot.
    /// </summary>
    /// <param name="pkm">The Pokémon to select.</param>
    /// <param name="slotNumber">The party slot number (0-5).</param>
    void SetSelectedPartyPokemon(PKM? pkm, int slotNumber);

    /// <summary>
    /// Exports a Pokémon to Pokémon Showdown format text.
    /// </summary>
    /// <param name="pkm">The Pokémon to export.</param>
    /// <returns>A Showdown-formatted text representation of the Pokémon.</returns>
    string ExportPokemonAsShowdown(PKM? pkm);

    /// <summary>
    /// Exports all Pokémon in the party to Pokémon Showdown format text.
    /// </summary>
    /// <returns>A Showdown-formatted text representation of the entire party.</returns>
    string ExportPartyAsShowdown();

    /// <summary>
    /// Gets the format string for displaying trainer IDs based on the current save file's format.
    /// </summary>
    /// <param name="isSid">Whether to get the format for SID instead of TID.</param>
    /// <returns>A format string for use with string formatting (e.g., "D5" for 5-digit format).</returns>
    string GetIdFormatString(bool isSid = false);

    /// <summary>
    /// Determines which type of slot is currently selected and retrieves the slot coordinates.
    /// </summary>
    /// <param name="partySlot">The party slot number if a party slot is selected.</param>
    /// <param name="boxNumber">The box number if a box slot is selected.</param>
    /// <param name="boxSlot">The box slot number if a box slot is selected.</param>
    /// <returns>The type of selected slot (Party, Box, or None).</returns>
    SelectedPokemonType GetSelectedPokemonSlot(out int partySlot, out int boxNumber, out int boxSlot);

    /// <summary>
    /// Imports a Mystery Gift into the save file.
    /// </summary>
    /// <param name="gift">The Mystery Gift data to import.</param>
    /// <param name="isSuccessful">Output parameter indicating whether the import was successful.</param>
    /// <param name="resultsMessage">Output parameter containing a message describing the result.</param>
    /// <returns>A completed task.</returns>
    Task ImportMysteryGift(DataMysteryGift gift, out bool isSuccessful, out string resultsMessage);

    /// <summary>
    /// Imports a Mystery Gift from raw data and file extension.
    /// </summary>
    /// <param name="data">The raw Mystery Gift file data.</param>
    /// <param name="fileExtension">The file extension (e.g., ".wc7", ".pgf").</param>
    /// <param name="isSuccessful">Output parameter indicating whether the import was successful.</param>
    /// <param name="resultsMessage">Output parameter containing a message describing the result.</param>
    /// <returns>A completed task.</returns>
    Task ImportMysteryGift(byte[] data, string fileExtension, out bool isSuccessful, out string resultsMessage);

    /// <summary>
    /// Moves or swaps a Pokémon between slots (box-to-box, party-to-box, box-to-party, or party-to-party).
    /// Handles special cases like Gen 1/2 box compacting and party compacting.
    /// </summary>
    /// <param name="sourceBoxNumber">The source box number (null for party or Let's Go storage).</param>
    /// <param name="sourceSlotNumber">The source slot number.</param>
    /// <param name="isSourceParty">Whether the source is a party slot.</param>
    /// <param name="destBoxNumber">The destination box number (null for party or Let's Go storage).</param>
    /// <param name="destSlotNumber">The destination slot number.</param>
    /// <param name="isDestParty">Whether the destination is a party slot.</param>
    void MovePokemon(int? sourceBoxNumber, int sourceSlotNumber, bool isSourceParty,
        int? destBoxNumber, int destSlotNumber, bool isDestParty);
}
