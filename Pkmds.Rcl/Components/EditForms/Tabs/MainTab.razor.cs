namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class MainTab : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

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
