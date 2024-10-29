namespace Pkmds.Web.Components.EditForms.Tabs;

public partial class MetTab : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    private Task<IEnumerable<ComboItem>> SearchMetLocations(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchMetLocations(searchString));

    private Task<IEnumerable<ComboItem>> SearchEggMetLocations(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchMetLocations(searchString, isEggLocation: true));

    private MetTimeOfDay GetMetTimeOfDay => Pokemon is not (PK2 and ICaughtData2 c2)
        ? MetTimeOfDay.None
        : (MetTimeOfDay)c2.MetTimeOfDay;

    private void SetMetTimeOfDay(MetTimeOfDay metTimeOfDay)
    {
        if (Pokemon is not (PK2 and ICaughtData2 c2))
        {
            return;
        }

        c2.MetTimeOfDay = (int)metTimeOfDay;
    }

    private enum MetTimeOfDay
    {
        None,
        Morning,
        Day,
        Night
    }

    private bool PokemonMetAsEgg => Pokemon is not null && (Pokemon.IsEgg || Pokemon.WasEgg || Pokemon.WasTradedEgg);

    private void MetAsEggChanged(bool newValue)
    {
        if (Pokemon is null)
        {
            return;
        }


        if (newValue == false)
        {
            if (Pokemon.IsEgg)
            {
                Pokemon.IsEgg = false;
            }
            Pokemon.EggDay = Pokemon.EggMonth = Pokemon.EggYear = 0;
            Pokemon.EggLocation = 0;
        }

        if (newValue == true)
        {
            var currentMetDate = Pokemon.MetDate;
            Pokemon.SetEggMetData(Pokemon.Version, Pokemon.Version);
            Pokemon.EggMetDate = Pokemon.MetDate = currentMetDate;
        }
    }
}
