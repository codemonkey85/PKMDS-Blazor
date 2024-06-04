namespace Pkmds.Web;

public static class CorsStartup
{
    public const string CorsPolicyName = "AllowLocalhost";

    public static IServiceCollection ConfigureCors(this IServiceCollection services, string[]? allowedOrigins, bool isDevelopment)
    {
        var corsAllowedOrigins = isDevelopment
            ? allowedOrigins ?? []
            :
            [
                "https://localhost:7102",
                "https://pkmds.azurewebsites.net",
                "https://pkmds.app"
            ];

        return services
            .AddCors(options => options.AddPolicy(CorsPolicyName, policy => policy
            .WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));
    }
}
