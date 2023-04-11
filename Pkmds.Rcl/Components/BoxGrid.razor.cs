namespace Pkmds.Rcl.Components;

public partial class BoxGrid
{
    [Parameter, EditorRequired]
    public BoxEdit? BoxEdit { get; set; }

    private int GridHeight => (56 + 4) * (AppState?.SaveFile?.BoxSlotCount == 20 ? 4 : 6);

    private static int GridWidth => (68 + 4) * 6;

    private string GridStyle => $"width: {GridWidth}px; height: {GridHeight}px;";
}
