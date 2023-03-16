namespace Pkmds.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

        var services = builder.Services;
        services.AddMauiBlazorWebView();

        services
            .AddMudServices()
            .AddSingleton<HttpClient>()
            .AddScoped<IAppState, AppState>()
            .AddSingleton<MainPage>();

#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
