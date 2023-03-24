namespace Pkmds.Rcl.Components;

public partial class BoxGrid
{
    [Parameter, EditorRequired]
    public BoxEdit? BoxEdit { get; set; }

    private int columns => AppState?.SaveFile?.BoxSlotCount == 20 ? 4 : 6;
    private const int rows = 5;
}
