namespace Pkmds.Rcl;

public interface IAppState
{
    public event Action? OnAppStateChanged;

    GameStrings? GameStrings { get; set; }

    string[] GenderForms { get; }

    SaveFile? SaveFile { get; set; }

    PKM? SelectedPokemon { get; set; }

    int? SelectedBoxSlot { get; set; }

    bool ShowProgressIndicator { get; set; }

    string FileDisplayName { get; set; }

    void Refresh();

    void ClearSelection();
}
