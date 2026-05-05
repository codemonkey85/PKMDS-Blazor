namespace Pkmds.Rcl.Services;

/// <summary>
/// Service for managing UI refresh events across the application.
/// Components can subscribe to specific events to re-render when relevant state changes.
/// </summary>
public interface IRefreshService
{
    /// <summary>
    /// Event triggered when general application state changes (e.g., save file loaded, language changed).
    /// </summary>
    event Action? OnAppStateChanged;

    /// <summary>
    /// Event triggered when box data changes (e.g., Pokémon moved, edited, or deleted in boxes).
    /// </summary>
    event Action? OnBoxStateChanged;

    /// <summary>
    /// Event triggered when party data changes (e.g., Pokémon moved, edited, or deleted in party).
    /// </summary>
    event Action? OnPartyStateChanged;

    /// <summary>
    /// Event triggered when an application update is available.
    /// </summary>
    event Action? OnUpdateAvailable;

    /// <summary>
    /// Event triggered when the UI theme changes between light and dark modes.
    /// </summary>
    event Action<bool>? OnThemeChanged;

    /// <summary>
    /// Triggers a general application state refresh.
    /// </summary>
    void Refresh();

    /// <summary>
    /// Triggers a refresh for components displaying box data.
    /// </summary>
    void RefreshBoxState();

    /// <summary>
    /// Triggers a refresh for components displaying party data.
    /// </summary>
    void RefreshPartyState();

    /// <summary>
    /// Triggers a refresh for both box and party data simultaneously.
    /// Useful for operations that affect both (e.g., Let's Go Pikachu/Eevee).
    /// </summary>
    void RefreshBoxAndPartyState();

    /// <summary>
    /// Triggers a theme change event.
    /// </summary>
    /// <param name="isDarkMode">True for dark mode, false for light mode.</param>
    void RefreshTheme(bool isDarkMode);

    /// <summary>
    /// Event triggered when the OS-level dark mode preference changes. Raised by the
    /// top-level MudThemeProvider watcher so layouts can recompute their effective theme.
    /// </summary>
    event Action<bool>? OnSystemThemeChanged;

    /// <summary>
    /// Broadcasts a change to the OS-level dark mode preference.
    /// </summary>
    /// <param name="systemIsDarkMode">True if the OS is in dark mode.</param>
    void RefreshSystemTheme(bool systemIsDarkMode);

    /// <summary>
    /// Event triggered when a component should navigate to the Party/Box tab.
    /// </summary>
    event Action? OnRequestJumpToPartyBox;

    /// <summary>
    /// Requests that the UI navigate to the Party/Box tab.
    /// </summary>
    void RequestJumpToPartyBox();

    /// <summary>
    /// Displays a message notifying the user that an application update is available.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    void ShowUpdateMessage();
}
