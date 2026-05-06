namespace Pkmds.Tests;

/// <summary>
/// Tests for box management SwapBoxes behavior.
/// </summary>
public class BoxManagementTests
{
    private const string TestFilesPath = "../../../TestFiles";

    [Fact]
    public void SwapBoxes_NoSaveFile_ReturnsFalse()
    {
        // Arrange
        var appState = new TestAppState { SaveFile = null };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act
        var result = appService.SwapBoxes(0, 1);

        // Assert
        result.Should().BeFalse();
        refreshService.RefreshBoxStateCount.Should().Be(0);
    }

    [Fact]
    public void SwapBoxes_ValidBoxes_SwapsAllSlots()
    {
        // Arrange
        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, "Black - Full Completion.sav"));
        SaveUtil.TryGetSaveFile(data, out var saveFile, "Black - Full Completion.sav").Should().BeTrue();

        var appState = new TestAppState { SaveFile = saveFile };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Snapshot all slots in box 0 and box 1 before the swap
        var slotCount = saveFile!.BoxSlotCount;
        var box0Before = Enumerable.Range(0, slotCount)
            .Select(s => saveFile.GetBoxSlotAtIndex(0, s).Species)
            .ToArray();
        var box1Before = Enumerable.Range(0, slotCount)
            .Select(s => saveFile.GetBoxSlotAtIndex(1, s).Species)
            .ToArray();

        // Act
        var result = appService.SwapBoxes(0, 1);

        // Assert
        result.Should().BeTrue();
        refreshService.RefreshBoxStateCount.Should().Be(1);

        for (var slot = 0; slot < slotCount; slot++)
        {
            saveFile.GetBoxSlotAtIndex(0, slot).Species.Should().Be(box1Before[slot],
                because: $"box 0 slot {slot} should contain what was in box 1 slot {slot}");
            saveFile.GetBoxSlotAtIndex(1, slot).Species.Should().Be(box0Before[slot],
                because: $"box 1 slot {slot} should contain what was in box 0 slot {slot}");
        }
    }

    [Fact]
    public void SwapBoxes_SameBox_LeavesBoxUnchanged()
    {
        // Arrange
        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, "Black - Full Completion.sav"));
        SaveUtil.TryGetSaveFile(data, out var saveFile, "Black - Full Completion.sav").Should().BeTrue();

        var appState = new TestAppState { SaveFile = saveFile };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        var slotCount = saveFile!.BoxSlotCount;
        var box0Before = Enumerable.Range(0, slotCount)
            .Select(s => saveFile.GetBoxSlotAtIndex(0, s).Species)
            .ToArray();

        // Act — swapping a box with itself should be a no-op
        appService.SwapBoxes(0, 0);

        // Assert — all slots remain unchanged regardless of what PKHeX returns
        for (var slot = 0; slot < slotCount; slot++)
        {
            saveFile.GetBoxSlotAtIndex(0, slot).Species.Should().Be(box0Before[slot],
                because: $"slot {slot} should be unchanged after swapping a box with itself");
        }
    }

    private class TestAppState : IAppState
    {
        public string CurrentLanguage { get; set; } = "en";
        public int CurrentLanguageId => 2;
        public SaveFile? SaveFile { get; set; }
        public BoxEdit? BoxEdit => null;
        public PKM? CopiedPokemon { get; set; }
        public int? SelectedBoxNumber { get; set; }
        public int? SelectedBoxSlotNumber { get; set; }
        public int? SelectedPartySlotNumber { get; set; }
        public bool ShowProgressIndicator { get; set; }
        public string AppVersion => "Test";
        public DateTime? AppBuildDate => null;
        public int? PinnedBoxNumber { get; set; }
        public string? SaveFileName { get; set; }
        public ManicEmuSaveHelper.ManicEmuSaveContext? ManicEmuSaveContext { get; set; }
        public bool SelectedSlotsAreValid => true;
        public bool IsHaXEnabled { get; set; }
        public SpriteStyle SpriteStyle { get; set; }
        public bool ShowLegalIndicator { get; set; } = true;
        public bool ShowFishyIndicator { get; set; } = true;
        public bool ShowIllegalIndicator { get; set; } = true;
        public SaveFile? SaveFileB { get; set; }
        public string? SaveFileNameB { get; set; }
        public bool HasUnsavedChangesB { get; set; }
        public BoxEdit? BoxEditB => null;
        public int? SelectedBoxNumberB { get; set; }
        public int? SelectedBoxSlotNumberB { get; set; }
        public int? SelectedPartySlotNumberB { get; set; }
        public bool HapticsEnabled { get; set; }
    }

    private class TestRefreshService : IRefreshService
    {
        private int RefreshCount { get; set; }
        public int RefreshBoxStateCount { get; private set; }
        private int RefreshPartyStateCount { get; set; }
        private int RefreshBoxAndPartyStateCount { get; set; }

        public void Refresh() => RefreshCount++;
        public void RefreshBoxState() => RefreshBoxStateCount++;
        public void RefreshPartyState() => RefreshPartyStateCount++;
        public void RefreshBoxAndPartyState() => RefreshBoxAndPartyStateCount++;

        public void RefreshTheme(bool isDarkMode)
        {
        }

        public void RefreshSystemTheme(bool systemIsDarkMode)
        {
        }

        public void ShowUpdateMessage()
        {
        }

        public void RequestJumpToPartyBox()
        {
        }

        public void RequestLoadSaveFile()
        {
        }

        public void LoadSaveFileFromDrop(IBrowserFile file)
        {
        }

#pragma warning disable CS0067
        public event Action? OnAppStateChanged;
        public event Action? OnBoxStateChanged;
        public event Action? OnPartyStateChanged;
        public event Action? OnUpdateAvailable;
        public event Action<bool>? OnThemeChanged;
        public event Action<bool>? OnSystemThemeChanged;
        public event Action? OnRequestJumpToPartyBox;
        public event Action? OnRequestLoadSaveFile;
        public event Action<IBrowserFile>? OnLoadSaveFileFromDrop;
#pragma warning restore CS0067
    }
}
