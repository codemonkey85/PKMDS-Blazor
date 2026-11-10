using Bunit;
using MudBlazor;

namespace Pkmds.Tests;

public class AutocompleteSelectTests
{
    [Fact]
    public async Task ClearingForm_RestoresDefaultLabelAndReopensAllAlternatives()
    {
        var appState = new TestAppState { SaveFile = new SAV8SWSH() };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);
        var popovers = ctx.Render<MudPopoverProvider>();
        string[] forms = ["Normal", "Heat", "Wash"];
        var cut = ctx.Render<AutocompleteSelect<byte>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, forms.Length).Select(i => (byte)i))
            .Add(component => component.Value, (byte)1)
            .Add(component => component.ToStringFunc, value => forms[value]));

        cut.Find("button[aria-label='Clear']").Click();
        cut.Render();
        cut.Find("input").GetAttribute("value").Should().Be("Normal");
        cut.Find(".mud-input-control").MouseDown();
        popovers.WaitForAssertion(() => popovers.FindAll("[role='option']")
            .Select(option => option.TextContent.Trim()).Should().Equal(forms));

        popovers.FindAll("[role='option']")[1].Click();
        cut.WaitForAssertion(() => cut.Find("input").GetAttribute("value").Should().Be("Heat"));
        cut.Find("button[aria-label='Clear']").Click();
        cut.WaitForAssertion(() => cut.Find("input").GetAttribute("value").Should().Be("Normal"));
    }

    [Fact]
    public async Task ClearingOptionalFilter_RestoresVisibleNullSentinel()
    {
        var appState = new TestAppState { SaveFile = new SAV8SWSH() };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);
        var popovers = ctx.Render<MudPopoverProvider>();
        var cut = ctx.Render<AutocompleteSelect<int?>>(parameters => parameters
            .Add(component => component.Items, new int?[] { null, 1 })
            .Add(component => component.Value, (int?)1)
            .Add(component => component.ToStringFunc, value => value?.ToString() ?? "Any"));

        cut.Find("input").GetAttribute("value").Should().Be("1");
        cut.Find("button[aria-label='Clear']").Click();
        cut.Render();
        cut.Find("input").GetAttribute("value").Should().Be("Any");
        cut.Find(".mud-input-control").MouseDown();
        popovers.WaitForAssertion(() => popovers.FindAll("[role='option']")
            .Select(option => option.TextContent.Trim()).Should().Equal("Any", "1"));
        popovers.FindAll("[role='option']")[0].Click();

        cut.WaitForAssertion(() => cut.Find("input").GetAttribute("value").Should().Be("Any"));
    }

    [Fact]
    public async Task ReopeningSelectedForm_ShowsAlternativesAndTypingStillFilters()
    {
        var appState = new TestAppState { SaveFile = new SAV8SWSH() };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);
        var popovers = ctx.Render<MudPopoverProvider>();
        string[] forms = ["Normal", "Heat", "Wash", "Frost", "Fan", "Mow"];
        byte selected = 1;
        var cut = ctx.Render<AutocompleteSelect<byte>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, forms.Length).Select(i => (byte)i))
            .Add(component => component.Value, selected)
            .Add(component => component.ValueChanged, value => selected = value)
            .Add(component => component.DebounceInterval, 0)
            .Add(component => component.ToStringFunc, value => forms[value]));

        cut.Find("input").Focus();

        popovers.WaitForAssertion(() => popovers.FindAll("[role='option']")
            .Select(option => option.TextContent.Trim()).Should().Equal(forms));
        selected.Should().Be(1);

        cut.Find("input").Input("Mo");

        popovers.WaitForAssertion(() => popovers.FindAll("[role='option']")
            .Select(option => option.TextContent.Trim()).Should().Equal("Mow"));
        selected.Should().Be(1);
        popovers.Find("[role='option']").Click();
        cut.WaitForAssertion(() => selected.Should().Be(5));
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData(100, 10)]
    [InlineData(5, 5)]
    public async Task LongList_BoundsSuggestionsAroundSelectionAndCanFindLaterItems(int? maxItems, int expectedCount)
    {
        var appState = new TestAppState { SaveFile = new SAV8SWSH() };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);
        var popovers = ctx.Render<MudPopoverProvider>();
        var cut = ctx.Render<AutocompleteSelect<int>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(1, 100))
            .Add(component => component.MaxItems, maxItems)
            .Add(component => component.Value, 50)
            .Add(component => component.DebounceInterval, 0)
            .Add(component => component.ToStringFunc, value => $"Item {value}"));

        cut.Find("input").Focus();

        popovers.WaitForAssertion(() =>
        {
            var suggestions = popovers.FindAll("[role='option']");
            suggestions.Should().HaveCount(expectedCount);
            suggestions.Select(option => option.TextContent.Trim()).Should().Contain("Item 50");
        });
        cut.Find("input").Input("Item 99");
        popovers.WaitForAssertion(() => popovers.FindAll("[role='option']")
            .Select(option => option.TextContent.Trim()).Should().Equal("Item 99"));
    }

    [Fact]
    public void MaxItems_DefaultsToBoundedMudBlazorValue()
    {
        var component = new AutocompleteSelect<ComboItem>();

        component.MaxItems.Should().Be(10);
    }

    [Fact]
    public async Task ExactTextMatch_CommitsMatchingValueWhenEnabled()
    {
        var appState = new TestAppState { SaveFile = new SAV8SWSH() };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);

        var pokeBall = new ComboItem("Poké Ball", 4);
        var masterBall = new ComboItem("Master Ball", 1);
        ComboItem? selected = pokeBall;
        var cut = ctx.Render<AutocompleteSelect<ComboItem>>(parameters => parameters
            .Add(component => component.Items, [pokeBall, masterBall])
            .Add(component => component.Value, selected)
            .Add(component => component.ValueChanged, value => selected = value)
            .Add(component => component.SelectExactMatchOnTextChange, true)
            .Add(component => component.ToStringFunc, item => item?.Text));

        cut.Find("input").Input("Master Ball");

        selected.Should().Be(masterBall);
    }

    [Fact]
    public async Task PartialText_DoesNotReplaceCurrentValue()
    {
        var appState = new TestAppState { SaveFile = new SAV8SWSH() };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);

        var pokeBall = new ComboItem("Poké Ball", 4);
        var masterBall = new ComboItem("Master Ball", 1);
        ComboItem? selected = pokeBall;
        var cut = ctx.Render<AutocompleteSelect<ComboItem>>(parameters => parameters
            .Add(component => component.Items, [pokeBall, masterBall])
            .Add(component => component.Value, selected)
            .Add(component => component.ValueChanged, value => selected = value)
            .Add(component => component.SelectExactMatchOnTextChange, true)
            .Add(component => component.ToStringFunc, item => item?.Text));

        cut.Find("input").Input("Master");

        selected.Should().Be(pokeBall);
    }
}
