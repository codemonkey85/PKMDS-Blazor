namespace Pkmds.Rcl;

public interface IAppState
{
    public event Action? OnAppStateChanged;

    int CurrentLanguageId { get; set; }

    Task<IEnumerable<ushort>> SearchPokemonNames(EntityContext generationContext, string searchString);

    string ConvertSpeciesToName(ushort species);

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
