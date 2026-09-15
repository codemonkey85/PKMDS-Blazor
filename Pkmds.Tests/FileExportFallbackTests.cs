namespace Pkmds.Tests;

public sealed class FileExportFallbackTests
{
    [Fact]
    public void MobileExportUsesUserInitiatedFallbackAndFeatureDetectsPicker()
    {
        var script = RepoFileTestHelper.ReadAllText("Pkmds.Rcl", "wwwroot", "js", "fileSave.js");

        script.Should().Contain("return pkmdsIsIOS() || /Android/i.test(navigator.userAgent);");
        script.Should().Contain("if (!supportsFS || isIOS)");
        script.Should().NotContain("if (!supportsFS || /Android/i.test(navigator.userAgent) || isIOS)");
        script.Should().Contain("await pkmdsDownloadPreparedBlob(fallbackFileName, fallbackBlob, true);");
    }

    [Theory]
    [InlineData(null, "Unknown browser error.")]
    [InlineData("SecurityError:\nMust be handling a user gesture", "SecurityError: Must be handling a user gesture")]
    public void ExportErrorDetail_IsCompactAndActionable(string? message, string expected)
    {
        Pkmds.Rcl.Components.Layout.MainLayout.GetExportErrorDetail(message).Should().Be(expected);
    }
}
