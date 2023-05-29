namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class MainTab : IDisposable
{
    private MudSelect<byte>? FormSelect { get; set; }

    protected override void OnInitialized() =>
        AppState.OnAppStateChanged += Refresh;

    public void Dispose() =>
        AppState.OnAppStateChanged -= Refresh;

    public void Refresh()
    {
        FormSelect?.ForceRender(true);
        StateHasChanged();
    }
}
