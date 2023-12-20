var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var services = builder.Services;

services
    .AddMudServices()
    .AddScoped<IAppState, AppState>()
    .AddScoped<IRefreshService, RefreshService>()
    .AddScoped<IAppService, AppService>()
    .AddFileSystemAccessService();

var app = builder.Build();

await app.RunAsync();
