using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Pkmds.Rcl.Components.Dialogs;

namespace Pkmds.Tests;

public class TabErrorBoundaryTests
{
    [Fact]
    public async Task ReportError_OpensReviewDialogWithOriginalExceptionAndTabContext()
    {
        var state = new TestAppState { SaveFile = new SAV9SV() };
        var refresh = new TestRefreshService();
        var service = new AppService(state, refresh, new LegalizationService(state));
        await using var ctx = BunitTestHelpers.CreateBunitContext(state, refresh, service);
        var reports = new RecordingReportService();
        ctx.Services.AddSingleton<IBugReportService>(reports);
        ctx.Render<MudPopoverProvider>();
        var provider = ctx.Render<MudDialogProvider>();
        var exception = new InvalidOperationException("Expected encounter search failure");
        RenderFragment content = _ => throw exception;
        var cut = ctx.Render<TabErrorBoundary>(parameters => parameters
            .Add(component => component.TabName, "Encounter DB")
            .Add(component => component.ChildContent, content));

        var button = cut.FindComponents<MudButton>()
            .Single(component => component.Markup.Contains("Report this error"));
        var reportTask = cut.InvokeAsync(() => button.Instance.OnClick.InvokeAsync());

        provider.WaitForAssertion(() =>
        {
            var dialog = provider.FindComponent<BugReportDialog>();
            dialog.Instance.CapturedException.Should().BeSameAs(exception);
            dialog.Instance.ErrorContext.Should().Be("Encounter DB");
            dialog.Instance.HasSaveFile.Should().BeTrue();
            dialog.Markup.Should().Contain("Expected encounter search failure");
        });
        reports.Submissions.Should().Be(0, "opening a report must allow review before submission");
        cut.Markup.Should().Contain("Your save is still loaded");

        provider.FindAll("button").Single(element => element.TextContent.Trim() == "Cancel").Click();
        await reportTask;
        reports.Submissions.Should().Be(0);
    }

    private sealed class RecordingReportService : IBugReportService
    {
        public int Submissions { get; private set; }

        public Task<BugReportResult> SubmitBugReportAsync(BugReportRequest request,
            CancellationToken cancellationToken = default)
        {
            Submissions++;
            return Task.FromResult(new BugReportResult(true, "https://example.invalid/issues/1"));
        }
    }

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
