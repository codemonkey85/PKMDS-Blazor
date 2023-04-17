namespace Pkmds.Rcl.Components.EditForms;

public partial class PokemonEditForm
{
    private MudSelect<byte>? FormSelect { get; set; }

    protected override void OnInitialized() =>
        AppState.OnAppStateChanged += Refresh;

    public void Dispose() =>
        AppState.OnAppStateChanged -= Refresh;

    private void Refresh()
    {
        FormSelect?.ForceRender(true);
        StateHasChanged();
    }
}
