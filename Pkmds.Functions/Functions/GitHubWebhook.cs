namespace Pkmds.Functions.Functions;

public class GitHubWebhook(IBlobService blobService, IConfiguration configuration, ILogger<GitHubWebhook> logger)
{
    [Function("GitHubWebhook")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "GitHubWebhook")]
        HttpRequest req,
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
            return new UnauthorizedResult();
        }

        var eventType = req.Headers["X-GitHub-Event"].ToString();
        if (eventType != "issues")
        {
            return new OkResult();
        }

        GitHubIssuePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<GitHubIssuePayload>(body);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize GitHub webhook payload");
            return new BadRequestObjectResult(new { error = "Invalid payload." });
        }

        if (payload?.Action != "closed")
        {
            return new OkResult();
        }

        logger.LogInformation("GitHub issue #{IssueNumber} closed — applying private-data retention", payload.Issue.Number);
        try
        {
            await blobService.CloseIssueAsync(payload.Issue.Number, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply private-data retention for issue #{IssueNumber}", payload.Issue.Number);
            // GitHub retries webhook deliveries that receive a non-2xx response. CloseIssueAsync
            // is idempotent, so a retry safely resumes any cleanup that failed partway through.
            return new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
        }

        return new OkResult();
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
