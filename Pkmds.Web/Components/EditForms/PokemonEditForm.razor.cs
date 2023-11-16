namespace Pkmds.Web.Components.EditForms;

public partial class PokemonEditForm : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;
}
