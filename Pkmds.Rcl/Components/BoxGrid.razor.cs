namespace Pkmds.Rcl.Components;

public partial class BoxGrid : IDisposable
{
    [Parameter, EditorRequired]
    public BoxEdit? BoxEdit { get; set; }

    [Parameter, EditorRequired]
    public int BoxNumber { get; set; }

    private static int GridHeight => (56 + 4) * 6;

    private int GridWidth => (68 + 4) * (AppState.SaveFile?.BoxSlotCount == 20 ? 4 : 6);

    private string GridStyle => $"width: {GridWidth}px; height: {GridHeight}px;";

    protected override void OnInitialized() =>
        AppState.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        AppState.OnAppStateChanged -= StateHasChanged;
}
