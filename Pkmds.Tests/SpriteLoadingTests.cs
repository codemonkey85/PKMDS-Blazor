using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Pkmds.Rcl.Components.MainTabPages;

namespace Pkmds.Tests;

public class SpriteLoadingTests
{
    [Fact]
    public async Task SlotErrorRestoresFallbackAndRemovesPreviousSuccess()
    {
        var state = new TestAppState { IsHaXEnabled = true };
        var refresh = new TestRefreshService();
        var service = new AppService(state, refresh, new LegalizationService(state));
        await using var ctx = BunitTestHelpers.CreateBunitContext(state, refresh, service);
        var pokemon = new PK9 { Species = (ushort)Species.Pikachu, Gender = 0 };
        var url = ImageHelper.GetPokemonHighResSpriteUrl(pokemon, SpriteStyle.Home, GameVersion.Any, false)!;
        PokemonSlotComponent.MarkHighResLoaded(url);
        try
        {
            var cut = ctx.Render<PokemonSlotComponent>(p => p
                .Add(c => c.SlotNumber, 0)
                .Add(c => c.Pokemon, pokemon)
                .Add(c => c.OnSlotClick, EventCallback.Empty)
                .Add(c => c.GetClassFunction, () => string.Empty));

            cut.Find("object.pkm-sprite").GetAttribute("style").Should().Be("display:none");
            cut.Find("img.pkm-sprite-hires").GetAttribute("crossorigin").Should().Be("anonymous");
            AssertCorsPrecedesSource(cut.Find("img.pkm-sprite-hires").OuterHtml);
            await cut.Find("img.pkm-sprite-hires").TriggerEventAsync("onerror", EventArgs.Empty);

            cut.Find("object.pkm-sprite").GetAttribute("style").Should().NotBe("display:none");
            cut.Find("img.pkm-sprite-hires").ClassList.Should().NotContain("pkm-sprite-hires--loaded");
            PokemonSlotComponent.IsHighResLoaded(url).Should().BeFalse();
        }
        finally
        {
            PokemonSlotComponent.MarkHighResFailed(url);
        }
    }

    [Fact]
    public async Task TradeSlotSwitchingGamesWaitsForTheNewUrlAndHandlesUnavailableVersions()
    {
        var state = new TestAppState { IsHaXEnabled = true, SpriteStyle = SpriteStyle.Game };
        var refresh = new TestRefreshService();
        var service = new AppService(state, refresh, new LegalizationService(state));
        await using var ctx = BunitTestHelpers.CreateBunitContext(state, refresh, service);
        var pokemon = new PK9 { Species = (ushort)Species.Pikachu, Gender = 0 };
        var cut = ctx.Render<TradeSlot>(p => p
            .Add(c => c.Pokemon, pokemon)
            .Add(c => c.OwnerVersion, GameVersion.SL));
        var oldUrl = cut.Find("img.pkm-sprite-hires").GetAttribute("src")!;
        AssertCorsPrecedesSource(cut.Find("img.pkm-sprite-hires").OuterHtml);
        var newUrl = ImageHelper.GetPokemonGameSpriteUrl(pokemon, GameVersion.SW)!;
        PokemonSlotComponent.MarkHighResFailed(newUrl);
        try
        {
            await cut.Find("img.pkm-sprite-hires").TriggerEventAsync("onload", EventArgs.Empty);
            cut.Find("object.pkm-sprite").GetAttribute("style").Should().Be("display:none");

            cut.Render(p => p.Add(c => c.OwnerVersion, GameVersion.SW));
            cut.Find("img.pkm-sprite-hires").GetAttribute("src").Should().Be(newUrl);
            cut.Find("object.pkm-sprite").GetAttribute("style").Should().NotBe("display:none");
            await cut.Find("img.pkm-sprite-hires").TriggerEventAsync("onload", EventArgs.Empty);
            await cut.Find("img.pkm-sprite-hires").TriggerEventAsync("onerror", EventArgs.Empty);
            cut.Find("object.pkm-sprite").GetAttribute("style").Should().NotBe("display:none");

            cut.Render(p => p.Add(c => c.OwnerVersion, GameVersion.GO));
            cut.FindAll("img.pkm-sprite-hires").Should().BeEmpty();
            cut.Find("object.pkm-sprite").GetAttribute("style").Should().NotBe("display:none");
        }
        finally
        {
            PokemonSlotComponent.MarkHighResFailed(oldUrl);
            PokemonSlotComponent.MarkHighResFailed(newUrl);
        }
    }

    [Fact]
    public async Task EmbeddedSlotKeepsBundledSpriteWithoutExternalOverlay()
    {
        var state = new TestAppState { IsHaXEnabled = true };
        var refresh = new TestRefreshService();
        var service = new AppService(state, refresh, new LegalizationService(state));
        await using var ctx = BunitTestHelpers.CreateBunitContext(state, refresh, service);
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/?host=test");
        var cut = ctx.Render<PokemonSlotComponent>(p => p
            .Add(c => c.SlotNumber, 0)
            .Add(c => c.Pokemon, new PK9 { Species = (ushort)Species.Pikachu })
            .Add(c => c.OnSlotClick, EventCallback.Empty)
            .Add(c => c.GetClassFunction, () => string.Empty));

        cut.FindAll("img.pkm-sprite-hires").Should().BeEmpty();
        cut.Find("object.pkm-sprite").GetAttribute("data").Should().StartWith("_content/Pkmds.Rcl/sprites/");
    }

    private static void AssertCorsPrecedesSource(string imageMarkup)
    {
        // Safari can cache a no-CORS request before Blazor sets a later crossorigin attribute.
        imageMarkup.Should().Contain("crossorigin=\"anonymous\"");
        imageMarkup.IndexOf("crossorigin=", StringComparison.Ordinal).Should()
            .BeLessThan(imageMarkup.IndexOf("src=", StringComparison.Ordinal));
    }
}
