namespace Pkmds.Tests;

[Collection("Sequential")]
public class HaXAbilityEditorTests
{
    [Fact]
    public async Task UltraSunHaX_OffersAndAppliesAnAbilityOutsideTheSpeciesSlots()
    {
        var save = new SAV7USUM();
        LocalizeUtil.InitializeStrings(GameLanguage.DefaultLanguage, save);
        var state = new TestAppState { SaveFile = save, IsHaXEnabled = true };
        var refresh = new TestRefreshService();
        var service = new AppService(state, refresh, new LegalizationService(state));
        await using var ctx = BunitTestHelpers.CreateBunitContext(state, refresh, service);
        var popovers = ctx.Render<MudPopoverProvider>();
        var pokemon = new PK7 { Species = (ushort)Species.Charizard, Ability = (ushort)Ability.Blaze };
        var cut = ctx.Render<MainTab>(parameters => parameters.Add(component => component.Pokemon, pokemon));
        var ability = cut.FindComponents<BrowseAutocomplete<ComboItem>>()
            .Single(component => component.Instance.Label == "Ability (HaX — any)");

        ability.Find("input").Focus();
        ability.Find("input").Input("Wonder Guard");
        popovers.WaitForAssertion(() => popovers.FindAll("[role='option']")
            .Should().ContainSingle(option => option.TextContent.Trim() == "Wonder Guard"));
        await popovers.FindAll("[role='option']").Single(option => option.TextContent.Trim() == "Wonder Guard")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.WaitForAssertion(() => pokemon.Ability.Should().Be((ushort)Ability.WonderGuard));
    }
}
