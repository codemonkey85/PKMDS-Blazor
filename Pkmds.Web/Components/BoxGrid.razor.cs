namespace Pkmds.Web.Components;

public partial class BoxGrid : IDisposable
{
    [Parameter, EditorRequired]
    public BoxEdit? BoxEdit { get; set; }

    [Parameter, EditorRequired]
    public int BoxNumber { get; set; }

    private string BoxGridClass => AppState.SaveFile?.BoxSlotCount == 20 ? "box-grid-20" : "box-grid-30";

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;
}
