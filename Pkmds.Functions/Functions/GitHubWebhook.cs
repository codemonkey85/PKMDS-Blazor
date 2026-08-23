namespace Pkmds.Functions.Functions;

public class GitHubWebhook
{
    public static async Task<IResult> Run(
        HttpRequest req,
        IStorageService storageService,
        IConfiguration configuration,
        ILogger<GitHubWebhook> logger,
        CancellationToken cancellationToken)
    {
        string body;
        using (var reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        var secret = configuration["GitHubWebhookSecret"];
        if (!string.IsNullOrWhiteSpace(secret) && !VerifySignature(body, secret, req.Headers["X-Hub-Signature-256"]))
        {
            logger.LogWarning("GitHub webhook signature verification failed");
            return Results.Unauthorized();
        }

        var eventType = req.Headers["X-GitHub-Event"].ToString();
        if (eventType != "issues")
        {
            return Results.Ok();
        }

        GitHubIssuePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<GitHubIssuePayload>(body);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize GitHub webhook payload");
            return Results.BadRequest(new { error = "Invalid payload." });
        }

        if (payload?.Action != "closed")
        {
            return Results.Ok();
        }

        {
            logger.LogInformation("GitHub issue #{IssueNumber} closed — cleaning up files", payload.Issue.Number);
            try
            {
                await storageService.DeleteIssueFilesAsync(payload.Issue.Number, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete files for issue #{IssueNumber}", payload.Issue.Number);
            }
        }

        return Results.Ok();
    }

    private static bool VerifySignature(string body, string secret, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || !signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var hexSignature = signatureHeader["sha256=".Length..];
        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromHexString(hexSignature);
        }
        catch (FormatException)
        {
            return false;
        }

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var actualBytes = HMACSHA256.HashData(keyBytes, bodyBytes);

        return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
