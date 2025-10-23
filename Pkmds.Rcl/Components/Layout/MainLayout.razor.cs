using Pkmds.Rcl.Extensions;

namespace Pkmds.Rcl.Components.Layout;

public partial class MainLayout : IDisposable
{
    [StringSyntax(StringSyntaxAttribute.Uri)]
    private const string GitHubRepoLink = "https://github.com/codemonkey85/PKMDS-Blazor";

    private const string GitHubTooltip = "Source code on GitHub";

    private IBrowserFile? browserLoadSaveFile;
    private bool isDarkMode;
    private MudThemeProvider? mudThemeProvider;

    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || mudThemeProvider is null)
        {
            return;
        }

        isDarkMode = await mudThemeProvider.GetSystemDarkModeAsync();
        await mudThemeProvider.WatchSystemDarkModeAsync(OnSystemPreferenceChanged);
        StateHasChanged();
    }

    private Task OnSystemPreferenceChanged(bool newValue)
    {
        isDarkMode = newValue;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void DrawerToggle() => AppService.ToggleDrawer();

    private async Task ShowLoadSaveFileDialog()
    {
        const string message = "Choose a save file";

        var dialogParameters = new DialogParameters { { nameof(FileUploadDialog.Message), message } };

        var dialog = await DialogService.ShowAsync<FileUploadDialog>("Load Save File",
            dialogParameters,
            new() { CloseOnEscapeKey = true, BackdropClick = false });

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

            if (SaveUtil.TryGetSaveFile(data, out var saveFile, selectedFile.Name))
            {
                AppState.SaveFile = saveFile;
                AppState.BoxEdit?.LoadBox(saveFile.CurrentBox);
            }
            else
            {
                const string message =
                    "The selected save file is invalid. If this save file came from a ROM hack, it is not supported. Otherwise, try saving in-game and re-exporting / re-uploading the save file.";
                await DialogService.ShowMessageBox("Error", message);
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
        RefreshService.RefreshBoxAndPartyState();
    }

    private async Task ExportSaveFile()
    {
        if (AppState.SaveFile is null)
        {
            return;
        }

        AppState.ShowProgressIndicator = true;
        await WriteFile(AppState.SaveFile.Write().ToArray(), browserLoadSaveFile?.Name ?? "save.sav", ".sav", "Save File");
        AppState.ShowProgressIndicator = false;
    }

    private async Task ShowLoadPokemonFileDialog()
    {
        const string title = "Load Pokémon File";
        const string message = "Choose a Pokémon file";

        var dialogParameters = new DialogParameters { { nameof(FileUploadDialog.Message), message } };

        var dialog = await DialogService.ShowAsync<FileUploadDialog>(
            title,
            dialogParameters,
            new() { CloseOnEscapeKey = true, BackdropClick = false });

        var result = await dialog.Result;
        if (result is { Data: IBrowserFile selectedFile })
        {
            await LoadPokemonFile(selectedFile, title);
        }
    }

    private async Task ShowLoadMysteryGiftFileDialog()
    {
        const string title = "Load Mystery Gift file";
        const string message = "Choose a Mystery Gift file";

        var dialogParameters = new DialogParameters { { nameof(FileUploadDialog.Message), message } };

        var dialog = await DialogService.ShowAsync<FileUploadDialog>(
            title,
            dialogParameters,
            new() { CloseOnEscapeKey = true, BackdropClick = false });

        var result = await dialog.Result;
        if (result is { Data: IBrowserFile selectedFile })
        {
            await LoadMysteryGiftFile(selectedFile, title);
        }
    }

    private async Task LoadPokemonFile(IBrowserFile browserLoadPokemonFile, string title)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
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

            saveFile.AdaptToSaveFile(pokemon);

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
        if (AppState.SaveFile is null)
        {
            return;
        }

        AppState.ShowProgressIndicator = true;

        try
        {
            await using var fileStream = browserLoadMysteryGiftFile.OpenReadStream(Constants.MaxFileSize);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var data = memoryStream.ToArray();

            if (!FileUtil.TryGetMysteryGift(data, out var mysteryGift,
                    Path.GetExtension(browserLoadMysteryGiftFile.Name)))
            {
                await DialogService.ShowMessageBox("Error", "The file is not a supported Mystery Gift file.");
                return;
            }

            if (mysteryGift.Species.IsInvalidSpecies())
            {
                await DialogService.ShowMessageBox("Error", "The Mystery Gift Pokémon is invalid.");
                return;
            }

            await AppService.ImportMysteryGift(data, Path.GetExtension(browserLoadMysteryGiftFile.Name), out _, out var resultsMessage);

            await DialogService.ShowMessageBox(title, resultsMessage);
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
        var data = GetPokemonFileData(pkm);
        await WriteFile(data, cleanFileName, $".{pkm.Extension}", "Pokémon File");

        AppState.ShowProgressIndicator = false;
    }

    private static byte[] GetPokemonFileData(PKM? pokemon) =>
        pokemon is null
            ? []
            : pokemon.DecryptedPartyData;

    private async Task WriteFile(byte[] data, string fileName, string fileTypeExtension, string fileTypeDescription)
    {
        if (!await FileSystemAccessService.IsSupportedAsync())
        {
            await WriteFileOldWay(data, fileName);
            return;
        }

        try
        {
            // Ensure that the FilePicker API is invoked correctly within a user gesture context
            await JSRuntime.InvokeVoidAsync("showFilePickerAndWrite", fileName, data, fileTypeExtension,
                fileTypeDescription);
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
