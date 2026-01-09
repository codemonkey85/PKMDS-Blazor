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
        RefreshService.RefreshTheme(isDarkMode);
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void OnIsDarkModeChanged(bool newValue)
    {
        isDarkMode = newValue;
        RefreshService.RefreshTheme(isDarkMode);
        StateHasChanged();
    }

    private void OnThemeSwitchChanged(bool newValue)
    {
        isDarkMode = newValue;
        RefreshService.RefreshTheme(isDarkMode);
        StateHasChanged();
    }

    private void OnVerboseLoggingChanged(bool newValue)
    {
        LoggingService.IsVerboseLoggingEnabled = newValue;
        Logger.LogInformation("Verbose logging {Status}", newValue
            ? "enabled"
            : "disabled");
        StateHasChanged();
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
            Logger.LogWarning("Attempted to load save file but no file was selected");
            await DialogService.ShowMessageBox("No file selected", "Please select a file to load.");
            return;
        }

        Logger.LogInformation("Loading save file: {FileName}", selectedFile.Name);
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
            Logger.LogDebug("Read {ByteCount} bytes from save file", data.Length);

            if (SaveUtil.TryGetSaveFile(data, out var saveFile, selectedFile.Name))
            {
                AppState.SaveFile = saveFile;
                AppState.BoxEdit?.LoadBox(saveFile.CurrentBox);
                Logger.LogInformation("Successfully loaded save file: {SaveType}, Generation: {Generation}",
                    saveFile.GetType().Name, saveFile.Generation);
            }
            else
            {
                Logger.LogError("Failed to load save file: {FileName} - Invalid save file format", selectedFile.Name);
                const string message =
                    "The selected save file is invalid. If this save file came from a ROM hack, it is not supported. Otherwise, try saving in-game and re-exporting / re-uploading the save file.";
                await DialogService.ShowMessageBox("Error", message);
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading save file: {FileName}", selectedFile.Name);
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

    private static string EnsureExtension(string fileName, string extension)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "save";
        }

        extension = extension.StartsWith('.')
            ? extension
            : $".{extension}";

        return fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}{extension}";
    }

    private async Task ExportSaveFile()
    {
        if (AppState.SaveFile is null)
        {
            Logger.LogWarning("Attempted to export save file but no save file is loaded");
            return;
        }

        Logger.LogInformation("Exporting save file");
        AppState.ShowProgressIndicator = true;

        var originalName = browserLoadSaveFile?.Name;

        // Only default to "save.sav" if we have no original filename at all
        if (string.IsNullOrWhiteSpace(originalName))
        {
            originalName = "save";
            const string fileExtensionFromName = ".sav";
            var finalName = EnsureExtension(originalName, fileExtensionFromName);
            Logger.LogDebug("Exporting save file as: {FileName}", finalName);

            await WriteFile(
                AppState.SaveFile.Write().ToArray(),
                finalName,
                fileExtensionFromName,
                "Save File");
        }
        else
        {
            // Preserve the original filename exactly as it was (with or without extension)
            var fileExtensionFromName = Path.GetExtension(originalName);
            Logger.LogDebug("Exporting save file as: {FileName}", originalName);

            await WriteFile(
                AppState.SaveFile.Write().ToArray(),
                originalName,
                fileExtensionFromName,
                "Save File");
        }

        Logger.LogInformation("Save file exported successfully");
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
            Logger.LogWarning("Attempted to load Pokémon file but no save file is loaded");
            return;
        }

        Logger.LogInformation("Loading Pokémon file: {FileName}", browserLoadPokemonFile.Name);
        AppState.ShowProgressIndicator = true;

        try
        {
            await using var fileStream = browserLoadPokemonFile.OpenReadStream(Constants.MaxFileSize);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var data = memoryStream.ToArray();
            Logger.LogDebug("Read {ByteCount} bytes from Pokémon file", data.Length);

            if (!FileUtil.TryGetPKM(data, out var pkm, Path.GetExtension(browserLoadPokemonFile.Name), saveFile))
            {
                Logger.LogError("Failed to load Pokémon file: {FileName} - Not a supported format", browserLoadPokemonFile.Name);
                await DialogService.ShowMessageBox("Error", "The file is not a supported Pokémon file.");
                return;
            }

            var pokemon = pkm.Clone();

            if (pkm.GetType() != saveFile.PKMType)
            {
                Logger.LogDebug("Converting Pokémon from {SourceType} to {TargetType}", pkm.GetType().Name, saveFile.PKMType.Name);
                pokemon = EntityConverter.ConvertToType(pkm, saveFile.PKMType, out var c);
                if (!c.IsSuccess || pokemon is null)
                {
                    Logger.LogError("Failed to convert Pokémon: {ConversionResult}", c.GetDisplayString(pkm, saveFile.PKMType));
                    await DialogService.ShowMessageBox("Error", c.GetDisplayString(pkm, saveFile.PKMType));
                    return;
                }
            }

            saveFile.AdaptToSaveFile(pokemon);

            var index = saveFile.NextOpenBoxSlot();
            if (index < 0)
            {
                Logger.LogWarning("No available box slots for importing Pokémon");
                return;
            }

            saveFile.GetBoxSlotFromIndex(index, out var box, out var slot);
            saveFile.SetBoxSlotAtIndex(pokemon, index);
            Logger.LogInformation("Pokémon imported successfully to Box {Box}, Slot {Slot}", box + 1, slot + 1);

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
            Logger.LogError(ex, "Error loading Pokémon file: {FileName}", browserLoadPokemonFile.Name);
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
            Logger.LogWarning("Attempted to load Mystery Gift file but no save file is loaded");
            return;
        }

        Logger.LogInformation("Loading Mystery Gift file: {FileName}", browserLoadMysteryGiftFile.Name);
        AppState.ShowProgressIndicator = true;

        try
        {
            await using var fileStream = browserLoadMysteryGiftFile.OpenReadStream(Constants.MaxFileSize);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var data = memoryStream.ToArray();
            Logger.LogDebug("Read {ByteCount} bytes from Mystery Gift file", data.Length);

            if (!FileUtil.TryGetMysteryGift(data, out var mysteryGift,
                    Path.GetExtension(browserLoadMysteryGiftFile.Name)))
            {
                Logger.LogError("Failed to load Mystery Gift file: {FileName} - Not a supported format", browserLoadMysteryGiftFile.Name);
                await DialogService.ShowMessageBox("Error", "The file is not a supported Mystery Gift file.");
                return;
            }

            if (mysteryGift.Species.IsInvalidSpecies())
            {
                Logger.LogError("Mystery Gift Pokémon is invalid: Species {Species}", mysteryGift.Species);
                await DialogService.ShowMessageBox("Error", "The Mystery Gift Pokémon is invalid.");
                return;
            }

            await AppService.ImportMysteryGift(data, Path.GetExtension(browserLoadMysteryGiftFile.Name), out var isSuccessful, out var resultsMessage);

            if (isSuccessful)
            {
                Logger.LogInformation("Mystery Gift imported successfully");
            }
            else
            {
                Logger.LogWarning("Mystery Gift import failed: {Message}", resultsMessage);
            }

            await DialogService.ShowMessageBox(title, resultsMessage);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading Mystery Gift file: {FileName}", browserLoadMysteryGiftFile.Name);
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
            Logger.LogWarning("Attempted to export Pokémon but no Pokémon is selected");
            return;
        }

        var pkm = AppService.EditFormPokemon;
        Logger.LogInformation("Exporting Pokémon: {Species}", pkm.Species);

        AppState.ShowProgressIndicator = true;

        pkm.RefreshChecksum();
        var cleanFileName = AppService.GetCleanFileName(pkm);
        var data = GetPokemonFileData(pkm);
        Logger.LogDebug("Exporting Pokémon as: {FileName}, Size: {Size} bytes", cleanFileName, data.Length);

        await WriteFile(data, cleanFileName, $".{pkm.Extension}", "Pokémon File");
        Logger.LogInformation("Pokémon exported successfully");

        AppState.ShowProgressIndicator = false;
    }

    private static byte[] GetPokemonFileData(PKM? pokemon) =>
        pokemon is null
            ? []
            : pokemon.DecryptedPartyData;

    private async Task WriteFile(byte[] data, string fileName, string fileTypeExtension, string fileTypeDescription)
    {
        Logger.LogDebug("Writing file: {FileName}, Size: {Size} bytes", fileName, data.Length);

        if (!await FileSystemAccessService.IsSupportedAsync())
        {
            Logger.LogDebug("File System Access API not supported, using legacy method");
            await WriteFileOldWay(data, fileName, fileTypeExtension);
            return;
        }

        try
        {
            await JSRuntime.InvokeVoidAsync(
                "showFilePickerAndWrite",
                fileName,
                data,
                fileTypeExtension,
                fileTypeDescription);
            Logger.LogDebug("File written successfully using File System Access API");
        }
        catch (JSException ex)
        {
            Logger.LogError(ex, "Error writing file using File System Access API: {FileName}", fileName);
        }
    }

    private async Task WriteFileOldWay(byte[] data, string fileName, string fileTypeExtension)
    {
        var finalName = EnsureExtension(fileName, fileTypeExtension);

        var base64String = Convert.ToBase64String(data);

        var element = await JSRuntime.InvokeAsync<IJSObjectReference>(
            "eval",
            "document.createElement('a')");

        // You can keep octet-stream or mirror the JS type.
        await element.InvokeVoidAsync(
            "setAttribute",
            "href",
            $"data:application/x-pokemon-savedata;base64,{base64String}");

        await element.InvokeVoidAsync("setAttribute", "download", finalName);

        // No need for target/rel; we're just triggering a download.
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
