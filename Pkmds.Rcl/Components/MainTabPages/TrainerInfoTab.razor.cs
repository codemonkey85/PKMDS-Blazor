namespace Pkmds.Rcl.Components.MainTabPages;

public partial class TrainerInfoTab : IDisposable
{
    private bool isSyncing;
    private string syncMessage = string.Empty;

    private DateTime? GameStartedDate
    {
        get;
        set
        {
            field = value;
            UpdateGameStarted();
        }
    }

    private TimeSpan? GameStartedTime
    {
        get;
        set
        {
            field = value;
            UpdateGameStarted();
        }
    }

    private DateTime? HallOfFameDate
    {
        get;
        set
        {
            field = value;
            UpdateHallOfFame();
        }
    }

    private TimeSpan? HallOfFameTime
    {
        get;
        set
        {
            field = value;
            UpdateHallOfFame();
        }
    }

    private InputDateType GameStartType => AppState.SaveFile switch
    {
        SAV4 or SAV5 or SAV6 or SAV7 or SAV8SWSH or SAV8BS or SAV8LA => InputDateType.DateTimeLocal,
        _ => InputDateType.Date
    };

    private List<ComboItem> Countries { get; set; } = [];

    private List<ComboItem> Regions { get; set; } = [];

    private List<ComboItem> ConsoleRegions { get; set; } = [];

    private List<ComboItem> Languages { get; set; } = [];

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        (GameStartedDate, GameStartedTime) = GetGameStarted();
        (HallOfFameDate, HallOfFameTime) = GetHallOfFame();

        if (AppState.SaveFile is not { Generation: { } saveGeneration } saveFile)
        {
            return;
        }

        var countriesName = saveGeneration switch
        {
            4 => "gen4_countries",
            5 => "gen5_countries",
            _ => "countries"
        };

        Countries = Util.GetCountryRegionList(countriesName, GameInfo.CurrentLanguage);
        UpdateCountry();

        ConsoleRegions = saveFile is IRegionOrigin
            ? [..GameInfo.FilteredSources.ConsoleRegions]
            : [];

        Languages = saveGeneration >= 3 && saveFile.Language >= 0
            ? [..GameInfo.LanguageDataSource(saveGeneration, saveFile.Context)]
            : [];
    }

    private void UpdateCountry()
    {
        if (AppState.SaveFile is not { Generation: { } saveGeneration } saveFile)
        {
            return;
        }

        var countryId = saveFile switch
        {
            SAV4 sav4Geo => sav4Geo.Country,
            SAV5 sav5Geo => sav5Geo.Country,
            SAV6 sav6Geo => sav6Geo.Country,
            SAV7 sav7Geo => sav7Geo.Country,
            _ => 0
        };

        if (countryId == 0)
        {
            var regionsName = saveGeneration switch
            {
                4 => "gen4_sr_default",
                5 => "gen5_sr_default",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(regionsName))
            {
                return;
            }

            Regions = Util.GetCountryRegionList(regionsName, GameInfo.CurrentLanguage);
            return;
        }

        var regionPrefix = saveGeneration switch
        {
            4 => "gen4_",
            5 => "gen5_",
            _ => string.Empty
        };

        Regions = Util.GetCountryRegionList($"{regionPrefix}sr_{countryId:000}", GameInfo.CurrentLanguage);
    }

    private async Task RunSyncAsync(string message, Action sync)
    {
        isSyncing = true;
        syncMessage = message;
        StateHasChanged();
        await Task.Yield();
        try { sync(); }
        finally
        {
            isSyncing = false;
        }
    }

    private Task OnGenderToggleAsync(Gender newGender) =>
        RunSyncAsync("Syncing OT gender to matching Pokémon…", () => OnGenderToggle(newGender));

    private Task OnOTNameChangedAsync(SaveFile saveFile, string value) =>
        RunSyncAsync("Syncing OT name to matching Pokémon…", () => OnOTNameChanged(saveFile, value));

    private Task OnTID16ChangedAsync(SaveFile saveFile, ushort value) =>
        RunSyncAsync("Syncing Trainer ID to matching Pokémon…", () => OnTID16Changed(saveFile, value));

    private Task OnSID16ChangedAsync(SaveFile saveFile, ushort value) =>
        RunSyncAsync("Syncing Secret ID to matching Pokémon…", () => OnSID16Changed(saveFile, value));

    private Task OnTrainerTID7ChangedAsync(SaveFile saveFile, uint value) =>
        RunSyncAsync("Syncing Trainer ID to matching Pokémon…", () => OnTrainerTID7Changed(saveFile, value));

    private Task OnTrainerSID7ChangedAsync(SaveFile saveFile, uint value) =>
        RunSyncAsync("Syncing Secret ID to matching Pokémon…", () => OnTrainerSID7Changed(saveFile, value));

    private void OnGenderToggle(Gender newGender)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        var oldGender = saveFile.Gender;
        var genderByte = (byte)newGender;
        saveFile.Gender = genderByte;

        // PKHeX's IsFromTrainer checks name + ID32 + gender to determine if a Pokémon
        // belongs to the active trainer. After a gender change, Pokémon with the old
        // OT_Gender would fail that check and trigger "Invalid Current handler value"
        // legality errors. Propagate the new gender to all matching Pokémon.
        if (oldGender != genderByte)
        {
            SyncOtGenderToPokemon(saveFile, oldGender, genderByte);
        }

        // Several games store gender-specific fashion/appearance data.
        // Changing gender without resetting it causes the player model to become
        // invisible in-game because clothing items don't exist for the new gender's model.
        // Skin color slots are gender-paired (even = male, odd = female).
        // Aligning the skin color's LSB to the new gender is required to avoid
        // a corrupted appearance (invisible player model) in games that tie
        // skin/appearance data to gender.
        switch (saveFile)
        {
            case SAV7 sav7:
                // SM/USUM: DressUpSkinColor must match the gender's parity or the
                // player model becomes invisible. Mirrors PKHeX WinForms UpdateSkinColor.
                sav7.MyStatus.DressUpSkinColor = sav7.MyStatus.DressUpSkinColor & ~1 | genderByte;
                break;
            case SAV8SWSH sav8:
                // SWSH: GenderAppearance is a separate byte from Gender, and a full
                // appearance reset is needed to keep skin/clothing compatible.
                var currentSkin = PlayerSkinColor8Extensions.GetSkinColorFromSkin(sav8.MyStatus.Skin);
                var skinIndex = (int)currentSkin & ~1 | genderByte;
                sav8.MyStatus.GenderAppearance = genderByte;
                sav8.MyStatus.ResetAppearance((PlayerSkinColor8)skinIndex);
                break;
        }
    }

    private static void OnOTNameChanged(SaveFile saveFile, string value)
    {
        var oldName = saveFile.OT;
        saveFile.OT = value;
        if (oldName == saveFile.OT)
        {
            return;
        }

        SyncPokemon(saveFile,
            pkm => IsOtMatch(pkm, saveFile.ID32, oldName, saveFile.Gender),
            pkm => pkm.OriginalTrainerName = saveFile.OT,
            pkm => IsHtMatch(pkm, oldName, oldGender: null),
            pkm => pkm.HandlingTrainerName = saveFile.OT);
    }

    private static void OnTID16Changed(SaveFile saveFile, ushort value)
    {
        var oldId32 = saveFile.ID32;
        saveFile.TID16 = value;
        SyncOtidToPokemon(saveFile, oldId32);
    }

    private static void OnSID16Changed(SaveFile saveFile, ushort value)
    {
        var oldId32 = saveFile.ID32;
        saveFile.SID16 = value;
        SyncOtidToPokemon(saveFile, oldId32);
    }

    private static void OnTrainerTID7Changed(SaveFile saveFile, uint value)
    {
        var oldId32 = saveFile.ID32;
        saveFile.TrainerTID7 = value;
        SyncOtidToPokemon(saveFile, oldId32);
    }

    private static void OnTrainerSID7Changed(SaveFile saveFile, uint value)
    {
        var oldId32 = saveFile.ID32;
        saveFile.TrainerSID7 = value;
        SyncOtidToPokemon(saveFile, oldId32);
    }

    private static void SyncOtidToPokemon(SaveFile saveFile, uint oldId32)
    {
        if (saveFile.ID32 == oldId32)
        {
            return;
        }

        var newTid16 = saveFile.TID16;
        var newSid16 = saveFile.SID16;
        // HT does not store a trainer ID, so only OT sync is needed here.
        SyncPokemon(saveFile,
            pkm => IsOtMatch(pkm, oldId32, saveFile.OT, saveFile.Gender),
            pkm =>
            {
                pkm.TID16 = newTid16;
                pkm.SID16 = newSid16;
            });
    }

    private static void SyncOtGenderToPokemon(SaveFile saveFile, byte oldGender, byte newGender) =>
        SyncPokemon(saveFile,
            pkm => IsOtMatch(pkm, saveFile.ID32, saveFile.OT, oldGender),
            pkm => pkm.OriginalTrainerGender = newGender,
            pkm => IsHtMatch(pkm, saveFile.OT, oldGender),
            pkm => pkm.HandlingTrainerGender = newGender);

    /// <summary>
    /// Iterates every party and box slot once, applying <paramref name="otMutate" /> to slots
    /// matching <paramref name="isOtMatch" /> and <paramref name="htMutate" /> to slots matching
    /// <paramref name="isHtMatch" />. Both checks run per slot so the whole save is covered in
    /// a single pass.
    /// </summary>
    private static void SyncPokemon(SaveFile saveFile,
        Func<PKM, bool> isOtMatch, Action<PKM> otMutate,
        Func<PKM, bool>? isHtMatch = null, Action<PKM>? htMutate = null)
    {
        for (var i = 0; i < saveFile.PartyCount; i++)
        {
            var pkm = saveFile.GetPartySlotAtIndex(i);
            if (!ApplySync(pkm, isOtMatch, otMutate, isHtMatch, htMutate))
            {
                continue;
            }

            saveFile.SetPartySlotAtIndex(pkm, i);
        }

        for (var box = 0; box < saveFile.BoxCount; box++)
        {
            for (var slot = 0; slot < saveFile.BoxSlotCount; slot++)
            {
                var pkm = saveFile.GetBoxSlotAtIndex(box, slot);
                if (!ApplySync(pkm, isOtMatch, otMutate, isHtMatch, htMutate))
                {
                    continue;
                }

                saveFile.SetBoxSlotAtIndex(pkm, box, slot);
            }
        }
    }

    private static bool ApplySync(PKM pkm,
        Func<PKM, bool> isOtMatch, Action<PKM> otMutate,
        Func<PKM, bool>? isHtMatch, Action<PKM>? htMutate)
    {
        var changed = false;
        if (isOtMatch(pkm))
        {
            otMutate(pkm);
            changed = true;
        }

        if (isHtMatch?.Invoke(pkm) != true)
        {
            return changed;
        }

        htMutate!(pkm);
        changed = true;

        return changed;
    }

    private static bool IsHtMatch(PKM pkm, string htName, byte? oldGender) =>
        pkm.Species != 0 &&
        pkm.CurrentHandler == 1 &&
        pkm.HandlingTrainerName == htName &&
        (oldGender is null || pkm.HandlingTrainerGender == oldGender);

    private static bool IsOtMatch(PKM pkm, uint id32, string ot, byte gender) =>
        pkm.Species != 0 &&
        pkm.ID32 == id32 &&
        pkm.OriginalTrainerName == ot &&
        // Gen 3 does not store OT gender on the PKM, so skip the gender check for those.
        (pkm.Format <= 3 || pkm.OriginalTrainerGender == gender);

    private static string FormatPlaytime(SaveFile saveFile) =>
        $"{saveFile.PlayedHours}:{saveFile.PlayedMinutes:D2}:{saveFile.PlayedSeconds:D2}";

    private static void ResetPlaytime(SaveFile saveFile)
    {
        saveFile.PlayedHours = 0;
        saveFile.PlayedMinutes = 0;
        saveFile.PlayedSeconds = 0;
    }

    private uint GetCoins() => AppState.SaveFile switch
    {
        SAV1 sav => sav.Coin,
        SAV2 sav => sav.Coin,
        SAV3 sav => sav.Coin,
        SAV4 sav => sav.Coin,
        _ => 0U
    };

    private void SetCoins(uint value)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        switch (saveFile)
        {
            case SAV1 sav:
                sav.Coin = value;
                break;
            case SAV2 sav:
                sav.Coin = value;
                break;
            case SAV3 sav:
                sav.Coin = value;
                break;
            case SAV4 sav:
                sav.Coin = value;
                break;
        }
    }

    private Task<IEnumerable<ComboItem>> SearchPokemonNames(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchPokemonNames(searchString));

    private ComboItem GetTrainerCardPokemon(SAV3FRLG sav, int index)
    {
        var g3Species = sav.GetWork(0x43 + index);
        var species = SpeciesConverter.GetNational3(g3Species);
        return AppService.GetSpeciesComboItem(species);
    }

    private static void SetTrainerCardPokemon(SAV3FRLG sav, int index, ComboItem speciesComboItem)
    {
        var species = (ushort)speciesComboItem.Value;
        var g3Species = SpeciesConverter.GetInternal3(species);
        sav.SetWork(0x43 + index, g3Species);
    }

    private (DateTime? Date, TimeSpan? Time) GetGameStarted()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return (null, null);
        }

        DateTime date;
        DateTime time;

        switch (saveFile)
        {
            case SAV4 sav:
                DateUtil.GetDateTime2000(sav.SecondsToStart, out date, out time);
                break;
            case SAV5 sav:
                DateUtil.GetDateTime2000(sav.SecondsToStart, out date, out time);
                break;
            case SAV6 sav:
                DateUtil.GetDateTime2000(sav.SecondsToStart, out date, out time);
                break;
            case SAV7 sav:
                DateUtil.GetDateTime2000(sav.SecondsToStart, out date, out time);
                break;
            case SAV8SWSH sav:
                DateUtil.GetDateTime2000(sav.SecondsToStart, out date, out time);
                break;
            case SAV8BS sav:
                DateUtil.GetDateTime2000(sav.SecondsToStart, out date, out time);
                break;
            case SAV8LA sav:
                DateUtil.GetDateTime2000(sav.SecondsToStart, out date, out time);
                break;
            case SAV9SV sav:
                date = sav.EnrollmentDate.Timestamp;
                time = sav.EnrollmentDate.Timestamp;
                break;
            default:
                return (null, null);
        }

        return (date, time.TimeOfDay);
    }

    private void UpdateGameStarted()
    {
        if (AppState.SaveFile is not { } saveFile || GameStartedDate is null || GameStartedTime is null)
        {
            return;
        }

        var date = GameStartedDate.Value;
        var time = GameStartedTime.Value;

        switch (saveFile)
        {
            case SAV4 sav:
                sav.SecondsToStart =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV5 sav:
                sav.SecondsToStart =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV6 sav:
                sav.SecondsToStart =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV7 sav:
                sav.SecondsToStart =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV8SWSH sav:
                sav.SecondsToStart =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV8BS sav:
                sav.SecondsToStart =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV8LA sav:
                sav.SecondsToStart =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV9SV sav:
                sav.EnrollmentDate.Timestamp = date;
                break;
            default:
                return;
        }
    }

    private (DateTime? Date, TimeSpan? Time) GetHallOfFame()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return (null, null);
        }

        DateTime date;
        DateTime time;

        switch (saveFile)
        {
            case SAV4 sav:
                DateUtil.GetDateTime2000(sav.SecondsToFame, out date, out time);
                break;
            case SAV5 sav:
                DateUtil.GetDateTime2000(sav.SecondsToFame, out date, out time);
                break;
            case SAV6 sav:
                DateUtil.GetDateTime2000(sav.SecondsToFame, out date, out time);
                break;
            case SAV7 sav:
                DateUtil.GetDateTime2000(sav.SecondsToFame, out date, out time);
                break;
            case SAV8SWSH sav:
                DateUtil.GetDateTime2000(sav.SecondsToFame, out date, out time);
                break;
            case SAV8BS sav:
                DateUtil.GetDateTime2000(sav.SecondsToFame, out date, out time);
                break;
            case SAV8LA sav:
                DateUtil.GetDateTime2000(sav.SecondsToFame, out date, out time);
                break;
            case SAV9SV sav:
                DateUtil.GetDateTime2000(sav.SecondsToFame, out date, out time);
                break;
            default:
                return (null, null);
        }

        return (date, time.TimeOfDay);
    }

    private void UpdateHallOfFame()
    {
        if (AppState.SaveFile is not { } saveFile || HallOfFameDate is null || HallOfFameTime is null)
        {
            return;
        }

        var date = HallOfFameDate.Value;
        var time = HallOfFameTime.Value;

        switch (saveFile)
        {
            case SAV4 sav:
                sav.SecondsToFame =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV5 sav:
                sav.SecondsToFame =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV6 sav:
                sav.SecondsToFame =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV7 sav:
                sav.SecondsToFame =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV8SWSH sav:
                sav.SecondsToFame =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV8BS sav:
                sav.SecondsToFame =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV8LA sav:
                sav.SecondsToFame =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV9SV sav:
                sav.SecondsToFame =
                    (uint)DateUtil.GetSecondsFrom2000(date, new(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            default:
                return;
        }
    }

    private ComboItem GetGen1RivalStarter(SAV1 sav1)
    {
        var nationalSpeciesId = SpeciesConverter.GetNational1(sav1.RivalStarter);
        return AppService.GetSpeciesComboItem(nationalSpeciesId);
    }

    private static void SetGen1RivalStarter(SAV1 sav1, ComboItem species)
    {
        var internalSpeciesId = SpeciesConverter.GetInternal1((byte)species.Value);
        sav1.RivalStarter = internalSpeciesId;
    }
}
