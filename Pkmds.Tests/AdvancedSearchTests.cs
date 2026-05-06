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
        var service = new AppService(appState, new TestRefreshService(), new LegalizationService());
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
            if (pkm.Species <= 0)
            {
                continue;
            }

            targetSpecies = pkm.Species;
            break;
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
        var (service, _) = CreateServiceFromFile("Black - Full Completion.sav");

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
        var (service, _) = CreateServiceFromFile("Black - Full Completion.sav");

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
    public void SearchPokemon_TypeFilter_IsOrderAgnostic()
    {
        // Arrange — Charizard is Fire (9) / Flying (2).
        var (service, saveFile) = CreateServiceFromFile("Black - Full Completion.sav");

        var charizard = new PK5 { Species = (ushort)Species.Charizard, CurrentLevel = 50 };
        charizard.RefreshChecksum();
        saveFile.SetBoxSlotAtIndex(charizard, 2, 0);

        const int flying = 2;
        const int fire = 9;

        // Act — Flying as Type1 should still surface Charizard (Flying is its secondary type).
        var byFlying = service.SearchPokemon(new AdvancedSearchFilter { Type1 = flying }).ToList();

        // Fire + Flying together should match regardless of slot order.
        var byBothOrderA = service
            .SearchPokemon(new AdvancedSearchFilter { Type1 = fire, Type2 = flying }).ToList();
        var byBothOrderB = service
            .SearchPokemon(new AdvancedSearchFilter { Type1 = flying, Type2 = fire }).ToList();

        // Assert
        byFlying.Should().Contain(r => r.Pokemon.Species == (ushort)Species.Charizard);
        byFlying.Should().AllSatisfy(r =>
        {
            var info = r.Pokemon.PersonalInfo;
            (info.Type1 == flying || info.Type2 == flying).Should().BeTrue();
        });

        byBothOrderA.Should().Contain(r => r.Pokemon.Species == (ushort)Species.Charizard);
        byBothOrderB.Should().Contain(r => r.Pokemon.Species == (ushort)Species.Charizard);
    }

    [Fact]
    public void SearchPokemon_FormFilter_MatchesExactForm()
    {
        // Arrange
        var (service, saveFile) = CreateServiceFromFile("Black - Full Completion.sav");

        // Place a Deerling with form 2 (Autumn).
        const byte autumnForm = 2;
        var deerling = new PK5
        {
            Species = (ushort)Species.Deerling,
            Form = autumnForm,
            CurrentLevel = 10,
        };
        deerling.RefreshChecksum();
        saveFile.SetBoxSlotAtIndex(deerling, 3, 0);

        // Act
        var results = service.SearchPokemon(new AdvancedSearchFilter
        {
            Species = (ushort)Species.Deerling,
            Form = autumnForm,
        }).ToList();

        // Assert
        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r =>
        {
            r.Pokemon.Species.Should().Be((ushort)Species.Deerling);
            r.Pokemon.Form.Should().Be(autumnForm);
        });
    }

    [Fact]
    public void SearchPokemon_MetDateRange_InclusiveBoundary()
    {
        // Arrange
        var (service, saveFile) = CreateServiceFromFile("Black - Full Completion.sav");

        // Pin a Pokémon with a known met date.
        var target = new DateOnly(2020, 6, 15);
        var pk5 = new PK5
        {
            Species = (ushort)Species.Rattata,
            CurrentLevel = 5,
            MetYear = (byte)(target.Year - 2000),
            MetMonth = (byte)target.Month,
            MetDay = (byte)target.Day,
        };
        pk5.RefreshChecksum();
        saveFile.SetBoxSlotAtIndex(pk5, 4, 0);

        // Act — inclusive range exactly on the target date
        var inclusive = service.SearchPokemon(new AdvancedSearchFilter
        {
            MetDateMin = target,
            MetDateMax = target,
        }).ToList();

        // Day before the target: target should be excluded
        var excludedAfter = service.SearchPokemon(new AdvancedSearchFilter
        {
            MetDateMax = target.AddDays(-1),
        }).ToList();

        // Assert
        inclusive.Should().Contain(r =>
            r.Pokemon.Species == (ushort)Species.Rattata && r.Pokemon.MetDate == target);
        excludedAfter.Should().NotContain(r =>
            r.Pokemon.Species == (ushort)Species.Rattata && r.Pokemon.MetDate == target);
    }

    [Fact]
    public void SearchPokemon_GigantamaxAndDynamaxFilters_MatchSwShFlags()
    {
        // Need a Gen 8 save since IGigantamax/IDynamaxLevel are G8PKM-only.
        var (service, saveFile) = CreateServiceFromFile("Test-Save-Shield.sav");

        var gmaxHighLevel = saveFile.BlankPKM.Clone();
        gmaxHighLevel.Species = (ushort)Species.Pikachu;
        gmaxHighLevel.CurrentLevel = 50;
        ((IGigantamax)gmaxHighLevel).CanGigantamax = true;
        ((IDynamaxLevel)gmaxHighLevel).DynamaxLevel = 10;
        gmaxHighLevel.RefreshChecksum();
        saveFile.SetBoxSlotAtIndex(gmaxHighLevel, 0, 0);

        var plainLowLevel = saveFile.BlankPKM.Clone();
        plainLowLevel.Species = (ushort)Species.Bulbasaur;
        plainLowLevel.CurrentLevel = 5;
        ((IGigantamax)plainLowLevel).CanGigantamax = false;
        ((IDynamaxLevel)plainLowLevel).DynamaxLevel = 2;
        plainLowLevel.RefreshChecksum();
        saveFile.SetBoxSlotAtIndex(plainLowLevel, 0, 1);

        var gmaxOnly = service.SearchPokemon(new AdvancedSearchFilter { CanGigantamax = true }).ToList();
        var maxedLevel = service.SearchPokemon(new AdvancedSearchFilter { DynamaxLevelMin = 10 }).ToList();

        gmaxOnly.Should().AllSatisfy(r =>
            ((IGigantamax)r.Pokemon).CanGigantamax.Should().BeTrue());
        gmaxOnly.Should().Contain(r => r.Pokemon.Species == (ushort)Species.Pikachu);
        gmaxOnly.Should().NotContain(r => r.Pokemon.Species == (ushort)Species.Bulbasaur);

        maxedLevel.Should().AllSatisfy(r =>
            ((IDynamaxLevel)r.Pokemon).DynamaxLevel.Should().BeGreaterThanOrEqualTo((byte)10));
        maxedLevel.Should().Contain(r => r.Pokemon.Species == (ushort)Species.Pikachu);
    }

    [Fact]
    public void SearchPokemon_MarkingsFilter_MatchesAllRequiredShapes()
    {
        var (service, saveFile) = CreateServiceFromFile("Black - Full Completion.sav");

        // Hand-craft a PK5 with Circle + Heart markings set (indices 0 and 3).
        var marked = new PK5 { Species = (ushort)Species.Zubat, CurrentLevel = 5 };
        ((IAppliedMarkings<bool>)marked).SetMarking(0, true); // Circle
        ((IAppliedMarkings<bool>)marked).SetMarking(3, true); // Heart
        marked.RefreshChecksum();
        saveFile.SetBoxSlotAtIndex(marked, 6, 0);

        // A Pokémon with only Circle (missing Heart) must be excluded.
        var partial = new PK5 { Species = (ushort)Species.Golbat, CurrentLevel = 5 };
        ((IAppliedMarkings<bool>)partial).SetMarking(0, true);
        partial.RefreshChecksum();
        saveFile.SetBoxSlotAtIndex(partial, 6, 1);

        var results = service.SearchPokemon(new AdvancedSearchFilter { RequiredMarkings = [0, 3] }).ToList();

        results.Should().Contain(r => r.Pokemon.Species == (ushort)Species.Zubat);
        results.Should().NotContain(r => r.Pokemon.Species == (ushort)Species.Golbat);
        results.Should().AllSatisfy(r =>
        {
            r.Pokemon.GetMarking(0).Should().NotBe(0);
            r.Pokemon.GetMarking(3).Should().NotBe(0);
        });
    }

    [Fact]
    public void SearchPokemon_PokerusFilter_DistinguishesStates()
    {
        // Arrange
        var (service, saveFile) = CreateServiceFromFile("Black - Full Completion.sav");

        var infected = new PK5 { Species = (ushort)Species.Bidoof, CurrentLevel = 5, PokerusStrain = 1, PokerusDays = 3 };
        infected.RefreshChecksum();
        saveFile.SetBoxSlotAtIndex(infected, 5, 0);

        var cured = new PK5 { Species = (ushort)Species.Starly, CurrentLevel = 5, PokerusStrain = 1, PokerusDays = 0 };
        cured.RefreshChecksum();
        saveFile.SetBoxSlotAtIndex(cured, 5, 1);

        // Act
        var infectedResults = service.SearchPokemon(new AdvancedSearchFilter { PokerusState = 1 }).ToList();
        var curedResults = service.SearchPokemon(new AdvancedSearchFilter { PokerusState = 2 }).ToList();

        // Assert
        infectedResults.Should().Contain(r => r.Pokemon.Species == (ushort)Species.Bidoof);
        infectedResults.Should().AllSatisfy(r => r.Pokemon.IsPokerusInfected.Should().BeTrue());
        infectedResults.Should().AllSatisfy(r => r.Pokemon.IsPokerusCured.Should().BeFalse());

        curedResults.Should().Contain(r => r.Pokemon.Species == (ushort)Species.Starly);
        curedResults.Should().AllSatisfy(r => r.Pokemon.IsPokerusCured.Should().BeTrue());
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

    private sealed class TestRefreshService : IRefreshService
    {
        public void Refresh()
        {
        }

        public void RefreshBoxState()
        {
        }

        public void RefreshPartyState()
        {
        }

        public void RefreshBoxAndPartyState()
        {
        }

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

#pragma warning disable CS0067
        public event Action? OnAppStateChanged;
        public event Action? OnBoxStateChanged;
        public event Action? OnPartyStateChanged;
        public event Action? OnUpdateAvailable;
        public event Action<bool>? OnThemeChanged;
        public event Action<bool>? OnSystemThemeChanged;
        public event Action? OnRequestJumpToPartyBox;
        public event Action? OnRequestLoadSaveFile;
#pragma warning restore CS0067
    }
}
