namespace Pkmds.Web.Components.EditForms;

public partial class PokemonEditForm : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    private void ExportAsShowdown() =>
        DialogService.Show<ShowdownExportDialog>(
            "Showdown Export",
            new DialogParameters
            {
                { nameof(ShowdownExportDialog.Pokemon), Pokemon }
            },
            new DialogOptions
            {
                CloseOnEscapeKey = true,
            });
}
