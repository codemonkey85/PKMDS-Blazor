namespace Pkmds.Rcl.Services;

/// <summary>
/// Manages loading, saving, and resetting persistent application settings.
/// Settings are serialized as JSON to the <c>pkmds_settings</c> localStorage key.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the currently active settings snapshot.
    /// </summary>
    AppSettings Settings { get; }

    /// <summary>
    /// Loads settings from localStorage. Falls back to migrating legacy individual keys
    /// (<c>pkmds_theme</c>, <c>pkmds_hax_enabled</c>) if the JSON key is not yet present.
    /// After loading, applies the settings to <see cref="IAppState" /> and <see cref="ILoggingService" />.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Persists <paramref name="settings" /> to localStorage and updates
    /// <see cref="IAppState" /> and <see cref="ILoggingService" /> accordingly.
    /// Also writes the <c>pkmds_theme</c> key for the JS early-load script.
    /// </summary>
    Task SaveAsync(AppSettings settings);

    /// <summary>
    /// Resets settings to their default values and persists them.
    /// The one-time PKHaX warning acknowledgement (<c>pkmds_hax_warning_ack</c>) is preserved.
    /// </summary>
    Task ResetAsync();
}
