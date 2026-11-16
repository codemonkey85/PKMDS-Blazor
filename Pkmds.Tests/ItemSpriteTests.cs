namespace Pkmds.Tests;

public sealed class ItemSpriteTests
{
    [Theory]
    [InlineData(29, "bi/bitem_33.png")] // Moomoo Milk
    [InlineData(68, "bi/bitem_50.png")] // Rare Candy
    [InlineData(200, "bi/bitem_234.png")] // Leftovers
    [InlineData(141, "bi/bitem_157.png")] // Lum Berry
    [InlineData(289, "bi/bitem_tm.png")] // TM01
    [InlineData(328, "bi/bitem_tm.png")] // TM40
    [InlineData(52, "bi/bitem_unk.png")] // Unused item
    [InlineData(121, "bi/bitem_unk.png")] // Orange Mail
    [InlineData(128, "bi/bitem_unk.png")] // Shadow Mail
    [InlineData(259, "bi/bitem_unk.png")] // Mach Bike: no transferable counterpart
    [InlineData(-1, "bi/bitem_unk.png")]
    [InlineData(65565, "bi/bitem_unk.png")] // Must not wrap to Moomoo Milk
    public void Gen3ItemsResolveToBundledSprites(int item, string expected)
    {
        var path = SpritePaths.GetItemSprite(item, EntityContext.Gen3);

        path.Should().Be(expected);
        RepoFileTestHelper.ReadAllBytes("Pkmds.Rcl", "wwwroot", "sprites", path)
            .Should().NotBeEmpty();
        ImageHelper.GetItemSpriteFilename(item, EntityContext.Gen3)
            .Should().Be($"_content/Pkmds.Rcl/sprites/{expected}");
    }
}
