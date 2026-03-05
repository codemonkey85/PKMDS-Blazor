namespace Pkmds.Web.Services;

/// <summary>
/// Blazor WebAssembly implementation of the refresh service.
/// Manages UI refresh events and provides JavaScript interop for service worker update notifications.
/// </summary>
public class RefreshService : IRefreshService
{
    /// <summary>
    /// Initializes a new instance and sets the singleton instance for JavaScript interop.
    /// </summary>
    public RefreshService() => Instance = this; // Set the singleton instance for JS interop

    /// <summary>
    /// Static singleton instance used by JavaScript to invoke update notifications.
    /// </summary>
    private static RefreshService? Instance { get; set; }

    /// <inheritdoc />
    public event Action? OnAppStateChanged;

    /// <inheritdoc />
    public event Action? OnBoxStateChanged;

    /// <inheritdoc />
    public event Action? OnPartyStateChanged;

    /// <inheritdoc />
    public event Action? OnUpdateAvailable;

    /// <inheritdoc />
    public event Action<bool>? OnThemeChanged;

    /// <inheritdoc />
    void IRefreshService.ShowUpdateMessage() => ShowUpdateMessage();

    /// <inheritdoc />
    public void Refresh() => OnAppStateChanged?.Invoke();

    /// <inheritdoc />
    public void RefreshBoxState()
    {
        OnBoxStateChanged?.Invoke();
        // Also trigger general app state change for components that only subscribe to OnAppStateChanged
        Refresh();
    }

    /// <inheritdoc />
    public void RefreshPartyState()
    {
        OnPartyStateChanged?.Invoke();
        // Also trigger general app state change for components that only subscribe to OnAppStateChanged
        Refresh();
    }

    /// <inheritdoc />
    public void RefreshBoxAndPartyState()
    {
        OnBoxStateChanged?.Invoke();
        OnPartyStateChanged?.Invoke();
        // Trigger general app state change once for both events
        Refresh();
    }

    /// <inheritdoc />
    public void RefreshTheme(bool isDarkMode) => OnThemeChanged?.Invoke(isDarkMode);

    /// <summary>
    /// JavaScript-invokable method called by the service worker when an app update is detected.
    /// </summary>
    [JSInvokable(nameof(ShowUpdateMessage))]
    public static void ShowUpdateMessage() => Instance?.OnUpdateAvailable?.Invoke();
}
