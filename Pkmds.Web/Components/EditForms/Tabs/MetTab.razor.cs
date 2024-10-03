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
}
