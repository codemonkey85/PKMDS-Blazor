namespace Pkmds.Rcl.Components;

public partial class WelcomeEmptyState
{
    private bool _isDragging;

    [Parameter]
    public EventCallback OnLoadSaveFile { get; set; }

    [Parameter]
    public EventCallback<IBrowserFile> OnFileDropped { get; set; }

    private void OnDragEnter() => _isDragging = true;

    private async Task OnFilesChanged(InputFileChangeEventArgs e)
    {
        _isDragging = false;
        if (e.FileCount == 0)
        {
            return;
        }

        var file = e.File;
        await OnFileDropped.InvokeAsync(file);
    }
}
