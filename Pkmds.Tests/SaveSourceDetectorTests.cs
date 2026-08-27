namespace Pkmds.Tests;

public class SaveSourceDetectorTests
{
    [Fact]
    public void Detect_LgpeSave_ReportsSwitchSource()
    {
        var sav = new SAV7b();

        var source = SaveSourceDetector.Detect(sav, "savedata.bin", isManicEmuArchive: false);

        source.Should().Be("Switch save (raw)");
    }
}
