namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class StatsTab : IDisposable
{
    protected override void OnInitialized() =>
        AppState.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        AppState.OnAppStateChanged -= StateHasChanged;
}
