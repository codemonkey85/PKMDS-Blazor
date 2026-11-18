# Publish trimming audit (.NET 11)

Release already uses `PublishTrimmed=true` and `TrimMode=full`, which trims
application and dependency code as well as framework code. There is no more
aggressive `TrimMode` to select. Further savings require removing dependencies,
disabling unused framework features, or narrowing reflection roots with adequate
coverage of their callers.

## Changes from this audit

- Remove Humanizer and its language resources. Its only app call formatted the
  Mystery Gift sort options `None` and `Pokémon`; explicit labels preserve those
  strings without reflection over enum descriptions or a localization library.
- Set `BlazorWebAssemblyDiagnosticsEnabled=false` in Release. SDK 11.0.100 enables
  framework metrics, EventSource, and HTTP activity propagation by default. This
  app has no OpenTelemetry/metrics consumer. Disabling these paths lets the
  trimmer remove their code; Serilog browser logging and exception stack traces
  remain available. Debug keeps the SDK defaults. To investigate framework
  telemetry, publish with `-p:BlazorWebAssemblyDiagnosticsEnabled=true`.
- Use source-generated `JsonElement` metadata for both success and error JSON
  responses in `BugReportService`, removing its two IL2026 call sites. Dispose
  the HTTP response after reading it. The request and response contracts are
  unchanged; this does not establish that global JSON reflection can be disabled.
- Record language version, trim diagnostics, and runtime feature switches in
  `measure-publish.ps1` reports so future comparisons expose SDK default changes.

## Size observations, November 18, 2026

Three sequential Release publishes used SDK 11.0.100, Mono WASM, SIMD, full
trimming, compression, and no AOT on the same Windows machine. The reference was
commit `2f331a1b`; the next two used the changes above. Application dependencies
were unchanged except for removing Humanizer. Detailed trim warnings were enabled
for every publish. Outputs were written to distinct empty directories under
`measurements/net11-trim-audit-{before,after,diagnostics-off}`.

| Output, bytes | Reference | Labels + JSON metadata | Also disable framework diagnostics |
| --- | ---: | ---: | ---: |
| Recursive `_framework` raw, excluding `.br`/`.gz` | 35,468,927 | 34,592,389 | 34,139,367 |
| Recursive `_framework` Brotli siblings | 7,946,192 | 7,556,525 | 7,411,275 |
| Entire `wwwroot`, including raw/compressed siblings | 73,360,358 | 71,662,568 | 70,884,242 |

The final framework Brotli total is **534,917 bytes smaller (6.7%)**. Removing
Humanizer eliminates 156 raw/compressed framework files, including 51 satellite
assemblies; its Brotli siblings alone occupied 385,486 bytes. Disabling diagnostics
saves another 145,250 framework Brotli bytes in this comparison.

These are bundle/disk totals, not initial-navigation transfer sizes: the recursive
totals include culture-specific and ICU assets, and the PWA may cache more than
the runtime initially requests. Project/native outputs and machine caches were
reused between publishes. This is a size audit, not a clean-build timing or
runtime-throughput comparison. Timestamped assembly metadata can cause small
compressed-size differences between builds.

## Settings retained

| Setting/surface | Decision |
| --- | --- |
| PKM hierarchy roots (`PreservePkmTypes.xml`) | Keep: batch editing, ribbons, and format conversion use reflection. Removing roots needs published feature coverage across formats. |
| Route/layout and JS-invokable roots | Keep: router/interop discovery needs them, including Bank/backup and embedded-host paths. |
| `InvariantGlobalization=false` | Keep culture-sensitive formatting/comparison and international text support; removing ICU changes behavior. |
| `InvariantTimezone=false` | Keep local date/time behavior used by backups and exports; UTC-only behavior would require a separate audit. |
| JSON reflection enabled | Keep for Blazor/dependency interop surfaces beyond the source-generated app serializers. |
| Stack traces | Keep for contained-error reports and troubleshooting. |
| `DebuggerSupport=false`, `MetadataUpdaterSupport=false`, `UseSizeOptimizedLinq=true`, `UseSystemResourceKeys=true` | Already resolved SDK Release defaults; adding the same properties would not shrink this publish. |
| AOT | Still disabled; larger AOT output is a separate speed/size tradeoff. See [the AOT/SIMD evaluation](WASM_AOT_SIMD_EVALUATION.md). |
| C# version | `net11.0` already resolves C# 15.0. Retain the [existing syntax/prototype decisions](DOTNET_11_UPGRADE.md#compatibility-decisions). |

## Repeating the audit

The Blazor SDK suppresses trim-analysis warnings by default, even with full
trimming. A successful normal publish therefore does not imply a clean trim
analysis. To expose detailed warnings:

```powershell
./measure-publish.ps1 -Label trim-audit -MSBuildArgs `
    '-p:SuppressTrimAnalysisWarnings=false','-p:TrimmerSingleWarn=false'
```

Or use `dotnet publish Pkmds.Web/Pkmds.Web.csproj -c Release` with the same two
properties and a separate `-o` directory, as this audit did.

Remaining warnings include PKHeX reflection/batch/localization paths, MudBlazor
expression/delegate reflection, Blazor component/Router discovery, JSInterop,
and Serilog destructuring. They require individual investigation or existing
roots and published feature checks; they are not proof that every path is safe.
The audit adds no broad warning suppression or additional roots.

## Validation

The diagnostics-off publish passed integrity verification for all 3,418
service-worker assets. Fresh isolated Chromium contexts completed the eight
application workloads (description loading, Bank add/import/read, generic ZIP
and Manic load/export) and the .NET AES/MD5 known-answer suite. Allocation/GC
counters remained available. The harness blocks service workers, producing its
existing registration warning; these runs do not verify PWA update behavior.
They use synthetic data and do not touch an existing user profile or submit
reports to the backend. Browser records and audit logs are under gitignored
`measurements/net11-trim-audit-*`.

Solution formatting and the Debug solution build passed. The normal build first
failed with NU1900 because NuGet's vulnerability service was unreachable; the
successful build used `-p:NuGetAudit=false` for that invocation and reported zero
warnings/errors. No repository audit setting changed. No `dotnet test` ran
locally; CI remains responsible for test execution. Bug-report backend submission,
Safari, and existing-profile update behavior were not exercised by this audit.

## UAT deployment follow-up

Independent review of `ed468803` found no actionable code issues. UAT smoke
testing then found an unresolved `%%CACHE_VERSION%%` in the active worker and
its offline-cache name, despite successful deployment stamping logs. The raw
worker had been stamped, but Azure served its stale precompressed sibling.

Both deployment workflows now use `tools/stamp-service-worker.mjs` to stamp the
raw worker and regenerate every existing Brotli/gzip sibling from those exact
bytes. Missing placeholders and unsafe version tokens fail deployment. The
service-worker asset manifest is unchanged: the worker is outside its inventory.
Post-stamp manifest verification and regression cases for compressed, raw-only,
and invalid inputs guard this deployment boundary. No IndexedDB data is removed.

## References

- [Trimming options and feature switches](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options)
- [Blazor .NET 11 trimming improvements](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-11#smaller-blazor-webassembly-publish-output)
- SDK defaults: installed `Microsoft.NET.Sdk.BlazorWebAssembly.Current.targets`
  and `Microsoft.NET.Sdk.BlazorWebAssembly.6_0.targets` under SDK 11.0.100.
