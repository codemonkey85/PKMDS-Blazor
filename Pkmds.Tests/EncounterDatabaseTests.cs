using Pkmds.Rcl.Models;

namespace Pkmds.Tests;

/// <summary>
/// Tests for <see cref="AppService.SearchEncounters" /> and <see cref="AppService.GeneratePokemonFromEncounter" />.
/// </summary>
public class EncounterDatabaseTests
{
    private const string TestFilesPath = "../../../TestFiles";

    // ── Helpers ───────────────────────────────────────────────────────────

    private static (AppService service, SaveFile saveFile) CreateServiceFromFile(string fileName)
    {
        var filePath = Path.Combine(TestFilesPath, fileName);
        var data = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(data, out var saveFile, fileName).Should().BeTrue();
        var appState = new TestAppState { SaveFile = saveFile };
        var service = new AppService(appState, new TestRefreshService());
        return (service, saveFile!);
    }

    // ── SearchEncounters ──────────────────────────────────────────────────

    [Fact]
    public void SearchEncounters_NullSpecies_ReturnsEmpty()
    {
        // Arrange
        var (service, _) = CreateServiceFromFile("POKEMON RED-0.sav");

        // Act — no species specified
        var results = service.SearchEncounters(new EncounterSearchFilter()).ToList();

        // Assert
        results.Should().BeEmpty("species is required to perform an encounter search");
    }

    [Fact]
    public void SearchEncounters_ValidSpecies_ReturnsAtLeastOneEncounter()
    {
        // Arrange — Pikachu (025) exists as a wild encounter in Gen 1 Red/Blue
        var (service, _) = CreateServiceFromFile("POKEMON RED-0.sav");

        // Act
        var results = service
            .SearchEncounters(new EncounterSearchFilter { Species = (ushort)Species.Pikachu })
            .ToList();

        // Assert
        results.Should().NotBeEmpty("Pikachu has at least one wild encounter in Gen 1");
        results.Should().AllSatisfy(r => r.Encounter.Species.Should().Be((ushort)Species.Pikachu));
    }

    [Fact]
    public void SearchEncounters_LevelFilter_ExcludesOutOfRangeEncounters()
    {
        // Arrange
        var (service, _) = CreateServiceFromFile("POKEMON RED-0.sav");
        const byte minLevel = 20;
        const byte maxLevel = 30;

        // Act
        var results = service
            .SearchEncounters(new EncounterSearchFilter { Species = (ushort)Species.Pikachu, LevelMin = minLevel, LevelMax = maxLevel })
            .ToList();

        // Assert — every returned encounter must overlap [minLevel, maxLevel]
        results.Should().AllSatisfy(r =>
        {
            r.Encounter.LevelMax.Should().BeGreaterThanOrEqualTo(minLevel,
                "encounter max level must be at least the filter min");
            r.Encounter.LevelMin.Should().BeLessThanOrEqualTo(maxLevel,
                "encounter min level must not exceed the filter max");
        });
    }

    [Fact]
    public void SearchEncounters_ShinyLockedFilter_ReturnsOnlyMatchingEncounters()
    {
        // Arrange — use a Gen 8+ save which has shiny-locked legendaries
        var (service, _) = CreateServiceFromFile("Test-Save-Shield.sav");

        // Act
        var lockedResults = service
            .SearchEncounters(new EncounterSearchFilter { Species = (ushort)Species.Zacian, IsShinyLocked = true })
            .ToList();

        var unlockedResults = service
            .SearchEncounters(new EncounterSearchFilter { Species = (ushort)Species.Zacian, IsShinyLocked = false })
            .ToList();

        // Assert
        lockedResults.Should().AllSatisfy(r => r.IsShinyLocked.Should().BeTrue());
        unlockedResults.Should().AllSatisfy(r => r.IsShinyLocked.Should().BeFalse());

        // The two sets must be disjoint (no encounter can be both locked and unlocked)
        var lockedEncs = lockedResults.Select(r => r.Encounter).ToHashSet(ReferenceEqualityComparer.Instance);
        unlockedResults.Should().AllSatisfy(r => lockedEncs.Should().NotContain(r.Encounter));
    }

    [Fact]
    public void SearchEncounters_TypeGroupFilter_ReturnsOnlyMatchingType()
    {
        // Arrange — Red has wild encounters for many Pokémon
        var (service, _) = CreateServiceFromFile("POKEMON RED-0.sav");

        // Act
        var wildOnly = service
            .SearchEncounters(new EncounterSearchFilter { Species = (ushort)Species.Pidgey, EncounterGroup = EncounterTypeGroup.Slot })
            .ToList();

        // Assert
        wildOnly.Should().NotBeEmpty("Pidgey has wild encounters in Gen 1");
        wildOnly.Should().AllSatisfy(r => r.EncounterTypeName.Should().Be("Wild"));
    }

    [Fact]
    public void SearchEncounters_VersionFilter_ReturnsOnlySpecifiedVersionEncounters()
    {
        // Arrange
        var (service, _) = CreateServiceFromFile("POKEMON RED-0.sav");

        // Act
        var redOnly = service
            .SearchEncounters(new EncounterSearchFilter { Species = (ushort)Species.Pikachu, Version = GameVersion.RD })
            .ToList();

        // Assert — every result must be from Red
        redOnly.Should().AllSatisfy(r =>
            r.Encounter.Version.Should().Be(GameVersion.RD));
    }

    // ── GeneratePokemonFromEncounter ──────────────────────────────────────

    [Fact]
    public void GeneratePokemonFromEncounter_WildEncounter_ReturnsLegalPokemon()
    {
        // Arrange — find a wild Pikachu encounter in Red
        var (service, _) = CreateServiceFromFile("POKEMON RED-0.sav");
        var wildEncounter = service
            .SearchEncounters(new EncounterSearchFilter { Species = (ushort)Species.Pikachu, EncounterGroup = EncounterTypeGroup.Slot })
            .FirstOrDefault();

        wildEncounter.Should().NotBeNull("there must be at least one wild Pikachu encounter in Gen 1");

        // Act
        var pkm = service.GeneratePokemonFromEncounter(wildEncounter!.Encounter);

        // Assert
        pkm.Should().NotBeNull("a legal Pokémon should be generated from a valid encounter");
        new LegalityAnalysis(pkm!).Valid.Should().BeTrue("the generated Pokémon must be legal");
    }

    [Fact]
    public void GeneratePokemonFromEncounter_StaticEncounter_ReturnsLegalPokemon()
    {
        // Arrange — find a static encounter (e.g., starter or gift Pokémon) in a Gen 5 save
        var (service, _) = CreateServiceFromFile("Black - Full Completion.sav");
        var staticEncounter = service
            .SearchEncounters(new EncounterSearchFilter { Species = (ushort)Species.Victini, EncounterGroup = EncounterTypeGroup.Static })
            .FirstOrDefault();

        staticEncounter.Should().NotBeNull("Victini has a static encounter in Gen 5 Black");

        // Act
        var pkm = service.GeneratePokemonFromEncounter(staticEncounter!.Encounter);

        // Assert
        pkm.Should().NotBeNull();
        new LegalityAnalysis(pkm!).Valid.Should().BeTrue();
    }

    [Fact]
    public void SearchEncounters_ResultsHaveExpectedFields()
    {
        // Arrange
        var (service, _) = CreateServiceFromFile("POKEMON RED-0.sav");

        // Act
        var results = service
            .SearchEncounters(new EncounterSearchFilter { Species = (ushort)Species.Pikachu })
            .ToList();

        // Assert — all required result fields must be populated
        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r =>
        {
            r.Encounter.Should().NotBeNull();
            r.SpeciesName.Should().NotBeNullOrWhiteSpace();
            r.GameName.Should().NotBeNullOrWhiteSpace();
            r.EncounterTypeName.Should().NotBeNullOrWhiteSpace();
            r.LevelRange.Should().NotBeNullOrWhiteSpace();
        });
    }

    // ── Mock helpers ──────────────────────────────────────────────────────

    private sealed class TestAppState : IAppState
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
        public bool SelectedSlotsAreValid => true;
        public bool IsHaXEnabled { get; set; }
    }

    private sealed class TestRefreshService : IRefreshService
    {
        public void Refresh() { }
        public void RefreshBoxState() { }
        public void RefreshPartyState() { }
        public void RefreshBoxAndPartyState() { }
        public void RefreshTheme(bool isDarkMode) { }
        public void ShowUpdateMessage() { }

#pragma warning disable CS0067
        public event Action? OnAppStateChanged;
        public event Action? OnBoxStateChanged;
        public event Action? OnPartyStateChanged;
        public event Action? OnUpdateAvailable;
        public event Action<bool>? OnThemeChanged;
#pragma warning restore CS0067
    }
}
