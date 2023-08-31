namespace Pkmds.Rcl;

public interface IAppState
{
    string CurrentLanguage { get; set; }

    int CurrentLanguageId { get; }

    SaveFile? SaveFile { get; set; }

    int? SelectedBoxNumber { get; set; }

    int? SelectedBoxSlotNumber { get; set; }

    int? SelectedPartySlotNumber { get; set; }

    bool ShowProgressIndicator { get; set; }

    string FileDisplayName { get; set; }
}
