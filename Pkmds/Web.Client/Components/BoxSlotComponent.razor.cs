namespace Pkmds.Web.Client.Components;

public class BoxSlotComponent : PokemonSlotComponent
{
    [Parameter, EditorRequired]
    public int BoxNumber { get; set; }
}
