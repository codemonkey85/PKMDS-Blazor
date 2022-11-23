namespace Pkmds.Rcl.Components;

public partial class BoxComponent
{
    [Parameter]
    public int BoxId { get; set; }

    private BoxEdit? BoxEdit { get; set; }

    protected override void OnParametersSet()
    {
        if (AppState.SaveFile is null)
        {
            return;
        }

        AppState.SelectedPokemon = null;
        AppState.Refresh();

        BoxEdit = new BoxEdit(AppState.SaveFile);
        BoxEdit.LoadBox(BoxId);
    }
}
