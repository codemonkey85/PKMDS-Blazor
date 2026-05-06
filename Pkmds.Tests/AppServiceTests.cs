namespace Pkmds.Tests;

/// <summary>
/// Tests for AppService functionality
/// </summary>
public class AppServiceTests
{
    private const string TestFilesPath = "../../../TestFiles";

    [Fact]
    public void GetCleanFileName_Gen5Pokemon_ReturnsCorrectFormat()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);
        FileUtil.TryGetPKM(data, out var pokemon, ".pk5").Should().BeTrue();

        var appState = new TestAppState();
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act
        var fileName = appService.GetCleanFileName(pokemon!);

        // Assert
        fileName.Should().Be("Lucario_B06DDFAD.pk5");
    }

    [Fact]
    public void GetStatModifierString_NeutralNature_ReturnsNeutral()
    {
        // Arrange
        var appState = new TestAppState();
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act
        var result = appService.GetStatModifierString(Nature.Hardy);

        // Assert
        result.Should().Be("(neutral)");
    }

    [Fact]
    public void GetStatModifierString_BoldNature_ReturnsCorrectModifier()
    {
        // Arrange
        var appState = new TestAppState();
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act - Bold: +Def, -Atk
        var result = appService.GetStatModifierString(Nature.Bold);

        // Assert
        result.Should().Contain("Def");
        result.Should().Contain("Atk");
        result.Should().Contain("↑");
        result.Should().Contain("↓");
    }

    [Fact]
    public void ClearSelection_ResetsAllSelections()
    {
        // Arrange
        var appState =
            new TestAppState { SelectedBoxNumber = 1, SelectedBoxSlotNumber = 5, SelectedPartySlotNumber = 2 };
        var refreshService = new TestRefreshService();
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);
        FileUtil.TryGetPKM(data, out var pokemon, ".pk5").Should().BeTrue();

        var appService = new AppService(appState, refreshService, new LegalizationService()) { EditFormPokemon = pokemon };

        // Act
        appService.ClearSelection();

        // Assert
        appState.SelectedBoxNumber.Should().BeNull();
        appState.SelectedBoxSlotNumber.Should().BeNull();
        appState.SelectedPartySlotNumber.Should().BeNull();
        appService.EditFormPokemon.Should().BeNull();
        refreshService.RefreshCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetSelectedPokemonSlot_PartySlot_ReturnsPartyType()
    {
        // Arrange
        var appState = new TestAppState { SelectedPartySlotNumber = 0 };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act
        var result = appService.GetSelectedPokemonSlot(out var partySlot, out var boxNumber, out var boxSlot);

        // Assert
        result.Should().Be(SelectedPokemonType.Party);
        partySlot.Should().Be(0);
        boxNumber.Should().Be(-1);
        boxSlot.Should().Be(-1);
    }

    [Fact]
    public void ExportPokemonAsShowdown_ValidPokemon_ReturnsShowdownFormat()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);
        FileUtil.TryGetPKM(data, out var pokemon, ".pk5").Should().BeTrue();

        var appState = new TestAppState();
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act
        var showdown = appService.ExportPokemonAsShowdown(pokemon);

        // Assert
        showdown.Should().NotBeNullOrEmpty();
        showdown.Should().Contain("Lucario");
    }

    [Fact]
    public void ExportPokemonAsShowdown_NullPokemon_ReturnsEmpty()
    {
        // Arrange
        var appState = new TestAppState();
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act
        var showdown = appService.ExportPokemonAsShowdown(null);

        // Assert
        showdown.Should().BeEmpty();
    }

    [Fact]
    public void ExportPartyAsShowdown_LetsGoPikachu_ReturnsShowdownFormat()
    {
        // Arrange - Load Let's Go Pikachu save file
        var filePath = Path.Combine(TestFilesPath, "Lets-Go-Pikachu-All-Pokemon.bin");
        var data = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(data, out var saveFile, "Lets-Go-Pikachu-All-Pokemon.bin").Should().BeTrue();

        var appState = new TestAppState { SaveFile = saveFile };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act
        var showdown = appService.ExportPartyAsShowdown();

        // Assert
        showdown.Should().NotBeNullOrEmpty("Let's Go save files should export party to Showdown format");
    }

    [Fact]
    public void ExportPartyAsShowdown_NoSaveFile_ReturnsEmpty()
    {
        // Arrange
        var appState = new TestAppState { SaveFile = null };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act
        var showdown = appService.ExportPartyAsShowdown();

        // Assert
        showdown.Should().BeEmpty();
    }

    [Fact]
    public void SetSelectedBoxPokemon_NullPokemon_AppliesTemplateWithSpeciesZero()
    {
        // Arrange - HandleNullOrEmptyPokemon applies EntityTemplates.TemplateFields when
        // EditFormPokemon is null or has Species == 0, so blank slots get trainer data defaults.
        var filePath = Path.Combine(TestFilesPath, "Black - Full Completion.sav");
        var data = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(data, out var saveFile, "Black - Full Completion.sav").Should().BeTrue();

        var appState = new TestAppState { SaveFile = saveFile };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act
        appService.SetSelectedBoxPokemon(null, 0, 0);

        // Assert: blank slot should produce a non-null PKM with Species=0 and OT from save
        appService.EditFormPokemon.Should().NotBeNull();
        appService.EditFormPokemon!.Species.Should().Be(0);
        appService.EditFormPokemon.OriginalTrainerName.Should().Be(saveFile!.OT);
    }

    [Fact]
    public void SetSelectedBoxPokemon_SpeciesZeroPokemon_AppliesTemplateWithSpeciesZero()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Black - Full Completion.sav");
        var data = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(data, out var saveFile, "Black - Full Completion.sav").Should().BeTrue();

        var appState = new TestAppState { SaveFile = saveFile };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // A PKM with Species=0 represents an empty slot; the template should be applied.
        var emptySlot = saveFile!.BlankPKM;

        // Act
        appService.SetSelectedBoxPokemon(emptySlot, 0, 0);

        // Assert
        appService.EditFormPokemon.Should().NotBeNull();
        appService.EditFormPokemon!.Species.Should().Be(0);
        appService.EditFormPokemon.OriginalTrainerName.Should().Be(saveFile.OT);
    }

    [Fact]
    public void GetConsoleRegionComboItems_ReturnsExpectedRegions()
    {
        // Arrange
        var appState = new TestAppState();
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act
        var result = appService.GetConsoleRegionComboItems();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(item => item.Text.Should().NotBeNullOrEmpty());
        // Verify the expected 3DS hardware regions are present by name
        // Note: combo item Values are raw byte values (0=Japan, 1=NA, 2=EU, 4=China, 5=Korea, 6=Taiwan),
        // which do not correspond to Region3DSIndex enum values (which include a None=0 entry).
        result.Should().Contain(i => i.Text.Contains("Japan"));
        result.Should().Contain(i => i.Text.Contains("America") || i.Text.Contains("NA"));
        result.Should().Contain(i => i.Text.Contains("Europe") || i.Text.Contains("EU"));
    }

    [Fact]
    public void TrySelectFirstEmptyBoxSlot_NoSaveFile_ReturnsFalse()
    {
        // Arrange
        var appState = new TestAppState { SaveFile = null };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act
        var result = appService.TrySelectFirstEmptyBoxSlot();

        // Assert
        result.Should().BeFalse();
        appState.SelectedBoxNumber.Should().BeNull();
        appState.SelectedBoxSlotNumber.Should().BeNull();
    }

    [Fact]
    public void TrySelectFirstEmptyBoxSlot_AllSlotsFull_ReturnsFalse()
    {
        // Arrange
        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, "Black - Full Completion.sav"));
        SaveUtil.TryGetSaveFile(data, out var saveFile, "Black - Full Completion.sav").Should().BeTrue();

        // Fill every slot so TrySelectFirstEmptyBoxSlot has nowhere to land
        for (var box = 0; box < saveFile!.BoxCount; box++)
        {
            for (var slot = 0; slot < saveFile.BoxSlotCount; slot++)
            {
                var pkm = saveFile.BlankPKM;
                pkm.Species = 1;
                saveFile.SetBoxSlotAtIndex(pkm, box, slot);
            }
        }

        var appState = new TestAppState { SaveFile = saveFile };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act
        var result = appService.TrySelectFirstEmptyBoxSlot();

        // Assert
        result.Should().BeFalse();
        appState.SelectedBoxNumber.Should().BeNull();
        appState.SelectedBoxSlotNumber.Should().BeNull();
    }

    [Fact]
    public void TrySelectFirstEmptyBoxSlot_HasEmptySlot_SelectsFirstEmptyAndReturnsTrue()
    {
        // Arrange
        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, "Black - Full Completion.sav"));
        SaveUtil.TryGetSaveFile(data, out var saveFile, "Black - Full Completion.sav").Should().BeTrue();

        // Fill all slots, then clear box 0 slot 0 — making it the known first empty slot
        for (var box = 0; box < saveFile!.BoxCount; box++)
        {
            for (var slot = 0; slot < saveFile.BoxSlotCount; slot++)
            {
                var pkm = saveFile.BlankPKM;
                pkm.Species = 1;
                saveFile.SetBoxSlotAtIndex(pkm, box, slot);
            }
        }

        saveFile.SetBoxSlotAtIndex(saveFile.BlankPKM, 0, 0);

        var appState = new TestAppState { SaveFile = saveFile };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act
        var result = appService.TrySelectFirstEmptyBoxSlot();

        // Assert
        result.Should().BeTrue();
        appState.SelectedBoxNumber.Should().Be(0);
        appState.SelectedBoxSlotNumber.Should().Be(0);
    }

    [Fact]
    public void TrySelectFirstEmptyBoxSlot_LetsGoSave_UsesFlatIndex()
    {
        // Arrange — SAV7b (Let's Go) uses a flat index across unified storage; SelectedBoxNumber
        // is never set and SelectedBoxSlotNumber is the flat index (box * BoxSlotCount + slot).
        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, "Lets-Go-Pikachu-All-Pokemon.bin"));
        SaveUtil.TryGetSaveFile(data, out var saveFile, "Lets-Go-Pikachu-All-Pokemon.bin").Should().BeTrue();
        saveFile.Should().BeOfType<SAV7b>();

        // Fill all slots, then clear box 0 slot 0 — flat index 0 is the known first empty slot
        for (var box = 0; box < saveFile.BoxCount; box++)
        {
            for (var slot = 0; slot < saveFile.BoxSlotCount; slot++)
            {
                var pkm = saveFile.BlankPKM;
                pkm.Species = 1;
                saveFile.SetBoxSlotAtIndex(pkm, box, slot);
            }
        }

        saveFile.SetBoxSlotAtIndex(saveFile.BlankPKM, 0, 0);

        var appState = new TestAppState { SaveFile = saveFile };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService());

        // Act
        var result = appService.TrySelectFirstEmptyBoxSlot();

        // Assert
        result.Should().BeTrue();
        // SetSelectedLetsGoPokemon uses flat index and does not set SelectedBoxNumber
        appState.SelectedBoxSlotNumber.Should().Be(0); // box 0 * BoxSlotCount + slot 0
        appState.SelectedBoxNumber.Should().BeNull();
    }

    [Fact]
    public void HasWonderCardSlots_NoSaveLoaded_ReturnsFalse()
    {
        var appState = new TestAppState { SaveFile = null };
        var appService = new AppService(appState, new TestRefreshService(), new LegalizationService());

        appService.HasWonderCardSlots().Should().BeFalse();
        appService.GetWonderCardSlots().Should().BeEmpty();
    }

    [Fact]
    public void HasWonderCardSlots_RubySave_ReturnsFalseBecauseRSLacksWonderCardStorage()
    {
        // SAV3RS uses SaveBlock3LargeRS which does NOT implement ISaveBlock3LargeExpansion —
        // RS has no wonder card slot, so the viewer should hide the tab on Ruby/Sapphire saves.
        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, "POKEMON RUBY_AXVE-0.sav"));
        SaveUtil.TryGetSaveFile(data, out var saveFile, "POKEMON RUBY_AXVE-0.sav").Should().BeTrue();

        var appState = new TestAppState { SaveFile = saveFile };
        var appService = new AppService(appState, new TestRefreshService(), new LegalizationService());

        appService.HasWonderCardSlots().Should().BeFalse();
        appService.GetWonderCardSlots().Should().BeEmpty();
    }

    [Fact]
    public void GetWonderCardSlots_EmeraldWithImportedWC3_ReturnsSinglePopulatedSlot()
    {
        // The bundled JPN Emerald save has the Old Sea Map WC3 imported (see PR #810 / issue #423).
        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, "Pocket Monsters - Emerald (Japan).sav"));
        SaveUtil.TryGetSaveFile(data, out var saveFile, "Pocket Monsters - Emerald (Japan).sav").Should().BeTrue();
        saveFile.Should().BeOfType<SAV3E>();

        var appState = new TestAppState { SaveFile = saveFile };
        var appService = new AppService(appState, new TestRefreshService(), new LegalizationService());

        appService.HasWonderCardSlots().Should().BeTrue();

        var slots = appService.GetWonderCardSlots();
        slots.Should().HaveCount(1);
        var slot = slots[0];
        slot.Index.Should().Be(0);
        slot.CardType.Should().Be(nameof(WonderCard3));
        slot.IsEmpty.Should().BeFalse();
        slot.Title.Should().NotBe("(empty)");
        slot.Title.Should().NotBeNullOrWhiteSpace();
        slot.CardId.Should().NotBeNull();
        // Gen 3 has no IMysteryGiftFlags-style bitmap, so Received is always null here.
        slot.Received.Should().BeNull();
    }

    [Fact]
    public void GetWonderCardSlots_BlackSave_ReturnsTwelveSlotsAllEmpty()
    {
        // Black/White's MysteryBlock5 exposes 12 PGF slots via IMysteryGiftStorageProvider.
        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, "Black - Full Completion.sav"));
        SaveUtil.TryGetSaveFile(data, out var saveFile, "Black - Full Completion.sav").Should().BeTrue();

        var appState = new TestAppState { SaveFile = saveFile };
        var appService = new AppService(appState, new TestRefreshService(), new LegalizationService());

        appService.HasWonderCardSlots().Should().BeTrue();

        var slots = appService.GetWonderCardSlots();
        slots.Should().HaveCount(12);
        slots.Select(s => s.Index).Should().Equal(Enumerable.Range(0, 12));
        slots.Should().OnlyContain(s => s.CardType == nameof(PGF));
        // The bundled save has no imported gifts; every slot should report empty + no Received flag.
        slots.Should().OnlyContain(s => s.IsEmpty);
        slots.Should().OnlyContain(s => s.Received == null || s.Received == false);
    }

    [Fact]
    public void GetWonderCardSlots_LetsGoSave_ReturnsTenWR7Slots()
    {
        // SAV7b stores 10 WR7 records via WB7Records (the storage block is named after WB7 but
        // actually casts to WR7 — see WB7Records.cs:50).
        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, "Lets-Go-Pikachu-All-Pokemon.bin"));
        SaveUtil.TryGetSaveFile(data, out var saveFile, "Lets-Go-Pikachu-All-Pokemon.bin").Should().BeTrue();
        saveFile.Should().BeOfType<SAV7b>();

        var appState = new TestAppState { SaveFile = saveFile };
        var appService = new AppService(appState, new TestRefreshService(), new LegalizationService());

        appService.HasWonderCardSlots().Should().BeTrue();

        var slots = appService.GetWonderCardSlots();
        slots.Should().HaveCount(10);
        slots.Should().OnlyContain(s => s.CardType == nameof(WR7));
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
        public int RefreshCount { get; private set; }
        private int RefreshBoxStateCount { get; set; }
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

#pragma warning disable CS0067 // Event is never used - these are required by interface but not needed in test mock
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
