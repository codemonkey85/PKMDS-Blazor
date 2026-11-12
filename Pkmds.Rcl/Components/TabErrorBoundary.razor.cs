namespace Pkmds.Rcl.Components;

public partial class TabErrorBoundary
{
    private ErrorBoundary? errorBoundary;

    [Parameter]
    [EditorRequired]
    public required string TabName { get; set; }

    [Parameter]
    [EditorRequired]
    public required RenderFragment ChildContent { get; set; }

    private void Recover() => errorBoundary?.Recover();

    private async Task ReportErrorAsync(Exception exception)
    {
        var parameters = new DialogParameters<BugReportDialog>
        {
            { dialog => dialog.HasSaveFile, AppState.SaveFile is not null },
            { dialog => dialog.AppVersion, AppState.AppVersion ?? string.Empty },
            { dialog => dialog.CapturedException, exception },
            { dialog => dialog.ErrorContext, TabName },
        };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        var dialog = await DialogService.ShowAsync<BugReportDialog>("Report this error", parameters, options);
        var result = await dialog.Result;
        if (result is { Data: BugReportResult submission })
        {
            Snackbar.Add(submission.ToSubmissionMarkup("Report submitted!"),
                submission.WarningMessage is null ? Severity.Success : Severity.Warning,
                options => options.RequireInteraction = true);
        }
    }
}
