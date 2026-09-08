using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Pkmds.Tests;

public class TabErrorBoundaryTests
{
    [Fact]
    public void ChildFailure_IsContainedAndCanRecover()
    {
        // Arrange
        using var ctx = new BunitContext();
        ctx.Services.AddMudServices();
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
