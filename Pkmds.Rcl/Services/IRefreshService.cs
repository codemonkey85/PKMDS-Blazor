namespace Pkmds.Rcl.Services;

public interface IRefreshService
{
    event Action? OnAppStateChanged;

    event Action? OnBoxStateChanged;

    event Action? OnPartyStateChanged;

    event Action? OnUpdateAvailable;

    event Action<bool>? OnThemeChanged;

    void Refresh();

    void RefreshBoxState();

    void RefreshPartyState();

    void RefreshBoxAndPartyState();

    void RefreshTheme(bool isDarkMode);

    // ReSharper disable once UnusedMember.Global
    void ShowUpdateMessage();
}
