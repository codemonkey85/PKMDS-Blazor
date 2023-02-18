namespace Pkmds.Rcl;

public record AppState : IAppState
{
    public event Action? OnAppStateChanged;

    public SaveFile? SaveFile { get; set; }

    public PKM? SelectedPokemon { get; set; }

    public int? SelectedBoxSlot { get; set; }

    public void Refresh() => OnAppStateChanged?.Invoke();
}
