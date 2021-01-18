using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace PKMDSBlazor.Shared
{
    public static class JavaMethods
    {
        public static async Task DownloadFile(this IJSRuntime JSRuntime, string fileName, string mimeType, byte[] file) =>
            await JSRuntime.InvokeVoidAsync("BlazorDownloadFile", fileName, mimeType, file);
    }
}
