namespace Pkmds.Rcl.Components.Layout;

public partial class MainLayout : IDisposable
{
    [StringSyntax(StringSyntaxAttribute.Uri)]
    private const string GitHubRepoLink = "https://github.com/codemonkey85/PKMDS-Blazor";

    private const string GitHubTooltip = "Source code on GitHub";

    private IBrowserFile? browserLoadSaveFile;
    private bool isDarkMode;
    private bool systemIsDarkMode;
    private bool settingsLoaded;
    private ThemeMode themeMode = ThemeMode.System;

    [Inject]
    private IBackupService BackupService { get; set; } = null!;

    [Inject]
    private ISettingsService SettingsService { get; set; } = null!;

    private bool IsUpdateAvailable { get; set; }
    private bool IsCheckingForUpdates { get; set; }
    private bool IsUpToDate { get; set; }
    private bool IsUpdateCheckFailed { get; set; }

    public void Dispose()
    {
        RefreshService.OnAppStateChanged -= StateHasChanged;
        RefreshService.OnUpdateAvailable -= ShowUpdateMessage;
        RefreshService.OnSystemThemeChanged -= OnSystemPreferenceChanged;
    }

    protected override void OnInitialized()
    {
        RefreshService.OnAppStateChanged += StateHasChanged;
        RefreshService.OnUpdateAvailable += ShowUpdateMessage;
        RefreshService.OnSystemThemeChanged += OnSystemPreferenceChanged;
    }

    private void ShowUpdateMessage()
    {
        IsUpdateAvailable = true;
        IsUpToDate = false;
        StateHasChanged();
    }

    private async Task CheckForUpdates()
    {
        IsCheckingForUpdates = true;
        IsUpToDate = false;
        IsUpdateCheckFailed = false;
        StateHasChanged();

        var result = await JSRuntime.InvokeAsync<string>("checkForUpdates");

        IsCheckingForUpdates = false;
        switch (result)
        {
            case "none":
                IsUpToDate = true;
                StateHasChanged();
                await Task.Delay(3000);
                IsUpToDate = false;
                break;
            case "error":
            case "no-sw":
                IsUpdateCheckFailed = true;
                StateHasChanged();
                await Task.Delay(4000);
                IsUpdateCheckFailed = false;
                break;
                // "found": JS already dispatched 'updateAvailable' → ShowUpdateMessage() sets IsUpdateAvailable = true
        }

        StateHasChanged();
    }

    private async Task ReloadApp() =>
        await JSRuntime.InvokeVoidAsync("location.reload");

    private bool ComputeIsDarkMode() => themeMode switch
    {
        ThemeMode.Dark => true,
        ThemeMode.Light => false,
        _ => systemIsDarkMode
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        // Warn users who are running inside a known in-app browser (e.g. Google Search App,
        // Facebook) whose WebView typically blocks file downloads.
        // The JS call is wrapped defensively because in-app browsers / extensions sometimes
        // inject a non-function global with the same name, which would otherwise crash the
        // entire layout init (see issue #732).
        // In embedded host mode the warning is irrelevant — a host that has explicitly
        // embedded PKMDS is a known container, not the hostile in-app webviews this
        // detector targets.
        try
        {
            var isInAppBrowser = !HostService.IsEmbedded
                && await JSRuntime.InvokeAsync<bool>("pkmdsIsInAppBrowser");
            if (isInAppBrowser)
            {
                Snackbar.Add(
                    new MarkupString(
                        "You appear to be using an in-app browser, which may not support file exports. " +
                        "For the best experience, please open this app in <strong>Safari</strong> (iOS) or " +
                        "<strong>Chrome</strong> (Android)."),
                    Severity.Warning,
                    options => options.RequireInteraction = true);
            }
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "In-app browser detection failed");
        }

        // Load all persisted settings (theme, PKHaX, verbose logging, trainer defaults)
        await SettingsService.LoadAsync();

        themeMode = SettingsService.Settings.ThemeMode switch
        {
            "dark" => ThemeMode.Dark,
            "light" => ThemeMode.Light,
            _ => ThemeMode.System
        };
        settingsLoaded = true;
        isDarkMode = ComputeIsDarkMode();
        RefreshService.RefreshTheme(isDarkMode);
        RefreshService.Refresh();

        StateHasChanged();

        // Signal embedded-host readiness AFTER settings load and theme apply
        // so the host can call window.PKMDS.host.loadSave() without racing the
        // Blazor boot sequence. Without this, hosts would have to poll for
        // window.PKMDS.host existence before calling in. See #787.
        if (HostService.IsEmbedded)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("PKMDS.host._sendMessage", "ready", new { });
            }
            catch (JSException ex)
            {
                Logger.LogWarning(ex, "Failed to fire host ready signal");
            }
        }
    }

    private async void OnSystemPreferenceChanged(bool newValue)
    {
        systemIsDarkMode = newValue;

        // Until persisted settings have loaded, `themeMode` is still the default `System`,
        // so a transient OS-dark signal would incorrectly flip `isDarkMode` to true even when
        // the user's persisted preference is explicitly Light (or Dark). That intermediate
        // flip leaves MudMainContent stranded in dark mode on first load — see issue #755.
        // Gating here preserves the cached `systemIsDarkMode` for later use once settings
        // load and the user's real preference is known.
        if (!settingsLoaded)
        {
            return;
        }

        if (themeMode == ThemeMode.System)
        {
            isDarkMode = newValue;
            RefreshService.RefreshTheme(isDarkMode);
            var themeStr = newValue
                ? "dark"
                : "light";

            // `OnSystemPreferenceChanged` is `async void` (required by MudThemeProvider's
            // WatchSystemDarkModeAsync callback), so any JSException thrown from the
            // setAppTheme call would otherwise be unobserved and could tear down rendering.
            try
            {
                await JSRuntime.InvokeVoidAsync("setAppTheme", themeStr);
            }
            catch (JSException ex)
            {
                Logger.LogWarning(ex, "Failed to apply system theme change to DOM");
            }

            StateHasChanged();
        }
    }

    private async Task OnThemeModeChanged(ThemeMode newMode)
    {
        themeMode = newMode;
        isDarkMode = ComputeIsDarkMode();
        RefreshService.RefreshTheme(isDarkMode);

        var themeStr = themeMode switch
        {
            ThemeMode.Light => "light",
            ThemeMode.Dark => "dark",
            _ => isDarkMode
                ? "dark"
                : "light"
        };
        await JSRuntime.InvokeVoidAsync("setAppTheme", themeStr);

        // Persist the new theme through the settings service (keeps pkmds_theme in sync too)
        await SettingsService.SaveAsync(SettingsService.Settings with
        {
            ThemeMode = themeMode switch
            {
                ThemeMode.Light => "light",
                ThemeMode.Dark => "dark",
                _ => "system"
            }
        });

        StateHasChanged();
    }

    private void DrawerToggle() => AppService.ToggleDrawer();

#if DEBUG
    private static bool IsDebugBuild => true;

    private static void TriggerCrash() =>
        throw new InvalidOperationException("Manually triggered crash (DEBUG build only) to test the ErrorBoundary + crash-report dialog.");
#else
    private static bool IsDebugBuild => false;

    private static void TriggerCrash() { }
#endif

    private async Task ShowBugReportDialog()
    {
        var parameters = new DialogParameters { { nameof(BugReportDialog.HasSaveFile), AppState.SaveFile is not null }, { nameof(BugReportDialog.AppVersion), AppState.AppVersion ?? string.Empty } };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        var dialog = await DialogService.ShowAsync<BugReportDialog>("Report a Bug", parameters, options);
        var result = await dialog.Result;
        if (result is { Data: string issueUrl })
        {
            Snackbar.Add(new MarkupString($"Bug report submitted! <a href=\"{issueUrl}\" target=\"_blank\">View issue</a>"),
                Severity.Success,
                options => options.RequireInteraction = true);
        }
    }

    private async Task ShowSettingsDialog()
    {
        var parameters =
            new DialogParameters { { nameof(AppSettingsDialog.InitialSettings), SettingsService.Settings } };

        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);

        var dialog = await DialogService.ShowAsync<AppSettingsDialog>("Settings", parameters, options);
        var result = await dialog.Result;

        if (result is { Data: AppSettings updated })
        {
            await SettingsService.SaveAsync(updated);
            RefreshService.Refresh();

            // Re-apply theme from the updated settings
            themeMode = updated.ThemeMode switch
            {
                "light" => ThemeMode.Light,
                "dark" => ThemeMode.Dark,
                _ => ThemeMode.System
            };
            isDarkMode = ComputeIsDarkMode();
            RefreshService.RefreshTheme(isDarkMode);

            var themeStr = isDarkMode
                ? "dark"
                : "light";
            await JSRuntime.InvokeVoidAsync("setAppTheme", themeStr);

            StateHasChanged();
        }
    }

    private async Task ShowSaveFileInfoDialog()
    {
        var parameters = new DialogParameters { { nameof(SaveFileInfoDialog.SaveFile), AppState.SaveFile } };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        await DialogService.ShowAsync<SaveFileInfoDialog>("Save File Info", parameters, options);
    }

    private async Task ShowSaveFileRepairDialog()
    {
        var parameters = new DialogParameters { { nameof(SaveFileRepairDialog.SaveFile), AppState.SaveFile } };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        await DialogService.ShowAsync<SaveFileRepairDialog>("Repair Save File", parameters, options);
    }

    private async Task ExportPartyAsShowdown()
    {
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        await DialogService.ShowAsync<ShowdownExportDialog>("Showdown Export", options);
    }

    private async Task ExportToPokePaste()
    {
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Medium);
        await DialogService.ShowAsync<PokePasteExportDialog>(
            "Export to PokePaste",
            new DialogParameters<PokePasteExportDialog>(),
            options);
    }

    private async Task ImportFromShowdown()
    {
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Medium);
        await DialogService.ShowAsync<ShowdownImportDialog>(
            "Import from Showdown / PokePaste",
            new DialogParameters<ShowdownImportDialog>(),
            options);
    }

    private async Task ShowBackupManagerDialog()
    {
        var parameters = new DialogParameters { { nameof(BackupManagerDialog.SaveFile), AppState.SaveFile }, { nameof(BackupManagerDialog.FileName), AppState.SaveFileName }, { nameof(BackupManagerDialog.IsManicEmu), AppState.ManicEmuSaveContext is not null }, { nameof(BackupManagerDialog.ManicEmuContext), AppState.ManicEmuSaveContext } };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Medium);
        var dialog = await DialogService.ShowAsync<BackupManagerDialog>("Backup Manager", parameters, options);
        var result = await dialog.Result;

        if (result is { Data: BackupRestoreResult restore })
        {
            await RestoreFromBackup(restore);
        }
        else if (result is { Data: BackupExportResult export })
        {
            var fileName = string.IsNullOrWhiteSpace(export.Entry.FileName)
                ? "save.sav"
                : export.Entry.FileName;
            var ext = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(ext))
            {
                ext = ".sav";
            }

            await WriteFile(export.SaveBytes, fileName, ext, "Save File");
        }
    }

    private async Task RestoreFromBackup(BackupRestoreResult restore)
    {
        AppService.ClearSelection();
        ParseSettings.ClearActiveTrainer();
        AppState.SaveFile = null;
        AppState.ShowProgressIndicator = true;

        try
        {
            var data = restore.SaveBytes;
            var fileName = restore.Entry.FileName;

            if (SaveFileLoader.TryLoad(data, fileName, out var saveFile, out var manicContext))
            {
                if (!saveFile.State.Exportable)
                {
                    Logger.LogWarning("Backup save file is not exportable (unsupported format/ROM hack): {FileName}", fileName);
                    await DialogService.ShowMessageBoxAsync("Unsupported save file",
                        "This backup cannot be restored — it may be from an unsupported ROM hack or format.");
                    AppState.ShowProgressIndicator = false;
                    return;
                }

                AppState.ManicEmuSaveContext = manicContext;
                FinishLoadingSaveFile(saveFile, fileName);
            }
            else
            {
                Logger.LogError("Failed to restore backup: invalid save data");
                await DialogService.ShowMessageBoxAsync("Error", "Failed to restore backup — the save data could not be parsed.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error restoring backup");
            await DialogService.ShowMessageBoxAsync("Error", $"Failed to restore backup: {ex.Message}");
        }

        AppState.ShowProgressIndicator = false;
        if (AppState.SaveFile is not null)
        {
            RefreshService.RefreshBoxAndPartyState();
            Snackbar.Add("Backup restored.", Severity.Success);
        }
    }

    private async Task ShowLoadSaveFileDialog()
    {
        const string message = "Choose a save file";
        const string manicEmuHint =
            "Tip: If you're using Manic EMU, upload the .3ds.sav or .3ds.save export directly " +
            "for seamless round-trip import support.";

        var dialogParameters = new DialogParameters { { nameof(FileUploadDialog.Message), message }, { nameof(FileUploadDialog.HintText), manicEmuHint } };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small, backdropClick: false);

        var dialog = await DialogService.ShowAsync<FileUploadDialog>("Load Save File",
            dialogParameters,
            options);

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
            await DialogService.ShowMessageBoxAsync("No file selected", "Please select a file to load.");
            return;
        }

        if (selectedFile.Size == 0)
        {
            Logger.LogWarning("Attempted to load empty save file: {FileName}", selectedFile.Name);
            await DialogService.ShowMessageBoxAsync("Error", "The selected file is empty.");
            return;
        }

        var fileExtension = Path.GetExtension(selectedFile.Name);
        if (fileExtension.Equals(".state", StringComparison.OrdinalIgnoreCase) ||
            fileExtension.Equals(".savestate", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogWarning("User attempted to load a save state: {FileName}", selectedFile.Name);
            await DialogService.ShowMessageBoxAsync("Wrong file type",
                "This looks like an emulator save state, not a save file. " +
                "Save states are internal emulator snapshots and cannot be edited here. " +
                "Please export the actual save file from your emulator instead (usually a .sav or .dsv file).");
            return;
        }

        Logger.LogInformation("Loading save file: {FileName}", selectedFile.Name);
        AppService.ClearSelection();
        ParseSettings.ClearActiveTrainer();
        AppState.SaveFile = null;
        AppState.ManicEmuSaveContext = null;
        AppState.ShowProgressIndicator = true;

        var data = Array.Empty<byte>();
        try
        {
            await using var fileStream = browserLoadSaveFile.OpenReadStream(Constants.MaxFileSize);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            data = memoryStream.ToArray();
            Logger.LogDebug("Read {ByteCount} bytes from save file", data.Length);

            // SaveFileLoader checks for a Manic EMU .3ds.sav ZIP (sdmc/… entries) before delegating
            // to SaveUtil.TryGetSaveFile for raw saves. The ordering matters: PKHeX's built-in
            // ZipReader would otherwise recognise the archive by its inner `main` entry and unwrap
            // it invisibly, so manicContext would never be set and export would silently produce
            // raw bytes that Manic EMU rejects on re-import (see issue #750).
            if (SaveFileLoader.TryLoad(data, selectedFile.Name, out var saveFile, out var manicContext))
            {
                if (!saveFile.State.Exportable)
                {
                    Logger.LogWarning("Save file is not exportable (unsupported format/ROM hack): {FileName}", selectedFile.Name);
                    await DialogService.ShowMessageBoxAsync("Unsupported save file",
                        "This save file cannot be loaded — it may be from an unsupported ROM hack or format.");
                    AppState.ShowProgressIndicator = false;
                    return;
                }

                if (manicContext is not null)
                {
                    AppState.ManicEmuSaveContext = manicContext;
                    Logger.LogInformation("Loaded save from Manic EMU .3ds.sav archive; entry: {EntryPath}", manicContext.SaveEntryPath);
                    Snackbar.Add(
                        "Manic EMU save archive detected — export will rebuild the ZIP for seamless re-import.",
                        Severity.Info);
                }

                FinishLoadingSaveFile(saveFile, selectedFile.Name);
            }
            else
            {
                Logger.LogError("Failed to load save file: {FileName} - Invalid save file format", selectedFile.Name);

                const string message =
                    "The selected save file is invalid. If this save file came from a ROM hack, it is not supported. Otherwise, try saving in-game and re-exporting / re-uploading the save file.";
                await DialogService.ShowMessageBoxAsync("Error", message);
                AppState.ShowProgressIndicator = false;
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading save file: {FileName}", selectedFile.Name);
            await DialogService.ShowMessageBoxAsync("Error", $"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }

        if (AppState.SaveFile is null)
        {
            AppState.ShowProgressIndicator = false;
            return;
        }

        // Auto-backup on successful load (non-fatal).
        // Use SaveFile.Write() to get properly serialized bytes — the original `data` array
        // may have been mutated in-place by decryption (e.g. SwishCrypto for Gen 8-9 saves),
        // making the raw array unparseable by TryGetSaveFile on restore.
        // For Manic EMU saves, rebuild the ZIP so that restore and export round-trip correctly
        // (exporting raw save bytes would produce a file Manic EMU can't re-import).
        if (SettingsService.Settings.IsAutoBackupEnabled)
        {
            try
            {
                var rawSave = AppState.SaveFile.Write().ToArray();
                var backupBytes = AppState.ManicEmuSaveContext is not null
                    ? ManicEmuSaveHelper.RebuildZip(AppState.ManicEmuSaveContext, rawSave)
                    : rawSave;
                await BackupService.CreateBackupAsync(
                    backupBytes, AppState.SaveFile, selectedFile.Name,
                    isManicEmu: AppState.ManicEmuSaveContext is not null, source: "auto");
                await BackupService.EnforceRetentionAsync(SettingsService.Settings.MaxBackupCount);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Auto-backup failed for {FileName}", selectedFile.Name);
            }
        }

        AppState.ShowProgressIndicator = false;
        RefreshService.RefreshBoxAndPartyState();
        Haptics.Success();
    }

    private void FinishLoadingSaveFile(SaveFile saveFile, string? fileName = null)
    {
        // Call InitFromSaveFileData to set ParseSettings.ActiveTrainer to the loaded save file.
        // This enables per-Pokémon handler state validation in HistoryVerifier.VerifyHandlerState,
        // matching PKHeX WinForms behaviour and preventing false-positive legality errors on
        // Pokémon whose OT matches the loaded trainer (e.g. BDSP Palkia).
        //
        // InitFromSaveFileData also sets AllowGBCartEra based on SAV1/SAV2.IsVirtualConsole,
        // which gates AllowGBEraEvents (Nintendo Event Mew, GS Ball Celebi, etc.) and
        // AllowGBStadium2. Physical Gen 1/2 saves correctly get AllowGBCartEra = true;
        // VC saves (filename "sav*.dat") get false. Renamed VC saves may be misidentified as
        // physical cartridge saves — that is a PKHeX bug tracked at
        // https://github.com/kwsch/PKHeX/issues/4734 and is not something we work around here,
        // as doing so breaks legitimate GB era events on real physical cartridge saves.
        ParseSettings.InitFromSaveFileData(saveFile);
        AppState.SaveFile = saveFile;
        AppState.SaveFileName = fileName;
        AppState.BoxEdit?.LoadBox(saveFile.CurrentBox);
        Logger.LogInformation("Successfully loaded save file: {SaveType}, Generation: {Generation}",
            saveFile.GetType().Name, saveFile.Generation);
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

        // Warn if the user has edits in the Pokémon editor that haven't been written
        // back to a slot — exporting now would produce a save file without those edits.
        if (!await UnsavedChangesGuard.ConfirmAsync(
                AppService,
                DialogService,
                "You have unsaved changes to a Pokémon. Save them to the slot before exporting, or export without those changes?",
                saveText: "Save & Export",
                discardText: "Export Anyway",
                snackbar: Snackbar))
        {
            return;
        }

        Logger.LogInformation("Exporting save file");
        AppState.ShowProgressIndicator = true;

        try
        {
            var rawSaveBytes = AppState.SaveFile.Write().ToArray();
            // Prefer AppState.SaveFileName — it's set consistently by both the upload path and
            // the restore-from-backup path, whereas browserLoadSaveFile?.Name is null whenever
            // the save wasn't loaded via the file picker (e.g. restored from a backup entry).
            // Without this fallback, Manic EMU saves loaded from backup would lose the
            // ".3ds.sav" compound extension on re-export.
            var originalName = AppState.SaveFileName ?? browserLoadSaveFile?.Name;

            // If the save was loaded from a Manic EMU .3ds.sav ZIP, rebuild the ZIP so the
            // user can import it directly back into Manic EMU without any manual repacking.
            if (AppState.ManicEmuSaveContext is not null)
            {
                // Echo back whichever compound extension the upload carried (.3ds.sav canonically, or
                // .3ds.save if the user renamed manually or iOS Safari mangled the suffix) so the
                // round-trip is bit-for-bit transparent. Flag the MIME as application/zip — the default
                // application/x-pokemon-savedata is wrong for an archive and confuses iOS Safari.
                var (exportName, compoundExt) = ManicEmuSaveHelper.GetExportFileName(originalName);
                Logger.LogDebug("Exporting save as Manic EMU {Extension}: {FileName}", compoundExt, exportName);

                var zipBytes = ManicEmuSaveHelper.RebuildZip(AppState.ManicEmuSaveContext, rawSaveBytes);
                await WriteFile(zipBytes, exportName, compoundExt, "Save File", mimeType: "application/zip");
            }
            // Only default to "save.sav" if we have no original filename at all
            else if (string.IsNullOrWhiteSpace(originalName))
            {
                originalName = "save";
                const string fileExtensionFromName = ".sav";
                var finalName = EnsureExtension(originalName, fileExtensionFromName);
                Logger.LogDebug("Exporting save file as: {FileName}", finalName);

                await WriteFile(rawSaveBytes, finalName, fileExtensionFromName, "Save File");
            }
            else
            {
                // Preserve the original filename exactly as it was (with or without extension)
                var fileExtensionFromName = Path.GetExtension(originalName);
                Logger.LogDebug("Exporting save file as: {FileName}", originalName);

                await WriteFile(rawSaveBytes, originalName, fileExtensionFromName, "Save File");
            }

            Logger.LogInformation("Save file exported successfully");
            Haptics.Success();
        }
        catch (Exception ex)
        {
            // Catches ZIP rebuild failures (InvalidDataException from the Manic EMU size guard,
            // malformed central directories) as well as any unexpected Write() error, so the
            // progress indicator always gets cleared in finally and the user isn't left staring
            // at a spinner with no feedback.
            Logger.LogError(ex, "Error exporting save file");
            Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            AppState.ShowProgressIndicator = false;
        }
    }

    private async Task ShowLoadPokemonFileDialog()
    {
        const string title = "Load Pokémon File";
        const string message = "Choose a Pokémon file";

        var dialogParameters = new DialogParameters { { nameof(FileUploadDialog.Message), message } };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small, backdropClick: false);

        var dialog = await DialogService.ShowAsync<FileUploadDialog>(
            title,
            dialogParameters,
            options);

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
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small, backdropClick: false);

        var dialog = await DialogService.ShowAsync<FileUploadDialog>(
            title,
            dialogParameters,
            options);

        var result = await dialog.Result;
        if (result is { Data: IBrowserFile selectedFile })
        {
            await LoadMysteryGiftFile(selectedFile);
        }
    }

    private async Task LoadPokemonFile(IBrowserFile browserLoadPokemonFile, string title)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            Logger.LogWarning("Attempted to load Pokémon file but no save file is loaded");
            return;
        }

        if (browserLoadPokemonFile.Size == 0)
        {
            Logger.LogWarning("Attempted to load empty Pokémon file: {FileName}", browserLoadPokemonFile.Name);
            Snackbar.Add("The selected file is empty.", Severity.Error);
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
                Logger.LogError("Failed to load Pokémon file: {FileName} - Not a supported format",
                    browserLoadPokemonFile.Name);
                Snackbar.Add("The file is not a supported Pokémon file.", Severity.Error);
                return;
            }

            var pokemon = pkm.Clone();

            if (pkm.GetType() != saveFile.PKMType)
            {
                Logger.LogDebug("Converting Pokémon from {SourceType} to {TargetType}", pkm.GetType().Name,
                    saveFile.PKMType.Name);
                pokemon = EntityConverter.ConvertToType(pkm, saveFile.PKMType, out var c);
                if (!c.IsSuccess || pokemon is null)
                {
                    Logger.LogError("Failed to convert Pokémon: {ConversionResult}",
                        c.GetDisplayString(pkm, saveFile.PKMType));
                    Snackbar.Add(c.GetDisplayString(pkm, saveFile.PKMType), Severity.Error);
                    return;
                }
            }

            saveFile.AdaptToSaveFile(pokemon);

            if (!await EnsureTargetSlotSelectedAsync(saveFile))
            {
                return;
            }

            AppService.EditFormPokemon = pokemon;
            var editedPkm = AppService.EditFormPokemon ?? pokemon;
            AppService.SavePokemon(editedPkm);
            Logger.LogInformation("Pokémon imported successfully via selected slot");

            var la = new LegalityAnalysis(editedPkm);
            if (la.Valid)
            {
                Snackbar.Add($"{title}: {SafeNameLookup.Species(editedPkm.Species)} imported successfully.", Severity.Success);
            }
            else
            {
                Snackbar.Add(
                    $"{title}: {SafeNameLookup.Species(editedPkm.Species)} imported, but legality check flagged issues. " +
                    "Review the Pokémon in the editor.",
                    Severity.Warning);
            }

            RefreshService.RequestJumpToPartyBox();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading Pokémon file: {FileName}", browserLoadPokemonFile.Name);
            await DialogService.ShowMessageBoxAsync("Error", $"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
        finally
        {
            AppState.ShowProgressIndicator = false;
        }
    }

    private async Task LoadMysteryGiftFile(IBrowserFile browserLoadMysteryGiftFile)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            Logger.LogWarning("Attempted to load Mystery Gift file but no save file is loaded");
            return;
        }

        if (browserLoadMysteryGiftFile.Size == 0)
        {
            Logger.LogWarning("Attempted to load empty Mystery Gift file: {FileName}", browserLoadMysteryGiftFile.Name);
            Snackbar.Add("The selected file is empty.", Severity.Error);
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

            var fileExtension = Path.GetExtension(browserLoadMysteryGiftFile.Name);

            // .wc3 files are not DataMysteryGift-compatible — WonderCard3 lives directly in the
            // save's wonder card slot, so route them to the dedicated WC3 import path before
            // reaching FileUtil.TryGetMysteryGift (which would reject them).
            if (string.Equals(fileExtension, ".wc3", StringComparison.OrdinalIgnoreCase))
            {
                await AppService.ImportWonderCard3(data, out var wc3ImportSuccessful, out var wc3ImportMessage);
                if (wc3ImportSuccessful)
                {
                    Logger.LogInformation("WC3 Wonder Card imported successfully: {Message}", wc3ImportMessage);
                    Snackbar.Add(wc3ImportMessage, Severity.Success);
                    // Notify subscribers (e.g. the Wonder Cards tab) that save state changed —
                    // otherwise the viewer keeps rendering the pre-import slot until the user
                    // navigates away and back.
                    RefreshService.Refresh();
                }
                else
                {
                    Logger.LogWarning("WC3 Wonder Card import failed: {Message}", wc3ImportMessage);
                    Snackbar.Add(wc3ImportMessage, Severity.Error);
                }

                return;
            }

            if (!FileUtil.TryGetMysteryGift(data, out var mysteryGift, fileExtension))
            {
                Logger.LogError("Failed to load Mystery Gift file: {FileName} - Not a supported format",
                    browserLoadMysteryGiftFile.Name);
                Snackbar.Add("The file is not a supported Mystery Gift file.", Severity.Error);
                return;
            }

            // Import the gift card to the mystery gift album when the save supports it.
            await AppService.ImportMysteryGift(data, fileExtension,
                out var albumImportSuccessful, out var albumImportMessage);

            if (albumImportSuccessful)
            {
                Logger.LogInformation("Mystery Gift card imported to album successfully");
                Snackbar.Add("Mystery Gift card added to Wonder Cards album.", Severity.Success);
                // Notify subscribers (e.g. the Wonder Cards tab) that save state changed.
                RefreshService.Refresh();
            }
            else
            {
                Logger.LogWarning("Mystery Gift album import: {Message}", albumImportMessage);
                Snackbar.Add(albumImportMessage, Severity.Warning);
            }

            // If the gift contains a Pokémon and is compatible with this save, generate it and
            // place it in the active slot. Incompatible cards (wrong generation, etc.) must not
            // produce a PKM even if their IsEntity flag is set.
            if (mysteryGift.IsEntity && mysteryGift.IsCardCompatible(saveFile, out _))
            {
                var originalPkm = mysteryGift.ConvertToPKM(saveFile, EncounterCriteria.Unrestricted);
                var pkm = originalPkm;
                if (pkm.GetType() != saveFile.PKMType)
                {
                    pkm = EntityConverter.ConvertToType(pkm, saveFile.PKMType, out var c);
                    if (!c.IsSuccess || pkm is null)
                    {
                        Logger.LogError("Failed to convert Mystery Gift Pokémon: {ConversionResult}",
                            c.GetDisplayString(originalPkm, saveFile.PKMType));
                        Snackbar.Add("Could not convert the gift Pokémon to the save file's format.", Severity.Error);
                        return;
                    }
                }

                saveFile.AdaptToSaveFile(pkm);

                if (!await EnsureTargetSlotSelectedAsync(saveFile))
                {
                    return;
                }

                AppService.EditFormPokemon = pkm;
                var editedPkm = AppService.EditFormPokemon ?? pkm;
                AppService.SavePokemon(editedPkm);
                Logger.LogInformation("Mystery Gift Pokémon placed in slot successfully");

                var la = new LegalityAnalysis(editedPkm);
                var speciesName = SafeNameLookup.Species(editedPkm.Species);
                if (la.Valid)
                {
                    Snackbar.Add($"{speciesName} received from Mystery Gift.", Severity.Success);
                }
                else
                {
                    Snackbar.Add(
                        $"{speciesName} received from Mystery Gift, but legality check flagged issues. " +
                        "Review the Pokémon in the editor.",
                        Severity.Warning);
                }

                RefreshService.RequestJumpToPartyBox();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading Mystery Gift file: {FileName}", browserLoadMysteryGiftFile.Name);
            await DialogService.ShowMessageBoxAsync("Error", $"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
        finally
        {
            AppState.ShowProgressIndicator = false;
        }
    }

    /// <summary>
    /// Ensures a target box slot is ready for writing. When a slot is already selected and
    /// occupied, prompts the user to overwrite, use the first available slot, or cancel.
    /// Falls back to the first empty box slot automatically when no slot is selected.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if a slot is ready and the caller should proceed;
    /// <see langword="false" /> if the caller should abort.
    /// </returns>
    private async Task<bool> EnsureTargetSlotSelectedAsync(SaveFile saveFile)
    {
        var slotType = AppService.GetSelectedPokemonSlot(out _, out _, out _);
        var isLetsGoWithSlot = saveFile is SAV7b && AppState.SelectedBoxSlotNumber.HasValue;
        var hasSelectedSlot = slotType != SelectedPokemonType.None || isLetsGoWithSlot;

        if (hasSelectedSlot)
        {
            if (AppService.EditFormPokemon?.Species == 0)
            {
                return true;
            }

            var occupantName = SafeNameLookup.Species(AppService.EditFormPokemon!.Species);
            var confirmed = await DialogService.ShowMessageBoxAsync(
                "Overwrite Pokémon?",
                $"The selected slot contains {occupantName}. Overwrite it?",
                yesText: "Overwrite",
                noText: "Use First Available Slot",
                cancelText: "Cancel");
            switch (confirmed)
            {
                case null:
                    return false;
                case false when !AppService.TrySelectFirstEmptyBoxSlot():
                    Logger.LogWarning("No available box slots");
                    Snackbar.Add("No empty box slots available. Free up a slot and try again.", Severity.Warning);
                    return false;
            }
        }
        else if (!AppService.TrySelectFirstEmptyBoxSlot())
        {
            Logger.LogWarning("No available box slots");
            Snackbar.Add("No empty box slots available. Free up a slot and try again.", Severity.Warning);
            return false;
        }

        return true;
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

    private static byte[] GetPokemonFileData(PKM? pokemon)
    {
        if (pokemon is null)
        {
            return [];
        }

        var data = new byte[pokemon.SIZE_PARTY];
        pokemon.WriteDecryptedDataParty(data);
        return data;
    }

    private async Task WriteFile(byte[] data, string fileName, string fileTypeExtension, string fileTypeDescription,
        string mimeType = "application/x-pokemon-savedata")
    {
        Logger.LogDebug("Writing file: {FileName}, Size: {Size} bytes, MIME: {Mime}", fileName, data.Length, mimeType);

        if (!await FileSystemAccessService.IsSupportedAsync())
        {
            Logger.LogDebug("File System Access API not supported, using legacy method");
            await WriteFileOldWay(data, fileName, fileTypeExtension, mimeType);
            return;
        }

        try
        {
            await JSRuntime.InvokeVoidAsync(
                "showFilePickerAndWrite",
                fileName,
                data,
                fileTypeExtension,
                fileTypeDescription,
                mimeType);
            Logger.LogDebug("File written successfully using File System Access API");
        }
        catch (JSException ex) when (ex.Message.Contains("AbortError", StringComparison.OrdinalIgnoreCase) ||
                                     ex.Message.Contains("aborted a request", StringComparison.OrdinalIgnoreCase))
        {
            // User dismissed the file picker — not an error.
            Logger.LogDebug("File save cancelled by user: {FileName}", fileName);
        }
        catch (JSException ex)
        {
            Logger.LogError(ex, "Error writing file using File System Access API: {FileName}", fileName);
            Snackbar.Add("Export failed. Please try again or use a different browser.", Severity.Error);
        }
    }

    private async Task WriteFileOldWay(byte[] data, string fileName, string fileTypeExtension, string mimeType)
    {
        var finalName = EnsureExtension(fileName, fileTypeExtension);

        var base64String = Convert.ToBase64String(data);

        var element = await JSRuntime.InvokeAsync<IJSObjectReference>(
            "eval",
            "document.createElement('a')");

        await element.InvokeVoidAsync(
            "setAttribute",
            "href",
            $"data:{mimeType};base64,{base64String}");

        await element.InvokeVoidAsync("setAttribute", "download", finalName);

        // No need for target/rel; we're just triggering a download.
        await element.InvokeVoidAsync("click");
    }

    private enum ThemeMode
    {
        Light,
        System,
        Dark
    }
}
