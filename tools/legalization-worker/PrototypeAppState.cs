namespace Pkmds.WorkerPrototype;

// The linked production engine reads only IsHaXEnabled. Implement its actual
// interface so additions to that contract still cause compilation failures here.
internal sealed class PrototypeAppState : IAppState
{
    public string CurrentLanguage { get; set; } = "en";
    public int CurrentLanguageId => (int)LanguageID.English;
    public SaveFile? SaveFile { get; set; }
    public string? SaveFileName { get; set; }
    public byte[]? OriginalSaveFileBytes { get; set; }
    public SaveArchiveContext? SaveArchiveContext { get; set; }
    public SaveFile? SaveFileB { get; set; }
    public string? SaveFileNameB { get; set; }
    public bool HasUnsavedChangesB { get; set; }
    public BoxEdit? BoxEdit => null;
    public BoxEdit? BoxEditB => null;
    public PKM? CopiedPokemon { get; set; }
    public int? SelectedBoxNumber { get; set; }
    public int? SelectedBoxSlotNumber { get; set; }
    public int? SelectedPartySlotNumber { get; set; }
    public int? SelectedBoxNumberB { get; set; }
    public int? SelectedBoxSlotNumberB { get; set; }
    public int? SelectedPartySlotNumberB { get; set; }
    public bool ShowProgressIndicator { get; set; }
    public string AppVersion => "worker-prototype";
    public DateTime? AppBuildDate => null;
    public bool SelectedSlotsAreValid => false;
    public bool IsHaXEnabled { get; set; }
    public SpriteStyle SpriteStyle { get; set; }
    public int? PinnedBoxNumber { get; set; }
    public bool ShowLegalIndicator { get; set; }
    public bool ShowFishyIndicator { get; set; }
    public bool ShowIllegalIndicator { get; set; }
    public bool HapticsEnabled { get; set; }
}
