namespace Pkmds.Web.Client.Services;

public interface IFileSaverService
{
    Task<byte[]> ExportSaveFileAsync(ExportSaveFileRequest request);
}
