namespace Pkmds.Functions.Services;

public interface IStorageService
{
    Task UploadAsync(
        int issueNumber,
        string fileName,
        Stream data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a time-limited pre-signed URL for reading the specified object. The returned string
    /// is fully percent-encoded and safe to embed in Markdown links.
    /// </summary>
    string GetPresignedUrl(int issueNumber, string fileName, TimeSpan expiry);

    Task DeleteIssueFilesAsync(
        int issueNumber,
        CancellationToken cancellationToken = default);
}
