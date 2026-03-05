namespace Pkmds.Tests;

/// <summary>
/// Tests for loading various save file formats
/// </summary>
public class SaveFileLoadingTests
{
    private const string TestFilesPath = "../../../TestFiles";

    [Theory]
    [InlineData("POKEMON RED-0.sav")]
    [InlineData("POKEMON BLUE-0.sav")]
    [InlineData("POKEMON YELLOW-0.sav")]
    [InlineData("Pokemon - Silver Version (UE) [C][!].sav")]
    [InlineData("PM_CRYSTAL_BXTJ-0.sav")]
    [InlineData("POKEMON RUBY_AXVE-0.sav")]
    [InlineData("POKEMON SAPP_AXPE-0.sav")]
    [InlineData("POKEMON EMER_BPEE-0.sav")]
    [InlineData("POKEMON FIRE_BPRE-0.sav")]
    [InlineData("POKEMON LEAF_BPGE-0.sav")]
    [InlineData("Manaphy Pearl.sav")]
    [InlineData("Pokemon Platinum.sav")]
    [InlineData("Pokemon Heart Gold  (JP)old.sav")]
    [InlineData("Black - Full Completion.sav")]
    [InlineData("Test-Save-White-2.sav")]
    [InlineData("x.sav")]
    [InlineData("y.sav")]
    [InlineData("sun.sav")]
    [InlineData("moon.sav")]
    [InlineData("ultra sun.sav")]
    [InlineData("ultra moon.sav")]
    [InlineData("Lets-Go-Pikachu-All-Pokemon.bin")]
    [InlineData("Test-Save-Shield.sav")]
    [InlineData("bdsp.bin")]
    [InlineData("Test-Save-Scarlet.sav")]
    public void LoadSaveFile_ValidFile_LoadsSuccessfully(string fileName)
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, fileName);
        var data = File.ReadAllBytes(filePath);

        // Act
        var success = SaveUtil.TryGetSaveFile(data, out var saveFile, fileName);

        // Assert
        success.Should().BeTrue();
        saveFile.Should().NotBeNull();
        saveFile.State.Exportable.Should().BeTrue();
    }

    [Fact]
    public void LoadSaveFile_InvalidData_ReturnsFalse()
    {
        // Arrange
        var invalidData = new byte[] { 0x00, 0x01, 0x02 };

        // Act
        var success = SaveUtil.TryGetSaveFile(invalidData, out _, "invalid.sav");

        // Assert
        success.Should().BeFalse();
    }
}
