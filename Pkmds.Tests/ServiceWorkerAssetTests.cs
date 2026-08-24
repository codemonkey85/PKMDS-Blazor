using System.Text.RegularExpressions;

namespace Pkmds.Tests;

public sealed partial class ServiceWorkerAssetTests
{
    [Fact]
    public void PublishedServiceWorkerPreCachesNativeRuntime()
    {
        var serviceWorker = RepoFileTestHelper.ReadAllText("Pkmds.Web", "wwwroot", "service-worker.published.js");
        var exclusions = OfflineAssetsExcludeRegex().Match(serviceWorker);

        exclusions.Success.Should().BeTrue();
        exclusions.Value.Should().NotContain("dotnet\\.native");
    }

    [Fact]
    public void PublishedServiceWorkerWaitsUnlessLegacyCacheCannotBoot()
    {
        var serviceWorker = RepoFileTestHelper.ReadAllText("Pkmds.Web", "wwwroot", "service-worker.published.js");
        var installHandler = InstallHandlerRegex().Match(serviceWorker);

        installHandler.Success.Should().BeTrue();
        LegacyRecoveryInstallRegex().IsMatch(installHandler.Value).Should().BeTrue();
        LiveLegacyClientGuardRegex().IsMatch(serviceWorker).Should().BeTrue();
        LegacyRecoveryClientSafetyRegex().IsMatch(serviceWorker).Should().BeTrue();
        NavigationCacheCleanupRegex().IsMatch(serviceWorker).Should().BeTrue();
        serviceWorker.Should().Contain("event.waitUntil(self.skipWaiting())");
    }

    [Fact]
    public void DeploymentWorkflowsStampTheDeployedServiceWorker()
    {
        foreach (var workflow in new[] { "main.yml", "uat.yml" })
        {
            var workflowContents = RepoFileTestHelper.ReadAllText(".github", "workflows", workflow);

            workflowContents.Should().Contain("files: 'release/wwwroot/service-worker.js'");
            workflowContents.Should().NotContain("files: 'release/wwwroot/service-worker.published.js'");
        }
    }

    [Fact]
    public void AutomaticCacheRecoveryDoesNotDeleteIndexedDb()
    {
        var cacheScript = RepoFileTestHelper.ReadAllText("Pkmds.Rcl", "wwwroot", "js", "appCache.js");

        cacheScript.Should().Contain("caches.delete");
        cacheScript.Should().Contain("r.unregister()");
        cacheScript.Should().NotContain("indexedDB.deleteDatabase");
    }

    [Fact]
    public void WaitingServiceWorkerUpdateIsPreservedAndDispatched()
    {
        var appScript = RepoFileTestHelper.ReadAllText("Pkmds.Web", "wwwroot", "js", "app.js");

        appScript.Should().Contain("""
            function notifyUpdateAvailable() {
                window._pkmdsUpdateWaiting = true;
                window.dispatchEvent(new CustomEvent('updateAvailable'));
            }
            """);
        WaitingRegistrationNotificationRegex().IsMatch(appScript).Should().BeTrue();
        appScript.Should().Contain("if (window._pkmdsUpdateWaiting)");
    }

    [Fact]
    public void ManualUpdateCheckRepairsMissingNewestWorkerRegistration()
    {
        var appScript = RepoFileTestHelper.ReadAllText("Pkmds.Web", "wwwroot", "js", "app.js");

        MissingNewestWorkerRepairRegex().IsMatch(appScript).Should().BeTrue();
        appScript.Should().Contain("if (!isBenignServiceWorkerUpdateError(err))");
        appScript.Should().Contain("window._swRegistrationPromise = Promise.resolve(registration)");
    }

    [GeneratedRegex(@"const offlineAssetsExclude = \[[^;]+;", RegexOptions.CultureInvariant)]
    private static partial Regex OfflineAssetsExcludeRegex();

    [GeneratedRegex(@"self\.addEventListener\('install',[\s\S]+?\n}\);", RegexOptions.CultureInvariant)]
    private static partial Regex InstallHandlerRegex();

    [GeneratedRegex(@"if \(await hasLegacyCacheWithoutNativeRuntime\(\)\)\s*\{[\s\S]+?await self\.skipWaiting\(\);\s*}", RegexOptions.CultureInvariant)]
    private static partial Regex LegacyRecoveryInstallRegex();

    [GeneratedRegex(@"const \{hasUnleasedClients} = await getLiveClientCacheLeases\(\);\s*if \(!hasUnleasedClients\)\s*\{\s*return false;\s*}", RegexOptions.CultureInvariant)]
    private static partial Regex LiveLegacyClientGuardRegex();

    [GeneratedRegex(@"if \(isLegacyRecovery\)\s*\{[\s\S]+?await currentCache\.delete\(legacyRecoveryMarkerUrl\);\s*return false;\s*}", RegexOptions.CultureInvariant)]
    private static partial Regex LegacyRecoveryClientSafetyRegex();

    [GeneratedRegex(@"const isNavigation = event\.request\.mode === 'navigate';[\s\S]+?if \(isNavigation\)\s*\{\s*await pruneUnusedAppCaches\(\);\s*}", RegexOptions.CultureInvariant)]
    private static partial Regex NavigationCacheCleanupRegex();

    [GeneratedRegex(@"if \(registration\.waiting && navigator\.serviceWorker\.controller\)\s*\{\s*notifyUpdateAvailable\(\);\s*\}", RegexOptions.CultureInvariant)]
    private static partial Regex WaitingRegistrationNotificationRegex();

    [GeneratedRegex(@"catch \(err\)\s*\{\s*if \(!isBenignServiceWorkerUpdateError\(err\)\)[\s\S]+?registration = await navigator\.serviceWorker\.register\([\s\S]+?registration\.addEventListener\('updatefound', signalUpdateFound\);", RegexOptions.CultureInvariant)]
    private static partial Regex MissingNewestWorkerRepairRegex();
}
