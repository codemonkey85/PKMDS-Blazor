namespace Pkmds.Rcl.Services;

/// <inheritdoc cref="ISettingsService" />
public sealed class SettingsService(
    IJSRuntime jsRuntime,
    IAppState appState,
    IHostService hostService,
    ILoggingService loggingService) : ISettingsService
{
    private const string SettingsKey = "pkmds_settings";
    private const string LegacyThemeKey = "pkmds_theme";
    private const string LegacyHaxKey = "pkmds_hax_enabled";

    /// <inheritdoc />
    public AppSettings Settings { get; private set; } = new();

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        try
        {
            var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", SettingsKey);

            if (json is not null)
            {
                try
                {
                    Settings = JsonSerializer.Deserialize(json, PkmdsJsonContext.Default.AppSettings) ?? new AppSettings();

                    // Re-persist if ThemeMode was invalid so pkmds_theme stays in sync.
                    if (NormalizeThemeMode(Settings.ThemeMode) != Settings.ThemeMode)
                    {
                        await SaveAsync(Settings);
                        return;
                    }
                }
                catch
                {
                    Settings = new AppSettings();
                    await SaveAsync(Settings);
                    await jsRuntime.InvokeVoidAsync("localStorage.removeItem", LegacyHaxKey);
                    return;
                }
            }
            else
            {
                // Migrate from legacy individual keys on first run
                var legacyTheme = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", LegacyThemeKey);
                var legacyHax = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", LegacyHaxKey);

                Settings = new AppSettings { ThemeMode = legacyTheme ?? "system", IsHaXEnabled = legacyHax == "true" };

                // Persist migrated settings so migration does not re-run on subsequent startups.
                await SaveAsync(Settings);
                await jsRuntime.InvokeVoidAsync("localStorage.removeItem", LegacyHaxKey);
                return;
            }

            ApplyEmbeddedHostOverrides();
            ApplyToServices();
        }
        catch (JSException)
        {
            // Fallback to in-memory defaults when localStorage is unavailable or blocked.
            Settings = new AppSettings();
            ApplyEmbeddedHostOverrides();
            ApplyToServices();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings)
    {
        Settings = settings with { ThemeMode = NormalizeThemeMode(settings.ThemeMode) };
        ApplyEmbeddedHostOverrides();
        ApplyToServices();

        var json = JsonSerializer.Serialize(Settings, PkmdsJsonContext.Default.AppSettings);
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", SettingsKey, json);

            // Keep pkmds_theme in sync for the JS early-load script (prevents FOUC on reload).
            // In embedded host mode the host owns appearance, so always remove the legacy key
            // — that way theme-init.js falls through to prefers-color-scheme on the next load
            // and we never persist a theme override that would survive into a future standalone
            // session and confuse the user.
            if (hostService.IsEmbedded || Settings.ThemeMode == "system")
            {
                await jsRuntime.InvokeVoidAsync("localStorage.removeItem", LegacyThemeKey);
            }
            else
            {
                await jsRuntime.InvokeVoidAsync("localStorage.setItem", LegacyThemeKey, Settings.ThemeMode);
            }
        }
        catch (JSException)
        {
            // Ignore persistence failures so the app continues with in-memory settings.
        }
    }

    /// <inheritdoc />
    public Task ResetAsync() => SaveAsync(new AppSettings());

    private static string NormalizeThemeMode(string value) =>
        value is "light" or "dark"
            ? value
            : "system";

    /// <summary>
    /// In embedded host mode, force ThemeMode to "system" so the app follows
    /// the host's appearance via prefers-color-scheme. The override is applied
    /// in-memory only — the user's standalone-mode preference (if any) is
    /// preserved in localStorage and re-takes effect outside embedded mode.
    /// </summary>
    private void ApplyEmbeddedHostOverrides()
    {
        if (hostService.IsEmbedded && Settings.ThemeMode != "system")
        {
            Settings = Settings with { ThemeMode = "system" };
        }
    }

    private void ApplyToServices()
    {
        appState.IsHaXEnabled = Settings.IsHaXEnabled;
        appState.SpriteStyle = Settings.SpriteStyle;
        appState.ShowLegalIndicator = Settings.ShowLegalIndicator;
        appState.ShowFishyIndicator = Settings.ShowFishyIndicator;
        appState.ShowIllegalIndicator = Settings.ShowIllegalIndicator;
        appState.HapticsEnabled = Settings.HapticsEnabled;
        loggingService.IsVerboseLoggingEnabled = Settings.IsVerboseLoggingEnabled;
    }
}
