using KristofferStrube.Blazor.FileSystemAccess;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Pkmds.Maui.Services;
using Pkmds.Rcl;
using Pkmds.Rcl.Services;

namespace Pkmds.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        var services = builder.Services;

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

        services.AddMauiBlazorWebView();

#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        services
            .AddMudServices(config =>
            {
                config.SnackbarConfiguration.PreventDuplicates = false;
                config.SnackbarConfiguration.ClearAfterNavigation = true;
            });

        services
            .AddFileSystemAccessService()
            .AddSingleton<IAppState, AppState>()
            .AddSingleton<IRefreshService, RefreshService>()
            .AddSingleton<IAppService, AppService>()
            .AddSingleton<IDragDropService, DragDropService>();

        return builder.Build();
    }
}
