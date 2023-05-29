namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class CosmeticTab : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    private int ColSpan => Pokemon?.MarkingCount == 6 ? 4 : 6;

    protected override void OnInitialized() =>
        AppState.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        AppState.OnAppStateChanged -= StateHasChanged;
}
