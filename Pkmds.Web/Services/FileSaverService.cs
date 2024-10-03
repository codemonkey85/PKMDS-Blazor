namespace Pkmds.Web.Services;

public class FileSaverService(HttpClient httpClient) : IFileSaverService
{
    private const string ApiRoot =
#if DEBUG
        "https://localhost:7102/";
#else
        "https://pkmds.app/";
#endif

    private const string SaveFileEndpoint = $"{ApiRoot}api/savefile";

    public async Task<byte[]> ExportSaveFileAsync(ExportSaveFileRequest request)
    {
        if (request is not { SaveFileData: not null })
        {
            return [];
        }

        var content = new ByteArrayContent(request.SaveFileData);

        try
        {
            var response = await httpClient.PostAsync(SaveFileEndpoint, content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ExportSaveFileResponse>();
                return result?.SaveFileData ?? [];
            }
            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return [];
        }
    }
}
