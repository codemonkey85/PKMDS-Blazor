namespace Pkmds.Functions.Functions;

public class SubmitBugReport(IGitHubService gitHubService, IBlobService blobService, ILogger<SubmitBugReport> logger)
{
    private const long MaxSaveFileSizeBytes = 8 * 1024 * 1024; // 8 MB

    // Mirror the dialog's client-side minimum so direct callers can't bypass it with a one-word report.
    private const int MinPrimaryTextLength = 20;

    [Function("SubmitBugReport")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SubmitBugReport")]
        HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (!req.HasFormContentType)
        {
            return new BadRequestObjectResult(new { error = "Expected multipart/form-data." });
        }

        IFormCollection form;
        try
        {
            form = await req.ReadFormAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read form data");
            return new BadRequestObjectResult(new { error = "Failed to read form data." });
        }

        var name = form["name"].ToString().Trim();
        var email = form["email"].ToString().Trim();
        var category = form["category"].ToString().Trim();
        var appVersion = form["appVersion"].ToString().Trim();
        var pkhexVersion = form["pkhexVersion"].ToString().Trim();
        var userAgent = form["userAgent"].ToString().Trim();
        // Bug-category structured fields.
        var actual = form["actual"].ToString().Trim();
        var steps = form["steps"].ToString().Trim();
        var expected = form["expected"].ToString().Trim();
        var reportedSaveSource = form["reportedSaveSource"].ToString().Trim();
        // Feature / Feedback free-text field.
        var details = form["details"].ToString().Trim();
        var saveGameName = form["saveGameName"].ToString().Trim();
        var saveRevision = form["saveRevision"].ToString().Trim();
        var saveFileName = form["saveFileName"].ToString().Trim();
        var saveFileSource = form["saveFileSource"].ToString().Trim();
        var saveFileType = form["saveFileType"].ToString().Trim();

        // The primary text is the bug's "what happened" or the feature/feedback details, depending
        // on category. Older clients (pre-structured) sent a single "description" field — fall back
        // to it so the endpoint stays backward compatible.
        var primaryText = category.Equals("Bug", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(category)
            ? FirstNonEmpty(actual, form["description"].ToString().Trim())
            : details;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            return new BadRequestObjectResult(new { error = "name and email are required." });
        }

        // primaryText is already trimmed at assignment (the source fields are .Trim()'d). A length
        // check below the minimum also catches the empty case. The message is category-agnostic
        // since the primary text is "what happened" for bugs and "details" for feature/feedback.
        if (primaryText.Length < MinPrimaryTextLength)
        {
            return new BadRequestObjectResult(new { error = $"Please provide at least {MinPrimaryTextLength} characters describing your report." });
        }

        var (titlePrefix, issueLabel) = category.ToLowerInvariant() switch
        {
            "feature" => ("[Feature]", "enhancement"),
            "feedback" => ("[Feedback]", "feedback"),
            _ => ("[Bug]", "bug"),
        };

        var shortTitle = primaryText.Length > 72
            ? $"{primaryText[..72]}…"
            : primaryText;
        var issueTitle = $"{titlePrefix} {shortTitle}";

        var saveFileSection = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(saveGameName) ||
            !string.IsNullOrWhiteSpace(saveFileName) ||
            !string.IsNullOrWhiteSpace(saveFileSource) ||
            !string.IsNullOrWhiteSpace(saveFileType))
        {
            saveFileSection.Append("\n\n## Save File\n");
            if (!string.IsNullOrWhiteSpace(saveFileName))
            {
                saveFileSection.Append($"\n- **File:** `{saveFileName}`");
            }

            if (!string.IsNullOrWhiteSpace(saveGameName))
            {
                saveFileSection.Append($"\n- **Game:** {saveGameName}");
            }

            if (!string.IsNullOrWhiteSpace(saveRevision))
            {
                saveFileSection.Append($"\n- **Revision:** {saveRevision}");
            }

            if (!string.IsNullOrWhiteSpace(saveFileSource))
            {
                saveFileSection.Append($"\n- **Detected source:** {saveFileSource}");
            }

            if (!string.IsNullOrWhiteSpace(saveFileType))
            {
                saveFileSection.Append($"\n- **PKHeX type:** `{saveFileType}`");
            }
        }

        var detailsSection = new StringBuilder();
        if (category.Equals("Feature", StringComparison.OrdinalIgnoreCase) ||
            category.Equals("Feedback", StringComparison.OrdinalIgnoreCase))
        {
            detailsSection.Append($"\n\n## Details\n\n{primaryText}");
        }
        else
        {
            // Bug: assemble the structured sections, omitting any the reporter left blank.
            detailsSection.Append($"\n\n## What happened\n\n{primaryText}");
            if (!string.IsNullOrWhiteSpace(steps))
            {
                detailsSection.Append($"\n\n## Steps\n\n{steps}");
            }

            if (!string.IsNullOrWhiteSpace(expected))
            {
                detailsSection.Append($"\n\n## Expected\n\n{expected}");
            }

            if (!string.IsNullOrWhiteSpace(reportedSaveSource))
            {
                detailsSection.Append($"\n\n## Where the save came from\n\n{reportedSaveSource}");
            }
        }

        var issueBody =
            $"**Reporter:** {name} ({email})\n" +
            $"**App version:** {appVersion}\n" +
            (string.IsNullOrWhiteSpace(pkhexVersion)
                ? string.Empty
                : $"**PKHeX.Core version:** {pkhexVersion}\n") +
            $"**User agent:** {userAgent}" +
            saveFileSection +
            detailsSection;

        int issueNumber;
        string issueUrl;
        try
        {
            (issueNumber, issueUrl) = await gitHubService.CreateIssueAsync(issueTitle, issueBody, issueLabel);
            logger.LogInformation("Created GitHub issue #{IssueNumber} for bug report from {Email}", issueNumber, email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create GitHub issue");
            return new ObjectResult(new { error = "Failed to create GitHub issue. Please try again later." }) { StatusCode = StatusCodes.Status502BadGateway };
        }

        var saveFile = form.Files["saveFile"];
        if (saveFile is not { Length: > 0 })
        {
            return new ObjectResult(new { issueNumber, issueUrl }) { StatusCode = StatusCodes.Status201Created };
        }

        {
            if (saveFile.Length > MaxSaveFileSizeBytes)
            {
                logger.LogWarning("Save file for issue #{IssueNumber} exceeds 8 MB limit ({Size} bytes) — skipping upload",
                    issueNumber, saveFile.Length);
            }
            else
            {
                var safeFileName = SanitizeFileName(saveFile.FileName);
                try
                {
                    await using var stream = saveFile.OpenReadStream();
                    await blobService.UploadAsync(issueNumber, safeFileName, stream, cancellationToken);
                    var blobPath = $"{issueNumber}/{safeFileName}";
                    var sasUrl = blobService.GetSasUrl(issueNumber, safeFileName, TimeSpan.FromDays(30));
                    var comment = sasUrl is not null
                        ? $"📎 [Download save file]({sasUrl}) — expires in 30 days (blob path: `{blobPath}`)"
                        : $"📎 Save file attached at blob path: `{blobPath}`";
                    await gitHubService.AddCommentAsync(issueNumber, comment);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to upload save file for issue #{IssueNumber}", issueNumber);
                    // Non-fatal: issue was already created; log and continue.
                }
            }
        }

        return new ObjectResult(new { issueNumber, issueUrl }) { StatusCode = StatusCodes.Status201Created };
    }

    private static string FirstNonEmpty(params string[] values) =>
        Array.Find(values, v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        name = invalid.Aggregate(name, (current, c) => current.Replace(c, '_'));

        return string.IsNullOrWhiteSpace(name)
            ? "save.bin"
            : name;
    }
}
