var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddMudServices()
    .AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
    .AddScoped<IAppState, AppState>()
    .AddScoped<IRefreshService, RefreshService>()
    .AddScoped<IAppService, AppService>()
    .AddFileSystemAccessService();

await builder.Build().RunAsync();
