using static Pkmds.Web.MarkingsHelper;

namespace Pkmds.Web.Components;

public partial class MarkingComponent
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    [Parameter, EditorRequired]
    public Markings Shape { get; set; }

    private string DisplayString => Shape switch
    {
        Markings.Circle => Circle,
        Markings.Triangle => Triangle,
        Markings.Square => Square,
        Markings.Heart => Heart,
        Markings.Star => Star,
        Markings.Diamond => Diamond,
        _ => string.Empty,
    };

    private string MarkingClass => $"marking{Pokemon?.GetMarking((int)Shape) switch
    {
        null => string.Empty,
        0 => " gray-mark",
        1 => Pokemon.Generation >= 7 ? " blue-mark" : " black-mark",
        2 => " red-mark",
        _ => string.Empty,
    }}";

    private void Toggle() => Pokemon?.ToggleMarking((int)Shape);
}
