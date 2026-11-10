using System.Text.Json;
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
    public void PublishedServiceWorkerCachesBundledSpritesLazily()
    {
        var serviceWorker = RepoFileTestHelper.ReadAllText("Pkmds.Web", "wwwroot", "service-worker.published.js");
        var exclusions = OfflineAssetsExcludeRegex().Match(serviceWorker);

        exclusions.Success.Should().BeTrue();
        exclusions.Value.Should().Contain("^_content\\/Pkmds\\.Rcl\\/sprites\\/");
        serviceWorker.Should().Contain("const bundledSpritePathPrefix = new URL('_content/Pkmds.Rcl/sprites/', baseUrl).href;");
        serviceWorker.Should().Contain("cache = await caches.open(isHostedSprite ? spriteCacheName : cacheName);");
    }

    [Fact]
    public void PublishedServiceWorkerUsesTheAppsHostedSpriteCollection()
    {
        var serviceWorker = RepoFileTestHelper.ReadAllText("Pkmds.Web", "wwwroot", "service-worker.published.js");

        serviceWorker.Should().Contain($"const spritePathPrefix = '{SpriteSource.PokemonBaseUrl}';");
    }

    [Fact]
    public void PublishedSpriteManifestMatchesAppAndCoversEveryMappedGameSet()
    {
        using var manifest = JsonDocument.Parse(RepoFileTestHelper.ReadAllText("tools", "data", "sprite-collection.json"));
        var root = manifest.RootElement;
        var publicBaseUrl = root.GetProperty("publicBaseUrl").GetString()!;
        var prefix = root.GetProperty("prefix").GetString()!;
        var images = root.GetProperty("objects").EnumerateArray().ToArray();
        var urls = images.Select(item => publicBaseUrl + item.GetProperty("key").GetString()![prefix.Length..])
            .ToHashSet();

        (publicBaseUrl + "pokemon/").Should().Be(SpriteSource.PokemonBaseUrl);
        prefix.Should().Be($"collections/{root.GetProperty("revision").GetString()}/");
        images.Length.Should().Be(root.GetProperty("objectCount").GetInt32());
        images.Sum(item => item.GetProperty("bytes").GetInt64()).Should().Be(root.GetProperty("totalBytes").GetInt64());
        urls.Should().Contain(SpriteSource.PlaceholderSpriteUrl);
        urls.Should().Contain(PokeApiSpriteUrls.GetPokeApiHomeSpriteUrl((ushort)Species.IronMoth)!);

        foreach (var version in Enum.GetValues<GameVersion>())
        {
            var url = PokeApiSpriteUrls.GetPokeApiVersionSpriteUrl((ushort)Species.Pikachu, version: version);
            if (url is not null)
            {
                urls.Should().Contain(url, $"the published collection must cover the {version} game mapping");
            }
        }
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
            workflowContents.Should().Contain("node tools/verify-service-worker-assets.mjs release/wwwroot");
            workflowContents.Should().Contain("node tools/sprites/verify-published.mjs");
        }
    }

    [Fact]
    public void TailwindBuildPrecedesCurrentProjectStaticAssetHashing()
    {
        var targets = RepoFileTestHelper.ReadAllText("Pkmds.Rcl", "Tailwind.targets");

        targets.Should().Contain("BeforeTargets=\"ResolveProjectStaticWebAssets;BeforeCompile;ResolveCurrentProjectStaticWebAssetsInputs;ResolveStaticWebAssetsInputs\"");
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
        appScript.Should().Contain("if (!shouldRepairServiceWorkerRegistration(err, registration))");
        appScript.Should().Contain("error.message.includes('Not found')");
        appScript.Should().Contain("window._swRegistrationPromise = Promise.resolve(registration)");
        appScript.Should().Contain("const currentRegistration = await window._swRegistrationPromise");
        RepairedRegistrationReloadRegex().IsMatch(appScript).Should().BeTrue();
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

    [GeneratedRegex(@"catch \(err\)\s*\{[\s\S]+?if \(!shouldRepairServiceWorkerRegistration\(err, registration\)\)[\s\S]+?registration = await navigator\.serviceWorker\.register\([\s\S]+?registration\.addEventListener\('updatefound', signalUpdateFound\);", RegexOptions.CultureInvariant)]
    private static partial Regex MissingNewestWorkerRepairRegex();

    [GeneratedRegex(@"if \(repairedRegistration\)\s*\{\s*notifyUpdateAvailable\(\);\s*return 'found';\s*\}[\s\S]+?if \(navigator\.serviceWorker\.controller \|\| repairedRegistration\)", RegexOptions.CultureInvariant)]
    private static partial Regex RepairedRegistrationReloadRegex();
}
