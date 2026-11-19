namespace Pkmds.Tests;

/// <summary>
/// Tests for form/appearance editor logic (Alcremie, Furfrou, Minior, Pumpkaboo/Gourgeist, Vivillon).
/// </summary>
public class FormEditorTests
{
    [Theory]
    [InlineData(0, 0)] // Vanilla Cream + Strawberry Sweet
    [InlineData(4, 3)] // Lemon Cream + Star Sweet
    [InlineData(8, 6)] // Rainbow Swirl + Ribbon Sweet
    public void AlcremieDialog_ConfirmSetsFormAndFormArgument(byte cream, uint deco)
    {
        var pk = new PK8 { Species = (ushort)Species.Alcremie, Form = cream };
        if (pk is IFormArgument fa)
        {
            fa.FormArgument = deco;
        }

        pk.Form.Should().Be(cream);
        ((IFormArgument)pk).FormArgument.Should().Be(deco);
    }

    [Fact]
    public void AlcremieDialog_CreamNames_HasNineEntries() => AlcremieEditorDialog.CreamNames.Should().HaveCount(9);

    [Fact]
    public void AlcremieDialog_DecorationNames_HasSevenEntries() => AlcremieEditorDialog.DecorationNames.Should().HaveCount(7);

    [Theory]
    [InlineData(1, 3u)] // Heart trim, 3 days remaining
    [InlineData(9, 5u)] // Pharaoh trim, 5 days
    public void FurfrouDialog_ConfirmSetsFormAndDaysRemaining(byte form, uint days)
    {
        var pk = new PK6 { Species = (ushort)Species.Furfrou, Form = form };
        if (pk is IFormArgument fa)
        {
            fa.FormArgument = days;
        }

        pk.Form.Should().Be(form);
        pk.FormArgument.Should().Be(days);
    }

    [Fact]
    public void FurfrouDialog_NaturalForm_FormArgumentIsZero()
    {
        var pk = new PK6 { Species = (ushort)Species.Furfrou, Form = 0 };
        if (pk is IFormArgument fa)
        {
            fa.FormArgument = 0;
        }

        pk.Form.Should().Be(0);
        pk.FormArgument.Should().Be(0u);
    }

    [Theory]
    [InlineData(0)] // Red Meteor
    [InlineData(6)] // Violet Meteor
    [InlineData(7)] // Red Core
    [InlineData(13)] // Violet Core
    public void MiniorDialog_CoreColorIndex_MapsToCorrectFormRange(byte form)
    {
        var isMeteor = form < MiniorColorDialog.MiniorMeteorCount;
        var isCore = form is >= MiniorColorDialog.MiniorMeteorCount and < MiniorColorDialog.MiniorMeteorCount + MiniorColorDialog.MiniorCoreCount;

        (isMeteor || isCore).Should().BeTrue();

        var pk = new PK7 { Species = (ushort)Species.Minior, Form = form };
        pk.Form.Should().Be(form);
    }

    [Theory]
    [InlineData(0)] // Small
    [InlineData(1)] // Average
    [InlineData(2)] // Large
    [InlineData(3)] // Super
    public void PumpkabooDialog_SizeIndex_CanBeSetOnPKM(byte form)
    {
        var pk = new PK6 { Species = (ushort)Species.Pumpkaboo, Form = form };
        pk.Form.Should().Be(form);
    }

    [Theory]
    [InlineData((ushort)Species.Flabébé, 0)] // Red
    [InlineData((ushort)Species.Flabébé, 4)] // White
    [InlineData((ushort)Species.Floette, 0)] // Red
    [InlineData((ushort)Species.Floette, 4)] // White
    [InlineData((ushort)Species.Florges, 0)] // Red
    [InlineData((ushort)Species.Florges, 4)] // White
    public void FlowerColorDialog_FlowerFormIndex_CanBeSetOnPKM(ushort species, byte form)
    {
        var pk = new PK6 { Species = species, Form = form };
        pk.Form.Should().Be(form);
    }

    [Fact]
    public void FlowerColorDialog_FloetteEternalFlower_IsForm5()
    {
        // Eternal Flower Floette (AZ's Floette) is form 5 — not selectable in the game but
        // present in PKHeX for save editing purposes.
        var pk = new PK6 { Species = (ushort)Species.Floette, Form = 5 };
        pk.Form.Should().Be(5);
    }

    [Fact]
    public void FormLegality_InvalidVivillonForm_IsNotLegal()
    {
        var pk = new PK6
        {
            Species = (ushort)Species.Vivillon,
            Form = 99 // out of range
        };

        var la = new LegalityAnalysis(pk);

        // An out-of-range form must never be considered legal.
        la.Valid.Should().BeFalse();
    }
}
