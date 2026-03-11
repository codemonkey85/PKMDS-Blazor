using Pkmds.Rcl.Models;

namespace Pkmds.Tests;

/// <summary>
/// Tests for <see cref="AppService.SearchPokemon" /> covering the main filter criteria.
/// </summary>
public class AdvancedSearchTests
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

    /// <summary>
    /// Counts all party + box Pokémon with Species > 0 directly from the save file.
    /// </summary>
    private static int CountOccupiedSlots(SaveFile sav)
    {
        var count = 0;
        for (var i = 0; i < sav.PartyCount; i++)
        {
            if (sav.GetPartySlotAtIndex(i).Species > 0)
            {
                count++;
            }
        }

        for (var box = 0; box < sav.BoxCount; box++)
        {
            for (var slot = 0; slot < sav.BoxSlotCount; slot++)
            {
                if (sav.GetBoxSlotAtIndex(box, slot).Species > 0)
                {
                    count++;
                }
            }
        }

        return count;
    }

    // ── Tests ─────────────────────────────────────────────────────────────

    [Fact]
    public void SearchPokemon_EmptyFilter_ReturnsAllOccupiedSlots()
    {
        // Arrange
        var (service, saveFile) = CreateServiceFromFile("Black - Full Completion.sav");
        var expected = CountOccupiedSlots(saveFile);

        // Act
        var results = service.SearchPokemon(new AdvancedSearchFilter()).ToList();

        // Assert
        results.Should().HaveCount(expected);
    }

    [Fact]
    public void SearchPokemon_SpeciesFilter_ReturnsOnlyMatchingSpecies()
    {
        // Arrange
        var (service, saveFile) = CreateServiceFromFile("Black - Full Completion.sav");

        // Pick the first non-empty party slot's species.
        ushort targetSpecies = 0;
        for (var i = 0; i < saveFile.PartyCount; i++)
        {
            var pkm = saveFile.GetPartySlotAtIndex(i);
            if (pkm.Species > 0)
            {
                targetSpecies = pkm.Species;
                break;
            }
        }

        targetSpecies.Should().BeGreaterThan(0, "there should be at least one party Pokémon");

        // Act
        var filter = new AdvancedSearchFilter { Species = targetSpecies };
        var results = service.SearchPokemon(filter).ToList();

        // Assert
        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.Pokemon.Species.Should().Be(targetSpecies));
    }

    [Fact]
    public void SearchPokemon_ShinyFilter_ReturnsOnlyShinies()
    {
        // Arrange
        var (service, saveFile) = CreateServiceFromFile("Black - Full Completion.sav");

        // Place a hand-crafted shiny Pokémon into box 1, slot 1.
        // PID=0 with TID16=0, SID16=0 satisfies the Gen 5 shiny XOR < 8 condition.
        var shinyPk5 = new PK5
        {
            Species = (ushort)Species.Pikachu,
            TID16 = 0,
            SID16 = 0,
            PID = 0,
            CurrentLevel = 1
        };
        shinyPk5.RefreshChecksum();
        saveFile.SetBoxSlotAtIndex(shinyPk5, 0, 0);

        shinyPk5.IsShiny.Should().BeTrue("our hand-crafted Pokémon must be shiny for this test to be valid");

        // Act
        var results = service.SearchPokemon(new AdvancedSearchFilter { IsShiny = true }).ToList();

        // Assert
        results.Should().NotBeEmpty("the shiny Pokémon we added must appear in results");
        results.Should().AllSatisfy(r => r.Pokemon.IsShiny.Should().BeTrue());
    }

    [Fact]
    public void SearchPokemon_MoveFilter_AnyOf_MatchesCorrectly()
    {
        // Arrange
        var (service, saveFile) = CreateServiceFromFile("Black - Full Completion.sav");

        // Place a Pokémon that knows Tackle (move 33) in a known box slot.
        const ushort tackle = 33; // Move.Tackle
        var pk5 = new PK5 { Species = (ushort)Species.Rattata, Move1 = tackle, CurrentLevel = 5 };
        pk5.RefreshChecksum();
        saveFile.SetBoxSlotAtIndex(pk5, 1, 0);

        // Act — search for any Pokémon that knows Tackle
        var anyTackle = service
            .SearchPokemon(new AdvancedSearchFilter { AnyMoves = [tackle] })
            .ToList();

        // Search for a move that definitely no Pokémon knows (0 = no move, filtered by Species>0 guard)
        var filterNomove = service
            .SearchPokemon(new AdvancedSearchFilter { AllMoves = [tackle, 9999] })
            .ToList();

        // Assert
        anyTackle.Should().Contain(r => r.Pokemon.Move1 == tackle || r.Pokemon.Move2 == tackle
                                                                  || r.Pokemon.Move3 == tackle
                                                                  || r.Pokemon.Move4 == tackle);
        filterNomove.Should().BeEmpty("no Pokémon can know move ID 9999");
    }

    [Fact]
    public void SearchPokemon_LevelRange_FiltersCorrectly()
    {
        // Arrange
        var (service, saveFile) = CreateServiceFromFile("Black - Full Completion.sav");

        const byte minLevel = 50;
        const byte maxLevel = 60;

        // Act
        var results = service
            .SearchPokemon(new AdvancedSearchFilter { LevelMin = minLevel, LevelMax = maxLevel })
            .ToList();

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.Pokemon.CurrentLevel.Should().BeGreaterThanOrEqualTo(minLevel);
            r.Pokemon.CurrentLevel.Should().BeLessThanOrEqualTo(maxLevel);
        });
    }

    [Fact]
    public void SearchPokemon_LegalFilter_ReturnsMixedResults()
    {
        // Arrange
        var (service, saveFile) = CreateServiceFromFile("Black - Full Completion.sav");

        // Act
        var all = service.SearchPokemon(new AdvancedSearchFilter()).ToList();
        var legal = service.SearchPokemon(new AdvancedSearchFilter { IsLegal = true }).ToList();
        var illegal = service.SearchPokemon(new AdvancedSearchFilter { IsLegal = false }).ToList();

        // Assert — every slot must be either legal or illegal, never neither
        (legal.Count + illegal.Count).Should().Be(all.Count,
            "legal + illegal counts must equal total (every Pokémon is one or the other)");

        legal.Should().AllSatisfy(r =>
            new LegalityAnalysis(r.Pokemon).Valid.Should().BeTrue());

        illegal.Should().AllSatisfy(r =>
            new LegalityAnalysis(r.Pokemon).Valid.Should().BeFalse());
    }

    [Fact]
    public void SearchPokemon_OtNameFilter_IsCaseInsensitive()
    {
        // Arrange
        var (service, saveFile) = CreateServiceFromFile("Black - Full Completion.sav");

        // Find the OT name stored in the save file.
        var otName = saveFile.OT;
        otName.Should().NotBeNullOrEmpty("the save file must have an OT name");

        // Act — search with the OT name in different cases
        var upperResults = service
            .SearchPokemon(new AdvancedSearchFilter { OriginalTrainerName = otName.ToUpperInvariant() })
            .ToList();
        var lowerResults = service
            .SearchPokemon(new AdvancedSearchFilter { OriginalTrainerName = otName.ToLowerInvariant() })
            .ToList();

        // Assert — both must return the same count
        upperResults.Should().HaveCount(lowerResults.Count,
            "OT name search must be case-insensitive");
        upperResults.Should().NotBeEmpty(
            "there must be at least one Pokémon with the save's OT name");
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
        public DateTime? AppBuildDate { get; }
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
