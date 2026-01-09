using Serilog;
using Serilog.Core;
using Serilog.Events;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var services = builder.Services;
var logging = builder.Logging;

// Configure Serilog with browser console sink
var levelSwitch = new LoggingLevelSwitch();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(levelSwitch)
    .Enrich.FromLogContext()
    .WriteTo.BrowserConsole()
    .CreateLogger();

logging.ClearProviders();
logging.AddSerilog(Log.Logger, dispose: true);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

services
    .AddMudServices(config =>
    {
        config.SnackbarConfiguration.PreventDuplicates = false;
        config.SnackbarConfiguration.ClearAfterNavigation = true;
    });

services
    .AddSingleton(_ => new HttpClient { BaseAddress = new(builder.HostEnvironment.BaseAddress) })
    .AddFileSystemAccessService()
    .AddSingleton<IAppState, AppState>()
    .AddSingleton<IRefreshService, RefreshService>()
    .AddSingleton<IAppService, AppService>()
    .AddSingleton<IDragDropService, DragDropService>()
    .AddSingleton<ILoggingService, LoggingService>()
    .AddSingleton(levelSwitch)
    .AddSingleton<JsService>()
    .AddSingleton<BlazorAesProvider>()
    .AddSingleton<BlazorMd5Provider>();

var app = builder.Build();

// Although Blazor WASM can target the whole .NET Framework API surface,
// during the Runtime, Microsoft has disabled the native support to some APIs
// under the System.Security.Cryptography namespace
// During startup we replace PKHeX unsupported cryptography APIs with a javascript-based alternative 
RuntimeCryptographyProvider.Aes = app.Services.GetRequiredService<BlazorAesProvider>();
RuntimeCryptographyProvider.Md5 = app.Services.GetRequiredService<BlazorMd5Provider>();

// Configure logging service to control log levels
var loggingService = app.Services.GetRequiredService<ILoggingService>();
loggingService.OnLoggingConfigurationChanged += () => levelSwitch.MinimumLevel = loggingService.IsVerboseLoggingEnabled
    ? LogEventLevel.Debug
    : LogEventLevel.Information;

await app.RunAsync();
