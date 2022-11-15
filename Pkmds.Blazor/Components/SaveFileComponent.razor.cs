namespace Pkmds.Blazor.Components;

public partial class SaveFileComponent
{
    private IBrowserFile? browserFile;

    private string OtName { get; set; } = string.Empty;

    private void HandleFile(InputFileChangeEventArgs e) => browserFile = e.File;

    private async Task LoadSaveFileAsync()
    {
        if (browserFile is null)
        {
            return;
        }

        await using var fileStream = browserFile.OpenReadStream(1000000L);
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var data = memoryStream.ToArray();
        AppState.SaveFile = SaveUtil.GetVariantSAV(data);
        if (AppState.SaveFile is null)
        {
            return;
        }

        OtName = AppState.SaveFile.OT;
        StateHasChanged();
    }
}
