namespace Pkmds.Rcl;

public record AppState : IAppState
{
    public AppState()
    {
        GameStrings = GameInfo.GetStrings("en-US");
    }

    public GameStrings? GameStrings { get; set; }

    public string[] GenderForms => new[] { "", "F", "" };

    public event Action? OnAppStateChanged;

    public SaveFile? SaveFile { get; set; }

    public PKM? SelectedPokemon { get; set; }

    public int? SelectedBoxSlot { get; set; }

    public void Refresh() => OnAppStateChanged?.Invoke();

    public void ClearSelection()
    {
        SelectedPokemon = null;
        SelectedBoxSlot = null;
        Refresh();
    }
}
