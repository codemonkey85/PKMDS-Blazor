namespace Pkmds.Web.Services;

public record RefreshService(IAppState AppState) : IRefreshService
{
    public event Action? OnAppStateChanged;
    public event Action? OnBoxStateChanged;
    public event Action? OnPartyStateChanged;
    public event Action? OnUpdateAvailable;

    public void Refresh() => OnAppStateChanged?.Invoke();

    public void RefreshBoxState() => OnBoxStateChanged?.Invoke();

    public void RefreshPartyState() => OnPartyStateChanged?.Invoke();

    public void RefreshBoxAndPartyState()
    {
        RefreshBoxState();
        RefreshPartyState();
    }

    [JSInvokable("ShowUpdateMessage")]
    public void ShowUpdateMessage() => OnUpdateAvailable?.Invoke();
}
