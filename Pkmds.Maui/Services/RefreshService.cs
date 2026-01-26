using Microsoft.JSInterop;
using Pkmds.Rcl.Services;

namespace Pkmds.Maui.Services;

public class RefreshService : IRefreshService
{
    public RefreshService() => Instance = this; // Set the singleton instance
    private static RefreshService? Instance { get; set; }

    public event Action? OnAppStateChanged;
    public event Action? OnBoxStateChanged;
    public event Action? OnPartyStateChanged;
    public event Action? OnUpdateAvailable;
    public event Action<bool>? OnThemeChanged;

    void IRefreshService.ShowUpdateMessage() => ShowUpdateMessage();

    public void Refresh() => OnAppStateChanged?.Invoke();

    public void RefreshBoxState()
    {
        OnBoxStateChanged?.Invoke();
        Refresh(); // Also trigger general app state change for components that only subscribe to OnAppStateChanged
    }

    public void RefreshPartyState()
    {
        OnPartyStateChanged?.Invoke();
        Refresh(); // Also trigger general app state change for components that only subscribe to OnAppStateChanged
    }

    public void RefreshBoxAndPartyState()
    {
        OnBoxStateChanged?.Invoke();
        OnPartyStateChanged?.Invoke();
        Refresh(); // Trigger general app state change once for both events
    }

    public void RefreshTheme(bool isDarkMode) => OnThemeChanged?.Invoke(isDarkMode);

    [JSInvokable(nameof(ShowUpdateMessage))]
    public static void ShowUpdateMessage() => Instance?.OnUpdateAvailable?.Invoke();
}
