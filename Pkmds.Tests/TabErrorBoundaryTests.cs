using Bunit;
using Microsoft.AspNetCore.Components;

namespace Pkmds.Tests;

public class TabErrorBoundaryTests
{
    [Fact]
    public void ChildFailure_IsContainedAndCanRecover()
    {
        // Arrange
        var appState = new TestAppState();
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService, new LegalizationService(appState));
        using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);
        var shouldThrow = true;
        RenderFragment childContent = builder =>
        {
            if (shouldThrow)
            {
                throw new InvalidOperationException("Expected test failure");
            }

            builder.AddContent(0, "Recovered content");
        };

        // Act
        var cut = ctx.Render<TabErrorBoundary>(parameters => parameters
            .Add(component => component.TabName, "Bag")
            .Add(component => component.ChildContent, childContent));

        // Assert
        cut.Markup.Should().Contain("Bag couldn't be displayed");
        cut.Markup.Should().Contain("Your save is still loaded");

        shouldThrow = false;
        cut.Find("button").Click();
        cut.Markup.Should().Contain("Recovered content");
        cut.Markup.Should().NotContain("couldn't be displayed");
    }
}
