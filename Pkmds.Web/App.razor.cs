using Microsoft.AspNetCore.Components;
using MudBlazor;
using Pkmds.Rcl.Components.Dialogs;

namespace Pkmds.Web;

public partial class App : IDisposable
{
    private ErrorBoundary? errorBoundary;
    private MudThemeProvider? mudThemeProvider;
    private bool isDarkMode;

    [Inject]
    private IRefreshService RefreshService { get; set; } = null!;

    [Inject]
    private IDialogOptionsHelper DialogOptionsHelper { get; set; } = null!;

    public void Dispose() => RefreshService.OnThemeChanged -= HandleThemeChanged;

    protected override void OnInitialized() =>
        RefreshService.OnThemeChanged += HandleThemeChanged;

    private void HandleThemeChanged(bool darkMode)
    {
        isDarkMode = darkMode;
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await JsRuntime.InvokeVoidAsync("addUpdateListener");

        if (mudThemeProvider is not null)
        {
            var systemIsDarkMode = await mudThemeProvider.GetSystemDarkModeAsync();
            RefreshService.RefreshSystemTheme(systemIsDarkMode);
            await mudThemeProvider.WatchSystemDarkModeAsync(OnSystemPreferenceChanged);
        }
    }

    private Task OnSystemPreferenceChanged(bool newValue)
    {
        RefreshService.RefreshSystemTheme(newValue);
        return Task.CompletedTask;
    }

    private async Task ShowCrashReportDialog(Exception? exception)
    {
        var parameters = new DialogParameters
        {
            { nameof(BugReportDialog.HasSaveFile), AppState.SaveFile is not null },
            { nameof(BugReportDialog.AppVersion), AppState.AppVersion ?? string.Empty },
            { nameof(BugReportDialog.CapturedException), exception },
        };
        var options = await DialogOptionsHelper.BuildAsync(
            MaxWidth.Small,
            closeOnEscapeKey: false,
            backdropClick: false);
        var dialog = await DialogService.ShowAsync<BugReportDialog>("Report this crash", parameters, options);
        await dialog.Result;
    }
}
