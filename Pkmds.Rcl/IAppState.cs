namespace Pkmds.Rcl;

public interface IAppState
{
    string CurrentLanguage { get; set; }

    int CurrentLanguageId { get; }

    SaveFile? SaveFile { get; set; }

    PKM? CopiedPokemon { get; set; }

    int? SelectedBoxNumber { get; set; }

    int? SelectedBoxSlotNumber { get; set; }

    int? SelectedPartySlotNumber { get; set; }

    bool ShowProgressIndicator { get; set; }

    string? AppVersion { get; }

    static string? PkhexVersion => Assembly.GetAssembly(typeof(PKM))?.GetName().Version?.ToString();

    bool SelectedSlotsAreValid { get; }
}
