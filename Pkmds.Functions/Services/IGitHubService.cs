namespace Pkmds.Functions.Services;

public interface IGitHubService
{
    Task<(int IssueNumber, string IssueUrl)> CreateIssueAsync(
        string title,
        string body,
        string label);

    Task AddCommentAsync(
        int issueNumber,
        string comment);
}
