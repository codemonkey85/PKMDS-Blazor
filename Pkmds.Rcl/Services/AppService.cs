namespace Pkmds.Rcl.Services;

public class AppService(IAppState appState, IRefreshService refreshService, ILegalizationService legalizationService) : IAppService
{
    private const string EnglishLang = "en";
    private const string DefaultPkmFileName = "pkm.bin";

    // Number of items to return from Search* methods when the query is empty. The autocomplete
    // dropdown uses this to populate an initial "preview" list so users see something to pick
    // from before they start typing. Matches the Ribbons filter's top-N behavior.
    private const int AutocompleteEmptyQueryTake = 30;

    // Block keys duplicated from SaveBlockAccessor8SWSH / SaveBlockAccessor9SV
    // because they are private in PKHeX.Core. Update if upstream changes.
    private const uint SwshTeamNamesKey = 0x1920C1E4;
    private const uint SvTeamNamesKey = 0x1920C1E4;

    private const uint SvRentalTeamsKey = 0x19CB0339;

    private const int TeamCount = 6;

    private const int SlotsPerTeam = 6;

    // Team name: 10 char16 + null terminator = 22 bytes
    private const int TeamNameByteLength = 22;

    private static readonly uint[] SwshRentalTeamKeys =
    [
        0x149A1DD0, // KRentalTeam1
        0x179A2289, // KRentalTeam2
        0x169A20F6, // KRentalTeam3
        0x199A25AF, // KRentalTeam4
        0x189A241C // KRentalTeam5
    ];

    private IAppState AppState { get; } = appState;

    private IRefreshService RefreshService { get; } = refreshService;

    private ILegalizationService LegalizationService { get; } = legalizationService;

    private static string[] NatureStatShortNames => ["Atk", "Def", "Spe", "SpA", "SpD"];

    public PKM? EditFormPokemon
    {
        get;
        set
        {
            field = value?.Clone();

            // Skip stat recalculation when the Pokémon already has persistent party
            // stats that we don't want to overwrite:
            // - HaX mode party Pokémon: user may have hand-edited battle stats
            // - PB7 (Let's Go): all storage is unified (SIZE_PARTY == SIZE_STORED),
            //   so party stats including current HP and status condition persist in
            //   the box and should not be reset
            var hasPersistedPartyStats = field is PB7 { PartyStatsPresent: true }
                                         || AppState.IsHaXEnabled && AppState.SelectedPartySlotNumber is not null;
            if (!hasPersistedPartyStats)
            {
                LoadPokemonStats(field);
            }

            SnapshotEditFormBaseline();
        }
    }

    // Bytes of EditFormPokemon at the moment it was last assigned (or last saved). Compared
    // against current bytes by EditFormHasUnsavedChanges to detect real user edits. Compared
    // against the slot's bytes instead would produce false positives, because the setter
    // clones + LoadPokemonStats normalizes the PKM (and Save round-trips can subtly differ).
    private byte[]? editFormBaselineBytes;

    private void SnapshotEditFormBaseline()
    {
        if (EditFormPokemon is not { } pokemon)
        {
            editFormBaselineBytes = null;
            return;
        }

        editFormBaselineBytes = new byte[pokemon.SIZE_STORED];
        pokemon.WriteDecryptedDataStored(editFormBaselineBytes);
    }

    public void ClearSelection()
    {
        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;
        AppState.SelectedPartySlotNumber = null;
        EditFormPokemon = null;
        RefreshService.Refresh();
    }

    public void PinBox(int boxNumber)
    {
        AppState.PinnedBoxNumber = boxNumber;
        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;
        AppState.SelectedPartySlotNumber = null;
        EditFormPokemon = null;
        RefreshService.Refresh();
    }

    public void UnpinBox()
    {
        AppState.PinnedBoxNumber = null;
        RefreshService.Refresh();
    }

    public string GetPokemonSpeciesName(ushort speciesId) => GetSpeciesComboItem(speciesId).Text;

    public IEnumerable<ComboItem> SearchPokemonNames(string searchString)
    {
        if (AppState.SaveFile is null)
        {
            return [];
        }

        var source = GameInfo.FilteredSources.Species
            .DistinctBy(species => species.Value);

        return string.IsNullOrEmpty(searchString)
            ? source.OrderBy(species => species.Text).Take(AutocompleteEmptyQueryTake)
            : source
                .Where(species => species.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                .OrderBy(species => species.Text);
    }

    public ComboItem GetSpeciesComboItem(ushort speciesId) => GameInfo.FilteredSources.Species
        .DistinctBy(species => species.Value)
        .FirstOrDefault(species => species.Value == speciesId) ?? new(string.Empty, (int)Species.None);

    public IEnumerable<ComboItem> SearchItemNames(string searchString)
    {
        if (AppState.SaveFile is null)
        {
            return [];
        }

        var source = GameInfo.FilteredSources.Items
            .DistinctBy(item => item.Value);

        return string.IsNullOrEmpty(searchString)
            ? source.OrderBy(item => item.Text).Take(AutocompleteEmptyQueryTake)
            : source
                .Where(item => item.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Text);
    }

    public ComboItem GetItemComboItem(int itemId) => GameInfo.FilteredSources.Items
        .DistinctBy(item => item.Value)
        .FirstOrDefault(item => item.Value == itemId) ?? new ComboItem(string.Empty, itemId);

    public string GetStatModifierString(Nature nature)
    {
        var (up, down) = nature.GetNatureModification();
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

        // Preserve current HP for party Pokémon — SetStats overwrites
        // Stat_HPCurrent with Stat_HPMax, losing any user-set value.
        var previousHp = pokemon.PartyStatsPresent
            ? pokemon.Stat_HPCurrent
            : -1;
        pokemon.SetStats(stats);
        if (previousHp >= 0)
        {
            pokemon.Stat_HPCurrent = Math.Min(previousHp, pokemon.Stat_HPMax);
        }
    }

    public IEnumerable<ComboItem> SearchMetLocations(string searchString, GameVersion gameVersion,
        EntityContext entityContext, bool isEggLocation = false)
    {
        if (AppState.SaveFile is null)
        {
            return [];
        }

        var source = GameInfo.GetLocationList(gameVersion, entityContext, isEggLocation)
            .DistinctBy(l => l.Value);

        return string.IsNullOrEmpty(searchString)
            ? source.OrderBy(l => l.Text).Take(AutocompleteEmptyQueryTake)
            : source
                .Where(l => l.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => l.Text);
    }

    public ComboItem GetMetLocationComboItem(ushort metLocationId, GameVersion gameVersion, EntityContext entityContext,
        bool isEggLocation = false) => AppState.SaveFile is null
        ? null!
        : GameInfo.GetLocationList(gameVersion, entityContext, isEggLocation)
            .DistinctBy(l => l.Value)
            .FirstOrDefault(metLocation => metLocation.Value == metLocationId) ?? null!;

    public IEnumerable<ComboItem> SearchMoves(string searchString)
    {
        if (AppState.SaveFile is null)
        {
            return [];
        }

        var source = GameInfo.FilteredSources.Moves
            .DistinctBy(move => move.Value);

        return string.IsNullOrEmpty(searchString)
            ? source.OrderBy(move => move.Text).Take(AutocompleteEmptyQueryTake)
            : source
                .Where(move => move.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                .OrderBy(move => move.Text);
    }

    public IEnumerable<ComboItem> GetMoves() => AppState.SaveFile is null
        ? []
        : GameInfo.FilteredSources.Moves
            .DistinctBy(move => move.Value)
            .OrderBy(move => move.Text);

    public ComboItem GetMoveComboItem(int moveId)
    {
        var item = GameInfo.FilteredSources.Moves
            .DistinctBy(move => move.Value)
            .FirstOrDefault(m => m.Value == moveId);
        if (item != null)
        {
            return item;
        }

        // FilteredSources.Moves is capped at the save's MaxMoveID; DLC moves (e.g. Blood Moon
        // for a base-game save) won't be in the list even though the move ID is stored in the
        // PKM. Fall back to the full move string table so the name always displays correctly.
        var strings = GameInfo.Strings;
        if (moveId > 0 && moveId < strings.Move.Count)
        {
            return new ComboItem(strings.Move[moveId], moveId);
        }

        return null!;
    }

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
                // If the edited slot was past PartyCount (e.g. HaX mode editing an empty slot)
                // the write would leave a gap; party is always a packed list, so compact.
                AppState.SaveFile.CompactParty();

                // Let's Go games store Pokémon in a unified storage system
                // Changes to party affect box display, so refresh both
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
                AppState.SaveFile.CompactBoxIfGen12(boxNumber);
                RefreshService.RefreshBoxState();
                break;
            case SelectedPokemonType.None when AppState.SaveFile is SAV7b:
                // Let's Go unified storage: save to box slot without box number
                AppState.SaveFile.SetBoxSlotAtIndex(pokemon, boxSlot);
                RefreshService.RefreshBoxAndPartyState();
                break;
        }

        // After saving, the edit form's current bytes are the user's accepted state — refresh
        // the baseline so EditFormHasUnsavedChanges doesn't keep reporting dirty until the
        // next selection change.
        SnapshotEditFormBaseline();
    }

    public bool EditFormHasUnsavedChanges()
    {
        if (EditFormPokemon is not { } pokemon || editFormBaselineBytes is null)
        {
            return false;
        }

        if (pokemon.SIZE_STORED != editFormBaselineBytes.Length)
        {
            return false;
        }

        var currentBytes = new byte[pokemon.SIZE_STORED];
        pokemon.WriteDecryptedDataStored(currentBytes);

        return !currentBytes.AsSpan().SequenceEqual(editFormBaselineBytes);
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
        AppState.PinnedBoxNumber = null;
        AppState.SelectedPartySlotNumber = null;

        AppState.SelectedBoxNumber = boxNumber;
        AppState.SelectedBoxSlotNumber = slotNumber;
        EditFormPokemon = pkm;

        if (AppState is { SaveFile: { } saveFile, BoxEdit: { } boxEdit })
        {
            boxEdit.LoadBox(boxNumber);
            saveFile.CurrentBox = boxEdit.CurrentBox;
        }

        HandleNullOrEmptyPokemon();
        RefreshService.Refresh();
    }

    public void SetSelectedPartyPokemon(PKM? pkm, int slotNumber)
    {
        AppState.PinnedBoxNumber = null;
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
        if (AppState.SaveFile is not { PartyCount: var partyCount } saveFile || partyCount == 0)
        {
            return string.Empty;
        }

        var sbShowdown = new StringBuilder();

        for (var slot = 0; slot < partyCount; slot++)
        {
            var pkm = saveFile.GetPartySlotAtIndex(slot);

            sbShowdown
                .AppendLine(ShowdownParsing.GetShowdownText(pkm))
                .AppendLine();
        }

        return sbShowdown.ToString().Trim();
    }

    public string ExportBoxAsShowdown(int boxNumber)
    {
        if (AppState.SaveFile is not { } sav)
        {
            return string.Empty;
        }

        var sbShowdown = new StringBuilder();

        for (var slot = 0; slot < sav.BoxSlotCount; slot++)
        {
            var pkm = sav.GetBoxSlotAtIndex(boxNumber, slot);
            if (pkm.Species == 0)
            {
                continue;
            }

            sbShowdown
                .AppendLine(ShowdownParsing.GetShowdownText(pkm))
                .AppendLine();
        }

        return sbShowdown.ToString().TrimEnd();
    }

    public IReadOnlyList<ShowdownSet> ParseShowdownText(string text) =>
        [.. ShowdownParsing.GetShowdownSets(text).Where(s => s.Species != 0)];

    public PKM? ConvertShowdownSetToPkm(ShowdownSet set)
    {
        if (AppState.SaveFile is not { } sav || set.Species == 0)
        {
            return null;
        }

        // Delegate to the Auto-Legality engine, which handles PID/IV correlation for
        // Gen 3-5, RNG-correlated encounters for Gen 8/9, egg hatching, alternate-form
        // fallback, and post-generation fixes. On Failed/Timeout, the outcome still
        // carries the best-effort attempt (with post-generation fixes already applied),
        // so we return it rather than a raw blank — the user can edit it further.
        var result = LegalizationService.GenerateFromSetSync(set, sav);
        return result.Pokemon;
    }

    public bool TryPlacePokemonInPartySlot(PKM pkm)
    {
        if (AppState.SaveFile is not { } sav || sav.PartyCount >= 6)
        {
            return false;
        }

        sav.SetPartySlotAtIndex(pkm, sav.PartyCount);
        RefreshService.RefreshPartyState();
        return true;
    }

    public int OverwriteParty(IReadOnlyList<PKM> pokemon)
    {
        if (AppState.SaveFile is not { } sav || pokemon.Count == 0)
        {
            return 0;
        }

        var list = pokemon.Take(6).ToList();
        sav.PartyData = list;
        RefreshService.RefreshPartyState();
        return list.Count;
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

    public bool TrySelectFirstEmptyBoxSlot()
    {
        if (AppState.SaveFile is not { } sav)
        {
            return false;
        }

        for (var box = 0; box < sav.BoxCount; box++)
        {
            for (var slot = 0; slot < sav.BoxSlotCount; slot++)
            {
                if (sav.GetBoxSlotAtIndex(box, slot).Species != 0)
                {
                    continue;
                }

                if (sav is SAV7b)
                {
                    // Let's Go uses a flat index across unified storage.
                    SetSelectedLetsGoPokemon(sav.BlankPKM, box * sav.BoxSlotCount + slot);
                }
                else
                {
                    SetSelectedBoxPokemon(sav.BlankPKM, box, slot);
                }

                return true;
            }
        }

        return false;
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

    public Task ImportWonderCard3(byte[] data, out bool isSuccessful, out string resultsMessage)
    {
        // WC3 files are not handled by FileUtil.TryGetMysteryGift / MysteryGift.GetMysteryGift —
        // WonderCard3 is stored verbatim in the save's wonder card slot rather than as a generic
        // DataMysteryGift. Emulate the WC3Plugin import sequence: write the card, optionally the
        // link-stats extra block, and the accompanying MysteryEvent3 script.
        if (AppState.SaveFile is not SAV3 sav)
        {
            isSuccessful = false;
            resultsMessage =
                "The loaded save file does not support WC3 Wonder Cards. Only Generation 3 saves accept WC3 files.";
            return Task.CompletedTask;
        }

        if (sav.LargeBlock is not ISaveBlock3LargeExpansion expansion)
        {
            isSuccessful = false;
            resultsMessage =
                "The loaded save file does not support WC3 Wonder Cards. Only Emerald and FireRed/LeafGreen saves accept WC3 files.";
            return Task.CompletedTask;
        }

        var cardSize = sav.Japanese ? WonderCard3.SIZE_JAP : WonderCard3.SIZE;
        var scriptOffset = cardSize + (WonderCard3Extra.SIZE * 2);
        var expectedSize = scriptOffset + MysteryEvent3.SIZE;

        if (data.Length != expectedSize)
        {
            // The other locale's expected size, used to detect a locale mismatch (e.g. a JPN
            // .wc3 loaded into an International save) and surface a clearer error than a raw
            // byte-count mismatch.
            var otherCardSize = sav.Japanese ? WonderCard3.SIZE : WonderCard3.SIZE_JAP;
            var otherExpectedSize = otherCardSize + (WonderCard3Extra.SIZE * 2) + MysteryEvent3.SIZE;

            isSuccessful = false;
            resultsMessage = data.Length == otherExpectedSize
                ? $"This is a {(sav.Japanese ? "International" : "Japanese")} Wonder Card ({data.Length} bytes), " +
                  $"but the loaded save is {(sav.Japanese ? "Japanese" : "International")}. " +
                  $"Use a {(sav.Japanese ? "Japanese" : "International")} ({expectedSize}-byte) WC3 file."
                : $"Invalid WC3 file size ({data.Length} bytes; expected {expectedSize} bytes for this save).";
            return Task.CompletedTask;
        }

        try
        {
            Memory<byte> memory = data;

            var card = new WonderCard3(memory[..cardSize]);
            card.FixChecksum();
            expansion.SetWonderCard(sav.Japanese, card.Data);

            // Type 2 = link-stats card; the first WonderCard3Extra block records wins/losses/trades.
            const byte CardTypeLinkStats = 2;
            if (card.Type == CardTypeLinkStats)
            {
                var extra = new WonderCard3Extra(memory.Slice(cardSize, WonderCard3Extra.SIZE));
                expansion.SetWonderCardExtra(sav.Japanese, extra.Data);
            }

            var script = new MysteryEvent3(memory[scriptOffset..]);
            script.FixChecksum();
            expansion.MysteryData = script;

            var title = card.Title.Trim();
            isSuccessful = true;
            resultsMessage = string.IsNullOrEmpty(title)
                ? "Wonder Card imported successfully."
                : $"Wonder Card \"{title}\" imported successfully.";
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            isSuccessful = false;
            resultsMessage = ex.Message;
            return Task.CompletedTask;
        }
    }

    public bool HasWonderCardSlots() => AppState.SaveFile switch
    {
        SAV3 sav => sav.LargeBlock is ISaveBlock3LargeExpansion,
        // Intentionally probes the interface rather than hardcoding SAV4..SAV7b: in PKHeX 26.4.11
        // only those generations implement IMysteryGiftStorageProvider (SAV8 / SAV9 deliberately
        // do not — BDSP's MysteryBlock8b is a separate received-gift history block, tracked in
        // #813). If upstream ever extends the interface to a future generation, this picks it up
        // automatically rather than silently ignoring the new save format.
        IMysteryGiftStorageProvider => true,
        _ => false,
    };

    public IReadOnlyList<WonderCardSlotInfo> GetWonderCardSlots()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return [];
        }

        if (saveFile is SAV3 sav3 && sav3.LargeBlock is ISaveBlock3LargeExpansion expansion)
        {
            return BuildGen3Slots(sav3, expansion);
        }

        if (saveFile is IMysteryGiftStorageProvider provider)
        {
            return BuildModernSlots(saveFile, provider);
        }

        return [];

        static IReadOnlyList<WonderCardSlotInfo> BuildGen3Slots(SAV3 sav, ISaveBlock3LargeExpansion expansion)
        {
            var card = expansion.GetWonderCard(sav.Japanese);
            // The wonder card slot is initialised to 0xFF / 0x00 padding on a fresh save.
            // Treat any slot whose card body has no real bytes set as empty (mirrors WC3Plugin's
            // HasWC3 check). CardID == 0 alone is not enough — some valid cards have ID 0.
            var isEmpty = !card.Data.ContainsAnyExcept<byte>(0, 0xFF);
            var title = card.Title.Replace('　', ' ').Trim();

            // The Mystery Event script lives in a separate slot; surface its presence as an
            // info note so users can confirm the script was written alongside the card.
            var scriptPresent = expansion.MysteryData.Data.ContainsAnyExcept<byte>(0, 0xFF);
            string? extra = (card.Type, scriptPresent) switch
            {
                (2, true) => "Link card; Mystery Event script present",
                (2, false) => "Link card",
                (_, true) => "Mystery Event script present",
                _ => null,
            };

            return
            [
                new WonderCardSlotInfo
                {
                    Index = 0,
                    Title = isEmpty ? "(empty)" : (string.IsNullOrEmpty(title) ? "(no title)" : title),
                    CardType = nameof(WonderCard3),
                    CardId = isEmpty ? null : card.CardID,
                    Species = null,
                    IsEmpty = isEmpty,
                    Received = null,
                    // Surface the script-present note even for empty card slots: a Mystery Event
                    // script can legitimately exist on its own (e.g. the user cleared the card
                    // but the script remains from a prior import). Suppress only the link-card
                    // sub-text when the card itself is empty, since "Type" comes from the card.
                    ExtraInfo = isEmpty
                        ? scriptPresent ? "Mystery Event script present" : null
                        : extra,
                },
            ];
        }

        static IReadOnlyList<WonderCardSlotInfo> BuildModernSlots(SaveFile saveFile, IMysteryGiftStorageProvider provider)
        {
            var storage = provider.MysteryGiftStorage;
            var flags = storage as IMysteryGiftFlags;
            var count = storage.GiftCountMax;
            var includeLockCapsule = saveFile is SAV4HGSS;
            var slots = new List<WonderCardSlotInfo>(count + (includeLockCapsule ? 1 : 0));

            for (var i = 0; i < count; i++)
            {
                slots.Add(BuildModernSlot(storage.GetMysteryGift(i), i, flags, extra: null));
            }

            if (includeLockCapsule && saveFile is SAV4HGSS hgss)
            {
                slots.Add(BuildModernSlot(hgss.LockCapsuleSlot, count, flags, extra: "Lock Capsule"));
            }

            return slots;
        }

        static WonderCardSlotInfo BuildModernSlot(DataMysteryGift gift, int index, IMysteryGiftFlags? flags, string? extra)
        {
            var isEmpty = gift.IsEmpty;
            var title = isEmpty ? "(empty)" : gift.CardTitle.Replace('　', ' ').Trim();
            ushort? species = isEmpty || !gift.IsEntity ? null : gift.Species;
            var cardId = isEmpty ? (int?)null : gift.CardID;

            bool? received = null;
            if (!isEmpty && flags is not null && cardId is { } id && (uint)id < flags.MysteryGiftReceivedFlagMax)
            {
                received = flags.GetMysteryGiftReceivedFlag(id);
            }

            return new WonderCardSlotInfo
            {
                Index = index,
                Title = string.IsNullOrEmpty(title) ? (isEmpty ? "(empty)" : "(no title)") : title,
                CardType = gift.GetType().Name,
                CardId = cardId,
                Species = species,
                IsEmpty = isEmpty,
                Received = received,
                ExtraInfo = extra,
            };
        }
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
        var sourcePokemon = isSourceParty
            ? saveFile.GetPartySlotAtIndex(sourceSlotNumber)
            : sourceBoxNumber.HasValue
                ? saveFile.GetBoxSlotAtIndex(sourceBoxNumber.Value, sourceSlotNumber)
                : saveFile.GetBoxSlotAtIndex(sourceSlotNumber);

        // Get destination Pokémon
        var destPokemon = isDestParty
            ? saveFile.GetPartySlotAtIndex(destSlotNumber)
            : destBoxNumber.HasValue
                ? saveFile.GetBoxSlotAtIndex(destBoxNumber.Value, destSlotNumber)
                : saveFile.GetBoxSlotAtIndex(destSlotNumber);

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

                        saveFile.CompactBoxIfGen12(destBoxNumber.Value);
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

                        saveFile.CompactBoxIfGen12(sourceBoxNumber.Value);
                    }
                    else // LetsGo storage
                    {
                        saveFile.SetBoxSlotAtIndex(saveFile.BlankPKM, sourceSlotNumber);
                    }

                    // Realign beyond-PartyCount drops to the append position so dropping on slot
                    // 5 with only 1 party member lands at slot 1, not slot 5 (mirrors PKHeX's
                    // SlotInfoParty.WriteTo). CompactParty is a safety net in case an upstream
                    // state was already inconsistent.
                    var targetSlot = destSlotNumber >= saveFile.PartyCount
                        ? saveFile.PartyCount
                        : destSlotNumber;

                    saveFile.SetPartySlotAtIndex(sourcePokemon, targetSlot);
                    saveFile.CompactParty();
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
                            saveFile.CompactBoxIfGen12(sourceBoxNumber.Value);
                        }
                        else // LetsGo storage
                        {
                            saveFile.SetBoxSlotAtIndex(saveFile.BlankPKM, sourceSlotNumber);
                        }

                        // Compact the destination too: dropping past the last filled slot (party
                        // in any gen, or a Gen 1/2 box) otherwise leaves a gap. For party→party
                        // moves, DeletePartySlot above can additionally leave PartyCount in an
                        // inconsistent state when the drop target was past the original count.
                        if (isDestParty)
                        {
                            saveFile.CompactParty();
                        }
                        else if (destBoxNumber.HasValue)
                        {
                            saveFile.CompactBoxIfGen12(destBoxNumber.Value);
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

    public IEnumerable<ComboItem> GetMemoryComboItems() =>
        new MemoryStrings(GameInfo.Strings).Memory;

    public IEnumerable<ComboItem> GetMemoryFeelingComboItems(int memoryGen)
    {
        var feelings = new MemoryStrings(GameInfo.Strings).GetMemoryFeelings(memoryGen).ToArray();
        return feelings.Select((f, i) => new ComboItem(f, i));
    }

    public IEnumerable<ComboItem> GetMemoryQualityComboItems()
    {
        var qualities = new MemoryStrings(GameInfo.Strings).GetMemoryQualities().ToArray();
        return qualities.Select((q, i) => new ComboItem(q, i));
    }

    public IEnumerable<ComboItem> GetMemoryArgumentComboItems(MemoryArgType argType, int memoryGen) =>
        new MemoryStrings(GameInfo.Strings).GetArgumentStrings(argType, memoryGen);

    public IEnumerable<ComboItem> GetLanguageComboItems(int generation, EntityContext context) =>
        GameInfo.LanguageDataSource((byte)generation, context);

    public IEnumerable<ComboItem> GetGeoCountryComboItems()
    {
        var items = new List<ComboItem> { new("—", 0) };
        for (var i = 1; i <= 255; i++)
        {
            var name = GeoLocation.GetCountryName("en", (byte)i);
            if (!string.IsNullOrWhiteSpace(name) && name != "INVALID")
            {
                items.Add(new ComboItem(name, i));
            }
        }

        return items;
    }

    public IEnumerable<ComboItem> GetGeoRegionComboItems(byte country)
    {
        if (country == 0)
        {
            return [new ComboItem("—", 0)];
        }

        var items = new List<ComboItem> { new("—", 0) };
        for (var regionId = 1; regionId <= 255; regionId++)
        {
            var name = GeoLocation.GetRegionName("en", country, (byte)regionId);
            if (!string.IsNullOrWhiteSpace(name) && name != "INVALID")
            {
                items.Add(new ComboItem(name, regionId));
            }
        }

        return items;
    }

    public IReadOnlyList<ComboItem> GetConsoleRegionComboItems() =>
        GameInfo.FilteredSources.ConsoleRegions;

    public LegalityAnalysis GetLegalityAnalysis(PKM pkm, bool isParty = false)
    {
        // PKHeX's per-format SetPKM hook (e.g. SAV8BS.SetPKM → pb8.UpdateHandler)
        // runs on every slot write and normalises CurrentHandler / HT fields to
        // match the loaded trainer. Raw slots read via GetBoxSlotAtIndex skip
        // that step, so the report can flag an "Invalid Current handler value"
        // that will silently disappear the next time anything writes the slot.
        // Mirror the adapt-on-write behaviour by analysing a cloned-and-adapted
        // copy so the displayed status matches what the save will actually hold.
        if (AppState.SaveFile is { } sav && pkm.GetType() == sav.PKMType)
        {
            var clone = pkm.Clone();
            sav.AdaptToSaveFile(clone, isParty);
            return new LegalityAnalysis(clone);
        }

        return new LegalityAnalysis(pkm);
    }

    public IEnumerable<AdvancedSearchResult> SearchPokemon(AdvancedSearchFilter filter)
    {
        if (AppState.SaveFile is not { } sav)
        {
            yield break;
        }

        // Party slots
        for (var i = 0; i < sav.PartyCount; i++)
        {
            var pkm = sav.GetPartySlotAtIndex(i);
            if (pkm is not { Species: > 0 })
            {
                continue;
            }

            if (Matches(pkm, filter))
            {
                yield return BuildSearchResult(pkm, true, 0, i);
            }
        }

        // Box slots
        for (var box = 0; box < sav.BoxCount; box++)
        {
            for (var slot = 0; slot < sav.BoxSlotCount; slot++)
            {
                var pkm = sav.GetBoxSlotAtIndex(box, slot);
                if (pkm is not { Species: > 0 })
                {
                    continue;
                }

                if (Matches(pkm, filter))
                {
                    yield return BuildSearchResult(pkm, false, box, slot);
                }
            }
        }
    }

    // ── Encounter Database ────────────────────────────────────────────────

    public IEnumerable<EncounterSearchResult> SearchEncounters(EncounterSearchFilter filter)
    {
        if (AppState.SaveFile is not { } sav)
        {
            yield break;
        }

        if (filter.Species is not { } species)
        {
            yield break;
        }

        var blankPkm = sav.BlankPKM.Clone();
        blankPkm.Species = species;
        if (filter.Form is { } form)
        {
            blankPkm.Form = form;
        }

        // When Version is null, pass an empty array so GenerateEncounters searches all
        // versions compatible with the PKM's format internally.
        var versions = filter.Version is { } v
            ? new[] { v }
            : [];

        var encounters =
            EncounterMovesetGenerator.GenerateEncounters(blankPkm, sav, ReadOnlyMemory<ushort>.Empty, versions);

        // Deduplicate by reference: WC8/MysteryGift objects are classes and the generator can
        // return the same instance multiple times (once per compatible game version, e.g. SW and SH).
        var seen = new HashSet<IEncounterable>(ReferenceEqualityComparer.Instance);

        foreach (var enc in encounters)
        {
            if (!seen.Add(enc))
            {
                continue;
            }

            // Level range filter — skip if the encounter's range doesn't overlap the requested range.
            if (filter.LevelMin is { } lmin && enc.LevelMax < lmin)
            {
                continue;
            }

            if (filter.LevelMax is { } lmax && enc.LevelMin > lmax)
            {
                continue;
            }

            // Shiny lock filter.
            if (filter.IsShinyLocked is { } shinyLocked)
            {
                var isLocked = enc.Shiny == Shiny.Never;
                if (isLocked != shinyLocked)
                {
                    continue;
                }
            }

            // Encounter type group filter.
            if (filter.EncounterGroup is { } group && GetEncounterTypeGroup(enc) != group)
            {
                continue;
            }

            yield return BuildEncounterResult(enc);
        }
    }

    public PKM? GeneratePokemonFromEncounter(IEncounterable encounter)
    {
        if (AppState.SaveFile is not { } sav)
        {
            return null;
        }

        // Always return the generated PKM — do not gate on LegalityAnalysis here.
        // Some encounter types (e.g. HOME Mystery Gifts) may not pass a strict legality
        // check even when generated correctly; the caller can run a legality check separately
        // and surface the result to the user.
        var pkm = encounter.ConvertToPKM(sav);

        // Some cross-game encounters (e.g. a Legends: Arceus static returning PA8 when the
        // loaded save is BDSP, which requires PB8) produce a PKM in the wrong format.
        // Attempt a format conversion via EntityConverter; return null if none is possible
        // so the caller can surface a meaningful error instead of crashing.
        var expectedType = sav.BlankPKM.GetType();
        if (pkm.GetType() != expectedType)
        {
            pkm = EntityConverter.ConvertToType(pkm, expectedType, out _);
        }

        // PKHeX's string encoding can silently produce an empty OT name when the save's
        // character encoding doesn't round-trip cleanly (e.g. Japanese Gen 3 saves where
        // StringConverter3 stops encoding at the first unrecognised character).
        // For non-trade encounters the OT should always be the player's own trainer name,
        // so fall back to the save file directly if the generated PKM has no OT.
        if (pkm is null || !string.IsNullOrEmpty(pkm.OriginalTrainerName)
                        || encounter is IFixedTrainer
                        || string.IsNullOrEmpty(sav.OT))
        {
            return pkm;
        }

        pkm.OriginalTrainerName = sav.OT;
        pkm.OriginalTrainerGender = sav.Gender;

        return pkm;
    }

    public bool SwapBoxes(int boxA, int boxB)
    {
        if (AppState.SaveFile is not { } sav)
        {
            return false;
        }

        var success = sav.SwapBox(boxA, boxB);
        if (success)
        {
            RefreshService.RefreshBoxState();
        }

        return success;
    }

    public IReadOnlyList<EvolutionMethod> GetDirectEvolutions(PKM pkm)
    {
        var tree = EvolutionTree.GetEvolutionTree(pkm.Context);
        var methods = tree.Forward.GetForward(pkm.Species, pkm.Form);
        var canHaveContest = pkm.CanHaveContestStats();
        return
        [
            .. methods.Span
                .ToArray()
                .Where(m => m.Species != 0
                            && m.Method != EvolutionType.LevelUpShedinja
                            && (m.Method != EvolutionType.LevelUpBeauty || canHaveContest))
                .OrderBy(m => m.Species)
        ];
    }


    public bool TryPlacePokemonInFirstAvailableSlot(PKM pkm)
    {
        if (AppState.SaveFile is not { } sav)
        {
            return false;
        }

        // Prefer an open party slot over a box slot.
        if (sav.PartyCount < 6)
        {
            sav.SetPartySlotAtIndex(pkm, sav.PartyCount);
            RefreshService.RefreshPartyState();
            return true;
        }

        // Scan boxes in order for the first empty slot (Species == 0).
        for (var box = 0; box < sav.BoxCount; box++)
        {
            for (var slot = 0; slot < sav.BoxSlotCount; slot++)
            {
                if (sav.GetBoxSlotAtIndex(box, slot).Species != 0)
                {
                    continue;
                }

                sav.SetBoxSlotAtIndex(pkm, box, slot);
                RefreshService.RefreshBoxState();
                return true;
            }
        }

        return false;
    }

    public bool HasBattleBox() => AppState.SaveFile is SAV5 or SAV6XY or SAV6AO;

    public IReadOnlyList<PKM> GetBattleBoxPokemon()
    {
        var result = new List<PKM>();
        switch (AppState.SaveFile)
        {
            case SAV5 sav5:
                for (var i = 0; i < BattleBox5.Count; i++)
                {
                    var pkm = sav5.GetStoredSlot(sav5.BattleBox[i].Span);
                    if (pkm.Species > 0)
                    {
                        result.Add(pkm);
                    }
                }

                break;
            case SAV6XY xy:
                for (var i = 0; i < BattleBox6.Count; i++)
                {
                    var pkm = xy.GetStoredSlot(xy.BattleBox[i].Span);
                    if (pkm.Species > 0)
                    {
                        result.Add(pkm);
                    }
                }

                break;
            case SAV6AO ao:
                for (var i = 0; i < BattleBox6.Count; i++)
                {
                    var pkm = ao.GetStoredSlot(ao.BattleBox[i].Span);
                    if (pkm.Species > 0)
                    {
                        result.Add(pkm);
                    }
                }

                break;
        }

        return result;
    }

    public bool IsBattleBoxLocked() => AppState.SaveFile switch
    {
        SAV5 sav5 => sav5.BattleBox.BattleBoxLocked,
        SAV6XY xy => xy.BattleBox.Locked,
        SAV6AO ao => ao.BattleBox.Locked,
        _ => false
    };

    public bool HasBattleTeams() => AppState.SaveFile is SAV7 or SAV8SWSH or SAV8BS or SAV9SV or SAV9ZA;

    public IReadOnlyList<PKM> GetBattleTeamPokemon(int teamIndex)
    {
        if (AppState.SaveFile is not { } sav)
        {
            return [];
        }

        var teamSlots = GetTeamSlots(sav);
        if (teamSlots.Length == 0)
        {
            return [];
        }

        var result = new List<PKM>();
        var startIdx = teamIndex * SlotsPerTeam;
        for (var i = 0; i < SlotsPerTeam; i++)
        {
            var idx = teamSlots[startIdx + i];
            if (idx < 0)
            {
                continue;
            }

            sav.GetBoxSlotFromIndex(idx, out var box, out var slot);
            var pkm = sav.GetBoxSlotAtIndex(box, slot);
            if (pkm.Species > 0)
            {
                result.Add(pkm);
            }
        }

        return result;
    }

    public string GetBattleTeamName(int teamIndex)
    {
        switch (AppState.SaveFile)
        {
            case SAV8BS s8b:
                return s8b.BoxLayout.GetTeamName(teamIndex);
            case SAV8SWSH s8:
                return ReadTeamNameFromBlock(s8.Blocks.GetBlock(SwshTeamNamesKey).Data, teamIndex);
            case SAV9SV s9:
                return ReadTeamNameFromBlock(s9.Blocks.GetBlock(SvTeamNamesKey).Data, teamIndex);
            default:
                return $"Team {teamIndex + 1}";
        }
    }

    public bool IsBattleTeamLocked(int teamIndex) => AppState.SaveFile switch
    {
        SAV7 s7 => s7.BoxLayout.GetIsTeamLocked(teamIndex),
        SAV8SWSH s8 => s8.TeamIndexes.GetIsTeamLocked(teamIndex),
        SAV8BS s8b => s8b.BoxLayout.GetIsTeamLocked(teamIndex),
        SAV9SV s9 => s9.TeamIndexes.GetIsTeamLocked(teamIndex),
        SAV9ZA s9z => s9z.TeamIndexes.GetIsTeamLocked(teamIndex),
        _ => false
    };

    public void SetBattleTeamLocked(int teamIndex, bool locked)
    {
        switch (AppState.SaveFile)
        {
            case SAV7 s7:
                s7.BoxLayout.SetIsTeamLocked(teamIndex, locked);
                break;
            case SAV8SWSH s8:
                s8.TeamIndexes.SetIsTeamLocked(teamIndex, locked);
                break;
            case SAV8BS s8b:
                {
                    // BoxLayout8b only exposes GetIsTeamLocked (bit read) and the LockedTeam byte property.
                    var current = s8b.BoxLayout.LockedTeam;
                    s8b.BoxLayout.LockedTeam = locked
                        ? (byte)(current | 1 << teamIndex)
                        : (byte)(current & ~(1 << teamIndex));
                    break;
                }
            case SAV9SV s9:
                s9.TeamIndexes.SetIsTeamLocked(teamIndex, locked);
                break;
            case SAV9ZA s9z:
                s9z.TeamIndexes.SetIsTeamLocked(teamIndex, locked);
                break;
        }

        RefreshService.Refresh();
    }

    public bool HasRentalTeams() => AppState.SaveFile is SAV8SWSH or SAV9SV;

    public int GetRentalTeamCount() => AppState.SaveFile switch
    {
        SAV8SWSH => SwshRentalTeamKeys.Length,
        SAV9SV => RentalTeamSet9.Count,
        _ => 0
    };

    public IReadOnlyList<PKM> GetRentalTeamPokemon(int teamIndex)
    {
        switch (AppState.SaveFile)
        {
            case SAV8SWSH s8:
                {
                    if ((uint)teamIndex >= SwshRentalTeamKeys.Length)
                    {
                        return [];
                    }

                    var block = s8.Blocks.GetBlock(SwshRentalTeamKeys[teamIndex]);
                    var rental = new RentalTeam8(block.Data.ToArray());
                    return rental.GetTeam().Where(pk => pk.Species > 0).ToArray<PKM>();
                }
            case SAV9SV s9:
                {
                    if ((uint)teamIndex >= RentalTeamSet9.Count)
                    {
                        return [];
                    }

                    var block = s9.Blocks.GetBlock(SvRentalTeamsKey);
                    var rentalSet = new RentalTeamSet9(block.Data.ToArray());
                    var rental = rentalSet.GetRentalTeam(teamIndex);
                    return rental.GetTeam().Where(pk => pk.Species > 0).ToArray<PKM>();
                }
            default:
                return [];
        }
    }

    public string GetRentalTeamName(int teamIndex)
    {
        switch (AppState.SaveFile)
        {
            case SAV8SWSH s8:
                {
                    if ((uint)teamIndex >= SwshRentalTeamKeys.Length)
                    {
                        return $"Rental Team {teamIndex + 1}";
                    }

                    var block = s8.Blocks.GetBlock(SwshRentalTeamKeys[teamIndex]);
                    var rental = new RentalTeam8(block.Data.ToArray());
                    var name = rental.TeamName;
                    return string.IsNullOrWhiteSpace(name)
                        ? $"Rental Team {teamIndex + 1}"
                        : name;
                }
            case SAV9SV s9:
                {
                    if ((uint)teamIndex >= RentalTeamSet9.Count)
                    {
                        return $"Rental Team {teamIndex + 1}";
                    }

                    var block = s9.Blocks.GetBlock(SvRentalTeamsKey);
                    var rentalSet = new RentalTeamSet9(block.Data.ToArray());
                    var rental = rentalSet.GetRentalTeam(teamIndex);
                    var name = rental.TeamName;
                    return string.IsNullOrWhiteSpace(name)
                        ? $"Rental Team {teamIndex + 1}"
                        : name;
                }
            default:
                return $"Rental Team {teamIndex + 1}";
        }
    }

    public string GetRentalTeamPlayerName(int teamIndex)
    {
        switch (AppState.SaveFile)
        {
            case SAV8SWSH s8:
                {
                    if ((uint)teamIndex >= SwshRentalTeamKeys.Length)
                    {
                        return string.Empty;
                    }

                    var block = s8.Blocks.GetBlock(SwshRentalTeamKeys[teamIndex]);
                    var rental = new RentalTeam8(block.Data.ToArray());
                    return rental.PlayerName;
                }
            case SAV9SV s9:
                {
                    if ((uint)teamIndex >= RentalTeamSet9.Count)
                    {
                        return string.Empty;
                    }

                    var block = s9.Blocks.GetBlock(SvRentalTeamsKey);
                    var rentalSet = new RentalTeamSet9(block.Data.ToArray());
                    return rentalSet.GetRentalTeam(teamIndex).PlayerName;
                }
            default:
                return string.Empty;
        }
    }

    public string ExportTeamAsShowdown(IReadOnlyList<PKM> team)
    {
        if (team.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var pkm in team)
        {
            if (pkm.Species == 0)
            {
                continue;
            }

            sb.AppendLine(ShowdownParsing.GetShowdownText(pkm)).AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    public void ClearBattleTeam(int teamIndex)
    {
        if (AppState.SaveFile is not { } sav)
        {
            return;
        }

        var teamSlots = GetTeamSlots(sav);
        if (teamSlots.Length == 0)
        {
            return;
        }

        var startIdx = teamIndex * SlotsPerTeam;
        for (var i = 0; i < SlotsPerTeam; i++)
        {
            teamSlots[startIdx + i] = -1;
        }

        SaveTeamSlots(sav);
        RefreshService.RefreshBoxState();
        RefreshService.Refresh();
    }

    public void ClearAllBattleTeams()
    {
        switch (AppState.SaveFile)
        {
            case SAV7 s7:
                s7.BoxLayout.ClearBattleTeams();
                s7.BoxLayout.SaveBattleTeams();
                break;
            case SAV8SWSH s8:
                s8.TeamIndexes.ClearBattleTeams();
                s8.TeamIndexes.SaveBattleTeams();
                break;
            case SAV8BS s8b:
                s8b.BoxLayout.ClearBattleTeams();
                s8b.BoxLayout.SaveBattleTeams();
                break;
            case SAV9SV s9:
                s9.TeamIndexes.ClearBattleTeams();
                s9.TeamIndexes.SaveBattleTeams();
                break;
            case SAV9ZA s9z:
                s9z.TeamIndexes.ClearBattleTeams();
                s9z.TeamIndexes.SaveBattleTeams();
                break;
            default:
                return;
        }

        RefreshService.RefreshBoxState();
        RefreshService.Refresh();
    }

    public void UnlockAllBattleTeams()
    {
        switch (AppState.SaveFile)
        {
            case SAV7 s7:
                s7.BoxLayout.UnlockAllTeams();
                break;
            case SAV8SWSH s8:
                s8.TeamIndexes.UnlockAllTeams();
                break;
            case SAV8BS s8b:
                s8b.BoxLayout.LockedTeam = 0;
                break;
            case SAV9SV s9:
                s9.TeamIndexes.UnlockAllTeams();
                break;
            case SAV9ZA s9z:
                s9z.TeamIndexes.UnlockAllTeams();
                break;
            default:
                return;
        }

        RefreshService.Refresh();
    }

    public void ClearBattleBox()
    {
        switch (AppState.SaveFile)
        {
            case SAV5 sav5:
                for (var i = 0; i < BattleBox5.Count; i++)
                {
                    sav5.BattleBox[i].Span.Clear();
                }

                break;
            case SAV6XY xy:
                for (var i = 0; i < BattleBox6.Count; i++)
                {
                    xy.BattleBox[i].Span.Clear();
                }

                break;
            case SAV6AO ao:
                for (var i = 0; i < BattleBox6.Count; i++)
                {
                    ao.BattleBox[i].Span.Clear();
                }

                break;
            default:
                return;
        }

        RefreshService.Refresh();
    }

    public void SetBattleBoxLocked(bool locked)
    {
        switch (AppState.SaveFile)
        {
            case SAV5 sav5:
                sav5.BattleBox.BattleBoxLocked = locked;
                break;
            case SAV6XY xy:
                xy.BattleBox.Locked = locked;
                break;
            case SAV6AO ao:
                ao.BattleBox.Locked = locked;
                break;
            default:
                return;
        }

        RefreshService.Refresh();
    }

    private AdvancedSearchResult BuildSearchResult(PKM pkm, bool isParty, int box, int slot)
    {
        var speciesName = GetPokemonSpeciesName(pkm.Species);
        var location = isParty
            ? $"Party {slot + 1}"
            : $"Box {box + 1}, Slot {slot + 1}";

        return new AdvancedSearchResult
        {
            Pokemon = pkm,
            SpeciesName = speciesName,
            Location = location,
            IsParty = isParty,
            BoxNumber = box,
            SlotNumber = slot
        };
    }

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="pkm" /> satisfies every
    /// non-null/non-empty criterion in <paramref name="f" />.
    /// Cheap equality checks run first; expensive legality analysis runs last.
    /// </summary>
    private static bool Matches(PKM pkm, AdvancedSearchFilter f)
    {
        // ── Basic ─────────────────────────────────────────────────────────

        if (f.Species.HasValue && pkm.Species != f.Species.Value)
        {
            return false;
        }

        if (f.Form.HasValue && pkm.Form != f.Form.Value)
        {
            return false;
        }

        if (f.IsShiny.HasValue && pkm.IsShiny != f.IsShiny.Value)
        {
            return false;
        }

        if (f.IsEgg.HasValue && pkm.IsEgg != f.IsEgg.Value)
        {
            return false;
        }

        if (f.Gender.HasValue)
        {
            // Issue spec uses -1 for genderless; PKHeX uses 2.
            var filterGender = f.Gender.Value == -1
                ? 2
                : f.Gender.Value;
            if (pkm.Gender != filterGender)
            {
                return false;
            }
        }

        if (f.Nature.HasValue && (byte)pkm.Nature != f.Nature.Value)
        {
            return false;
        }

        if (f.Ability.HasValue && pkm.Ability != f.Ability.Value)
        {
            return false;
        }

        if (f.HeldItem.HasValue && pkm.HeldItem != f.HeldItem.Value)
        {
            return false;
        }

        if (f.Ball.HasValue && pkm.Ball != f.Ball.Value)
        {
            return false;
        }

        if (f.OriginGame.HasValue && pkm.Version != f.OriginGame.Value)
        {
            return false;
        }

        // ── Types (order-agnostic multiset match against PersonalInfo) ────

        if (f.Type1.HasValue || f.Type2.HasValue)
        {
            var (t1, t2) = pkm.GetGenerationTypes();

            if (f.Type1.HasValue && f.Type2.HasValue)
            {
                var a = f.Type1.Value;
                var b = f.Type2.Value;
                var pairMatches = (a == t1 && b == t2) || (a == t2 && b == t1);
                if (!pairMatches)
                {
                    return false;
                }
            }
            else
            {
                var filterType = f.Type1 ?? f.Type2!.Value;
                if (filterType != t1 && filterType != t2)
                {
                    return false;
                }
            }
        }

        // ── Tera Type (Gen 9 SV only) ─────────────────────────────────────

        if (f.TeraType.HasValue)
        {
            if (pkm is not ITeraType tera || (byte)tera.TeraType != f.TeraType.Value)
            {
                return false;
            }
        }

        // ── Conditional flags ─────────────────────────────────────────────

        if (f.IsFavorite.HasValue)
        {
            if (pkm is not IFavorite fav || fav.IsFavorite != f.IsFavorite.Value)
            {
                return false;
            }
        }

        if (f.IsAlpha.HasValue)
        {
            if (pkm is not IAlpha alpha || alpha.IsAlpha != f.IsAlpha.Value)
            {
                return false;
            }
        }

        if (f.IsShadow.HasValue)
        {
            if (pkm is not IShadowCapture shadow || shadow.IsShadow != f.IsShadow.Value)
            {
                return false;
            }
        }

        if (f.CanGigantamax.HasValue)
        {
            if (pkm is not IGigantamax gmax || gmax.CanGigantamax != f.CanGigantamax.Value)
            {
                return false;
            }
        }

        if (f.DynamaxLevelMin.HasValue)
        {
            if (pkm is not IDynamaxLevel dmax || dmax.DynamaxLevel < f.DynamaxLevelMin.Value)
            {
                return false;
            }
        }

        // ── Origin (met date / location / Pokerus) ────────────────────────

        if (f.MetLocation.HasValue && pkm.MetLocation != f.MetLocation.Value)
        {
            return false;
        }

        if (f.MetDateMin.HasValue || f.MetDateMax.HasValue)
        {
            if (pkm.MetDate is not { } metDate)
            {
                return false;
            }

            if (f.MetDateMin.HasValue && metDate < f.MetDateMin.Value)
            {
                return false;
            }

            if (f.MetDateMax.HasValue && metDate > f.MetDateMax.Value)
            {
                return false;
            }
        }

        if (f.PokerusState.HasValue)
        {
            var state = pkm switch
            {
                { IsPokerusCured: true } => 2,
                { IsPokerusInfected: true } => 1,
                _ => 0,
            };
            if (state != f.PokerusState.Value)
            {
                return false;
            }
        }

        // ── Language ──────────────────────────────────────────────────────

        if (f.LanguageId.HasValue && pkm.Language != f.LanguageId.Value)
        {
            return false;
        }

        // ── Level ─────────────────────────────────────────────────────────

        if (f.LevelMin.HasValue && pkm.CurrentLevel < f.LevelMin.Value)
        {
            return false;
        }

        if (f.LevelMax.HasValue && pkm.CurrentLevel > f.LevelMax.Value)
        {
            return false;
        }

        // ── IVs ───────────────────────────────────────────────────────────

        if (f.HpIvMin.HasValue && pkm.IV_HP < f.HpIvMin.Value)
        {
            return false;
        }

        if (f.AtkIvMin.HasValue && pkm.IV_ATK < f.AtkIvMin.Value)
        {
            return false;
        }

        if (f.DefIvMin.HasValue && pkm.IV_DEF < f.DefIvMin.Value)
        {
            return false;
        }

        if (f.SpaIvMin.HasValue && pkm.IV_SPA < f.SpaIvMin.Value)
        {
            return false;
        }

        if (f.SpdIvMin.HasValue && pkm.IV_SPD < f.SpdIvMin.Value)
        {
            return false;
        }

        if (f.SpeIvMin.HasValue && pkm.IV_SPE < f.SpeIvMin.Value)
        {
            return false;
        }

        // ── EVs ───────────────────────────────────────────────────────────

        if (f.HpEvMin.HasValue && pkm.EV_HP < f.HpEvMin.Value)
        {
            return false;
        }

        if (f.AtkEvMin.HasValue && pkm.EV_ATK < f.AtkEvMin.Value)
        {
            return false;
        }

        if (f.DefEvMin.HasValue && pkm.EV_DEF < f.DefEvMin.Value)
        {
            return false;
        }

        if (f.SpaEvMin.HasValue && pkm.EV_SPA < f.SpaEvMin.Value)
        {
            return false;
        }

        if (f.SpdEvMin.HasValue && pkm.EV_SPD < f.SpdEvMin.Value)
        {
            return false;
        }

        if (f.SpeEvMin.HasValue && pkm.EV_SPE < f.SpeEvMin.Value)
        {
            return false;
        }

        // ── Trainer ───────────────────────────────────────────────────────

        if (f.OriginalTrainerName is { Length: > 0 } otName
            && !pkm.OriginalTrainerName.Contains(otName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (f.TrainerId.HasValue && pkm.TID16 != (ushort)f.TrainerId.Value)
        {
            return false;
        }

        // ── Moves ─────────────────────────────────────────────────────────

        if (f.AnyMoves.Count > 0)
        {
            var m1 = pkm.Move1;
            var m2 = pkm.Move2;
            var m3 = pkm.Move3;
            var m4 = pkm.Move4;
            var anyMatch = f.AnyMoves.Any(m => m == m1 || m == m2 || m == m3 || m == m4);
            if (!anyMatch)
            {
                return false;
            }
        }

        if (f.AllMoves.Count > 0)
        {
            var m1 = pkm.Move1;
            var m2 = pkm.Move2;
            var m3 = pkm.Move3;
            var m4 = pkm.Move4;
            var allMatch = f.AllMoves.All(m => m == m1 || m == m2 || m == m3 || m == m4);
            if (!allMatch)
            {
                return false;
            }
        }

        // ── Hidden Power type ─────────────────────────────────────────────

        if (f.HiddenPowerType.HasValue)
        {
            Span<int> ivs = [pkm.IV_HP, pkm.IV_ATK, pkm.IV_DEF, pkm.IV_SPA, pkm.IV_SPD, pkm.IV_SPE];
            if (HiddenPower.GetType(ivs, pkm.Context) != f.HiddenPowerType.Value)
            {
                return false;
            }
        }

        // ── Ribbons / Marks (reflection — runs before legality) ───────────

        if (f.RequiredRibbons.Count > 0)
        {
            var pkmType = pkm.GetType();
            if (f.RequiredRibbons
                .Select(ribbonName => pkmType.GetProperty(ribbonName))
                .Any(prop => prop?.GetValue(pkm) is not true))
            {
                return false;
            }
        }

        // ── Markings (shape toggles — Gen 3+) ─────────────────────────────

        if (f.RequiredMarkings.Count > 0)
        {
            if (pkm is not IAppliedMarkings marks)
            {
                return false;
            }

            foreach (var index in f.RequiredMarkings)
            {
                if ((uint)index >= (uint)marks.MarkingCount)
                {
                    // Persisted filters can outlive the save they were built against;
                    // unsupported indices on this PKM are "not set" rather than an error.
                    return false;
                }

                if (pkm.GetMarking(index) == 0)
                {
                    return false;
                }
            }
        }

        // ── Legality (expensive — evaluated last) ─────────────────────────

        if (!f.IsLegal.HasValue)
        {
            return true;
        }

        var isLegal = new LegalityAnalysis(pkm).Valid;
        return isLegal == f.IsLegal.Value;
    }

    private void HandleNullOrEmptyPokemon()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        // Empty box slots in PKHeX return a non-null PKM with Species=0, not null.
        // Apply the template for both cases so new Pokémon start with the correct
        // OT/ID/Language/Version/MetDate/Ball — matching EntityTemplates.TemplateFields.
        if (EditFormPokemon is not (null or { Species: 0 }))
        {
            return;
        }

        // Apply trainer data and other defaults to the blank PKM so new Pokémon
        // start with correct OT/ID/Language/Version/MetDate/Ball — matching what
        // PKHeX WinForms does via EntityTemplates.TemplateFields.
        // Species is reset to 0 afterward so the user is prompted to pick one.
        var blank = saveFile.BlankPKM.Clone();
        EntityTemplates.TemplateFields(blank, saveFile);
        blank.Species = 0;
        blank.ClearNickname(); // Remove the template species name set by TemplateFields
        EditFormPokemon = blank;
    }

    /// <summary>
    /// Compacts a box by shifting all Pokémon left to fill gaps (for Gen 1 and Gen 2 games).
    /// In these generations, boxes were lists, not grids, so they should have no gaps.
    /// </summary>
    private EncounterSearchResult BuildEncounterResult(IEncounterable enc)
    {
        var speciesName = GetPokemonSpeciesName(enc.Species);

        var gameName = GameInfo.FilteredSources.Games
                           .FirstOrDefault(g => g.Value == (int)enc.Version)?.Text
                       ?? enc.Version.ToString();

        var levelRange = enc.LevelMin == enc.LevelMax
            ? $"Lv. {enc.LevelMin}"
            : $"Lv. {enc.LevelMin}–{enc.LevelMax}";

        var location = GetEncounterLocationName(enc);

        return new EncounterSearchResult
        {
            Encounter = enc,
            SpeciesName = speciesName,
            GameName = gameName,
            EncounterTypeName = GetEncounterTypeName(enc),
            LevelRange = levelRange,
            IsShinyLocked = enc.Shiny == Shiny.Never,
            IsMysteryGift = enc is MysteryGift,
            Location = location
        };
    }

    /// <summary>
    /// Returns the human-readable location name for an encounter, or <see langword="null" />
    /// when no location is associated (e.g., location ID is 0).
    /// </summary>
    private static string? GetEncounterLocationName(IEncounterable enc)
    {
        var locationId = enc.Location != 0
            ? enc.Location
            : enc.EggLocation;
        if (locationId == 0)
        {
            return null;
        }

        var isEgg = locationId != enc.Location;
        return GameInfo.GetLocationName(isEgg, locationId, enc.Generation, enc.Generation, enc.Version);
    }

    /// <summary>
    /// Classifies an <see cref="IEncounterable" /> into one of the five
    /// <see cref="EncounterTypeGroup" /> buckets.
    /// </summary>
    private static EncounterTypeGroup GetEncounterTypeGroup(IEncounterable enc)
    {
        if (enc.IsEgg)
        {
            return EncounterTypeGroup.Egg;
        }

        if (enc is MysteryGift)
        {
            return EncounterTypeGroup.Mystery;
        }

        var name = enc.Name;
        if (name.Contains("Wild", StringComparison.OrdinalIgnoreCase))
        {
            return EncounterTypeGroup.Slot;
        }

        return name.Contains("Trade", StringComparison.OrdinalIgnoreCase)
            ? EncounterTypeGroup.Trade
            : EncounterTypeGroup.Static;
    }

    private static string GetEncounterTypeName(IEncounterable enc) => GetEncounterTypeGroup(enc) switch
    {
        EncounterTypeGroup.Egg => "Egg",
        EncounterTypeGroup.Mystery => "Mystery Gift",
        EncounterTypeGroup.Slot => "Wild",
        EncounterTypeGroup.Trade => "Trade",
        EncounterTypeGroup.Static => "Static",
        _ => "Unknown"
    };

    private static int[] GetTeamSlots(SaveFile sav) => sav switch
    {
        SAV7 s7 => s7.BoxLayout.TeamSlots,
        SAV8SWSH s8 => s8.TeamIndexes.TeamSlots,
        SAV8BS s8b => s8b.BoxLayout.TeamSlots,
        SAV9SV s9 => s9.TeamIndexes.TeamSlots,
        SAV9ZA s9z => s9z.TeamIndexes.TeamSlots,
        _ => []
    };

    private static string ReadTeamNameFromBlock(Span<byte> data, int teamIndex)
    {
        var offset = teamIndex * TeamNameByteLength;
        if (offset + TeamNameByteLength > data.Length)
        {
            return $"Team {teamIndex + 1}";
        }

        var name = StringConverter8.GetString(data.Slice(offset, TeamNameByteLength));
        return string.IsNullOrWhiteSpace(name)
            ? $"Team {teamIndex + 1}"
            : name;
    }

    private static void SaveTeamSlots(SaveFile sav)
    {
        switch (sav)
        {
            case SAV7 s7:
                s7.BoxLayout.SaveBattleTeams();
                break;
            case SAV8SWSH s8:
                s8.TeamIndexes.SaveBattleTeams();
                break;
            case SAV8BS s8b:
                s8b.BoxLayout.SaveBattleTeams();
                break;
            case SAV9SV s9:
                s9.TeamIndexes.SaveBattleTeams();
                break;
            case SAV9ZA s9z:
                s9z.TeamIndexes.SaveBattleTeams();
                break;
        }
    }
}
