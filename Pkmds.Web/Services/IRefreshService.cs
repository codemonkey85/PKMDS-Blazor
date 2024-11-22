namespace Pkmds.Web.Services;

public interface IRefreshService
{
    event Action? OnAppStateChanged;

    event Action? OnBoxStateChanged;

    event Action? OnPartyStateChanged;

    event Action? OnUpdateAvailable;

    void Refresh();

    void RefreshBoxState();

    void RefreshPartyState();

    void RefreshBoxAndPartyState();

    void ShowUpdateMessage();
}
