namespace Pkmds.Web.Components.Layout;

public partial class MainLayout : IDisposable
{
    private bool isDarkMode;
    private MudThemeProvider? mudThemeProvider;


    private const string DeploymentId = "%%CACHE_VERSION%%";

    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || mudThemeProvider is null)
        {
            return;
        }

        isDarkMode = await mudThemeProvider.GetSystemPreference();
        await mudThemeProvider.WatchSystemPreference(OnSystemPreferenceChanged);
        StateHasChanged();
    }

    private Task OnSystemPreferenceChanged(bool newValue)
    {
        isDarkMode = newValue;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void DrawerToggle() => AppService.ToggleDrawer();

    private IBrowserFile? browserLoadSaveFile;

    private async Task ShowLoadSaveFileDialog()
    {
        const string message = "Choose a save file";

        var dialogParameters = new DialogParameters
        {
            { nameof(FileUploadDialog.Message), message }
        };

        var dialog = await DialogService.ShowAsync<FileUploadDialog>("Load Save File",
            parameters: dialogParameters,
            options: new DialogOptions
            {
                CloseOnEscapeKey = true,
                BackdropClick = false,
            });

        var result = await dialog.Result;
        if (result is { Data: IBrowserFile selectedFile })
        {
            browserLoadSaveFile = selectedFile;
            await LoadSaveFile(selectedFile);
        }
    }

    private async Task LoadSaveFile(IBrowserFile selectedFile)
    {
        if (browserLoadSaveFile is null)
        {
            await DialogService.ShowMessageBox("No file selected", "Please select a file to load.");
            return;
        }

        AppState.SaveFile = null;
        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;
        AppState.ShowProgressIndicator = true;

        try
        {
            await using var fileStream = browserLoadSaveFile.OpenReadStream(Constants.MaxFileSize);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var data = memoryStream.ToArray();
            AppState.SaveFile = SaveUtil.GetVariantSAV(data, path: selectedFile.Name);

            if (AppState.SaveFile is null)
            {
                await DialogService.ShowMessageBox("Error", "The file is not a supported save file.");
                return;
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageBox("Error", $"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
        finally
        {
            AppState.ShowProgressIndicator = false;
        }

        if (AppState.SaveFile is null)
        {
            return;
        }

        RefreshService.Refresh();
    }

    private async Task ExportSaveFile()
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
        await WriteFile(AppState.SaveFile.Write(), browserLoadSaveFile?.Name ?? "save.sav", ".sav", "Save File");
        AppState.ShowProgressIndicator = false;
    }

    private async Task ShowLoadPokemonFileDialog()
    {
        const string title = "Load Pokémon File";
        const string message = "Choose a Pokémon file";

        var dialogParameters = new DialogParameters
        {
            { nameof(FileUploadDialog.Message), message }
        };

        var dialog = await DialogService.ShowAsync<FileUploadDialog>(
            title,
            parameters: dialogParameters,
            options: new DialogOptions
            {
                CloseOnEscapeKey = true,
                BackdropClick = false,
            });

        var result = await dialog.Result;
        if (result is { Data: IBrowserFile selectedFile })
        {
            var browserLoadPokemonFile = selectedFile;
            await LoadPokemonFile(browserLoadPokemonFile, title);
        }
    }

    private async Task ShowLoadMysteryGiftFileDialog()
    {
        const string title = "Load Mystery Gift file";
        const string message = "Choose a Mystery Gift file";

        var dialogParameters = new DialogParameters
        {
            { nameof(FileUploadDialog.Message), message }
        };

        var dialog = await DialogService.ShowAsync<FileUploadDialog>(
            title,
            parameters: dialogParameters,
            options: new DialogOptions
            {
                CloseOnEscapeKey = true,
                BackdropClick = false,
            });

        var result = await dialog.Result;
        if (result is { Data: IBrowserFile selectedFile })
        {
            var browserLoadPokemonFile = selectedFile;
            await LoadMysteryGiftFile(browserLoadPokemonFile, title);
        }
    }

    private async Task LoadPokemonFile(IBrowserFile browserLoadPokemonFile, string title)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        if (browserLoadPokemonFile is null)
        {
            await DialogService.ShowMessageBox("No file selected", "Please select a file to load.");
            return;
        }

        AppState.ShowProgressIndicator = true;

        try
        {
            await using var fileStream = browserLoadPokemonFile.OpenReadStream(Constants.MaxFileSize);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var data = memoryStream.ToArray();

            if (!FileUtil.TryGetPKM(data, out var pkm, Path.GetExtension(browserLoadPokemonFile.Name), saveFile))
            {
                await DialogService.ShowMessageBox("Error", "The file is not a supported Pokémon file.");
                return;
            }

            var pokemon = pkm.Clone();

            if (pkm.GetType() != saveFile.PKMType)
            {
                pokemon = EntityConverter.ConvertToType(pkm, saveFile.PKMType, out var c);
                if (!c.IsSuccess() || pokemon is null)
                {
                    await DialogService.ShowMessageBox("Error", c.GetDisplayString(pkm, saveFile.PKMType));
                    return;
                }
            }

            saveFile.AdaptPKM(pokemon);

            var index = saveFile.NextOpenBoxSlot();
            if (index < 0)
            {
                return;
            }

            saveFile.GetBoxSlotFromIndex(index, out var box, out var slot);
            saveFile.SetBoxSlotAtIndex(pokemon, index);

            const string messageStart = "The Pokémon has been imported and stored in";

            var message = saveFile is IBoxDetailNameRead boxDetail
                ? $"{messageStart} '{boxDetail.GetBoxName(box)}' (Box {box + 1}), Slot {slot + 1}."
                : $"{messageStart} Box {box + 1}, Slot {slot + 1}.";

            await DialogService.ShowMessageBox(
                title,
                message);
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageBox("Error", $"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
        finally
        {
            AppState.ShowProgressIndicator = false;
        }

        RefreshService.RefreshBoxState();
    }

    private async Task LoadMysteryGiftFile(IBrowserFile browserLoadMysteryGiftFile, string title)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        if (browserLoadMysteryGiftFile is null)
        {
            await DialogService.ShowMessageBox("No file selected", "Please select a file to load.");
            return;
        }

        AppState.ShowProgressIndicator = true;

        try
        {
            await using var fileStream = browserLoadMysteryGiftFile.OpenReadStream(Constants.MaxFileSize);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var data = memoryStream.ToArray();

            if (!FileUtil.TryGetMysteryGift(data, out var mysteryGift, Path.GetExtension(browserLoadMysteryGiftFile.Name)))
            {
                await DialogService.ShowMessageBox("Error", "The file is not a supported Mystery Gift file.");
                return;
            }

            if (mysteryGift.Species == (ushort)Species.None)
            {
                return;
            }

            var temp = mysteryGift.ConvertToPKM(saveFile);
            var pokemon = temp.Clone();

            if (temp.GetType() != saveFile.PKMType)
            {
                pokemon = EntityConverter.ConvertToType(temp, saveFile.PKMType, out var c);

                if (!c.IsSuccess() || pokemon is null)
                {
                    await DialogService.ShowMessageBox("Error", c.GetDisplayString(temp, saveFile.PKMType));
                    return;
                }
            }

            saveFile.AdaptPKM(pokemon);

            var index = saveFile.NextOpenBoxSlot();
            if (index < 0)
            {
                return;
            }

            saveFile.GetBoxSlotFromIndex(index, out var box, out var slot);
            saveFile.SetBoxSlotAtIndex(pokemon, index);

            const string messageStart = "The Pokémon has been imported and stored in";

            var message = saveFile is IBoxDetailNameRead boxDetail
                ? $"{messageStart} '{boxDetail.GetBoxName(box)}' (Box {box + 1}), Slot {slot + 1}."
                : $"{messageStart} Box {box + 1}, Slot {slot + 1}.";

            await DialogService.ShowMessageBox(
                title,
                message);
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageBox("Error", $"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
        finally
        {
            AppState.ShowProgressIndicator = false;
        }

        RefreshService.RefreshBoxState();
    }

    private async Task ExportSelectedPokemon()
    {
        if (AppService.EditFormPokemon is null)
        {
            return;
        }

        var pkm = AppService.EditFormPokemon;

        AppState.ShowProgressIndicator = true;

        pkm.RefreshChecksum();
        var cleanFileName = AppService.GetCleanFileName(pkm);
        await WriteFile(pkm.Data, cleanFileName, $".{pkm.Extension}", "Pokémon File");

        AppState.ShowProgressIndicator = false;
    }

    private async Task WriteFile(byte[] data, string fileName, string fileTypeExtension, string fileTypeDescription)
    {
        if (await FileSystemAccessService.IsSupportedAsync() == false)
        {
            await WriteFileOldWay(data, fileName);
            return;
        }

        try
        {
            // Ensure that the FilePicker API is invoked correctly within a user gesture context
            await JSRuntime.InvokeVoidAsync("showFilePickerAndWrite", fileName, data, fileTypeExtension, fileTypeDescription);
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
