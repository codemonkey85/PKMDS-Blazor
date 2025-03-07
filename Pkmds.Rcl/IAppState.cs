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

    static string? AppVersion { get; } // => Assembly.GetAssembly(typeof(Program))?.GetName().Version?.ToString();

    static string? PkhexVersion { get; } // => Assembly.GetAssembly(typeof(PKM))?.GetName().Version?.ToString();

    bool SelectedSlotsAreValid { get; }
}
