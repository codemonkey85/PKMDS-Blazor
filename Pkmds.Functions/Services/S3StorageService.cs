namespace Pkmds.Functions.Services;

public class S3StorageService(IAmazonS3 s3Client, IConfiguration configuration, ILogger<S3StorageService> logger) : IStorageService
{
    private readonly string bucketName = configuration["S3BucketName"]
                                         ?? throw new InvalidOperationException("S3BucketName configuration is required.");

    public async Task UploadAsync(
        int issueNumber,
        string fileName,
        Stream data,
        CancellationToken cancellationToken = default)
    {
        var key = $"{issueNumber}/{fileName}";
        await s3Client.PutObjectAsync(
            new PutObjectRequest { BucketName = bucketName, Key = key, InputStream = data },
            cancellationToken);
        logger.LogInformation("Uploaded object {Key} for issue #{IssueNumber}", key, issueNumber);
    }

    public string GetPresignedUrl(int issueNumber, string fileName, TimeSpan expiry)
    {
        var key = $"{issueNumber}/{fileName}";
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry),
        };

        // GetPreSignedURL percent-encodes the key already, matching the SAS URL's AbsoluteUri behavior.
        return s3Client.GetPreSignedURL(request);
    }

    public async Task DeleteIssueFilesAsync(
        int issueNumber,
        CancellationToken cancellationToken = default)
    {
        var prefix = $"{issueNumber}/";
        var listRequest = new ListObjectsV2Request { BucketName = bucketName, Prefix = prefix };
        var deleted = 0;

        ListObjectsV2Response response;
        do
        {
            response = await s3Client.ListObjectsV2Async(listRequest, cancellationToken);
            if (response.S3Objects.Count > 0)
            {
                await s3Client.DeleteObjectsAsync(
                    new DeleteObjectsRequest
                    {
                        BucketName = bucketName,
                        Objects = [.. response.S3Objects.Select(o => new KeyVersion { Key = o.Key })],
                    },
                    cancellationToken);
                deleted += response.S3Objects.Count;
            }

            listRequest.ContinuationToken = response.NextContinuationToken;
        }
        while (response.IsTruncated == true);

        logger.LogInformation("Deleted {Count} object(s) for issue #{IssueNumber}", deleted, issueNumber);
    }
}
