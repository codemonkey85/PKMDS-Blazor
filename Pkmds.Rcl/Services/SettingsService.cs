namespace Pkmds.Rcl.Services;

/// <summary>
/// <inheritdoc cref="ISettingsService"/>
/// </summary>
public sealed class SettingsService(
    IJSRuntime jsRuntime,
    IAppState appState,
    ILoggingService loggingService) : ISettingsService
{
    private const string SettingsKey = "pkmds_settings";
    private const string LegacyThemeKey = "pkmds_theme";
    private const string LegacyHaxKey = "pkmds_hax_enabled";

    private AppSettings _settings = new();

    /// <inheritdoc/>
    public AppSettings Settings => _settings;

    /// <inheritdoc/>
    public async Task LoadAsync()
    {
        var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", SettingsKey);

        if (json is not null)
        {
            try
            {
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                _settings = new AppSettings();
            }
        }
        else
        {
            // Migrate from legacy individual keys on first run
            var legacyTheme = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", LegacyThemeKey);
            var legacyHax = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", LegacyHaxKey);

            _settings = new AppSettings
            {
                ThemeMode = legacyTheme ?? "system",
                IsHaXEnabled = legacyHax == "true",
            };
        }

        ApplyToServices();
    }

    /// <inheritdoc/>
    public async Task SaveAsync(AppSettings settings)
    {
        _settings = settings;
        ApplyToServices();

        var json = JsonSerializer.Serialize(_settings);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", SettingsKey, json);

        // Keep pkmds_theme in sync for the JS early-load script (prevents FOUC on reload)
        if (_settings.ThemeMode == "system")
        {
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", LegacyThemeKey);
        }
        else
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", LegacyThemeKey, _settings.ThemeMode);
        }
    }

    /// <inheritdoc/>
    public Task ResetAsync() => SaveAsync(new AppSettings());

    private void ApplyToServices()
    {
        appState.IsHaXEnabled = _settings.IsHaXEnabled;
        loggingService.IsVerboseLoggingEnabled = _settings.IsVerboseLoggingEnabled;
    }
}
