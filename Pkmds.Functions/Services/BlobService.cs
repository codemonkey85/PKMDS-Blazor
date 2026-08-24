namespace Pkmds.Functions.Services;

public class BlobService(IConfiguration configuration, ILogger<BlobService> logger) : IBlobService
{
    private const string AttachmentBlobName = "attachment.bin";
    private const string ClosedIssueMarkerPrefix = "issues/closed";
    private const int ContactClosureRetentionDays = 30;
    private const int ContactMaximumRetentionDays = 365;

    private readonly BlobContainerClient containerClient = CreateContainerClient(configuration);

    public async Task<bool> UploadAttachmentAsync(
        int issueNumber,
        Stream data,
        CancellationToken cancellationToken = default)
    {
        var blobName = $"attachments/{issueNumber}/{AttachmentBlobName}";
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(data, true, cancellationToken);

        if (await IsIssueClosedAsync(issueNumber, cancellationToken))
        {
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            logger.LogInformation(
                "Discarded a private attachment for already-closed issue #{IssueNumber}", issueNumber);
            return false;
        }

        logger.LogInformation("Stored a private attachment for issue #{IssueNumber}", issueNumber);
        return true;
    }

    public async Task StoreContactAsync(
        int issueNumber,
        string email,
        CancellationToken cancellationToken = default)
    {
        var blobClient = containerClient.GetBlobClient($"contacts/open/{issueNumber}.json");
        var contact = new PrivateContactRecord(issueNumber, email, DateTimeOffset.UtcNow);
        await blobClient.UploadAsync(BinaryData.FromObjectAsJson(contact), true, cancellationToken);

        if (await IsIssueClosedAsync(issueNumber, cancellationToken))
        {
            await MoveContactToClosedRetentionAsync(issueNumber, cancellationToken);
        }

        logger.LogInformation("Stored private contact details for issue #{IssueNumber}", issueNumber);
    }

    public async Task CloseIssueAsync(
        int issueNumber,
        CancellationToken cancellationToken = default)
    {
        // Persist the marker before cleanup. Storage operations reconcile against it after their
        // writes, closing the race where GitHub delivers an issue-close event while the original
        // submission is still storing private data. The marker also makes webhook retries safe.
        var closedMarker = containerClient.GetBlobClient(GetClosedIssueMarkerName(issueNumber));
        await closedMarker.UploadAsync(BinaryData.FromString("closed"), true, cancellationToken);

        var deleted = 0;
        foreach (var prefix in new[] { $"attachments/{issueNumber}/", $"{issueNumber}/" })
        {
            await foreach (var blob in containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix,
                               cancellationToken))
            {
                var blobClient = containerClient.GetBlobClient(blob.Name);
                await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                deleted++;
            }
        }

        await MoveContactToClosedRetentionAsync(issueNumber, cancellationToken);
        logger.LogInformation("Closed private report data for issue #{IssueNumber}; deleted {Count} attachment blob(s)",
            issueNumber, deleted);
    }

    private async Task MoveContactToClosedRetentionAsync(int issueNumber, CancellationToken cancellationToken)
    {
        var openContact = containerClient.GetBlobClient($"contacts/open/{issueNumber}.json");
        Azure.Response<BlobDownloadResult> content;
        try
        {
            content = await openContact.DownloadContentAsync(cancellationToken);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == StatusCodes.Status404NotFound)
        {
            // A concurrent webhook retry or post-write reconciliation may already have moved it.
            return;
        }

        var contact = content.Value.Content.ToObjectFromJson<PrivateContactRecord>();
        var latestClosedDeletion = DateTimeOffset.UtcNow.AddDays(ContactClosureRetentionDays);
        if (contact is null || latestClosedDeletion >= contact.SubmittedAtUtc.AddDays(ContactMaximumRetentionDays))
        {
            // Moving a nearly-expired record would reset its blob creation time and could extend it
            // beyond the 12-month ceiling. Delete it at closure instead.
            await openContact.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            return;
        }

        var closedContact = containerClient.GetBlobClient($"contacts/closed/{issueNumber}.json");
        try
        {
            await closedContact.UploadAsync(
                content.Value.Content,
                new BlobUploadOptions
                {
                    Conditions = new BlobRequestConditions { IfNoneMatch = Azure.ETag.All },
                },
                cancellationToken);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status is StatusCodes.Status409Conflict or StatusCodes.Status412PreconditionFailed)
        {
            // A concurrent reconciliation or webhook retry already created the immutable closed
            // record. Do not overwrite it: resetting creation time could extend retention.
        }

        await openContact.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private async Task<bool> IsIssueClosedAsync(int issueNumber, CancellationToken cancellationToken) =>
        await containerClient.GetBlobClient(GetClosedIssueMarkerName(issueNumber)).ExistsAsync(cancellationToken);

    private static string GetClosedIssueMarkerName(int issueNumber) =>
        $"{ClosedIssueMarkerPrefix}/{issueNumber}.marker";

    private static BlobContainerClient CreateContainerClient(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorageConnectionString"]
                               ?? throw new InvalidOperationException(
                                   "AzureStorageConnectionString configuration is required.");
        var containerName = configuration["BlobContainerName"] ?? "bug-reports";
        var containerClient = new BlobServiceClient(connectionString).GetBlobContainerClient(containerName);
        containerClient.CreateIfNotExists();
        return containerClient;
    }

    private sealed record PrivateContactRecord(int IssueNumber, string Email, DateTimeOffset SubmittedAtUtc);
}
