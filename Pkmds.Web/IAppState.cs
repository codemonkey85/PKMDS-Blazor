namespace Pkmds.Web;

public interface IAppState
{
    string CurrentLanguage { get; set; }

    int CurrentLanguageId { get; }

    SaveFile? SaveFile { get; set; }

    int? SelectedBoxNumber { get; set; }

    int? SelectedBoxSlotNumber { get; set; }

    int? SelectedPartySlotNumber { get; set; }

    bool ShowProgressIndicator { get; set; }

    static string? AppVersion => Assembly.GetAssembly(typeof(App))?.GetName().Version?.ToString();

    static string? PkhexVersion => Assembly.GetAssembly(typeof(PKM))?.GetName().Version?.ToString();
}
