namespace Pkmds.Web.Services;

public record RefreshService(IAppState AppState) : IRefreshService
{
    public event Action? OnAppStateChanged;
    public event Action? OnBoxStateChanged;
    public event Action? OnPartyStateChanged;

    public void Refresh() => OnAppStateChanged?.Invoke();

    public void RefreshBoxState() => OnBoxStateChanged?.Invoke();

    public void RefreshPartyState() => OnPartyStateChanged?.Invoke();
}
