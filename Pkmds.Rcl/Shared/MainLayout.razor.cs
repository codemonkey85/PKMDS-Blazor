namespace Pkmds.Rcl.Shared;

public partial class MainLayout
{
    private bool drawerOpen = true;

    private void DrawerToggle() => drawerOpen = !drawerOpen;

    private const long MaxFileSize = 4_000_000L; // bytes
    private IBrowserFile? browserFile;

    private async Task ShowFileDialog()
    {
        var dialog = await DialogService.ShowAsync<FileUploadDialog>();
        var result = await dialog.Result;
        if (result is { Data: IBrowserFile selectedFile })
        {
            browserFile = selectedFile;
            await LoadSaveFileAsync();
        }
    }

    private async Task LoadSaveFileAsync()
    {
        if (browserFile is null)
        {
            return;
        }

        AppState.SaveFile = null;
        AppState.SelectedBoxSlot = null;
        AppState.SelectedPokemon = null;
        AppState.ShowProgressIndicator = true;
        AppState.Refresh();
        await using var fileStream = browserFile.OpenReadStream(MaxFileSize);
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var data = memoryStream.ToArray();
        AppState.SaveFile = SaveUtil.GetVariantSAV(data);
        AppState.ShowProgressIndicator = false;
        if (AppState.SaveFile is null)
        {
            return;
        }

        AppState.FileDisplayName = $"{AppState.SaveFile.OT} ({AppState.SaveFile.DisplayTID}, {AppState.SaveFile.Version})";
        AppState.Refresh();
    }

    //private MudTheme myTheme = new()
    //{
    //    Palette = new Palette
    //    {
    //        //Primary = "#0074D9",
    //        //Secondary = "#3D9970",
    //        //Info = "#001f3f",
    //        //Success = "#2ECC40",
    //        //Warning = "#FF851B",
    //        //Error = "#F012BE",
    //        //AppbarBackground = "#85144b",
    //        // more color properties
    //        //TextPrimary = Colors.Shades.White,
    //    }
    //};
}
