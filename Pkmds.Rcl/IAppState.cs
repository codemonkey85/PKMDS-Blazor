namespace Pkmds.Rcl;

public interface IAppState
{
    public event Action? OnAppStateChanged;

    SaveFile? SaveFile { get; set; }

    PKM? SelectedPokemon { get; set; }

    int? SelectedBoxSlot { get; set; }

    void Refresh();
}
