namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class StatsTab : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    protected override void OnInitialized() =>
        AppState.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        AppState.OnAppStateChanged -= StateHasChanged;
}
