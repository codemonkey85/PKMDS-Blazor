namespace Pkmds.Rcl.Components;

public class BoxSlotComponent : PokemonSlotComponent
{
    [Parameter, EditorRequired]
    public new int BoxNumber { get; set; }
}
