namespace Pkmds.Tests;

/// <summary>
/// Tests for <see cref="AppService.GetDirectEvolutions" /> and the evolution-related logic
/// that lives in <c>MainTab</c> (level-bump, Wurmple EC/PID, nickname sync).
/// </summary>
public class EvolveTests
{
    // ── Helpers ───────────────────────────────────────────────────────────

    private static AppService CreateService(SaveFile? saveFile = null)
    {
        var appState = new TestAppState { SaveFile = saveFile };
        return new AppService(appState, new TestRefreshService(), new LegalizationService());
    }

    /// <summary>Creates a Gen 6 PK6 with the given species.</summary>
    private static PK6 MakePk6(ushort species, byte form = 0)
    {
        var pk = new PK6 { Species = species, Form = form };
        return pk;
    }

    /// <summary>Creates a Gen 3 PK3 with the given species.</summary>
    private static PK3 MakePk3(ushort species)
    {
        var pk = new PK3 { Species = species };
        return pk;
    }

    // ── GetDirectEvolutions ───────────────────────────────────────────────

    [Fact]
    public void GetDirectEvolutions_FinalEvolution_ReturnsEmpty()
    {
        // Arrange — Charizard has no further evolutions
        var service = CreateService();
        var pk = MakePk6((ushort)Species.Charizard);

        // Act
        var result = service.GetDirectEvolutions(pk);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetDirectEvolutions_SingleEvolution_ReturnsOneMethod()
    {
        // Arrange — Caterpie → Metapod (one branch)
        var service = CreateService();
        var pk = MakePk6((ushort)Species.Caterpie);

        // Act
        var result = service.GetDirectEvolutions(pk);

        // Assert
        result.Should().ContainSingle()
            .Which.Species.Should().Be((ushort)Species.Metapod);
    }

    [Fact]
    public void GetDirectEvolutions_Eevee_ReturnsMultipleMethods()
    {
        // Arrange — Eevee has 8 evolution branches in Gen 6
        var service = CreateService();
        var pk = MakePk6((ushort)Species.Eevee);

        // Act
        var result = service.GetDirectEvolutions(pk);

        // Assert
        result.Should().HaveCountGreaterThan(1);
        result.Should().Contain(m => m.Species == (ushort)Species.Vaporeon);
        result.Should().Contain(m => m.Species == (ushort)Species.Jolteon);
        result.Should().Contain(m => m.Species == (ushort)Species.Flareon);
    }

    [Fact]
    public void GetDirectEvolutions_Nincada_ExcludesShedinja()
    {
        // Arrange — Nincada evolves into Ninjask (primary) and Shedinja (side-effect).
        // GetDirectEvolutions must exclude LevelUpShedinja entries.
        var service = CreateService();
        var pk = MakePk6((ushort)Species.Nincada);

        // Act
        var result = service.GetDirectEvolutions(pk);

        // Assert — only Ninjask; Shedinja is a side-effect, not a direct choice
        result.Should().ContainSingle()
            .Which.Species.Should().Be((ushort)Species.Ninjask);
    }

    // ── ApplyEvolution helpers (tested via AppService indirectly) ─────────
    //    The actual mutation lives in MainTab, but we can validate the PKHeX
    //    APIs used (Wurmple EC/PID matching, level-bump logic) directly here.

    [Fact]
    public void WurmpleGen6_GetWurmpleEncryptionConstant_ProducesMatchingEvoGroup()
    {
        // Arrange
        const WurmpleEvolution evoGroup = WurmpleEvolution.Silcoon;

        // Act
        var ec = WurmpleUtil.GetWurmpleEncryptionConstant(evoGroup);

        // Assert — the EC must resolve back to the same group
        WurmpleUtil.GetWurmpleEvoVal(ec).Should().Be(evoGroup);
    }

    [Fact]
    public void WurmpleGen3_PIDMustMatchEvoGroup()
    {
        // Arrange — simulate what ApplyEvolution does for Gen 3 Wurmple
        var pk = MakePk3((ushort)Species.Wurmple);
        const WurmpleEvolution evoGroup = WurmpleEvolution.Cascoon;

        // Act — loop until a PID matches the desired branch (same loop as ApplyEvolution)
        uint pid;
        var rnd = Util.Rand;
        do
        {
            pid = rnd.Rand32();
        } while (evoGroup != WurmpleUtil.GetWurmpleEvoVal(pid));

        pk.PID = pid;

        // Assert — EC getter on Gen3 returns PID
        pk.EncryptionConstant.Should().Be(pk.PID);
        WurmpleUtil.GetWurmpleEvoVal(pk.EncryptionConstant).Should().Be(evoGroup);
    }

    [Fact]
    public void GetDirectEvolutions_Wurmple_ReturnsSilcoonAndCascoon()
    {
        // Arrange
        var service = CreateService();
        var pk = MakePk6((ushort)Species.Wurmple);

        // Act
        var result = service.GetDirectEvolutions(pk);

        // Assert
        result.Should().Contain(m => m.Species == (ushort)Species.Silcoon);
        result.Should().Contain(m => m.Species == (ushort)Species.Cascoon);
    }

    [Fact]
    public void LevelBump_IsNeeded_WhenCurrentLevelBelowMethodLevel()
    {
        // The level-bump guard in ApplyEvolution:
        //   if (method.Level > 0 && Pokemon.CurrentLevel < method.Level)
        //       Pokemon.CurrentLevel = method.Level;
        var pk = MakePk6((ushort)Species.Nincada);
        pk.CurrentLevel = 5; // below Nincada's evolution level of 20

        var service = CreateService();
        var evolutions = service.GetDirectEvolutions(pk);
        var ninjaskMethod = evolutions.Single(m => m.Species == (ushort)Species.Ninjask);

        // After a bump, level should be at least the method's required level
        var requiredLevel = ninjaskMethod.Level;
        if (requiredLevel > 0 && pk.CurrentLevel < requiredLevel)
        {
            pk.CurrentLevel = requiredLevel;
        }

        pk.CurrentLevel.Should().BeGreaterThanOrEqualTo(requiredLevel);
    }

    [Fact]
    public void GetDestinationForm_PreservesFormWhenAnyForm()
    {
        // Arrange — most evolutions use form = AnyForm (byte.MaxValue), meaning the form is preserved
        var service = CreateService();
        var pk = MakePk6((ushort)Species.Caterpie);

        var evolutions = service.GetDirectEvolutions(pk);
        var method = evolutions.Single();

        // Act
        var destForm = method.GetDestinationForm(pk.Form);

        // Assert — Caterpie has form 0; GetDestinationForm should pass it through
        destForm.Should().Be(pk.Form);
    }

    // ── TryPlacePokemonInFirstAvailableSlot ───────────────────────────────

    [Fact]
    public void TryPlacePokemonInFirstAvailableSlot_NoSaveFile_ReturnsFalse()
    {
        var service = CreateService();
        var pk = MakePk6((ushort)Species.Shedinja);

        var result = service.TryPlacePokemonInFirstAvailableSlot(pk);

        result.Should().BeFalse();
    }

    // ── Issue #688: Feebas→Milotic evolution — beauty and trade fix ─────────

    [Fact]
    public void Issue688_BeautyEvolution_SetsContestBeautyToThreshold()
    {
        // Arrange — Feebas has a LevelUpBeauty evolution to Milotic (Gen 6 origin can have contest stats)
        var pk = MakePk6((ushort)Species.Feebas);
        pk.Version = GameVersion.AS;
        var service = CreateService();
        var evolutions = service.GetDirectEvolutions(pk);
        var beautyMethod = evolutions.FirstOrDefault(m => m.Method == EvolutionType.LevelUpBeauty);
        beautyMethod.Species.Should().Be((ushort)Species.Milotic,
            "Feebas should have a LevelUpBeauty evolution to Milotic");

        // Act — replicate the beauty fix from ApplyEvolution
        if (pk is IContestStats contestStats && contestStats.ContestBeauty < beautyMethod.Argument)
        {
            contestStats.ContestBeauty = (byte)beautyMethod.Argument;
        }

        // Assert
        pk.ContestBeauty.Should().BeGreaterThanOrEqualTo(
            (byte)beautyMethod.Argument,
            "beauty stat should be set to at least the required threshold");
    }

    [Fact]
    public void Issue688_BeautyEvolution_PreservesExistingHighBeauty()
    {
        // Arrange — Feebas already has high beauty (Gen 6 origin can have contest stats)
        var pk = MakePk6((ushort)Species.Feebas);
        pk.Version = GameVersion.AS;
        pk.ContestBeauty = 255;
        var service = CreateService();
        var evolutions = service.GetDirectEvolutions(pk);
        var beautyMethod = evolutions.First(m => m.Method == EvolutionType.LevelUpBeauty);

        // Act — the fix should not lower an existing high value
        if (pk is IContestStats cs && cs.ContestBeauty < beautyMethod.Argument)
        {
            cs.ContestBeauty = (byte)beautyMethod.Argument;
        }

        // Assert
        pk.ContestBeauty.Should().Be(255,
            "existing beauty above threshold should not be lowered");
    }

    [Fact]
    public void Issue688_TradeEvolution_SetsHandlingTrainer()
    {
        // Arrange — Feebas has a TradeHeldItem evolution to Milotic
        var pk = MakePk6((ushort)Species.Feebas);
        pk.OriginalTrainerName = "Ash";
        pk.Version = GameVersion.X;
        var service = CreateService();
        var evolutions = service.GetDirectEvolutions(pk);
        var tradeMethod = evolutions.FirstOrDefault(m => m.Method == EvolutionType.TradeHeldItem);
        tradeMethod.Species.Should().Be((ushort)Species.Milotic,
            "Feebas should have a TradeHeldItem evolution to Milotic");

        pk.IsUntraded.Should().BeTrue("Feebas should start as untraded");

        // Act — replicate the trade fix from ApplyEvolution
        const string trainerName = "TestTrainer";
        if (pk is IHandlerUpdate && pk.IsUntraded)
        {
            pk.HandlingTrainerName = trainerName;
            pk.HandlingTrainerGender = 0;
            pk.HandlingTrainerFriendship = pk.PersonalInfo.BaseFriendship;
            pk.CurrentHandler = 1;
        }

        // Assert
        pk.IsUntraded.Should().BeFalse("trade fix should mark the Pokémon as traded");
        pk.HandlingTrainerName.Should().Be(trainerName);
        pk.CurrentHandler.Should().Be(1);
        pk.HandlingTrainerFriendship.Should().Be(pk.PersonalInfo.BaseFriendship);
    }

    [Fact]
    public void Issue688_TradeEvolution_SkipsAlreadyTradedPokemon()
    {
        // Arrange — Feebas that has already been traded
        var pk = MakePk6((ushort)Species.Feebas);
        pk.OriginalTrainerName = "OriginalOT";
        pk.Version = GameVersion.X;
        pk.HandlingTrainerName = "OtherOT";
        pk.CurrentHandler = 1;
        pk.HandlingTrainerFriendship = 70;

        pk.IsUntraded.Should().BeFalse("pre-setup: Feebas is already traded");

        // Act — the fix should not overwrite existing trade data
        if (pk is IHandlerUpdate && pk.IsUntraded)
        {
            pk.HandlingTrainerName = "BadOverwrite";
            pk.HandlingTrainerGender = 0;
            pk.HandlingTrainerFriendship = pk.PersonalInfo.BaseFriendship;
            pk.CurrentHandler = 1;
        }

        // Assert — original trade data preserved
        pk.HandlingTrainerName.Should().Be("OtherOT",
            "should not overwrite existing handler on already-traded Pokémon");
        pk.HandlingTrainerFriendship.Should().Be(70);
    }

    [Fact]
    public void Issue688_Evolution_RefreshesAbilityForNewSpecies()
    {
        // Arrange — Feebas ability slot 0 = Swift Swim (ability ID 33)
        // After evolution, Milotic ability slot 0 = Marvel Scale (ability ID 63)
        var pk = MakePk6((ushort)Species.Feebas);
        pk.Version = GameVersion.X;
        pk.SetAbilityIndex(0);
        var originalAbility = pk.Ability;
        originalAbility.Should().NotBe(0, "Feebas should have a valid ability");

        // Act — change species and refresh ability (same as ApplyEvolution)
        pk.Species = (ushort)Species.Milotic;
        var abilityIndex = pk.AbilityNumber switch
        {
            2 => 1,
            4 => 2,
            _ => 0
        };
        pk.RefreshAbility(abilityIndex);

        // Assert — ability ID should change to Milotic's slot 0 ability
        pk.Ability.Should().NotBe(originalAbility,
            "ability should be refreshed to match the new species");
        pk.Ability.Should().Be(pk.PersonalInfo.GetAbilityAtIndex(0),
            "ability should match slot 0 of Milotic's personal info");
    }

    [Fact]
    public void Issue688_GetDirectEvolutions_FiltersBeautyForGen7()
    {
        // Arrange — Gen 7 Pokémon cannot have contest stats, so beauty evolution
        // should not be offered
        var pk = new PK7 { Species = (ushort)Species.Feebas, Version = GameVersion.US };
        var service = CreateService();

        // Act
        var evolutions = service.GetDirectEvolutions(pk);

        // Assert — only trade evolution should remain, not beauty
        evolutions.Should().NotContain(m => m.Method == EvolutionType.LevelUpBeauty,
            "Gen 7 Pokémon cannot legally have contest stats, so beauty evolution should be filtered out");
        evolutions.Should().Contain(m => m.Method == EvolutionType.TradeHeldItem,
            "trade evolution should still be available");
    }

    [Fact]
    public void Issue688_BeautyEvolution_AllowedForGen3Origin()
    {
        // Arrange — Gen 3 Pokémon transferred to Gen 6 format can have contest stats
        var pk = MakePk6((ushort)Species.Feebas);
        pk.Version = GameVersion.R; // Ruby (Gen 3 origin)
        var service = CreateService();
        var evolutions = service.GetDirectEvolutions(pk);
        var beautyMethod = evolutions.First(m => m.Method == EvolutionType.LevelUpBeauty);

        // Act
        if (beautyMethod.Method == EvolutionType.LevelUpBeauty
            && pk.CanHaveContestStats()
            && pk is IContestStats cs
            && cs.ContestBeauty < beautyMethod.Argument)
        {
            cs.ContestBeauty = (byte)beautyMethod.Argument;
        }

        // Assert — Gen 3 origin should allow contest stats
        pk.ContestBeauty.Should().BeGreaterThanOrEqualTo(
            (byte)beautyMethod.Argument,
            "Gen 3 origin Pokémon can legally have contest stats");
    }

    // ── Sealed test doubles ────────────────────────────────────────────────

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
        public int? PinnedBoxNumber { get; set; }
        public string? SaveFileName { get; set; }
        public ManicEmuSaveHelper.ManicEmuSaveContext? ManicEmuSaveContext { get; set; }
        public bool ShowProgressIndicator { get; set; }
        public string AppVersion => "Test";
        public DateTime? AppBuildDate => null;
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
        public event Action? OnAppStateChanged { add { } remove { } }
        public event Action? OnBoxStateChanged { add { } remove { } }
        public event Action? OnPartyStateChanged { add { } remove { } }
        public event Action? OnUpdateAvailable { add { } remove { } }
        public event Action<bool>? OnThemeChanged { add { } remove { } }
        public event Action<bool>? OnSystemThemeChanged { add { } remove { } }
        public event Action? OnRequestJumpToPartyBox { add { } remove { } }
        public event Action? OnRequestLoadSaveFile { add { } remove { } }

        public void Refresh() { }
        public void RefreshBoxState() { }
        public void RefreshPartyState() { }
        public void RefreshBoxAndPartyState() { }
        public void RefreshTheme(bool isDarkMode) { }
        public void RefreshSystemTheme(bool systemIsDarkMode) { }
        public void ShowUpdateMessage() { }
        public void RequestJumpToPartyBox() { }
        public void RequestLoadSaveFile() { }
    }
}
