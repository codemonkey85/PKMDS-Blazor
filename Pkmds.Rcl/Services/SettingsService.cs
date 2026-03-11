namespace Pkmds.Rcl.Services;

/// <inheritdoc cref="ISettingsService"/>
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
        try
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
                    await SaveAsync(_settings);
                    await jsRuntime.InvokeVoidAsync("localStorage.removeItem", LegacyHaxKey);
                    return;
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

                // Persist migrated settings so migration does not re-run on subsequent startups.
                await SaveAsync(_settings);
                await jsRuntime.InvokeVoidAsync("localStorage.removeItem", LegacyHaxKey);
                return;
            }

            ApplyToServices();
        }
        catch (JSException)
        {
            // Fallback to in-memory defaults when localStorage is unavailable or blocked.
            _settings = new AppSettings();
            ApplyToServices();
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(AppSettings settings)
    {
        _settings = settings with { ThemeMode = NormalizeThemeMode(settings.ThemeMode) };
        ApplyToServices();

        var json = JsonSerializer.Serialize(_settings);
        try
        {
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
        catch (JSException)
        {
            // Ignore persistence failures so the app continues with in-memory settings.
        }
    }

    /// <inheritdoc/>
    public Task ResetAsync() => SaveAsync(new AppSettings());

    private static string NormalizeThemeMode(string value) =>
        value is "light" or "dark" ? value : "system";

    private void ApplyToServices()
    {
        appState.IsHaXEnabled = _settings.IsHaXEnabled;
        loggingService.IsVerboseLoggingEnabled = _settings.IsVerboseLoggingEnabled;
    }
}
