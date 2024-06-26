var builder = WebAssemblyHostBuilder.CreateDefault(args);
var services = builder.Services;

services
    .AddMudServices()
    .AddFileSystemAccessService()
    .AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
    .AddScoped<IAppState, AppState>()
    .AddScoped<IRefreshService, RefreshService>()
    .AddScoped<IAppService, AppService>()
    .AddScoped<IFileSaverService, FileSaverService>();

await builder.Build().RunAsync();
