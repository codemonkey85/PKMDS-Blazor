namespace Pkmds.Rcl.Components.Dialogs;

public partial class AboutDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    private void Close() => MudDialog.Close(DialogResult.Cancel());
}
