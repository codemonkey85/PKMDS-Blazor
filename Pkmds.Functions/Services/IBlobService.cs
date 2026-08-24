namespace Pkmds.Functions.Services;

public interface IBlobService
{
    Task UploadAttachmentAsync(
        int issueNumber,
        Stream data,
        CancellationToken cancellationToken = default);

    Task StoreContactAsync(
        int issueNumber,
        string email,
        CancellationToken cancellationToken = default);

    Task CloseIssueAsync(
        int issueNumber,
        CancellationToken cancellationToken = default);
}
