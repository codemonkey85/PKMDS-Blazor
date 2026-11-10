using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Pkmds.Tests;

public class BrowseAutocompleteTests
{
    [Fact]
    public async Task ArrowBrowsingAndSelection_DoNotRequestInputFocus()
    {
        await using var ctx = CreateContext();
        var popovers = ctx.Render<MudPopoverProvider>();
        var selected = 2;
        var cut = ctx.Render<BrowseAutocomplete<int>>(parameters => parameters
            .Add(component => component.Label, "Species")
            .Add(component => component.Value, selected)
            .Add(component => component.ValueChanged, value => selected = value)
            .Add(component => component.SearchFunc, (_, _) => Task.FromResult(Enumerable.Range(1, 20))));

        cut.Find("button[aria-label='Browse Species options']").Click();
        popovers.WaitForAssertion(() => popovers.FindAll(".browse-autocomplete-option").Should().HaveCount(10));
        popovers.FindAll(".browse-autocomplete-option")[4].Click();

        cut.WaitForAssertion(() => selected.Should().Be(5));
        cut.Find("input").GetAttribute("value").Should().Be("5");
        ctx.JSInterop.Invocations.Should().NotContain(invocation =>
            invocation.Identifier == "Blazor._internal.domWrapper.focus" || invocation.Identifier == "mudElementRef.select");
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData(100, 10)]
    [InlineData(5, 5)]
    public async Task BrowsePages_IncludeSelectedValueAndReachEntireSource(int? maxItems, int expectedCount)
    {
        await using var ctx = CreateContext();
        var popovers = ctx.Render<MudPopoverProvider>();
        var cut = ctx.Render<BrowseAutocomplete<int>>(parameters => parameters
            .Add(component => component.Label, "Species")
            .Add(component => component.Value, 50)
            .Add(component => component.MaxItems, maxItems)
            .Add(component => component.SearchFunc, (_, _) => Task.FromResult(Enumerable.Range(1, 100))));

        cut.Find("button[aria-label='Browse Species options']").Click();
        popovers.WaitForAssertion(() =>
        {
            popovers.FindAll(".browse-autocomplete-option").Should().HaveCount(expectedCount);
            popovers.Find("[aria-selected='true']").TextContent.Should().Be("50");
        });

        while (!popovers.Find("button[aria-label='Previous Species options']").HasAttribute("disabled"))
        {
            popovers.Find("button[aria-label='Previous Species options']").Click();
        }

        popovers.FindAll(".browse-autocomplete-option")[0].TextContent.Should().Be("1");
        while (!popovers.Find("button[aria-label='Next Species options']").HasAttribute("disabled"))
        {
            popovers.Find("button[aria-label='Next Species options']").Click();
        }

        popovers.FindAll(".browse-autocomplete-option")[^1].TextContent.Should().Be("100");
        popovers.FindAll(".browse-autocomplete-option").Count.Should().BeLessThanOrEqualTo(expectedCount);
    }

    [Fact]
    public async Task TappingText_SwitchesFromBrowseToFilteredSearch()
    {
        await using var ctx = CreateContext();
        var popovers = ctx.Render<MudPopoverProvider>();
        string[] forms = ["Normal", "Heat", "Wash"];
        var cut = ctx.Render<BrowseAutocomplete<string>>(parameters => parameters
            .Add(component => component.Label, "Form")
            .Add(component => component.Value, "Heat")
            .Add(component => component.Strict, false)
            .Add(component => component.DebounceInterval, 0)
            .Add(component => component.SearchFunc, (query, _) =>
                Task.FromResult(forms.Where(form => form.Contains(query ?? "", StringComparison.OrdinalIgnoreCase)))));

        cut.Find("button[aria-label='Browse Form options']").Click();
        popovers.WaitForAssertion(() => popovers.FindAll(".browse-autocomplete-option").Should().HaveCount(3));
        cut.Find("input").Focus();
        cut.Find("input").Input("Wa");

        popovers.WaitForAssertion(() =>
        {
            popovers.FindAll(".mud-popover-open .browse-autocomplete-option").Should().BeEmpty();
            popovers.FindAll(".mud-popover-open [role='option']").Single().TextContent.Trim().Should().Be("Wash");
        });
    }

    [Fact]
    public async Task ClosingBrowse_CancelsPendingResultsAndNotifiesParent()
    {
        await using var ctx = CreateContext();
        var popovers = ctx.Render<MudPopoverProvider>();
        var pending = new TaskCompletionSource<IEnumerable<int>>();
        var cancellation = CancellationToken.None;
        var closed = false;
        var cut = ctx.Render<BrowseAutocomplete<int>>(parameters => parameters
            .Add(component => component.Label, "Species")
            .Add(component => component.OpenChanged, open => closed = !open)
            .Add(component => component.SearchFunc, (_, token) => { cancellation = token; return pending.Task; }));

        var opening = cut.Find("button[aria-label='Browse Species options']").ClickAsync(new MouseEventArgs());
        popovers.WaitForAssertion(() => popovers.Find(".mud-popover-open").TextContent.Should().Contain("Loading options"));
        cut.Find("button[aria-label='Browse Species options']").Click();
        cancellation.IsCancellationRequested.Should().BeTrue();
        pending.SetResult([1, 2, 3]);
        await opening;

        popovers.FindAll(".mud-popover-open").Should().BeEmpty();
        closed.Should().BeTrue();
    }

    [Fact]
    public async Task BrowsePreservesDefaultRestorationForWrappedNullableOptions()
    {
        await using var ctx = CreateContext();
        var popovers = ctx.Render<MudPopoverProvider>();
        var cut = ctx.Render<AutocompleteSelect<int?>>(parameters => parameters
            .Add(component => component.Label, "Nature filter")
            .Add(component => component.Items, new int?[] { null, 1, 2 })
            .Add(component => component.Value, (int?)1)
            .Add(component => component.ToStringFunc, value => value?.ToString() ?? "Any"));

        cut.Find("button[aria-label='Clear']").Click();
        cut.Find("button[aria-label='Browse Nature filter options']").Click();
        popovers.WaitForAssertion(() => popovers.Find(".browse-autocomplete-option[aria-selected='true']")
            .TextContent.Should().Be("Any"));
        popovers.FindAll(".browse-autocomplete-option")[2].Click();
        cut.Find("input").GetAttribute("value").Should().Be("2");
    }

    private static BunitContext CreateContext()
    {
        var appState = new TestAppState { SaveFile = new SAV8SWSH() };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));
        var context = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);
        context.JSInterop.SetupModule("./_content/Pkmds.Rcl/js/browseAutocomplete.js")
            .SetupModule("watch", _ => true);
        return context;
    }
}
