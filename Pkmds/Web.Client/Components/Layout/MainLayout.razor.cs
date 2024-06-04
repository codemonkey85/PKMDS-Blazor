namespace Pkmds.Web.Client.Components.Layout;

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
            DisableBackdropClick = true
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
        switch (AppState.SaveFile.Version)
        {
            case GameVersion.BD:
            case GameVersion.SP:
            case GameVersion.BDSP:
                await ExportBdsp();
                break;
            case GameVersion.SN:
            case GameVersion.MN:
            case GameVersion.SM:
            case GameVersion.US:
            case GameVersion.UM:
            case GameVersion.USUM:
                await ExportSmUsUm();
                break;
            default:
                await ExportSupportedSaveFile();
                break;
        };
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

    private bool IsWebAssembly() => JSRuntime is IJSInProcessRuntime;

    private const string UnsupportedSaveFileExportMessage =
        "Save export not supported for BDSP, Sun / Moon, or Ultra Sun / Moon at this time. " +
        "See: https://github.com/codemonkey85/PKMDS-Blazor/issues/12#issuecomment-1872579636";

    private async Task ExportSmUsUm()
    {
        if (AppState.SaveFile is null)
        {
            return;
        }

        if (!IsWebAssembly())
        {
            await ExportSupportedSaveFile();
            return;
        }

        try
        {
            var success = await ExportSaveUsingService();
            if (!success)
            {
                await DialogService.ShowMessageBox(
                    "Unsupported File",
                    UnsupportedSaveFileExportMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task ExportBdsp()
    {
        if (AppState.SaveFile is null)
        {
            return;
        }

        if (!IsWebAssembly())
        {
            await ExportSupportedSaveFile();
            return;
        }

        try
        {
            var success = await ExportSaveUsingService();
            if (!success)
            {
                await DialogService.ShowMessageBox(
                    "Unsupported File",
                    UnsupportedSaveFileExportMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task<bool> ExportSaveUsingService()
    {
        AppState.ShowProgressIndicator = true;

        if (AppState.SaveFile is null)
        {
            return Failure();
        }

        var saveFileData = await FileSaverService.ExportSaveFileAsync(new ExportSaveFileRequest { SaveFileData = AppState.SaveFile.Data });

        if (saveFileData is { Length: > 0 })
        {
            await WriteFile(saveFileData, browserLoadSaveFile?.Name ?? "save.sav");
            return Success();
        }

        return Failure();

        bool Success()
        {
            AppState.ShowProgressIndicator = false;
            return true;
        }

        bool Failure()
        {
            AppState.ShowProgressIndicator = false;
            return false;
        }
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

    [Inject] private HttpClient? HttpClient { get; set; }

    private const string ApiRoot =
#if DEBUG
        "https://localhost:7102/";
#else
        "https://pkmds.app/";
#endif

    private async Task Test()
    {
        if (HttpClient is null)
        {
            return;
        }

        Console.WriteLine($"{nameof(ApiRoot)}: '{ApiRoot}'");

        var response = await HttpClient.GetAsync($"{ApiRoot}api/test");
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine(result);
        }
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
