namespace Pkmds.Web.Services;

public class RefreshService : IRefreshService
{
    public static RefreshService? Instance { get; private set; }

    public event Action? OnAppStateChanged;
    public event Action? OnBoxStateChanged;
    public event Action? OnPartyStateChanged;
    public event Action? OnUpdateAvailable;

    public RefreshService() => Instance = this; // Set the singleton instance

    [JSInvokable(nameof(ShowUpdateMessage))]
    public static void ShowUpdateMessage() => Instance?.OnUpdateAvailable?.Invoke();

    void IRefreshService.ShowUpdateMessage() => ShowUpdateMessage();

    public void Refresh() => OnAppStateChanged?.Invoke();

    public void RefreshBoxState() => OnBoxStateChanged?.Invoke();

    public void RefreshPartyState() => OnPartyStateChanged?.Invoke();

    public void RefreshBoxAndPartyState()
    {
        RefreshBoxState();
        RefreshPartyState();
    }
}
