namespace Pkmds.Tests;

/// <summary>
/// Tests the Crystal-only GS Ball / Celebi event toggle exposed by the Trainer Info tab
/// (<see cref="SAV2.EnableGSBallMobileEvent"/>), including persistence across a save/reload.
/// </summary>
public class GsBallEventTests
{
    private const string TestFilesPath = "../../../TestFiles";
    private const string CrystalSaveFileName = "PM_CRYSTAL_BXTJ-0.sav";

    [Fact]
    public void EnableGSBallMobileEvent_FlipsFlagAndPersistsAfterReload()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, CrystalSaveFileName);
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, CrystalSaveFileName).Should().BeTrue();
        var crystal = saveFile.Should().BeOfType<SAV2>().Subject;
        crystal.Version.Should().Be(GameVersion.C);

        // Sanity check: the event starts disabled in the test fixture.
        crystal.IsEnabledGSBallMobileEvent.Should().BeFalse();

        // Act
        crystal.EnableGSBallMobileEvent();

        // Assert: the in-memory flag flips on.
        crystal.IsEnabledGSBallMobileEvent.Should().BeTrue();

        // Assert: the flag survives a write + reload, with checksums intact.
        var savedData = crystal.Write();
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, CrystalSaveFileName).Should().BeTrue();
        var reloadedCrystal = reloadedSave.Should().BeOfType<SAV2>().Subject;

        reloadedCrystal.IsEnabledGSBallMobileEvent.Should().BeTrue();
        reloadedCrystal.ChecksumsValid.Should().BeTrue();
    }
}
