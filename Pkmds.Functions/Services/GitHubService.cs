namespace Pkmds.Functions.Services;

public class GitHubService(IConfiguration configuration) : IGitHubService
{
    private readonly GitHubClient client = new(new ProductHeaderValue("pkmds-bug-reporter"))
    {
        Credentials = new Credentials(
            configuration["GitHubPat"] ?? throw new InvalidOperationException("GitHubPat configuration is required."))
    };

    private readonly string owner = configuration["GitHubOwner"]
                                    ?? throw new InvalidOperationException("GitHubOwner configuration is required.");

    private readonly string repo = configuration["GitHubRepo"]
                                   ?? throw new InvalidOperationException("GitHubRepo configuration is required.");

    public async Task<(int IssueNumber, string IssueUrl)> CreateIssueAsync(
        string title,
        string body,
        string label)
    {
        var newIssue = new NewIssue(title) { Body = body };
        newIssue.Labels.Add(label);

        var issue = await client.Issue.Create(owner, repo, newIssue);
        return (issue.Number, issue.HtmlUrl);
    }

    public async Task AddCommentAsync(
        int issueNumber,
        string comment) =>
        await client.Issue.Comment.Create(owner, repo, issueNumber, comment);
}
