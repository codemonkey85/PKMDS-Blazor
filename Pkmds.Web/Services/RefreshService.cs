namespace Pkmds.Web.Services;

public class RefreshService : IRefreshService
{
    public RefreshService() => Instance = this; // Set the singleton instance
    private static RefreshService? Instance { get; set; }

    public event Action? OnAppStateChanged;
    public event Action? OnBoxStateChanged;
    public event Action? OnPartyStateChanged;
    public event Action? OnUpdateAvailable;

    void IRefreshService.ShowUpdateMessage() => ShowUpdateMessage();

    public void Refresh() => OnAppStateChanged?.Invoke();

    public void RefreshBoxState() => OnBoxStateChanged?.Invoke();

    public void RefreshPartyState() => OnPartyStateChanged?.Invoke();

    public void RefreshBoxAndPartyState()
    {
        RefreshBoxState();
        RefreshPartyState();
    }

    [JSInvokable(nameof(ShowUpdateMessage))]
    public static void ShowUpdateMessage() => Instance?.OnUpdateAvailable?.Invoke();
}
