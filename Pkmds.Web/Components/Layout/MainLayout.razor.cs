using System.Security.Cryptography;

namespace Pkmds.Web.Components.Layout;

public partial class MainLayout
{
    private const string AppTitle = "PKMDS Save Editor";

    private bool isDarkMode;
    private MudThemeProvider? mudThemeProvider;

    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && mudThemeProvider is not null)
        {
            isDarkMode = await mudThemeProvider.GetSystemPreference();
            await mudThemeProvider.WatchSystemPreference(OnSystemPreferenceChanged);
            StateHasChanged();
        }
    }

    private Task OnSystemPreferenceChanged(bool newValue)
    {
        isDarkMode = newValue;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void DrawerToggle() => AppService.ToggleDrawer();

    private IBrowserFile? browserLoadSaveFile;

    private async Task ShowLoadSaveFileDialogAsync()
    {
        var dialog = await DialogService.ShowAsync<FileUploadDialog>("Load Save File", options: new DialogOptions
        {
            CloseOnEscapeKey = true,
            BackdropClick = false,
        });

        var result = await dialog.Result;
        if (result is { Data: IBrowserFile selectedFile })
        {
            browserLoadSaveFile = selectedFile;
            await LoadSaveFileAsync();
        }
    }

    private async Task LoadSaveFileAsync()
    {
        if (browserLoadSaveFile is null)
        {
            return;
        }

        AppState.SaveFile = null;
        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;
        AppState.ShowProgressIndicator = true;

        await using var fileStream = browserLoadSaveFile.OpenReadStream(Constants.MaxFileSize);
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var data = memoryStream.ToArray();
        AppState.SaveFile = SaveUtil.GetVariantSAV(data);
        AppState.ShowProgressIndicator = false;
        if (AppState.SaveFile is null)
        {
            return;
        }

        RefreshService.Refresh();
    }

    private async Task ExportSaveFileAsync()
    {
        if (AppState.SaveFile is null)
        {
            return;
        }
        
        await ExportSupportedSaveFile();
    }

    private async Task ExportSupportedSaveFile()
    {
        if (AppState.SaveFile is null)
        {
            return;
        }
        AppState.ShowProgressIndicator = true;
        await WriteFile(AppState.SaveFile.Write(), browserLoadSaveFile?.Name ?? "save.sav");
        AppState.ShowProgressIndicator = false;
    }

    private async Task ExportSelectedPokemonAsync()
    {
        if (AppService.EditFormPokemon is null)
        {
            return;
        }

        var pkm = AppService.EditFormPokemon;

        AppState.ShowProgressIndicator = true;

        pkm.RefreshChecksum();
        var cleanFileName = AppService.GetCleanFileName(pkm);
        await WriteFile(pkm.Data, cleanFileName);

        AppState.ShowProgressIndicator = false;
    }

    private async Task WriteFile(byte[] data, string fileName)
    {
        if (await FileSystemAccessService.IsSupportedAsync() == false)
        {
            await WriteFileOldWay(data, fileName);
            return;
        }

        try
        {
            // Ensure that the FilePicker API is invoked correctly within a user gesture context
            await JSRuntime.InvokeVoidAsync("showSaveFilePickerAndWrite", fileName, data);
        }
        catch (JSException ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task WriteFileOldWay(byte[] data, string fileName)
    {
        // Convert the byte array to a base64 string
        var base64String = Convert.ToBase64String(data);

        // Create a download link element
        var element = await JSRuntime.InvokeAsync<IJSObjectReference>("eval", "document.createElement('a')");

        // Set the download link properties
        await element.InvokeVoidAsync("setAttribute", "href", "data:application/octet-stream;base64," + base64String);
        await element.InvokeVoidAsync("setAttribute", "target", "_blank");
        await element.InvokeVoidAsync("setAttribute", "rel", "noopener noreferrer");
        await element.InvokeVoidAsync("setAttribute", "download", fileName);

        // Programmatically click the download link
        await element.InvokeVoidAsync("click");
    }
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
