namespace Pkmds.Tests;

public class LegalityUiTests
{
    [Theory]
    [InlineData(LegalityStatus.Legal, "Legal")]
    [InlineData(LegalityStatus.Fishy, "Legal with warnings")]
    [InlineData(LegalityStatus.Illegal, "Illegal")]
    public void GetStatusLabel_UsesUserFacingLegalityLanguage(LegalityStatus status, string expected) =>
        LegalityUi.GetStatusLabel(status).Should().Be(expected);

    [Fact]
    public void GetSeverityLabel_FishyResult_IsPresentedAsWarning() =>
        LegalityHelpers.GetSeverityLabel(PKHexSeverity.Fishy).Should().Be("Warning");

    [Fact]
    public void GetDisplayMessage_FishyResult_ReplacesTechnicalSeverityLabel()
    {
        var (saveFile, _, _, appService) = BunitTestHelpers.LoadSave("Black - Full Completion.sav");
        var analysis = appService.GetLegalityAnalysis(saveFile.GetPartySlotAtIndex(0));
        var result = CheckResult.Get(
            PKHexSeverity.Fishy,
            CheckIdentifier.EVs,
            LegalityCheckResultCode.EffortEXPIncreased);

        var message = LegalityUi.GetDisplayMessage(analysis, in result);

        message.Should().StartWith("Warning:");
        message.Should().Contain("EVs", "the underlying PKHeX finding must be preserved");
        message.Should().NotContain("Fishy");
    }
}
