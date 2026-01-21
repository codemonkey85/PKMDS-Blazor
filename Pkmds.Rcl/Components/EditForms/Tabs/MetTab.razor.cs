namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class MetTab : IDisposable
{
    private EntityContext currentLocationSearchContext = EntityContext.None;

    private GameVersion currentLocationSearchVersion = GameVersion.Any;

    private EntityContext originFormat = EntityContext.None;

    /// <summary>
    /// Currently loaded met location group that is populating Met and Egg location comboboxes
    /// </summary>
    private GameVersion origintrack;

    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    private MetTimeOfDay GetMetTimeOfDay => Pokemon is not (PK2 and ICaughtData2 c2)
        ? MetTimeOfDay.None
        : (MetTimeOfDay)c2.MetTimeOfDay;

    private bool PokemonMetAsEgg => Pokemon is not null && (Pokemon.IsEgg || Pokemon.WasEgg || Pokemon.WasTradedEgg);

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        CheckMetLocationChange(saveFile.Version, saveFile.Context);
    }

    private void CheckMetLocationChange(GameVersion version, EntityContext context)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        // Does the list of locations need to be changed to another group?
        var group = GameUtil.GetMetLocationVersionGroup(version);
        if (group is GameVersion.Invalid)
        {
            group = GameUtil.GetMetLocationVersionGroup(saveFile.Version);
            if (group is GameVersion.Invalid || version is GameVersion.Any)
            {
                version = group = context.GetSingleGameVersion();
            }
        }

        if (group != origintrack || context != originFormat)
        {
            currentLocationSearchVersion = version;
            currentLocationSearchContext = context;
        }

        origintrack = group;
        originFormat = context;
    }

    private ComboItem GetMetLocation()
    {
        if (Pokemon is not { } pkm)
        {
            return new("NONE", -1);
        }

        CheckMetLocationChange(pkm.Version, pkm.Context);

        return AppService.GetMetLocationComboItem(Pokemon.MetLocation, currentLocationSearchVersion,
            currentLocationSearchContext);
    }

    private ComboItem GetEggMetLocation()
    {
        if (Pokemon is not { } pkm)
        {
            return new("NONE", -1);
        }

        CheckMetLocationChange(pkm.Version, pkm.Context);
        return AppService.GetMetLocationComboItem(Pokemon.EggLocation, currentLocationSearchVersion,
            currentLocationSearchContext, true);
    }

    private void OriginGameChanged()
    {
        if (Pokemon is not { } pkm)
        {
            return;
        }

        CheckMetLocationChange(pkm.Version, pkm.Context);
    }

    private Task<IEnumerable<ComboItem>> SearchMetLocations(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchMetLocations(searchString, currentLocationSearchVersion,
            currentLocationSearchContext));

    private Task<IEnumerable<ComboItem>> SearchEggMetLocations(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchMetLocations(searchString, currentLocationSearchVersion,
            currentLocationSearchContext, true));

    private void SetMetTimeOfDay(MetTimeOfDay metTimeOfDay)
    {
        if (Pokemon is not (PK2 and ICaughtData2 c2))
        {
            return;
        }

        c2.MetTimeOfDay = (int)metTimeOfDay;
    }

    private void MetAsEggChanged(bool newValue)
    {
        if (Pokemon is null)
        {
            return;
        }

        switch (newValue)
        {
            case false:
                {
                    if (Pokemon.IsEgg)
                    {
                        Pokemon.IsEgg = false;
                    }

                    Pokemon.EggDay = Pokemon.EggMonth = Pokemon.EggYear = 0;
                    Pokemon.EggLocation = 0;
                    break;
                }
            case true:
                {
                    var currentMetDate = Pokemon.MetDate;
                    Pokemon.SetEggMetData(Pokemon.Version, Pokemon.Version);
                    Pokemon.EggMetDate = Pokemon.MetDate = currentMetDate;
                    break;
                }
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private enum MetTimeOfDay
    {
        None,
        Morning,
        Day,
        Night
    }
}
