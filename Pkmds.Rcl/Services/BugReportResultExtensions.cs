using System.Text.Encodings.Web;

namespace Pkmds.Rcl.Services;

public static class BugReportResultExtensions
{
    private const string GitHubIssuePathPrefix = "/codemonkey85/PKMDS-Blazor/issues/";

    public static MarkupString ToSubmissionMarkup(this BugReportResult result, string successMessage)
    {
        var message = HtmlEncoder.Default.Encode(result.WarningMessage ?? successMessage);
        if (!TryGetSafeIssueUri(result.IssueUrl, out var issueUri))
        {
            return new MarkupString(message);
        }

        var encodedUrl = HtmlEncoder.Default.Encode(issueUri.AbsoluteUri);
        return new MarkupString(
            $"{message} <a href=\"{encodedUrl}\" target=\"_blank\" rel=\"noopener noreferrer\">View issue</a>");
    }

    private static bool TryGetSafeIssueUri(string? value, [NotNullWhen(true)] out Uri? issueUri)
    {
        issueUri = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var candidate) ||
            candidate.Scheme != Uri.UriSchemeHttps ||
            !candidate.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
            !candidate.IsDefaultPort ||
            !string.IsNullOrEmpty(candidate.UserInfo) ||
            !string.IsNullOrEmpty(candidate.Query) ||
            !string.IsNullOrEmpty(candidate.Fragment) ||
            !candidate.AbsolutePath.StartsWith(GitHubIssuePathPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var issueNumber = candidate.AbsolutePath[GitHubIssuePathPrefix.Length..];
        if (!int.TryParse(issueNumber, CultureInfo.InvariantCulture, out var parsedIssueNumber) ||
            parsedIssueNumber <= 0)
        {
            return false;
        }

        issueUri = candidate;
        return true;
    }
}
