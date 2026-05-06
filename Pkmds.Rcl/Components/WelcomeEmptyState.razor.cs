namespace Pkmds.Rcl.Components;

public partial class WelcomeEmptyState
{
    private bool _isDragging;

    [Parameter]
    public EventCallback OnLoadSaveFile { get; set; }

    [Parameter]
    public EventCallback<IBrowserFile> OnFileDropped { get; set; }

    private void SetDragging() => _isDragging = true;

    private void ClearDragging() => _isDragging = false;

    private async Task OnFilesChanged(InputFileChangeEventArgs e)
    {
        _isDragging = false;
        if (e.FileCount == 0)
        {
            return;
        }

        await OnFileDropped.InvokeAsync(e.File);
    }
}
