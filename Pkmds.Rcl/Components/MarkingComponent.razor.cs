using static Pkmds.Rcl.MarkingsHelper;

namespace Pkmds.Rcl.Components;

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
        _ => string.Empty
    };

    private string MarkingClass => $"w-[100px] aspect-square flex justify-center items-center text-[3.0rem] text-center cursor-pointer select-none border border-gray-300 rounded-[10px]{Pokemon?.GetMarking((int)Shape) switch
    {
        null => string.Empty,
        0 => " text-gray-500",
        1 => Pokemon.Generation >= 7 ? " text-blue-500" : " text-black",
        2 => " text-red-500",
        _ => string.Empty
    }}";

    private void Toggle() => Pokemon?.ToggleMarking((int)Shape);
}
