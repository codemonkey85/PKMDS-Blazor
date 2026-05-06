namespace Pkmds.Rcl.Components;

public partial class WelcomeEmptyState
{
    [Parameter]
    public EventCallback OnLoadSaveFile { get; set; }

    [Parameter]
    public EventCallback<IBrowserFile> OnFileDropped { get; set; }

    private async Task OnFilesChanged(InputFileChangeEventArgs e)
    {
        if (e.FileCount == 0)
        {
            return;
        }

        await OnFileDropped.InvokeAsync(e.File);
    }
}
