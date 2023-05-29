namespace Pkmds.Rcl.Shared;

public partial class MainLayout : IDisposable
{
    private bool drawerOpen = true;
    private bool isDarkMode;
    private MudThemeProvider? mudThemeProvider;

    protected override void OnInitialized() =>
        AppState.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        AppState.OnAppStateChanged -= StateHasChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && mudThemeProvider is not null)
        {
            isDarkMode = await mudThemeProvider.GetSystemPreference();
            await mudThemeProvider.WatchSystemPreference(OnSystemPreferenceChanged);
            StateHasChanged();
        }
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task OnSystemPreferenceChanged(bool newValue)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        isDarkMode = newValue;
        StateHasChanged();
    }

    private void DrawerToggle() => drawerOpen = !drawerOpen;

    private const long MaxFileSize = 4_000_000L; // bytes
    private IBrowserFile? browserLoadSaveFile;

    private async Task ShowLoadSaveFileDialogAsync()
    {
        var dialog = await DialogService.ShowAsync<FileUploadDialog>();
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
        AppState.SelectedSlotNumber = null;
        AppState.ShowProgressIndicator = true;

        await using var fileStream = browserLoadSaveFile.OpenReadStream(MaxFileSize);
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var data = memoryStream.ToArray();
        AppState.SaveFile = SaveUtil.GetVariantSAV(data);
        AppState.ShowProgressIndicator = false;
        if (AppState.SaveFile is null)
        {
            return;
        }

        AppState.FileDisplayName = $"{AppState.SaveFile.OT} ({AppState.SaveFile.DisplayTID}, {AppState.SaveFile.Version}, {AppState.SaveFile.PlayTimeString})";
        AppState.Refresh();
    }

    private async Task ExportSaveFileAsync()
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
        if (AppState.EditFormPokemon is null)
        {
            return;
        }

        var pkm = AppState.EditFormPokemon;

        AppState.ShowProgressIndicator = true;

        pkm.RefreshChecksum();
        var cleanFileName = AppState.GetCleanFileName(pkm);
        await WriteFile(pkm.Data, cleanFileName);

        AppState.ShowProgressIndicator = false;
    }

    private async Task WriteFile(byte[] data, string fileName)
    {
        // Convert the byte array to a base64 string
        var base64String = Convert.ToBase64String(data);

        // Create a download link element
        var element = await JSRuntime.InvokeAsync<IJSObjectReference>("eval", "document.createElement('a')");

        // Set the download link properties
        await element.InvokeVoidAsync("setAttribute", "href", "data:application/octet-stream;base64," + base64String);
        await element.InvokeVoidAsync("setAttribute", "download", fileName);

        // Programmatically click the download link
        await element.InvokeVoidAsync("click");
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
