namespace Pkmds.Tests;

public sealed class FileExportFallbackTests
{
    [Fact]
    public void MobileExportUsesUserInitiatedFallbackAndFeatureDetectsPicker()
    {
        var script = RepoFileTestHelper.ReadAllText("Pkmds.Rcl", "wwwroot", "js", "fileSave.js");

        script.Should().Contain("return pkmdsIsIOS() || /Android/i.test(navigator.userAgent);");
        script.Should().Contain("window.pkmdsSupportsSaveFilePicker = function ()");
        script.Should().Contain("if (!supportsFS)");
        script.Should().NotContain("if (!supportsFS || /Android/i.test(navigator.userAgent) || isIOS)");
        script.Should().Contain("await pkmdsDownloadPreparedBlob(fallbackFileName, fallbackBlob, true);");
    }

    [Fact]
    public void UserTapFallbackPreservesCancellationSemantics()
    {
        var script = RepoFileTestHelper.ReadAllText("Pkmds.Rcl", "wwwroot", "js", "fileSave.js");

        script.Should().Contain("reject(new DOMException('The user aborted a request.', 'AbortError'));");
        script.Should().Contain("cancel.addEventListener('click', () => finish(false));");
        script.Should().Contain("setTimeout(() => finish(true), 400)");
        script.Should().Contain("if (pkmdsIsAbortError(fallbackError))");
    }

    [Fact]
    public void ExportersUseSavePickerSpecificSupportProbe()
    {
        string[][] paths =
        [
            ["Components", "Layout", "MainLayout.razor.cs"],
            ["Components", "Dialogs", "BankExportDialog.razor.cs"],
            ["Components", "Dialogs", "BulkExportDialog.razor.cs"],
            ["Components", "MainTabPages", "PokemonBankTab.razor.cs"],
            ["Components", "MainTabPages", "TradeTab.razor.cs"]
        ];

        foreach (var path in paths)
        {
            var source = RepoFileTestHelper.ReadAllText(["Pkmds.Rcl", .. path]);

            source.Should().Contain("pkmdsSupportsSaveFilePicker");
            source.Should().NotContain("FileSystemAccessService.IsSupportedAsync()");
        }
    }

    [Theory]
    [InlineData(null, "Unknown browser error.")]
    [InlineData("SecurityError:\nMust be handling a user gesture", "SecurityError: Must be handling a user gesture")]
    public void ExportErrorDetail_IsCompactAndActionable(string? message, string expected)
    {
        Pkmds.Rcl.Components.Layout.MainLayout.GetExportErrorDetail(message).Should().Be(expected);
    }
}
