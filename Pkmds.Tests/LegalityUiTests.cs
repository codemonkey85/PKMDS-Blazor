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
        LegalityHelpers.GetSeverityLabel(PKHeX.Core.Severity.Fishy).Should().Be("Warning");

    [Fact]
    public void GetDisplayMessage_FishyResult_ReplacesTechnicalSeverityLabel()
    {
        var (saveFile, _, _, appService) = BunitTestHelpers.LoadSave("Black - Full Completion.sav");
        var analysis = appService.GetLegalityAnalysis(saveFile.GetPartySlotAtIndex(0));
        var result = CheckResult.Get(
            PKHeX.Core.Severity.Fishy,
            CheckIdentifier.EVs,
            LegalityCheckResultCode.EffortEXPIncreased);

        var message = LegalityUi.GetDisplayMessage(analysis, in result);

        message.Should().Be("Warning: All EVs are zero, but leveled above Met Level.");
        message.Should().NotContain("Fishy");
    }
}
