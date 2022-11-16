namespace Pkmds.Blazor;

public class AppState
{
    public Action? OnAppStateChanged;

    public SaveFile? SaveFile { get; set; }

    public PKM? SelectedPokemon { get; set; }
}
