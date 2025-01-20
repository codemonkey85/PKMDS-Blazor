namespace Pkmds.Web.Components;

public partial class FileUploadDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public string Message { get; set; } = "Choose a file to upload.";

    private IBrowserFile? browserFile;

    private void HandleFile(IBrowserFile? file) => browserFile = file;

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());

    private void Ok() => MudDialog.Close(DialogResult.Ok(browserFile));
}
