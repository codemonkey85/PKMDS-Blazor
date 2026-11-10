using Bunit;
using MudBlazor;

namespace Pkmds.Tests;

public class AutocompleteSelectTests
{
    [Fact]
    public Task KeyboardClearingForm_RestoresNormalAfterBlur() =>
        AssertKeyboardClearRestoresLabelAsync<byte>(1, 0, [0, 1, 2],
            value => new[] { "Normal", "Heat", "Wash" }[value], "Normal");

    [Fact]
    public Task KeyboardClearingNature_RestoresHardyAfterBlur() =>
        AssertKeyboardClearRestoresLabelAsync(Nature.Adamant, Nature.Hardy, [Nature.Hardy, Nature.Adamant],
            value => value.ToString(), "Hardy");

    [Fact]
    public Task KeyboardClearingOptionalFilter_RestoresAnyAfterBlur() =>
        AssertKeyboardClearRestoresLabelAsync<int?>(1, null, [null, 1, 2],
            value => value?.ToString() ?? "Any", "Any");

    [Fact]
    public Task KeyboardClearingForm_RestoresNormalWhenCommittedWithTab() =>
        AssertKeyboardClearRestoresLabelAsync<byte>(1, 0, [0, 1, 2],
            value => new[] { "Normal", "Heat", "Wash" }[value], "Normal", commitWithTab: true);

    private static async Task AssertKeyboardClearRestoresLabelAsync<T>(T initialValue, T resetValue,
        T[] items, Func<T?, string?> toString, string expectedLabel, bool commitWithTab = false)
    {
        var appState = new TestAppState { SaveFile = new SAV8SWSH() };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);
        var popovers = ctx.Render<MudPopoverProvider>();
        var selected = initialValue;
        var changes = 0;
        var cut = ctx.Render<AutocompleteSelect<T>>(parameters => parameters
            .Add(component => component.Items, items)
            .Add(component => component.Value, initialValue)
            .Add(component => component.ValueChanged, value => { selected = value!; changes++; })
            .Add(component => component.DebounceInterval, 0)
            .Add(component => component.ToStringFunc, toString));

        cut.Find("input").Focus();
        // Ctrl+A/Backspace produces an empty input event without clicking Clear.
        cut.Find("input").Input(string.Empty);
        cut.Find("input").GetAttribute("value").Should().BeNullOrEmpty();
        selected.Should().Be(resetValue);
        if (commitWithTab)
        {
            cut.Find("input").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Tab" });
        }
        else
        {
            cut.Find("input").Blur();
        }

        cut.WaitForAssertion(() => cut.Find("input").GetAttribute("value").Should().Be(expectedLabel));
        changes.Should().Be(1, "restoring the label must not commit the reset value twice");
        cut.Find("input").Focus();
        popovers.WaitForAssertion(() => popovers.FindAll("[role='option']").Should().HaveCount(items.Length));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Wa")]
    public async Task KeyboardClearing_AllowsContinuedTypingAndSuggestionClickAfterBlur(string query)
    {
        var appState = new TestAppState { SaveFile = new SAV8SWSH() };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);
        var popovers = ctx.Render<MudPopoverProvider>();
        string[] forms = ["Normal", "Heat", "Wash"];
        var cut = ctx.Render<AutocompleteSelect<byte>>(parameters => parameters
            .Add(component => component.Items, new byte[] { 0, 1, 2 })
            .Add(component => component.Value, (byte)1)
            .Add(component => component.DebounceInterval, 0)
            .Add(component => component.ToStringFunc, value => forms[value]));

        cut.Find("input").Focus();
        cut.Find("input").Input(string.Empty);
        cut.Find("input").Input(query);
        if (query.Length == 0)
        {
            cut.Find("input").GetAttribute("value").Should().BeNullOrEmpty();
        }
        else
        {
            cut.Find("input").GetAttribute("value").Should().Be(query);
        }
        popovers.WaitForAssertion(() => popovers.FindAll("[role='option']")
            .Select(option => option.TextContent.Trim()).Should().Equal(query.Length == 0 ? forms : ["Wash"]));
        cut.Find("input").Blur();
        popovers.FindAll("[role='option']").Single(option => option.TextContent.Trim() == "Wash").Click();

        cut.WaitForAssertion(() => cut.Find("input").GetAttribute("value").Should().Be("Wash"));
    }

    [Fact]
    public async Task KeyboardClearingWithoutDefaultOption_StaysEmptyAfterBlur()
    {
        var appState = new TestAppState { SaveFile = new SAV8SWSH() };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);
        ctx.Render<MudPopoverProvider>();
        var cut = ctx.Render<AutocompleteSelect<int>>(parameters => parameters
            .Add(component => component.Items, new[] { 1, 2 })
            .Add(component => component.Value, 1)
            .Add(component => component.DebounceInterval, 0));

        cut.Find("input").Focus();
        cut.Find("input").Input(string.Empty);
        cut.Find("input").Blur();

        cut.Find("input").GetAttribute("value").Should().BeNullOrEmpty();
    }

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
