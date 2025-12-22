namespace Pkmds.Rcl.Components;

public class BoxSlotComponent : PokemonSlotComponent
{
    [Parameter, EditorRequired]
    public int BoxNumberParam { get; set; }

    protected override int? BoxNumber => BoxNumberParam;
}
