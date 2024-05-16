var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

services
    .AddMudServices()
    .AddFileSystemAccessService()
    .AddScoped<IAppState, AppState>()
    .AddScoped<IRefreshService, RefreshService>()
    .AddScoped<IAppService, AppService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
