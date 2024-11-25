namespace Pkmds.Web.Components.MainTabPages;

public partial class TrainerInfoTab : IDisposable
{
    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    private DateTime? GameStartedDate { get; set; }

    private TimeSpan? GameStartedTime { get; set; }

    private DateTime? HallOfFameDate { get; set; }

    private TimeSpan? HallOfFameTime { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        (GameStartedDate, GameStartedTime) = GetGameStarted();
        (HallOfFameDate, HallOfFameTime) = GetHallOfFame();
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
        _ => 0U,
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
            default:
                return (null, null);
        };

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
                sav.SecondsToStart = (uint)DateUtil.GetSecondsFrom2000(date, new DateTime(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV5 sav:
                sav.SecondsToStart = (uint)DateUtil.GetSecondsFrom2000(date, new DateTime(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV6 sav:
                sav.SecondsToStart = (uint)DateUtil.GetSecondsFrom2000(date, new DateTime(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV7 sav:
                sav.SecondsToStart = (uint)DateUtil.GetSecondsFrom2000(date, new DateTime(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            default:
                return;
        };
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
            default:
                return (null, null);
        };

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
                sav.SecondsToFame = (uint)DateUtil.GetSecondsFrom2000(date, new DateTime(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV5 sav:
                sav.SecondsToFame = (uint)DateUtil.GetSecondsFrom2000(date, new DateTime(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV6 sav:
                sav.SecondsToFame = (uint)DateUtil.GetSecondsFrom2000(date, new DateTime(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            case SAV7 sav:
                sav.SecondsToFame = (uint)DateUtil.GetSecondsFrom2000(date, new DateTime(2000, 1, 1, time.Hours, time.Minutes, time.Seconds));
                break;
            default:
                return;
        };
    }
}
