namespace Pkmds.Rcl.Components.MainTabPages;

public partial class TrainerInfoTab : IDisposable
{
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
        SAV9SV => InputDateType.Date,
        _ => InputDateType.Date
    };

    private List<ComboItem> Countries { get; set; } = [];

    private List<ComboItem> Regions { get; set; } = [];

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        (GameStartedDate, GameStartedTime) = GetGameStarted();
        (HallOfFameDate, HallOfFameTime) = GetHallOfFame();

        if (AppState.SaveFile is not { Generation: { } saveGeneration })
        {
            return;
        }

        var countriesName = saveGeneration switch
        {
            4 => "gen4_countries",
            5 => "gen5_countries",
            6 => "countries",
            7 => "countries",
            _ => "countries"
        };

        Countries = Util.GetCountryRegionList(countriesName, GameInfo.CurrentLanguage);
        UpdateCountry();
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

    private void OnGenderToggle(Gender newGender)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        saveFile.Gender = (byte)newGender;
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

    private void SetGen1RivalStarter(SAV1 sav1, ComboItem species)
    {
        var internalSpeciesId = SpeciesConverter.GetInternal1((byte)species.Value);
        sav1.RivalStarter = internalSpeciesId;
    }
}
