namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class CosmeticTab : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;
}
