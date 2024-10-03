namespace Pkmds.Web.Services;

public interface IFileSaverService
{
    Task<byte[]> ExportSaveFileAsync(ExportSaveFileRequest request);
}
