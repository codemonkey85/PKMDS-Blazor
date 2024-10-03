namespace Pkmds.Web.Components.Dialogs;

public partial class ShowdownExportDialog
{
    [Parameter]
    public PKM? Pokemon { get; set; }

    [CascadingParameter]
    private MudDialogInstance? MudDialog { get; set; }

    private string ShowdownExport => Pokemon is not null ? AppService.ExportPokemonAsShowdown(Pokemon) : AppService.ExportPartyAsShowdown();

    private void Close() => MudDialog?.Close();
}
