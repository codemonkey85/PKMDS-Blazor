using Pkmds.Rcl.Components;

namespace Pkmds.Rcl.Services;

public class AppService(IAppState appState, IRefreshService refreshService) : IAppService
{
    private const string EnglishLang = "en";
    private const string DefaultPkmFileName = "pkm.bin";

    private IAppState AppState { get; } = appState;

    private IRefreshService RefreshService { get; } = refreshService;

    private static string[] NatureStatShortNames => ["Atk", "Def", "Spe", "SpA", "SpD"];

    public PKM? EditFormPokemon
    {
        get;
        set
        {
            field = value?.Clone();
            LoadPokemonStats(field);
        }
    }

    public bool IsDrawerOpen
    {
        get;
        set
        {
            field = value;
            RefreshService.Refresh();
        }
    }

    public void ToggleDrawer() => IsDrawerOpen = !IsDrawerOpen;

    public void ClearSelection()
    {
        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;
        AppState.SelectedPartySlotNumber = null;
        EditFormPokemon = null;
        RefreshService.Refresh();
    }

    public string GetPokemonSpeciesName(ushort speciesId) => GetSpeciesComboItem(speciesId).Text;

    public IEnumerable<ComboItem> SearchPokemonNames(string searchString) =>
        AppState.SaveFile is null || searchString is not { Length: > 0 }
            ? []
            : GameInfo.FilteredSources.Species
                .DistinctBy(species => species.Value)
                .Where(species => species.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                .OrderBy(species => species.Text);

    public ComboItem GetSpeciesComboItem(ushort speciesId) => GameInfo.FilteredSources.Species
        .DistinctBy(species => species.Value)
        .FirstOrDefault(species => species.Value == speciesId) ?? new(string.Empty, (int)Species.None);

    public IEnumerable<ComboItem> SearchItemNames(string searchString) =>
        AppState.SaveFile is null || searchString is not { Length: > 0 }
            ? []
            : GameInfo.FilteredSources.Items
                .DistinctBy(item => item.Value)
                .Where(item => item.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Text);

    public ComboItem GetItemComboItem(int itemId) => GameInfo.FilteredSources.Items
        .DistinctBy(item => item.Value)
        .FirstOrDefault(item => item.Value == itemId) ?? null!;

    public ComboItem GetAbilityComboItem(int abilityId) => GameInfo.FilteredSources.Abilities
        .DistinctBy(ability => ability.Value)
        .FirstOrDefault(ability => ability.Value == abilityId) ?? null!;

    public IEnumerable<ComboItem> SearchAbilityNames(string searchString) =>
        AppState.SaveFile is null || searchString is not { Length: > 0 }
            ? []
            : GameInfo.FilteredSources.Abilities
                .DistinctBy(ability => ability.Value)
                .Where(ability => ability.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                .OrderBy(ability => ability.Text);

    public string GetStatModifierString(Nature nature)
    {
        var (up, down) = NatureAmp.GetNatureModification(nature);
        return up == down
            ? "(neutral)"
            : $"({NatureStatShortNames[up]} ↑, {NatureStatShortNames[down]} ↓)";
    }

    public void LoadPokemonStats(PKM? pokemon)
    {
        if (AppState.SaveFile is null || pokemon is null)
        {
            return;
        }

        var pt = AppState.SaveFile.Personal;
        var pi = pt.GetFormEntry(pokemon.Species, pokemon.Form);
        Span<ushort> stats = stackalloc ushort[6];
        pokemon.LoadStats(pi, stats);
        pokemon.SetStats(stats);
    }

    public IEnumerable<ComboItem> SearchMetLocations(string searchString, GameVersion gameVersion,
        EntityContext entityContext, bool isEggLocation = false) =>
        AppState.SaveFile is null || searchString is not { Length: > 0 }
            ? []
            : GameInfo.GetLocationList(gameVersion, entityContext, isEggLocation)
                .DistinctBy(l => l.Value)
                .Where(metLocation => metLocation.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                .OrderBy(metLocation => metLocation.Text);

    public ComboItem GetMetLocationComboItem(ushort metLocationId, GameVersion gameVersion, EntityContext entityContext,
        bool isEggLocation = false) => AppState.SaveFile is null
        ? null!
        : GameInfo.GetLocationList(gameVersion, entityContext, isEggLocation)
            .DistinctBy(l => l.Value)
            .FirstOrDefault(metLocation => metLocation.Value == metLocationId) ?? null!;

    public IEnumerable<ComboItem> SearchMoves(string searchString) =>
        AppState.SaveFile is null || searchString is not { Length: > 0 }
            ? []
            : GameInfo.FilteredSources.Moves
                .DistinctBy(move => move.Value)
                .Where(move => move.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                .OrderBy(move => move.Text);

    public IEnumerable<ComboItem> GetMoves() => AppState.SaveFile is null
        ? []
        : GameInfo.FilteredSources.Moves
            .DistinctBy(move => move.Value)
            .OrderBy(move => move.Text);

    public ComboItem GetMoveComboItem(int moveId) => GameInfo.FilteredSources.Moves
        .DistinctBy(move => move.Value)
        .FirstOrDefault(metLocation => metLocation.Value == moveId) ?? null!;

    public void SavePokemon(PKM? pokemon)
    {
        if (AppState.SaveFile is null || pokemon is null)
        {
            return;
        }

        var selectedPokemonType = GetSelectedPokemonSlot(out var partySlot, out var boxNumber, out var boxSlot);
        switch (selectedPokemonType)
        {
            case SelectedPokemonType.Party:
                AppState.SaveFile.SetPartySlotAtIndex(pokemon, partySlot);

                if (AppState.SaveFile is SAV7b)
                {
                    RefreshService.RefreshBoxAndPartyState();
                }
                else
                {
                    RefreshService.RefreshPartyState();
                }

                break;
            case SelectedPokemonType.Box:
                AppState.SaveFile.SetBoxSlotAtIndex(pokemon, boxNumber, boxSlot);
                RefreshService.RefreshBoxState();
                break;
            case SelectedPokemonType.None when AppState.SaveFile is SAV7b:
                AppState.SaveFile.SetBoxSlotAtIndex(pokemon, boxSlot);
                RefreshService.RefreshBoxAndPartyState();
                break;
        }
    }

    public string GetCleanFileName(PKM pkm) => pkm.Context switch
    {
        EntityContext.SplitInvalid or EntityContext.MaxInvalid => DefaultPkmFileName,
        EntityContext.Gen1 or EntityContext.Gen2 => pkm switch
        {
            PK1 pk1 => $"{GameInfo.GetStrings(EnglishLang).Species[pk1.Species]}_{pk1.DV16}.{pk1.Extension}",
            PK2 pk2 => $"{GameInfo.GetStrings(EnglishLang).Species[pk2.Species]}_{pk2.DV16}.{pk2.Extension}",
            _ => DefaultPkmFileName
        },
        _ => $"{GameInfo.GetStrings(EnglishLang).Species[pkm.Species]}_{pkm.PID:X}.{pkm.Extension}"
    };

    public void SetSelectedLetsGoPokemon(PKM? pkm, int slotNumber)
    {
        AppState.SelectedPartySlotNumber = null;

        AppState.SelectedBoxSlotNumber = slotNumber;
        EditFormPokemon = pkm;

        HandleNullOrEmptyPokemon();
        RefreshService.Refresh();
    }

    public void SetSelectedBoxPokemon(PKM? pkm, int boxNumber, int slotNumber)
    {
        AppState.SelectedPartySlotNumber = null;

        AppState.SelectedBoxNumber = boxNumber;
        AppState.SelectedBoxSlotNumber = slotNumber;
        EditFormPokemon = pkm;

        HandleNullOrEmptyPokemon();
        RefreshService.Refresh();
    }

    public void SetSelectedPartyPokemon(PKM? pkm, int slotNumber)
    {
        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;

        AppState.SelectedPartySlotNumber = slotNumber;
        EditFormPokemon = pkm;

        HandleNullOrEmptyPokemon();
        RefreshService.Refresh();
    }

    public void DeletePokemon(int partySlotNumber)
    {
        if (AppState is not { SaveFile: { } saveFile })
        {
            return;
        }

        // Validate party requirements: must keep at least one non-Egg battle-ready Pokémon
        var battleReadyCount = 0;
        for (var i = 0; i < saveFile.PartyCount; i++)
        {
            if (i == partySlotNumber)
            {
                continue; // Skip the one being deleted
            }

            var partyMon = saveFile.GetPartySlotAtIndex(i);
            if (partyMon is { Species: > 0, IsEgg: false })
            {
                battleReadyCount++;
            }
        }

        // Prevent deletion if it would leave no battle-ready Pokémon
        if (battleReadyCount == 0)
        {
            // Cannot delete the last battle-ready Pokémon - silently prevent
            return;
        }

        saveFile.DeletePartySlot(partySlotNumber);

        AppState.SelectedPartySlotNumber = null;

        RefreshService.RefreshPartyState();
    }

    public void DeletePokemon(int boxNumber, int boxSlotNumber)
    {
        if (AppState is not { SaveFile: { } saveFile })
        {
            return;
        }

        saveFile.SetBoxSlotAtIndex(saveFile.BlankPKM, boxNumber, boxSlotNumber);

        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;

        RefreshService.RefreshBoxState();
    }

    public string ExportPokemonAsShowdown(PKM? pkm) => pkm is null
        ? string.Empty
        : ShowdownParsing.GetShowdownText(pkm);

    public string ExportPartyAsShowdown()
    {
        if (AppState.SaveFile is not { HasParty: true, PartyCount: var partyCount } saveFile)
        {
            return string.Empty;
        }

        var sbShowdown = new StringBuilder();

        for (var slot = 0; slot < partyCount; slot++)
        {
            var pkm = saveFile.GetPartySlotAtIndex(slot);

            sbShowdown.AppendLine(ShowdownParsing.GetShowdownText(pkm));
            sbShowdown.AppendLine();
        }

        return sbShowdown.ToString().Trim();
    }

    public string GetIdFormatString(bool isSid = false)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return string.Empty;
        }

        var format = saveFile.GetTrainerIDFormat();
        return (format, isSid) switch
        {
            (TrainerIDFormat.SixteenBit, false) => TrainerIDExtensions.TID16,
            (TrainerIDFormat.SixteenBit, true) => TrainerIDExtensions.SID16,
            (TrainerIDFormat.SixDigit, false) => TrainerIDExtensions.TID7,
            (TrainerIDFormat.SixDigit, true) => TrainerIDExtensions.SID7,
            _ => "D"
        };
    }

    public SelectedPokemonType GetSelectedPokemonSlot(out int partySlot, out int boxNumber, out int boxSlot)
    {
        const int defaultValue = -1;

        partySlot = AppState.SelectedPartySlotNumber ?? defaultValue;
        boxNumber = AppState.SelectedBoxNumber ?? defaultValue;
        boxSlot = AppState.SelectedBoxSlotNumber ?? defaultValue;

        return (partySlot, boxNumber, boxSlot) switch
        {
            (not defaultValue, defaultValue, defaultValue) => SelectedPokemonType.Party,
            (defaultValue, not defaultValue, not defaultValue) => SelectedPokemonType.Box,
            _ => SelectedPokemonType.None
        };
    }

    public Task ImportMysteryGift(DataMysteryGift gift, out bool isSuccessful, out string resultsMessage)
    {
        try
        {
            if (AppState.SaveFile is not { } saveFile)
            {
                isSuccessful = false;
                resultsMessage = "No save file loaded.";
                return Task.CompletedTask;
            }

            if (!gift.IsCardCompatible(saveFile, out var msg))
            {
                isSuccessful = false;
                resultsMessage = msg;
                return Task.CompletedTask;
            }

            var cards = GetMysteryGiftProvider(saveFile);
            var album = LoadMysteryGifts(saveFile, cards);
            var flags = cards as IMysteryGiftFlags;
            var index = 0;

            var lastUnfilled = GetLastUnfilledByType(gift, album);
            if (lastUnfilled > -1)
            {
                index = lastUnfilled;
            }

            if (gift is PCD { IsLockCapsule: true })
            {
                index = 11;
            }

            var other = album[index];
            if (gift is PCD { CanConvertToPGT: true } pcd && other is PGT)
            {
                gift = pcd.Gift;
            }
            else if (gift.Type != other.Type)
            {
                isSuccessful = false;
                resultsMessage = $"{gift.Type} != {other.Type}";
                return Task.CompletedTask;
            }
            else if (gift is PCD g && g is { IsLockCapsule: true } != (index == 11))
            {
                isSuccessful = false;
                resultsMessage = $"{GameInfo.Strings.Item[533]} slot not valid.";
                return Task.CompletedTask;
            }

            album[index] = gift.Clone();

            List<string> receivedFlags = [];

            SetCardId(gift.CardID, flags, receivedFlags);
            SaveReceivedFlags(flags, receivedFlags);
            SaveReceivedCards(saveFile, cards, album);

            isSuccessful = true;
            resultsMessage = "The Mystery Gift has been successfully imported.";
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            resultsMessage = ex.Message;
            isSuccessful = false;
            return Task.CompletedTask;
        }

        static int GetLastUnfilledByType(DataMysteryGift gift, DataMysteryGift[] album)
        {
            for (var i = 0; i < album.Length; i++)
            {
                var exist = album[i];
                if (!exist.IsEmpty)
                {
                    continue;
                }

                if (exist.Type != gift.Type)
                {
                    continue;
                }

                return i;
            }

            return -1;
        }

        static DataMysteryGift[] LoadMysteryGifts(SaveFile saveFile, IMysteryGiftStorage cards)
        {
            var count = cards.GiftCountMax;
            var size = saveFile is SAV4HGSS
                ? count + 1
                : count;
            var result = new DataMysteryGift[size];
            for (var i = 0; i < count; i++)
            {
                result[i] = cards.GetMysteryGift(i);
            }

            if (saveFile is SAV4HGSS s4)
            {
                result[^1] = s4.LockCapsuleSlot;
            }

            return result;
        }

        static IMysteryGiftStorage GetMysteryGiftProvider(SaveFile saveFile) =>
            saveFile is IMysteryGiftStorageProvider provider
                ? provider.MysteryGiftStorage
                : throw new Exception(
                    $"{SaveFileNameDisplay.FriendlyGameName(saveFile.Version)} does not support Mystery Gifts.");

        static void SetCardId(int cardId, IMysteryGiftFlags? flags, List<string> receivedFlags)
        {
            if (flags is null || (uint)cardId >= flags.MysteryGiftReceivedFlagMax)
            {
                return;
            }

            var card = cardId.ToString("0000");
            if (!receivedFlags.Contains(card))
            {
                receivedFlags.Add(card);
            }
        }

        static void SaveReceivedFlags(IMysteryGiftFlags? flags, List<string> receivedFlags)
        {
            if (flags is null)
            {
                return; // nothing to save
            }

            var count = flags.MysteryGiftReceivedFlagMax;
            for (var i = 1; i < count; i++)
            {
                if (flags.GetMysteryGiftReceivedFlag(i))
                {
                    receivedFlags.Add(i.ToString("0000"));
                }
            }

            // Store the list of set flag indexes back to the bitflag array.
            flags.ClearReceivedFlags();
            foreach (var o in receivedFlags)
            {
                if (!int.TryParse(o, out var index))
                {
                    continue;
                }

                flags.SetMysteryGiftReceivedFlag(index, true);
            }
        }

        static void SaveReceivedCards(SaveFile saveFile, IMysteryGiftStorage cards, DataMysteryGift[] album)
        {
            if (cards is MysteryBlock4 s4)
            {
                // Replace the line causing the error with the following code
                s4.IsDeliveryManActive = album.Any(g => !g.IsEmpty);
                MysteryBlock4.UpdateSlotPGT(album, saveFile is SAV4HGSS);
                if (saveFile is SAV4HGSS hgss)
                {
                    hgss.LockCapsuleSlot = (PCD)album[^1];
                }
            }

            var count = cards.GiftCountMax;
            for (var i = 0; i < count; i++)
            {
                cards.SetMysteryGift(i, album[i]);
            }

            if (cards is MysteryBlock5 s5)
            {
                s5.EndAccess(); // need to encrypt the at-rest data with the seed.
            }
        }
    }

    public Task ImportMysteryGift(byte[] data, string fileExtension, out bool isSuccessful, out string resultsMessage)
    {
        var gift = MysteryGift.GetMysteryGift(data, fileExtension);
        if (gift is not null)
        {
            return ImportMysteryGift(gift, out isSuccessful, out resultsMessage);
        }

        isSuccessful = false;
        resultsMessage = "The Mystery Gift could not be imported.";
        return Task.CompletedTask;
    }

    public void MovePokemon(int? sourceBoxNumber, int sourceSlotNumber, bool isSourceParty,
        int? destBoxNumber, int destSlotNumber, bool isDestParty)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        // Validate slot numbers are non-negative
        if (sourceSlotNumber < 0 || destSlotNumber < 0)
        {
            return;
        }

        // Validate party slot bounds
        if (isSourceParty && sourceSlotNumber >= 6)
        {
            return;
        }

        if (isDestParty && destSlotNumber >= 6)
        {
            return;
        }

        // Validate box slot bounds
        if (!isSourceParty && sourceBoxNumber.HasValue && sourceSlotNumber >= saveFile.BoxSlotCount)
        {
            return;
        }

        if (!isDestParty && destBoxNumber.HasValue && destSlotNumber >= saveFile.BoxSlotCount)
        {
            return;
        }

        // Get source Pokémon
        PKM? sourcePokemon;
        if (isSourceParty)
        {
            sourcePokemon = saveFile.GetPartySlotAtIndex(sourceSlotNumber);
        }
        else if (sourceBoxNumber.HasValue)
        {
            sourcePokemon = saveFile.GetBoxSlotAtIndex(sourceBoxNumber.Value, sourceSlotNumber);
        }
        else // LetsGo storage
        {
            sourcePokemon = saveFile.GetBoxSlotAtIndex(sourceSlotNumber);
        }

        // Get destination Pokémon
        PKM? destPokemon;
        if (isDestParty)
        {
            destPokemon = saveFile.GetPartySlotAtIndex(destSlotNumber);
        }
        else if (destBoxNumber.HasValue)
        {
            destPokemon = saveFile.GetBoxSlotAtIndex(destBoxNumber.Value, destSlotNumber);
        }
        else // LetsGo storage
        {
            destPokemon = saveFile.GetBoxSlotAtIndex(destSlotNumber);
        }

        // Determine if this is a swap or a move
        var isSwap = sourcePokemon.Species > 0 && destPokemon.Species > 0;

        // Validate party requirements: must keep at least one non-Egg battle-ready Pokémon
        if (isSourceParty && !isDestParty && !isSwap)
        {
            // Moving from party to box (not a swap)
            // Count remaining battle-ready Pokémon after this move
            var battleReadyCount = 0;
            for (var i = 0; i < saveFile.PartyCount; i++)
            {
                if (i == sourceSlotNumber)
                {
                    continue; // Skip the one being moved
                }

                var partyMon = saveFile.GetPartySlotAtIndex(i);
                if (partyMon is { Species: > 0, IsEgg: false })
                {
                    battleReadyCount++;
                }
            }

            // Prevent move if it would leave no battle-ready Pokémon
            if (battleReadyCount == 0)
            {
                // Cannot move the last battle-ready Pokémon - silently prevent
                return;
            }
        }

        switch (isSourceParty)
        {
            // Special handling when moving from party: PKHeX.Core auto-compacts party
            // We need to use DeletePartySlot instead of SetPartySlotAtIndex for proper compacting
            case true when !isDestParty && !isSwap:
                {
                    // Moving FROM party TO box (non-swap case)
                    // Set the box slot first, then delete from party
                    // PKHeX will automatically compact the party
                    if (destBoxNumber.HasValue)
                    {
                        saveFile.SetBoxSlotAtIndex(sourcePokemon, destBoxNumber.Value, destSlotNumber);

                        // Gen 1 and Gen 2 boxes should be compacted like party (they were lists, not grids)
                        if (saveFile.Context is EntityContext.Gen1 or EntityContext.Gen2)
                        {
                            CompactBox(saveFile, destBoxNumber.Value);
                        }
                    }
                    else // LetsGo storage
                    {
                        saveFile.SetBoxSlotAtIndex(sourcePokemon, destSlotNumber);
                    }

                    // Delete from party using DeletePartySlot (properly compacts the party)
                    saveFile.DeletePartySlot(sourceSlotNumber);
                    break;
                }
            case false when isDestParty && !isSwap:
                {
                    // Moving FROM box TO party (non-swap case)
                    // Delete from box first
                    if (sourceBoxNumber.HasValue)
                    {
                        saveFile.SetBoxSlotAtIndex(saveFile.BlankPKM, sourceBoxNumber.Value, sourceSlotNumber);

                        // Gen 1 and Gen 2 boxes should be compacted like party (they were lists, not grids)
                        if (saveFile.Context is EntityContext.Gen1 or EntityContext.Gen2)
                        {
                            CompactBox(saveFile, sourceBoxNumber.Value);
                        }
                    }
                    else // LetsGo storage
                    {
                        saveFile.SetBoxSlotAtIndex(saveFile.BlankPKM, sourceSlotNumber);
                    }

                    // Add to party at the first available empty slot (or the specified slot if within PartyCount)
                    // PKHeX.Core's party is kept compact, so we should add at PartyCount position
                    // unless the user explicitly dropped on an occupied slot (which would be a swap)
                    // or on an empty slot within the current party range
                    var targetSlot = destSlotNumber;
                    if (destSlotNumber >= saveFile.PartyCount)
                    {
                        // User dropped beyond current party - add at end of party (PartyCount position)
                        targetSlot = saveFile.PartyCount;
                    }

                    saveFile.SetPartySlotAtIndex(sourcePokemon, targetSlot);
                    break;
                }
            default:
                {
                    if (isSwap)
                    {
                        // Swap: exchange the two Pokémon
                        // For swaps, we can set both at once since we're not creating/deleting slots
                        if (isSourceParty)
                        {
                            saveFile.SetPartySlotAtIndex(destPokemon, sourceSlotNumber);
                        }
                        else if (sourceBoxNumber.HasValue)
                        {
                            saveFile.SetBoxSlotAtIndex(destPokemon, sourceBoxNumber.Value, sourceSlotNumber);
                        }
                        else // LetsGo storage
                        {
                            saveFile.SetBoxSlotAtIndex(destPokemon, sourceSlotNumber);
                        }

                        if (isDestParty)
                        {
                            saveFile.SetPartySlotAtIndex(sourcePokemon, destSlotNumber);
                        }
                        else if (destBoxNumber.HasValue)
                        {
                            saveFile.SetBoxSlotAtIndex(sourcePokemon, destBoxNumber.Value, destSlotNumber);
                        }
                        else // LetsGo storage
                        {
                            saveFile.SetBoxSlotAtIndex(sourcePokemon, destSlotNumber);
                        }
                    }
                    else
                    {
                        // General case: move between boxes or within same storage
                        // Set destination first
                        if (isDestParty)
                        {
                            saveFile.SetPartySlotAtIndex(sourcePokemon, destSlotNumber);
                        }
                        else if (destBoxNumber.HasValue)
                        {
                            saveFile.SetBoxSlotAtIndex(sourcePokemon, destBoxNumber.Value, destSlotNumber);
                        }
                        else // LetsGo storage
                        {
                            saveFile.SetBoxSlotAtIndex(sourcePokemon, destSlotNumber);
                        }

                        // Then blank out source
                        if (isSourceParty)
                        {
                            // Use DeletePartySlot for proper party compacting
                            saveFile.DeletePartySlot(sourceSlotNumber);
                        }
                        else if (sourceBoxNumber.HasValue)
                        {
                            saveFile.SetBoxSlotAtIndex(saveFile.BlankPKM, sourceBoxNumber.Value, sourceSlotNumber);

                            // Gen 1 and Gen 2 boxes should be compacted like party (they were lists, not grids)
                            if (saveFile.Context is EntityContext.Gen1 or EntityContext.Gen2)
                            {
                                CompactBox(saveFile, sourceBoxNumber.Value);
                            }
                        }
                        else // LetsGo storage
                        {
                            saveFile.SetBoxSlotAtIndex(saveFile.BlankPKM, sourceSlotNumber);
                        }

                        // For Gen 1/2: If we just moved within the same box or moved into a box, compact the destination box too
                        if (saveFile.Context is EntityContext.Gen1 or EntityContext.Gen2 && !isDestParty && destBoxNumber.HasValue)
                        {
                            // Check if destination box differs from source box, or if we're moving within same box
                            // Either way, compact the destination box to ensure proper list format
                            if (!isSourceParty)
                            {
                                CompactBox(saveFile, destBoxNumber.Value);
                            }
                        }
                    }

                    break;
                }
        }

        // Refresh the UI based on what changed
        if (isSourceParty || isDestParty)
        {
            if (saveFile is SAV7b)
            {
                RefreshService.RefreshBoxAndPartyState();
            }
            else
            {
                RefreshService.RefreshPartyState();
                if (!isSourceParty || !isDestParty)
                {
                    RefreshService.RefreshBoxState();
                }
            }
        }
        else
        {
            RefreshService.RefreshBoxState();
        }
    }

    private void HandleNullOrEmptyPokemon()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        EditFormPokemon ??= saveFile.BlankPKM;

        if (EditFormPokemon.Species.IsInvalidSpecies())
        {
            EditFormPokemon.Version = saveFile.Version.GetSingleVersion();
        }
    }

    /// <summary>
    /// Compacts a box by shifting all Pokémon left to fill gaps (for Gen 1 and Gen 2 games).
    /// In these generations, boxes were lists, not grids, so they should have no gaps.
    /// </summary>
    private static void CompactBox(SaveFile saveFile, int boxNumber)
    {
        var boxSlotCount = saveFile.BoxSlotCount;
        var compacted = new PKM[boxSlotCount];
        var writeIndex = 0;

        // Collect all non-blank Pokémon
        for (var i = 0; i < boxSlotCount; i++)
        {
            var pkm = saveFile.GetBoxSlotAtIndex(boxNumber, i);
            if (pkm.Species > 0)
            {
                compacted[writeIndex++] = pkm;
            }
        }

        // Fill remaining slots with blank Pokémon
        for (var i = writeIndex; i < boxSlotCount; i++)
        {
            compacted[i] = saveFile.BlankPKM;
        }

        // Write the compacted box back
        for (var i = 0; i < boxSlotCount; i++)
        {
            saveFile.SetBoxSlotAtIndex(compacted[i], boxNumber, i);
        }
    }
}
