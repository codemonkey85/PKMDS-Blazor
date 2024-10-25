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
}
