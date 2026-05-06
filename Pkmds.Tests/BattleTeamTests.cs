namespace Pkmds.Tests;

/// <summary>
/// Tests for battle team service methods. Since no test save files ship with populated
/// battle teams, the tests programmatically place Pokémon into box slots and register
/// those slots into the save file's TeamSlots array.
/// </summary>
public class BattleTeamTests
{
    private const string TestFilesPath = "../../../TestFiles";

    [Fact]
    public void HasBattleTeams_Gen7Save_ReturnsTrue()
    {
        var (_, appState, refreshService, _) = BunitTestHelpers.LoadSave("moon.sav");
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        appService.HasBattleTeams().Should().BeTrue();
    }

    [Fact]
    public void HasBattleTeams_Gen5Save_ReturnsFalse()
    {
        var (_, appState, refreshService, _) = BunitTestHelpers.LoadSave("Black - Full Completion.sav");
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        appService.HasBattleTeams().Should().BeFalse();
    }

    [Fact]
    public void HasBattleBox_Gen5Save_ReturnsTrue()
    {
        var (_, appState, refreshService, _) = BunitTestHelpers.LoadSave("Black - Full Completion.sav");
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        appService.HasBattleBox().Should().BeTrue();
    }

    [Fact]
    public void HasBattleBox_Gen7Save_ReturnsFalse()
    {
        var (_, appState, refreshService, _) = BunitTestHelpers.LoadSave("moon.sav");
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        appService.HasBattleBox().Should().BeFalse();
    }

    [Fact]
    public void GetBattleTeamPokemon_Gen7_PopulatedTeam_ReturnsCorrectPokemon()
    {
        // Arrange — load a Gen 7 save and place Pokémon in box slots
        var (saveFile, appState, refreshService, _) = BunitTestHelpers.LoadSave("moon.sav");
        var sav7 = (SAV7)saveFile;
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        // Place 3 Pokémon in box 0, slots 0–2
        var species = new ushort[] { 25, 133, 150 }; // Pikachu, Eevee, Mewtwo
        for (var i = 0; i < species.Length; i++)
        {
            var pkm = sav7.BlankPKM;
            pkm.Species = species[i];
            sav7.SetBoxSlotAtIndex(pkm, 0, i);
        }

        // Register those slots to battle team 0
        for (var i = 0; i < 6; i++)
        {
            sav7.BoxLayout.TeamSlots[i] = i < species.Length
                ? i // flat index: box 0 * BoxSlotCount + slot i = i
                : -1;
        }

        sav7.BoxLayout.SaveBattleTeams();

        // Act
        var team = appService.GetBattleTeamPokemon(0);

        // Assert
        team.Should().HaveCount(3);
        team[0].Species.Should().Be(25);
        team[1].Species.Should().Be(133);
        team[2].Species.Should().Be(150);
    }

    [Fact]
    public void GetBattleTeamPokemon_Gen7_EmptyTeam_ReturnsEmpty()
    {
        var (saveFile, appState, refreshService, _) = BunitTestHelpers.LoadSave("moon.sav");
        var sav7 = (SAV7)saveFile;
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        // Clear team 1 (all slots = -1)
        for (var i = 6; i < 12; i++)
        {
            sav7.BoxLayout.TeamSlots[i] = -1;
        }

        sav7.BoxLayout.SaveBattleTeams();

        var team = appService.GetBattleTeamPokemon(1);
        team.Should().BeEmpty();
    }

    [Fact]
    public void IsBattleTeamLocked_Gen7_DefaultUnlocked()
    {
        var (_, appState, refreshService, _) = BunitTestHelpers.LoadSave("moon.sav");
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        for (var i = 0; i < 6; i++)
        {
            appService.IsBattleTeamLocked(i).Should().BeFalse($"team {i} should default to unlocked");
        }
    }

    [Fact]
    public void SetBattleTeamLocked_Gen7_TogglesLockState()
    {
        var (_, appState, refreshService, _) = BunitTestHelpers.LoadSave("moon.sav");
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        // Lock team 2
        appService.SetBattleTeamLocked(2, true);
        appService.IsBattleTeamLocked(2).Should().BeTrue();

        // Unlock team 2
        appService.SetBattleTeamLocked(2, false);
        appService.IsBattleTeamLocked(2).Should().BeFalse();
    }

    [Fact]
    public void ExportTeamAsShowdown_Gen7_PopulatedTeam_ReturnsShowdownText()
    {
        // Arrange
        var (saveFile, appState, refreshService, _) = BunitTestHelpers.LoadSave("moon.sav");
        var sav7 = (SAV7)saveFile;
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        // Place a Pikachu in box 0 slot 0
        var pkm = sav7.BlankPKM;
        pkm.Species = 25;
        sav7.SetBoxSlotAtIndex(pkm, 0, 0);

        // Register it as team 0 slot 0
        sav7.BoxLayout.TeamSlots[0] = 0;
        for (var i = 1; i < 6; i++)
        {
            sav7.BoxLayout.TeamSlots[i] = -1;
        }

        sav7.BoxLayout.SaveBattleTeams();

        // Act
        var team = appService.GetBattleTeamPokemon(0);
        var showdown = appService.ExportTeamAsShowdown(team);

        // Assert
        showdown.Should().NotBeNullOrEmpty();
        showdown.Should().Contain("Pikachu");
    }

    [Fact]
    public void GetBattleTeamPokemon_Gen7_MultipleTeams_AreIndependent()
    {
        // Arrange
        var (saveFile, appState, refreshService, _) = BunitTestHelpers.LoadSave("moon.sav");
        var sav7 = (SAV7)saveFile;
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        // Place Pokémon in box 0
        var pkm1 = sav7.BlankPKM;
        pkm1.Species = 6; // Charizard
        sav7.SetBoxSlotAtIndex(pkm1, 0, 0);

        var pkm2 = sav7.BlankPKM;
        pkm2.Species = 9; // Blastoise
        sav7.SetBoxSlotAtIndex(pkm2, 0, 1);

        // Team 0: Charizard only
        sav7.BoxLayout.TeamSlots[0] = 0;
        for (var i = 1; i < 6; i++)
        {
            sav7.BoxLayout.TeamSlots[i] = -1;
        }

        // Team 1: Blastoise only
        sav7.BoxLayout.TeamSlots[6] = 1;
        for (var i = 7; i < 12; i++)
        {
            sav7.BoxLayout.TeamSlots[i] = -1;
        }

        sav7.BoxLayout.SaveBattleTeams();

        // Act
        var team0 = appService.GetBattleTeamPokemon(0);
        var team1 = appService.GetBattleTeamPokemon(1);

        // Assert
        team0.Should().HaveCount(1);
        team0[0].Species.Should().Be(6);

        team1.Should().HaveCount(1);
        team1[0].Species.Should().Be(9);
    }

    [Fact]
    public void HasBattleTeams_Gen8Shield_ReturnsTrue()
    {
        var (_, appState, refreshService, _) = BunitTestHelpers.LoadSave("Test-Save-Shield.sav");
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        appService.HasBattleTeams().Should().BeTrue();
    }

    [Fact]
    public void GetBattleTeamPokemon_NoSaveFile_ReturnsEmpty()
    {
        var appState = new TestAppState { SaveFile = null };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        var team = appService.GetBattleTeamPokemon(0);
        team.Should().BeEmpty();
    }

    [Fact]
    public void GetBattleBoxPokemon_NoSaveFile_ReturnsEmpty()
    {
        var appState = new TestAppState { SaveFile = null };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        var result = appService.GetBattleBoxPokemon();
        result.Should().BeEmpty();
    }

    [Fact]
    public void HasRentalTeams_Gen7Save_ReturnsFalse()
    {
        var (_, appState, refreshService, _) = BunitTestHelpers.LoadSave("moon.sav");
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        appService.HasRentalTeams().Should().BeFalse();
    }

    [Fact]
    public void HasRentalTeams_Gen8Shield_ReturnsTrue()
    {
        var (_, appState, refreshService, _) = BunitTestHelpers.LoadSave("Test-Save-Shield.sav");
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        appService.HasRentalTeams().Should().BeTrue();
    }

    [Fact]
    public void GetBattleTeamName_Gen7_ReturnsDefaultName()
    {
        var (_, appState, refreshService, _) = BunitTestHelpers.LoadSave("moon.sav");
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        // Gen 7 doesn't have custom team names — falls through to default
        var name = appService.GetBattleTeamName(0);
        name.Should().Be("Team 1");
    }

    [Fact]
    public void ExportTeamAsShowdown_EmptyTeam_ReturnsEmpty()
    {
        var appState = new TestAppState();
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        var showdown = appService.ExportTeamAsShowdown([]);
        showdown.Should().BeEmpty();
    }

    [Fact]
    public void ClearBattleTeam_Gen7_RemovesTeamMembers()
    {
        var (saveFile, appState, refreshService, _) = BunitTestHelpers.LoadSave("moon.sav");
        var sav7 = (SAV7)saveFile;
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        var pkm = sav7.BlankPKM;
        pkm.Species = 25;
        sav7.SetBoxSlotAtIndex(pkm, 0, 0);
        sav7.BoxLayout.TeamSlots[0] = 0;
        for (var i = 1; i < 6; i++)
        {
            sav7.BoxLayout.TeamSlots[i] = -1;
        }

        sav7.BoxLayout.SaveBattleTeams();
        appService.GetBattleTeamPokemon(0).Should().HaveCount(1, "precondition: team has 1 member");

        appService.ClearBattleTeam(0);

        appService.GetBattleTeamPokemon(0).Should().BeEmpty();
    }

    [Fact]
    public void ClearBattleTeam_Gen7_DoesNotAffectOtherTeams()
    {
        var (saveFile, appState, refreshService, _) = BunitTestHelpers.LoadSave("moon.sav");
        var sav7 = (SAV7)saveFile;
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        var pkm1 = sav7.BlankPKM;
        pkm1.Species = 6;
        sav7.SetBoxSlotAtIndex(pkm1, 0, 0);

        var pkm2 = sav7.BlankPKM;
        pkm2.Species = 9;
        sav7.SetBoxSlotAtIndex(pkm2, 0, 1);

        sav7.BoxLayout.TeamSlots[0] = 0;
        for (var i = 1; i < 6; i++)
        {
            sav7.BoxLayout.TeamSlots[i] = -1;
        }

        sav7.BoxLayout.TeamSlots[6] = 1;
        for (var i = 7; i < 12; i++)
        {
            sav7.BoxLayout.TeamSlots[i] = -1;
        }

        sav7.BoxLayout.SaveBattleTeams();

        appService.ClearBattleTeam(0);

        appService.GetBattleTeamPokemon(0).Should().BeEmpty();
        appService.GetBattleTeamPokemon(1).Should().HaveCount(1);
        appService.GetBattleTeamPokemon(1)[0].Species.Should().Be(9);
    }

    [Fact]
    public void ClearAllBattleTeams_Gen7_ClearsAllTeamsAndUnlocks()
    {
        var (saveFile, appState, refreshService, _) = BunitTestHelpers.LoadSave("moon.sav");
        var sav7 = (SAV7)saveFile;
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        var pkm = sav7.BlankPKM;
        pkm.Species = 25;
        sav7.SetBoxSlotAtIndex(pkm, 0, 0);
        sav7.BoxLayout.TeamSlots[0] = 0;
        for (var i = 1; i < 6; i++)
        {
            sav7.BoxLayout.TeamSlots[i] = -1;
        }

        sav7.BoxLayout.SaveBattleTeams();
        appService.SetBattleTeamLocked(0, true);

        appService.ClearAllBattleTeams();

        for (var t = 0; t < 6; t++)
        {
            appService.GetBattleTeamPokemon(t).Should().BeEmpty($"team {t} should be cleared");
            appService.IsBattleTeamLocked(t).Should().BeFalse($"team {t} should be unlocked");
        }
    }

    [Fact]
    public void UnlockAllBattleTeams_Gen7_UnlocksAllTeams()
    {
        var (_, appState, refreshService, _) = BunitTestHelpers.LoadSave("moon.sav");
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        appService.SetBattleTeamLocked(0, true);
        appService.SetBattleTeamLocked(2, true);
        appService.SetBattleTeamLocked(4, true);

        appService.UnlockAllBattleTeams();

        for (var t = 0; t < 6; t++)
        {
            appService.IsBattleTeamLocked(t).Should().BeFalse($"team {t} should be unlocked");
        }
    }

    [Fact]
    public void HasBattleBox_Gen6XY_ReturnsTrue()
    {
        var (_, appState, refreshService, _) = BunitTestHelpers.LoadSave("x.sav");
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        appService.HasBattleBox().Should().BeTrue();
    }

    [Fact]
    public void SetBattleBoxLocked_Gen5_TogglesLockState()
    {
        var (_, appState, refreshService, _) = BunitTestHelpers.LoadSave("Black - Full Completion.sav");
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));

        appService.SetBattleBoxLocked(true);
        appService.IsBattleBoxLocked().Should().BeTrue();

        appService.SetBattleBoxLocked(false);
        appService.IsBattleBoxLocked().Should().BeFalse();
    }
}
