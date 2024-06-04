string[] ApiRoots =
#if DEBUG
    ["https://localhost:7102/"];
#else
    ["https://pkmds.azurewebsites.net/", "https://pkmds.app/"];
#endif

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services
    .AddMudServices()
    .AddFileSystemAccessService()
    .AddScoped<IAppState, AppState>()
    .AddScoped<IRefreshService, RefreshService>()
    .AddScoped<IAppService, AppService>()
    .AddScoped<IFileSaverService, ServerFileSaverService>()
    .AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

services.AddCors(
    options => options.AddDefaultPolicy(
        builder => builder.WithOrigins(ApiRoots)
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseCors();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Pkmds.Web.Client._Imports).Assembly);

var apiGroup = app.MapGroup("api");
apiGroup.MapPost("savefile", async (HttpRequest request) =>
{
    byte[] returnData = [];

    using var reader = new StreamReader(request.Body);

    using var memoryStream = new MemoryStream();
    await reader.BaseStream.CopyToAsync(memoryStream);
    var saveFileData = memoryStream.ToArray();

    if (saveFileData is { Length: > 0 })
    {
        var saveFile = SaveUtil.GetVariantSAV(saveFileData);
        if (saveFile is not null)
        {
            returnData = saveFile.Write();
        }
    }

    return new ExportSaveFileResponse
    {
        SaveFileData = returnData
    };
}).Accepts<byte[]>("application/octet-stream");

app.Run();
