namespace Pkmds.Web.Components;

public class BoxSlotComponent : PokemonSlotComponent
{
    [Parameter, EditorRequired]
    public int BoxNumber { get; set; }
}
