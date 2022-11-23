namespace Pkmds.Rcl;

public interface IAppState
{
    public event Action? OnAppStateChanged;

    SaveFile? SaveFile { get; set; }

    PKM? SelectedPokemon { get; set; }
    void Refresh();
}
