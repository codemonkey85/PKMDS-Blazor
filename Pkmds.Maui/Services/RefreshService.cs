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

    public void RefreshBoxState() => OnBoxStateChanged?.Invoke();

    public void RefreshPartyState() => OnPartyStateChanged?.Invoke();

    public void RefreshBoxAndPartyState()
    {
        RefreshBoxState();
        RefreshPartyState();
        Refresh(); // Also trigger general app state change for components that only subscribe to OnAppStateChanged
    }

    public void RefreshTheme(bool isDarkMode) => OnThemeChanged?.Invoke(isDarkMode);

    [JSInvokable(nameof(ShowUpdateMessage))]
    public static void ShowUpdateMessage()
    {
    }
}
