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
    /// Clears the current selection (both box and party slots) and resets the edit form.
    /// </summary>
    void ClearSelection();

    /// <summary>
    /// Pins a box to the secondary panel, replacing the Pokémon editor.
    /// Clears the current Pokémon selection.
    /// </summary>
    /// <param name="boxNumber">The 0-based box number to pin.</param>
    void PinBox(int boxNumber);

    /// <summary>
    /// Unpins the currently pinned box, restoring the Pokémon editor panel.
    /// </summary>
    void UnpinBox();

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
    /// Returns true if <see cref="EditFormPokemon"/> differs byte-for-byte from the
    /// data currently stored in its source slot — i.e. the user has edits that have
    /// not been committed via <see cref="SavePokemon"/>.
    /// </summary>
    bool EditFormHasUnsavedChanges();

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
    /// Exports all Pokémon in a box to Pokémon Showdown format text, skipping empty slots.
    /// </summary>
    /// <param name="boxNumber">The 0-based box index to export.</param>
    /// <returns>A Showdown-formatted text representation of the non-empty box slots.</returns>
    string ExportBoxAsShowdown(int boxNumber);

    /// <summary>
    /// Parses raw Showdown / PokePaste text into a list of <see cref="ShowdownSet" /> objects.
    /// Sets with an unknown species (Species == 0) are excluded from the result.
    /// </summary>
    /// <param name="text">The raw Showdown-format text to parse.</param>
    /// <returns>A read-only list of successfully parsed sets.</returns>
    IReadOnlyList<ShowdownSet> ParseShowdownText(string text);

    /// <summary>
    /// Converts a <see cref="ShowdownSet" /> into a <see cref="PKM" /> compatible with the current save file.
    /// Applies trainer info (OT name, gender) from the save file when not provided by the template.
    /// </summary>
    /// <param name="set">The battle template to convert.</param>
    /// <returns>
    /// The generated <see cref="PKM" />, or <see langword="null" /> if no save file is loaded
    /// or the set has no species.
    /// </returns>
    PKM? ConvertShowdownSetToPkm(ShowdownSet set);

    /// <summary>
    /// Places a Pokémon in the first available party slot (does not fall back to boxes).
    /// </summary>
    /// <param name="pkm">The Pokémon to place.</param>
    /// <returns>
    /// <see langword="true" /> if the Pokémon was placed in the party;
    /// <see langword="false" /> if the party is full or no save file is loaded.
    /// </returns>
    bool TryPlacePokemonInPartySlot(PKM pkm);

    /// <summary>
    /// Replaces the entire party with the provided Pokémon (up to 6).
    /// Any excess Pokémon beyond 6 are silently dropped.
    /// </summary>
    /// <param name="pokemon">The list of Pokémon to write into the party.</param>
    /// <returns>The number of Pokémon actually written.</returns>
    int OverwriteParty(IReadOnlyList<PKM> pokemon);

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
    /// Finds the first empty box slot in the save file and selects it.
    /// For Let's Go saves (SAV7b), uses flat unified-storage indexing.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if an empty slot was found and selected; <see langword="false" /> if all slots are
    /// full or no save is loaded.
    /// </returns>
    bool TrySelectFirstEmptyBoxSlot();

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
    /// Returns <see langword="true" /> if the loaded save file exposes any wonder card /
    /// mystery gift slots that the in-app viewer can display. Mirrors the support matrix in
    /// PKHeX WinForms' <c>SAV_Wondercard</c>: Generation 3 Emerald / FRLG, plus all Gen 4–7
    /// saves that implement <see cref="IMysteryGiftStorageProvider" /> (DP/Pt/HGSS, BW/B2W2,
    /// XY/ORAS, SM/USUM, LGPE).
    /// </summary>
    bool HasWonderCardSlots();

    /// <summary>
    /// Returns a generation-agnostic snapshot of every wonder card / mystery gift slot in
    /// the loaded save file (including empty slots, so the viewer can show full storage
    /// capacity). The list is empty when the save has no wonder card storage.
    /// </summary>
    IReadOnlyList<WonderCardSlotInfo> GetWonderCardSlots();

    /// <summary>
    /// Imports a Generation 3 Wonder Card (<c>.wc3</c>) file into the loaded save file.
    /// WC3 files are not <see cref="DataMysteryGift" />-compatible because <see cref="WonderCard3" />
    /// is stored as a raw save-slot struct rather than a generic mystery gift; this method writes
    /// the card, the optional <see cref="WonderCard3Extra" /> link-stats block, and the accompanying
    /// <see cref="MysteryEvent3" /> script directly into the save's wonder card slots.
    /// Only Emerald and FireRed/LeafGreen saves are supported (Ruby/Sapphire have no wonder card slot).
    /// </summary>
    /// <param name="data">The raw <c>.wc3</c> file bytes.</param>
    /// <param name="isSuccessful">Output parameter indicating whether the import was successful.</param>
    /// <param name="resultsMessage">Output parameter containing a message describing the result.</param>
    /// <returns>A completed task.</returns>
    Task ImportWonderCard3(byte[] data, out bool isSuccessful, out string resultsMessage);

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

    /// <summary>
    /// Gets all available memory combo items for the memory ID dropdown.
    /// </summary>
    /// <returns>A collection of memories as combo items.</returns>
    IEnumerable<ComboItem> GetMemoryComboItems();

    /// <summary>
    /// Gets all available memory feeling combo items for the given memory generation.
    /// </summary>
    /// <param name="memoryGen">The memory generation (6 for Gen 6/7, 8 for Gen 8+).</param>
    /// <returns>A collection of feelings as combo items.</returns>
    IEnumerable<ComboItem> GetMemoryFeelingComboItems(int memoryGen);

    /// <summary>Gets memory intensity/quality combo items.</summary>
    IEnumerable<ComboItem> GetMemoryQualityComboItems();

    /// <summary>Gets argument combo items for a given memory argument type and generation.</summary>
    IEnumerable<ComboItem> GetMemoryArgumentComboItems(MemoryArgType argType, int memoryGen);

    /// <summary>Gets language combo items for the given generation.</summary>
    IEnumerable<ComboItem> GetLanguageComboItems(int generation, EntityContext context);

    /// <summary>
    /// Gets all valid geo-location country combo items (ID → English name).
    /// Entry 0 is the "none/blank" country (—).
    /// </summary>
    IEnumerable<ComboItem> GetGeoCountryComboItems();

    /// <summary>
    /// Gets all geo-location region combo items for a given country (ID → English name).
    /// Only valid when <paramref name="country" /> is non-zero.
    /// </summary>
    IEnumerable<ComboItem> GetGeoRegionComboItems(byte country);

    /// <summary>
    /// Gets combo items for 3DS console hardware regions (Japan, North America, Europe, etc.).
    /// Used for the <see cref="IRegionOrigin.ConsoleRegion" /> field (Gen 6–7 only).
    /// </summary>
    IReadOnlyList<ComboItem> GetConsoleRegionComboItems();

    /// <summary>
    /// Performs a full legality analysis on the given Pokémon using PKHeX.Core's
    /// <see cref="LegalityAnalysis" /> engine.
    /// </summary>
    /// <param name="pkm">The Pokémon to analyse.</param>
    /// <param name="isParty">
    /// True if <paramref name="pkm" /> lives in (or is
    /// about to be written to) a party slot. Forwarded to
    /// <see cref="SaveFile.AdaptToSaveFile" /> so the adapt step matches what
    /// the eventual SetPartySlotAtIndex / SetBoxSlotAtIndex call will do.
    /// </param>
    /// <returns>A <see cref="LegalityAnalysis" /> result.</returns>
    LegalityAnalysis GetLegalityAnalysis(PKM pkm, bool isParty = false);

    /// <summary>
    /// Sweeps all party and box slots and returns every Pokémon that satisfies
    /// every non-null criterion in <paramref name="filter" />.
    /// Legality checks (if requested) are evaluated last due to their cost.
    /// </summary>
    /// <param name="filter">The multi-criteria search filter.</param>
    /// <returns>A lazily-evaluated sequence of matching results.</returns>
    IEnumerable<AdvancedSearchResult> SearchPokemon(AdvancedSearchFilter filter);

    /// <summary>
    /// Queries PKHeX.Core encounter tables for the species specified in <paramref name="filter" />
    /// and returns all encounters that satisfy the given criteria.
    /// </summary>
    /// <param name="filter">
    /// Filter criteria. <see cref="EncounterSearchFilter.Species" /> must be non-null for any
    /// results to be returned.
    /// </param>
    /// <returns>A lazily-evaluated sequence of matching encounter results.</returns>
    IEnumerable<EncounterSearchResult> SearchEncounters(EncounterSearchFilter filter);

    /// <summary>
    /// Generates a <see cref="PKM" /> from the given encounter template using the current
    /// save file as the trainer info source.
    /// Callers are responsible for performing any legality analysis on the generated Pokémon.
    /// </summary>
    /// <param name="encounter">The encounter template to generate from.</param>
    /// <returns>
    /// The generated <see cref="PKM" /> if the save file is loaded;
    /// <see langword="null" /> if the save file is not loaded.
    /// </returns>
    PKM? GeneratePokemonFromEncounter(IEncounterable encounter);

    /// <summary>
    /// Swaps all Pokémon slots between two boxes.
    /// </summary>
    /// <param name="boxA">The 0-based index of the first box.</param>
    /// <param name="boxB">The 0-based index of the second box.</param>
    /// <returns>
    /// <see langword="true" /> if the swap succeeded;
    /// <see langword="false" /> if no save file is loaded or if locked slots prevent the swap.
    /// </returns>
    bool SwapBoxes(int boxA, int boxB);

    /// <summary>
    /// Places a Pokémon in the first available slot: the party (if not full), then the
    /// first empty box slot scanning boxes in order.
    /// </summary>
    /// <param name="pkm">The Pokémon to place.</param>
    /// <returns>
    /// <see langword="true" /> if the Pokémon was placed successfully;
    /// <see langword="false" /> if no save file is loaded or all slots are occupied.
    /// </returns>
    bool TryPlacePokemonInFirstAvailableSlot(PKM pkm);

    /// <summary>
    /// Returns the immediate forward evolutions for a Pokémon — the direct children in the
    /// evolution tree, one entry per branch.
    /// <see cref="EvolutionType.LevelUpShedinja" /> entries are excluded because Shedinja is
    /// generated as a side-effect of the Nincada → Ninjask evolution, not as a direct choice.
    /// </summary>
    /// <param name="pkm">The Pokémon to query.</param>
    /// <returns>A list of <see cref="EvolutionMethod" /> values (may be empty for final evolutions).</returns>
    IReadOnlyList<EvolutionMethod> GetDirectEvolutions(PKM pkm);

    /// <summary>
    /// Returns <see langword="true" /> if the current save file has a Battle Box (Gen 5–6).
    /// </summary>
    bool HasBattleBox();

    /// <summary>
    /// Gets the Pokémon stored in the Battle Box (Gen 5–6). Empty slots are excluded.
    /// </summary>
    IReadOnlyList<PKM> GetBattleBoxPokemon();

    /// <summary>
    /// Returns whether the Battle Box is locked (Gen 5–6).
    /// </summary>
    bool IsBattleBoxLocked();

    /// <summary>
    /// Returns <see langword="true" /> if the current save file supports Battle Teams (Gen 7+).
    /// </summary>
    bool HasBattleTeams();

    /// <summary>
    /// Gets the Pokémon in a battle team by resolving box slot references. Empty slots are excluded.
    /// </summary>
    /// <param name="teamIndex">The 0-based team index (0–5).</param>
    IReadOnlyList<PKM> GetBattleTeamPokemon(int teamIndex);

    /// <summary>
    /// Gets the display name for a battle team.
    /// </summary>
    /// <param name="teamIndex">The 0-based team index (0–5).</param>
    string GetBattleTeamName(int teamIndex);

    /// <summary>
    /// Returns whether a battle team is locked.
    /// </summary>
    /// <param name="teamIndex">The 0-based team index (0–5).</param>
    bool IsBattleTeamLocked(int teamIndex);

    /// <summary>
    /// Sets the lock state of a battle team.
    /// </summary>
    /// <param name="teamIndex">The 0-based team index (0–5).</param>
    /// <param name="locked">Whether to lock or unlock the team.</param>
    void SetBattleTeamLocked(int teamIndex, bool locked);

    /// <summary>
    /// Returns <see langword="true" /> if the current save file supports Rental Teams (Gen 8 SWSH, Gen 9 SV).
    /// </summary>
    bool HasRentalTeams();

    /// <summary>
    /// Gets the number of rental team slots available.
    /// </summary>
    int GetRentalTeamCount();

    /// <summary>
    /// Gets the Pokémon in a rental team. Empty slots are excluded.
    /// </summary>
    /// <param name="teamIndex">The 0-based rental team index.</param>
    IReadOnlyList<PKM> GetRentalTeamPokemon(int teamIndex);

    /// <summary>
    /// Gets the display name of a rental team.
    /// </summary>
    /// <param name="teamIndex">The 0-based rental team index.</param>
    string GetRentalTeamName(int teamIndex);

    /// <summary>
    /// Gets the player name associated with a rental team.
    /// </summary>
    /// <param name="teamIndex">The 0-based rental team index.</param>
    string GetRentalTeamPlayerName(int teamIndex);

    /// <summary>
    /// Exports a list of Pokémon to Showdown format text.
    /// </summary>
    /// <param name="team">The Pokémon to export.</param>
    string ExportTeamAsShowdown(IReadOnlyList<PKM> team);

    /// <summary>
    /// Clears a single battle team by setting all its slot references to empty (Gen 7+).
    /// </summary>
    /// <param name="teamIndex">The 0-based team index (0–5).</param>
    void ClearBattleTeam(int teamIndex);

    /// <summary>
    /// Clears all battle teams and unlocks them (Gen 7+).
    /// </summary>
    void ClearAllBattleTeams();

    /// <summary>
    /// Unlocks all battle teams (Gen 7+).
    /// </summary>
    void UnlockAllBattleTeams();

    /// <summary>
    /// Clears all Pokémon from the Battle Box (Gen 5–6).
    /// </summary>
    void ClearBattleBox();

    /// <summary>
    /// Sets the Battle Box lock state (Gen 5–6).
    /// </summary>
    /// <param name="locked">Whether to lock or unlock the Battle Box.</param>
    void SetBattleBoxLocked(bool locked);
}
