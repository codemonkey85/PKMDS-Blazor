using Bunit;

namespace Pkmds.Tests;

public class AutocompleteSelectTests
{
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
