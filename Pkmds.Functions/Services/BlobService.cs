namespace Pkmds.Functions.Services;

public class BlobService(IConfiguration configuration, ILogger<BlobService> logger) : IBlobService
{
    private const string AttachmentBlobName = "attachment.bin";
    private const int ContactClosureRetentionDays = 30;
    private const int ContactMaximumRetentionDays = 365;

    private readonly BlobContainerClient containerClient = CreateContainerClient(configuration);

    public async Task UploadAttachmentAsync(
        int issueNumber,
        Stream data,
        CancellationToken cancellationToken = default)
    {
        var blobName = $"attachments/{issueNumber}/{AttachmentBlobName}";
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(data, true, cancellationToken);
        logger.LogInformation("Stored a private attachment for issue #{IssueNumber}", issueNumber);
    }

    public async Task StoreContactAsync(
        int issueNumber,
        string email,
        CancellationToken cancellationToken = default)
    {
        var blobClient = containerClient.GetBlobClient($"contacts/open/{issueNumber}.json");
        var contact = new PrivateContactRecord(issueNumber, email, DateTimeOffset.UtcNow);
        await blobClient.UploadAsync(BinaryData.FromObjectAsJson(contact), true, cancellationToken);
        logger.LogInformation("Stored private contact details for issue #{IssueNumber}", issueNumber);
    }

    public async Task CloseIssueAsync(
        int issueNumber,
        CancellationToken cancellationToken = default)
    {
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
        if (!await openContact.ExistsAsync(cancellationToken))
        {
            return;
        }

        var content = await openContact.DownloadContentAsync(cancellationToken);
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
        await closedContact.UploadAsync(content.Value.Content, true, cancellationToken);
        await openContact.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

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
