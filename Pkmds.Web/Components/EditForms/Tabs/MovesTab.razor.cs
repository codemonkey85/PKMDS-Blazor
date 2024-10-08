namespace Pkmds.Web.Components.EditForms.Tabs;

public partial class MovesTab : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    private Task<IEnumerable<ComboItem>> SearchMoves(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchMoves(searchString));
}
