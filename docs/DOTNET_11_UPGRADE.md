# .NET 11 migration (#1374)

## Configuration

The app, tests, shared preview library, and desktop/native preview consumers now
target .NET 11. `global.json` selects stable SDK 11.0.100 and keeps prerelease SDKs
disabled and the Microsoft.Testing.Platform runner. Install `wasm-tools` for this
SDK even if the workload was already installed for .NET 10.

WebAssembly and JSInterop central package versions are 11.0.0. Gateway replaces
DevServer, with `StaticWebAssetSpaFallbackEnabled` enabled for direct navigation
to `/bench` and other app routes. Both watch scripts continue to use `dotnet
watch`; launch URLs are unchanged. Production still publishes static files.

Functions also inherits `net11.0`. The deployed `pkmds-functions` app is now
configured for **.NET 11 Isolated**, as confirmed by the deployment preflight
and subsequent read-only Azure inspection. The agent did not change that setting.
Both deployment workflows check the Windows Function App's `netFrameworkVersion`
before ZIP deployment and stop if it is not `v11.0`. The UAT frontend job remains
independent, so a runtime mismatch blocks backend replacement without blocking
the static preview publish.

Both workflows publish Functions with `-r win-x64 --self-contained true`. A
successful ZIP deployment alone did not establish worker health: UAT report
submission returned HTTP 500, and Application Insights showed worker startup
failing because the Windows instance had `11.0.0-rc.3.26522.148`, but lacked the
required stable `Microsoft.NETCore.App` 11.0.0. The portal's v11.0 selection did
not guarantee stable runtime availability on that instance.

The self-contained package includes stable .NET/ASP.NET Core 11.0.0 and its own
Windows executable; `worker.config.json` launches that executable rather than
the instance's `dotnet.exe`. This increases deployment size and makes runtime
servicing dependent on republishing with updated runtime packs. After deploying,
`tools/verify-functions-health.mjs` sends an empty, non-form request and requires
the endpoint's expected HTTP 400 validation response. It exercises worker startup,
DI, and HTTP binding without creating a GitHub report or uploading user content.
The probe retries for cold start and fails deployment validation on worker errors.

Functions uses `Azure.Functions.Sdk` 1.0.1, pinned through `global.json` so both
the main project and generated extension project can resolve it. This replaces
`Microsoft.NET.Sdk` and the `Microsoft.Azure.Functions.Worker.Sdk` package. The
existing explicit Worker 2.52.0 and HTTP extension 2.1.1 references remain;
the project SDK supplies the output type and Functions v4 defaults.
`Directory.Solution.targets` runs the documented extension-generation hook
after solution restore, avoiding AZFW0108 and restore-during-build fallback.

UAT and production deploy the same backend. Preserve production's backend-first
deployment gate and the existing request/response contract. Keep a coordinated
stack/package rollback available; changing the runtime selection alone is not
a deployment or a compatibility test.

Build/Test, CodeQL, UAT, and production already select the SDK from `global.json`.
Build/Test now performs a trimmed Release publish and service-worker manifest
verification, in addition to the existing Functions build and CI tests.

## Compatibility decisions

The resolved Web dependency graph contains Components, WebAssembly, JSInterop,
and JSInterop.WebAssembly 11.0.0. PKHeX.Core remains 26.10.18, MudBlazor 9.12.0,
bUnit 2.15.2, and FileSystemAccess 4.0.0. Their successful compilation does not
establish every browser or native-platform behavior. Dependencies remain at
their existing versions to keep this migration focused.

The SDK continues to select Mono for browser WASM. AOT and SIMD remain disabled;
full trimming and the existing reflection/JS-invokable roots remain enabled.
No Runtime Async or CoreCLR-specific optimization is enabled in shared projects.

New BL0016 diagnostics exposed unguarded interop in Web. The crypto bridge now
translates `JSException` into `CryptographicException`, preserving the original
exception and aborting the operation. It must never substitute empty encrypted
data. Failure to register the optional update listener is logged without
preventing application startup. No actionable BL0010 diagnostics appeared.

.NET 11 validates ZIP CRC32 on entry reads. Generic and Manic readers already
catch `InvalidDataException`/`IOException`; Bank's import boundary reports read
errors before adding parsed Pokémon. `ArchiveIntegrityTests` adds valid,
bad-CRC, and truncated ZIP cases for both save readers. The Manic writer,
compression/header conventions, owned save buffers, and expandable output
streams are unchanged. IndexedDB schemas and serialization formats are unchanged.

| Feature | Affected code | Benefit and compatibility cost | Decision |
| --- | --- | --- | --- |
| Gateway | Web, launch profiles, watch scripts | Supported standalone host; direct-route and framework-asset HTTP checks pass. IDE debugger attachment still needs verification. | Adopt |
| JS interop diagnostics | `JsService`, `App.OnAfterRenderAsync` | Explicit crypto failure and resilient notification setup; no ignored results changed blindly. | Adopt BL0016 handling |
| ZIP integrity | Generic/Manic readers, Bank import | Reject invalid checksums; malformed archive paths must fail without writing user records. | Adopt runtime behavior; add regression coverage |
| Union types/closed cases | `LegalizationResult`, legalization call sites | Case-specific payloads could improve correctness, but require a separate prototype preserving best-effort Pokémon, change summaries, timeout, and HaX warnings. No allocation benefit established. | Defer prototype |
| Collection expression arguments | `AppService` encounter set, `DescriptionService` index | Existing constructors expose reference-identity/ordinal comparers and capacity clearly; shorter syntax has no measured benefit. | Retain constructors |
| Labeled break/continue | Nested storage/legalization loops | Existing early returns and loops do not justify new control flow in the migration. | Defer |
| Extension indexers/other C# 15 syntax | Shared helpers | No concrete use outweighs extra API/call-site review; memory-safety previews are outside scope. | Defer |
| Read-only stream adapters | `SaveArchiveHelper`, `ManicEmuSaveHelper` | Existing array-backed `MemoryStream` readers already avoid copying input; snapshots and expandable writers remain necessary. | Retain current streams |
| JSON/Base64 APIs | Description, Bank, Backup, embedded host | Existing source generation and trim-safe `JsonElement` interop preserve persisted formats and stale-JS compatibility. No measured benefit from replacement. | Defer |
| Virtualization | Pokédex grid; Bank/search/encounter tables | Only the Pokédex currently opts into virtualization. Variable-height behavior and increased framework overscan need UI/memory checks; pagination does not become virtualization automatically. | Retain UI; defer larger changes |
| File-based `#:include` | Data generators/diagnostics | No generator refactor needed for retargeting; generated output must stay stable. | Defer helper extraction |
| Runtime Async | Functions/tools | Requires a compatible CoreCLR host and separate cancellation/exception measurements; inappropriate as a global Mono WASM switch. | Defer |
| CoreCLR-on-WASM, AOT, SIMD | Web publish settings | Runtime support, startup, download size, and Safari support need separate evidence. | Retain current settings |

## Local validation, November 12, 2026

- `dotnet format --no-restore` completed successfully.
- `dotnet build -c Debug` passed for the solution with zero warnings/errors,
  including the test assembly and the .NET 11 Functions project.
- The Windows preview worker and shared Preview/Core projects passed Debug build.
- Web Release publish with full trimming passed; manifest verification checked
  3,469 service-worker assets. Functions Release publish also passed.
- The .NET 11 Functions publish started under local Core Tools 4.0.7317 (host
  4.1038.300.25164). The worker initialized and discovered `GitHubWebhook` and
  `SubmitBugReport`. It ran from the published directory without user secrets;
  no endpoints were invoked and no GitHub/blob operations were performed.
- Gateway served the app, `/bench` fallback, and framework JavaScript. Chromium
  loaded the UI. Direct JavaScript AES roundtrip and the empty-input MD5 vector
  passed. These checks do not replace save-operation smoke tests through .NET.
- All five published-browser benchmark workloads completed. Tests were compiled
  but **not run locally**, per repository policy; their execution remains in CI.

The macOS NativeAOT project restore could not retrieve `Microsoft.DotNet.ILCompiler`
because the local NuGet connection failed TLS authentication. Actual macOS/iOS
builds, Xcode compatibility, and IDE debugging remain platform validation gates.
The platform consumers and hardcoded output paths were retargeted together with
Core, rather than leaving incompatible .NET 10 consumers of a .NET 11 library.

### Initial performance observations

The unchanged source at `1ce1b2fe90eb0285d9b5615e7fcce16fbf7034dd` was exported
to a separate directory and published with SDK 10.0.403 / Mono 10.0.14. The
migration was published with SDK 11.0.100 / Mono 11.0.0. Both retained full trim,
no AOT, no SIMD, and the same application dependency versions. The same harness
and Windows headless Chromium 156.0.8078.4 exercised the synthetic workloads
with five warm-up iterations and fresh browser contexts.

| Measurement | .NET 10 | .NET 11 |
| --- | ---: | ---: |
| Entire output directory, bytes (includes compressed variants) | 69,825,174 | 72,859,967 |
| `_framework` Brotli files, bytes | 7,327,298 | 7,877,892 |
| Legality mean, ms/iteration, repeated-run range | 3.986–4.006 | 3.858–4.019 |
| Encryption mean, ms/iteration, repeated-run range | 0.052–0.055 | 0.052–0.054 |
| Search mean, ms/iteration, repeated-run range | 0.886–0.896 | 0.873–0.896 |
| Encounter generation mean, ms/iteration, repeated-run range | 4.010–4.150 | 4.090–4.120 |
| Navigation through completed benchmark, ms | 1,821–1,845 | 1,851–1,934 |

Three interleaved reference runs and three migration runs produced these ranges.
An earlier migration run took 5,250 ms and had substantially slower workloads;
it is reported separately because it ran during other build activity. Batch
lookup rounded to 0.004 ms/iteration in the repeated runs and is too short for
a useful comparison. Navigation timing includes the benchmark; it is not an
isolated startup measurement. Framework Brotli size increased by 550,594 bytes
(about 7.5%). These initial observations establish no meaningful throughput
improvement. Raw local reports are in gitignored `measurements/net10.md` and
`measurements/net11.md`.

These publishes were interrupted/retried and run alongside other work, so cold
publish-time comparisons are unavailable. Repeat `measure-publish.ps1` on an idle
machine before making build-time claims. Allocation/GC, peak memory, isolated
startup, large-list responsiveness, description loading, bulk Bank import, and
archive throughput are not measured by these runs.

## Encounter-search and contained-error follow-up

The maintainer found an `ArgumentOutOfRangeException` searching Pikachu in a
Violet save. PKHeX.Core 26.10.18 includes transfer-only version IDs with
`EntityContext.None` in its modern game range, but its encounter search cannot
build evolution chains for those IDs. The app now filters that explicit version
list before generation, while retaining trainer optimization and compatible
historical encounters. Newer PKHeX source guards its memory overload, but the
shipped package still requires this filter. Regression cases cover modern-save
all-game/version-specific searches and unsupported versions.

The local Violet UI search returned 1,346 compatible encounters, or 561 with
Violet selected. Tab error panels now offer **Report this error**, opening the
existing report dialog with the original exception, stack trace, and tab name
prefilled. Users review the report before submitting; attachments remain opt-in.
Opening and cancelling the dialog were checked locally without sending a report.
A component regression test verifies exception/context forwarding and no automatic
submission. Tests compile locally and run only in CI.

## Remaining release gates

- CI tests (including archive-integrity cases), CodeQL, and UAT deployment/load.
- Broader save-generation coverage, legalization outcome presentation, explicit
  AES/MD5 vectors through the .NET bridge, and embedded-host interop.
- Pokédex variable-height scrolling, rendered rows, memory, selection/drag, and
  Safari compatibility; IDE debugging and local-PKHeX watch.
- Controlled cached/new frontend contract checks against the shared Functions
  backend, coordinated Azure .NET 11 stack selection/deployment, and macOS/iOS
  native builds.

### Published-app smoke checks after fixture approval

The maintainer approved the fixture upload after automatic review initially
blocked it. Tests used `TestFiles/moon.sav` and its locally generated derivatives,
with the .NET 10 and .NET 11 publishes served successively at the same isolated
loopback origin. No production browser storage was cleared or modified.

- The .NET 10 app loaded the fixture, created one automatic backup, and added six
  party Pokémon to Bank through the UI.
- The existing page detected the .NET 11 worker and displayed the update banner.
  Clicking Reload activated the new worker and booted the upgraded app. SHA-256
  hashes of the complete original backup record and six Bank records matched
  before/after the upgrade, including metadata and stored Pokémon/save bytes.
- The upgraded Backup Manager displayed and restored the pre-upgrade backup.
  Bank displayed all six original Pokémon. Bank JSON export/import added another
  six records while the original six retained their exact record hash.
- Legality analysis completed for all six party Pokémon: five legal, no warnings,
  and Clefable illegal with an unmatched-origin-encounter message.
- Changing trainer money from 7,680 to 12,345, exporting the raw save, and reloading
  that export preserved the edit and yielded a valid SAV7SM.
- Generic ZIP and Manic `.3ds.sav` wrappers both loaded, accepted a money edit to
  23,456, exported, and reloaded with the edit intact. Each retained its unrelated
  `metadata.txt` entry unchanged. The generic filename and Manic compound suffix
  were preserved (Manic adds its normal timestamp).
- A ZIP with unchanged save bytes but incorrect header CRC32 values was rejected
  with the normal invalid-save dialog, without loading a save.

Exports exercised the app's real browser-download fallback with the native save
picker disabled in the test page; native picker dialogs were not automated.
Some offscreen-tab interactions required dispatching a click after Playwright
scroll/hit-testing timeouts, so these functional checks do not establish tab
layout/scrolling correctness. They also do not establish Safari compatibility or
all-generation crypto coverage. Local exports and the temporary loopback server
script are under gitignored `measurements/`.

No backend reports or messages were submitted. This change does not deploy or
claim to close all acceptance criteria.

## References

- [Issue and original investigation](https://github.com/codemonkey85/PKMDS-Blazor/issues/1374)
- [ASP.NET Core 10 to 11 migration](https://learn.microsoft.com/en-us/aspnet/core/migration/100-to-110?view=aspnetcore-11.0)
- [Blazor 11 and Gateway](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-11?view=aspnetcore-11.0)
- [.NET 11 compatibility changes](https://learn.microsoft.com/en-us/dotnet/core/compatibility/11)
- [ZIP CRC32 validation](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/11/ziparchive-entry-crc32-validation)
- [Azure Functions isolated worker](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#net-11)
- [Solution restore hook (AZFW0108)](https://learn.microsoft.com/en-us/azure/azure-functions/errors-diagnostics/msbuild-sdk-rules/azfw0108)
