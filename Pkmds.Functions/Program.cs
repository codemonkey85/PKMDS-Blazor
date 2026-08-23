var builder = WebApplication.CreateBuilder(args);

builder.Services
    // Serves requests via Lambda Function URLs (and API Gateway HTTP APIs, which share the same
    // payload format) when running in Lambda; falls back to Kestrel for local dev.
    .AddAWSLambdaHosting(LambdaEventSource.HttpApi)
    .AddSingleton<IAmazonS3, AmazonS3Client>()
    .AddSingleton<IGitHubService, GitHubService>()
    .AddSingleton<IStorageService, S3StorageService>()
    .AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy
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
                .AllowAnyHeader();
        });
    });

var app = builder.Build();

app.UseCors();

// Routes keep the "/api" prefix used by the old Azure Functions HTTP triggers so the frontend
// and the GitHub webhook configuration only need their base URL updated, not their paths.
app.MapPost("/api/SubmitBugReport", SubmitBugReport.Run);
app.MapPost("/api/GitHubWebhook", GitHubWebhook.Run);

app.Run();
