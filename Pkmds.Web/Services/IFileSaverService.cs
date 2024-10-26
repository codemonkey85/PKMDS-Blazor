namespace Pkmds.Web.Services;

public interface IFileSaverService
{
    Task<byte[]> ExportSaveFile(ExportSaveFileRequest request);
}
