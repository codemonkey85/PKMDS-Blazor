namespace Pkmds.Rcl.Components;

public partial class WelcomeEmptyState
{
    private bool isDragging;

    [Parameter]
    public EventCallback<IBrowserFile> OnFileDropped { get; set; }

    private string DragClass => isDragging
        ? "mud-file-upload-dragarea mud-file-upload-dragarea-clickable mud-border-primary"
        : "mud-file-upload-dragarea mud-file-upload-dragarea-clickable";

    private async Task OnFilesChanged(InputFileChangeEventArgs e)
    {
        if (e.FileCount == 0)
        {
            return;
        }

        await OnFileDropped.InvokeAsync(e.File);
    }
}
