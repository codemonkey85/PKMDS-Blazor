namespace Pkmds.Rcl.Components;

public class PkmSprite : MudImage
{
    [Parameter, EditorRequired] public PKM? Pokemon { get; set; }

    public PkmSprite()
    {
        ObjectFit = ObjectFit.Contain;
        ObjectPosition = ObjectPosition.Center;
    }

    protected override void OnParametersSet()
    {
        Src = SpriteHelper.GetPokemonSpriteFilename(Pokemon);
    }
}
