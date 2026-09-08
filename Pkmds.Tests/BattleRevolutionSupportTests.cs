namespace Pkmds.Tests;

public class BattleRevolutionSupportTests
{
    [Fact]
    public void PbrSave_LoadsWithProfilesAndRoundTripsActiveProfileEdits()
    {
        // Arrange
        var data = File.ReadAllBytes(Path.Combine("../../../TestFiles", "PbrSaveData"));

        // Act
        var loaded = SaveFileLoader.TryLoad(data, "PbrSaveData", out var saveFile, out var archiveContext);

        // Assert
        loaded.Should().BeTrue();
        archiveContext.Should().BeNull();
        var battleRevolution = saveFile.Should().BeOfType<SAV4BR>().Subject;
        battleRevolution.SaveNames.Should().HaveCount(4);
        battleRevolution.IsSupportedForEditing(out var reason).Should().BeTrue(reason);

        battleRevolution.CurrentSlot = 2;
        battleRevolution.CurrentOT = "TEST";
        var exported = battleRevolution.Write().ToArray();
        exported.Should().HaveCount(SaveUtil.SIZE_G4BR);

        SaveFileLoader.TryLoad(exported, "PbrSaveData", out var reloaded, out _).Should().BeTrue();
        var reloadedBattleRevolution = reloaded.Should().BeOfType<SAV4BR>().Subject;
        reloadedBattleRevolution.CurrentSlot = 2;
        reloadedBattleRevolution.CurrentOT.Should().Be("TEST");
    }

    [Fact]
    public void PbrSave_UsesProfileNameAndFriendlyGameNameWithoutGender()
    {
        // Arrange
        var (_, appState, _, appService) = BunitTestHelpers.LoadSave("PbrSaveData");
        var battleRevolution = appState.SaveFile.Should().BeOfType<SAV4BR>().Subject;

        // Act
        var display = SaveFileNameDisplay.SaveFileNameDisplayString(appState, appService);

        // Assert
        display.Should().StartWith(battleRevolution.CurrentOT);
        display.Should().Contain("Battle Revolution");
        display.Should().NotContain("PKHex");
        display.Should().NotContain(Constants.MaleGenderUnicode);
        display.Should().NotContain(Constants.FemaleGenderUnicode);
    }

    [Fact]
    public void PbrSave_DoesNotOfferBagTab()
    {
        var (saveFile, _, _, _) = BunitTestHelpers.LoadSave("PbrSaveData");

        SaveFileComponent.HasEditableInventory(saveFile).Should().BeFalse();
    }
}
