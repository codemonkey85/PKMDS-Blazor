var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services
    .AddOptions<ServerAppSettings>()
    .BindConfiguration(nameof(ServerAppSettings))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var appSettings = builder.Configuration.Get<ServerAppSettings>() ?? new();

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

services
    .ConfigureCors(appSettings.CorsPolicies.AllowedOrigins, builder.Environment.IsDevelopment());

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

app.UseCors(CorsStartup.CorsPolicyName);

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

    // ReSharper disable once InvertIf
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
