var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddSingleton<IGitHubService, GitHubService>()
    .AddSingleton<IBlobService, BlobService>()
    .AddCors(options => options.AddDefaultPolicy(policy => policy
                .SetIsOriginAllowed(origin =>
                {
                    var host = new Uri(origin).Host;
                    // Production: GitHub Pages
                    // UAT: Azure Static Web Apps preview URLs (PR number changes per PR)
                    // Dev: localhost (any port)
                    return host == "codemonkey85.github.io"
                           || host.EndsWith(".azurestaticapps.net")
                           || host == "localhost";
                })
                .AllowAnyMethod()
                .AllowAnyHeader()));

builder.Build().Run();
