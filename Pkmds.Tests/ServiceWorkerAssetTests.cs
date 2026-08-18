using System.Text.RegularExpressions;

namespace Pkmds.Tests;

public sealed partial class ServiceWorkerAssetTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Pkmds.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static string ReadRepoFile(params string[] pathSegments) =>
        File.ReadAllText(Path.Combine([RepoRoot, .. pathSegments]));

    [Fact]
    public void PublishedServiceWorkerPreCachesNativeRuntime()
    {
        var serviceWorker = ReadRepoFile("Pkmds.Web", "wwwroot", "service-worker.published.js");
        var exclusions = OfflineAssetsExcludeRegex().Match(serviceWorker);

        exclusions.Success.Should().BeTrue();
        exclusions.Value.Should().NotContain("dotnet\\.native");
    }

    [Fact]
    public void PublishedServiceWorkerWaitsForExplicitUpdateHandoff()
    {
        var serviceWorker = ReadRepoFile("Pkmds.Web", "wwwroot", "service-worker.published.js");
        var installHandler = InstallHandlerRegex().Match(serviceWorker);

        installHandler.Success.Should().BeTrue();
        installHandler.Value.Should().NotContain("skipWaiting");
        serviceWorker.Should().Contain("event.waitUntil(self.skipWaiting())");
    }

    [Fact]
    public void AutomaticCacheRecoveryDoesNotDeleteIndexedDb()
    {
        var cacheScript = ReadRepoFile("Pkmds.Rcl", "wwwroot", "js", "appCache.js");

        cacheScript.Should().Contain("caches.delete");
        cacheScript.Should().Contain("r.unregister()");
        cacheScript.Should().NotContain("indexedDB.deleteDatabase");
    }

    [Fact]
    public void WaitingServiceWorkerUpdateIsPreservedAndDispatched()
    {
        var appScript = ReadRepoFile("Pkmds.Web", "wwwroot", "js", "app.js");

        appScript.Should().Contain("""
            function notifyUpdateAvailable() {
                window._pkmdsUpdateWaiting = true;
                window.dispatchEvent(new CustomEvent('updateAvailable'));
            }
            """);
        appScript.Should().Contain("""
            if (registration.waiting && navigator.serviceWorker.controller) {
                notifyUpdateAvailable();
            }
            """);
        appScript.Should().Contain("if (window._pkmdsUpdateWaiting)");
    }

    [GeneratedRegex(@"const offlineAssetsExclude = \[[^;]+;", RegexOptions.CultureInvariant)]
    private static partial Regex OfflineAssetsExcludeRegex();

    [GeneratedRegex(@"self\.addEventListener\('install',[\s\S]+?\n}\);", RegexOptions.CultureInvariant)]
    private static partial Regex InstallHandlerRegex();
}
