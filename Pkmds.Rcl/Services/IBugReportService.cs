namespace Pkmds.Rcl.Services;

public enum BugReportCategory
{
    Bug,
    Feature,
    Feedback,
}

public sealed record BugReportRequest(
    string Name,
    string Email,
    BugReportCategory Category,
    string AppVersion,
    string UserAgent,
    // Bug category — structured fields. Actual is the one required field (and also carries the
    // pre-filled dump on the crash path); the rest are optional context.
    string? Actual = null,
    string? Steps = null,
    string? Expected = null,
    string? ReportedSaveSource = null,
    // Feature / Feedback categories — single free-text field.
    string? Details = null,
    string? PkhexVersion = null,
    byte[]? SaveFileBytes = null,
    string? SaveFileName = null,
    string? SaveGameName = null,
    string? SaveRevision = null,
    string? SaveFileSource = null,
    string? SaveFileType = null,
    Exception? CapturedException = null);

public sealed record BugReportResult(bool Success, string? IssueUrl = null, string? ErrorMessage = null);

public interface IBugReportService
{
    Task<BugReportResult> SubmitBugReportAsync(BugReportRequest request, CancellationToken cancellationToken = default);
}
