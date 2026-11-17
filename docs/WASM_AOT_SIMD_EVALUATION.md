# Independent .NET 11 AOT and SIMD evaluation (#883)

Measured November 16, 2026. Related: [#883](https://github.com/codemonkey85/PKMDS-Blazor/issues/883),
[#1387](https://github.com/codemonkey85/PKMDS-Blazor/issues/1387), and
[#1390](https://github.com/codemonkey85/PKMDS-Blazor/issues/1390).
This evaluates Mono configurations; CoreCLR is a separate investigation.

**Decision: enable SIMD, keep AOT disabled.** The small SIMD payload increase and
repeatable Bank improvements justify focused adoption with capability checks.
It does not raise the documented browser feature floor beyond the final exception
handling already used by this .NET 11 Mono baseline. Broad hot-path speedups are
not established. AOT has substantial download/build costs and remains a separate
decision. See [browser support](BROWSER_SUPPORT.md) for the current policy.

The measurements below are dated observations of the fixed source revision, not
fresh benchmarks of every subsequent dependency update. The rollout PR is based
on `397aee4d` (MudBlazor 9.14.0); the matched matrices used `47195a1f`
(MudBlazor 9.13.0). Do not silently reinterpret those matrices as measurements
of the newer dependency graph. Final-build validation is recorded separately.

## Windows measurement method

- Fixed detached source checkout: `47195a1f4053da794f6d1d6c8730e34fc8da2afe`.
  SDK 11.0.100, Mono/WASM 11.0.0, Emscripten 6.0.2, workload set 11.0.100.1.
  PKHeX.Core 26.10.18, MudBlazor 9.13.0. All four configurations retain identical
  resolved package versions, full trimming, compression, and Mono.
- Explicit `RunAOTCompilation` and `WasmEnableSIMD` command-line properties;
  `UseMonoRuntime=true`, fixed `Version=2026.11.16.883` and
  `SourceRevisionId=47195a1f`. Application source and datasets stay fixed.
  Measurement/browser collector changes live outside the isolated source checkout.
  The first publish regenerated tracked Tailwind CSS; later source-status records
  show only that generated-file change. Final published Tailwind, description
  datasets, Bank JavaScript, and backup JavaScript hashes match across all builds.
- Three sequential clean publishes per configuration. Order: none/SIMD/AOT/both,
  both/AOT/SIMD/none, SIMD/none/both/AOT. Release Web/RCL/Core outputs and the
  chosen publish directory are removed before each publish. Restore is primed;
  the existing Tailwind CLI is copied before timing. SDK/NuGet/build-server/OS
  caches stay warm. Implicit restore remains timed. These are clean project
  output measurements, not cold-machine measurements.
- Browser measurements follow all publishes, using the same harness installation,
  with one invocation per configuration/suite in each of three interleaved rounds.
  Both the five hot paths and eight application workloads retain their existing
  warm-up/iteration counts. Every navigation uses a fresh nonpersistent context,
  blocked service workers, 1280×720 viewport, and no-store Brotli loopback HTTP.
  Each invocation starts a browser process. OS/filesystem caches stay warm.
- Application fixtures are deterministic and checked for matching SHA-256 values.
  Their validators check every Bank row and archive's edited bytes and metadata.
  Hot-path encounter generation can vary Pokémon properties; its inputs are
  representative matched configurations, not byte-identical generated encounters.
  No user save or browser profile is used.

Commands and timing definitions are in the [harness README](../tools/bench/README.md).
The evaluation-specific drivers, fault-injection probes, and narrowly guarded
stream-cancellation handling were retained with the handoff. They are diagnostic
overlays, not a claim that the default repository runner includes every probe.
The first attempted publish in the working checkout encountered a locked Release
output; it produced no timing result. All reported publishes use the isolated
checkout. Development servers remain open on this desktop; their presence and
normal OS activity limit how far build-time differences can be generalized.

## Windows clean publish results

| Measurement | No AOT/SIMD | SIMD only | AOT only | AOT + SIMD |
| --- | ---: | ---: | ---: | ---: |
| Publish run 1, seconds | 58.96 | 50.57 | 241.43 | 242.05 |
| Publish run 2, seconds | 46.60 | 46.43 | 224.38 | 262.95 |
| Publish run 3, seconds | 45.67 | 46.30 | 221.68 | 236.81 |
| Median publish, seconds | 46.60 | 46.43 | 224.38 | 242.05 |
| Framework Brotli bytes, all three runs | 7895841 | 7933208 | 14748868 | 15065106 |
| Native WASM raw bytes, final publish | 3065233 | 3077078 | 47216974 | 49174166 |
| Native WASM Brotli bytes, final publish | 961444 | 964595 | 8852799 | 9139890 |

Framework totals include recursive framework assets/cultures. They are not startup
transfer sizes. Total `wwwroot` includes compressed siblings and is disk usage;
exact values and native hashes remain in raw reports.

## Windows Chromium throughput and fresh-context startup

Machine: Windows NT 10.0.26200, AMD Ryzen 7 9800X3D,
16 logical processors, 61.6 GiB RAM.
Node v24.14.0; Chromium 156.0.8078.4. Each workload cell
is the range of per-invocation means across three invocations.

| Measurement | No AOT/SIMD | SIMD only | AOT only | AOT + SIMD |
| --- | ---: | ---: | ---: | ---: |
| Welcome DOM, hot-path invocations, ms | 609.7–617.0 | 611.2–624.2 | 727.9–751.7 | 760.0–770.0 |
| Local startup encoded-body bytes at welcome visibility | 7237499 | 7274866 | 14090526 | 14406764 |
| legality, ms/iteration | 3.7740–3.9840 | 3.8530–4.0180 | 0.5070–0.5420 | 0.4940–0.5260 |
| encryption, ms/iteration | 0.0500–0.0531 | 0.0523–0.0538 | 0.0138–0.0141 | 0.0136–0.0142 |
| search, ms/iteration | 0.8550–0.9245 | 0.8610–0.8890 | 0.3100–0.3165 | 0.3085–0.3130 |
| encounter-generation, ms/iteration | 3.9600–4.2300 | 3.9600–4.1400 | 4.5300–4.6800 | 4.5000–4.6500 |
| batch-lookup, ms/iteration | 0.0035–0.0040 | 0.0036–0.0042 | 0.0007–0.0009 | 0.0008–0.0010 |

## Windows application boundaries

| Measurement | No AOT/SIMD | SIMD only | AOT only | AOT + SIMD |
| --- | ---: | ---: | ---: | ---: |
| description-first-load, ms/iteration | 826.467–828.167 | 823.567–828.533 | 823.467–829.300 | 817.767–822.033 |
| bank-add-range, ms/iteration | 50.667–50.933 | 44.233–45.467 | 31.633–31.967 | 31.500–31.767 |
| bank-json-import, ms/iteration | 27.667–28.800 | 26.700–27.300 | 27.467–27.700 | 27.133–27.333 |
| bank-read, ms/iteration | 221.433–225.400 | 186.133–189.467 | 103.900–106.400 | 101.033–103.067 |
| generic-zip-load, ms/iteration | 0.800–0.840 | 0.720–1.300 | 0.260–0.280 | 0.280–0.340 |
| generic-zip-export, ms/iteration | 0.920–0.980 | 0.940–1.000 | 1.160–1.200 | 0.740–0.820 |
| manic-load, ms/iteration | 0.680–0.960 | 0.620–1.000 | 0.120–0.200 | 0.140–0.400 |
| manic-export, ms/iteration | 0.340–0.580 | 0.320–0.360 | 0.300–0.420 | 0.180–0.380 |

All completed application fixture dictionaries match across configurations,
repetitions, and browsers. The generated fixtures and operation boundaries remain
those in the application baseline. Managed allocation/collection/heap/timer
diagnostics are retained in each raw report; they are not total peak memory or
a measurement of real UI responsiveness.

## Additional Windows engine correctness checks

### firefox 157.0 on win32 10.0.26200

| Measurement | No AOT/SIMD | SIMD only | AOT only | AOT + SIMD |
| --- | ---: | ---: | ---: | ---: |
| hotpaths | Passed | Passed | Passed | Passed |
| application | Passed | Passed | Passed | Passed |

### webkit 27.2 on win32 10.0.26200

| Measurement | No AOT/SIMD | SIMD only | AOT only | AOT + SIMD |
| --- | ---: | ---: | ---: | ---: |
| hotpaths | Passed | Passed | Passed | Passed |
| application | Timeout (90 s) | Timeout (90 s) | Timeout (90 s) | Timeout (90 s) |

These are correctness smoke samples, not repeated performance comparisons for
these engines. Windows WebKit is not Safari/iOS; branded Chrome/Edge and physical
Apple/mobile devices remain separate checks.

Raw local evidence: `measurements/eval883-net11-{none,simd,aot,both}-{1,2,3}.{log,publish.json,md}`,
`eval883-verified-{chromium,firefox,webkit}-{none,simd,aot,both}-{hotpaths,application}.runtime.jsonl`,
and `eval883-summary.json`. Failed browser invocations retain raw errors.
The Windows report and source/harness handoff were preserved for the Mac continuation;
these paths identify the original Windows artifacts, not files included in this repository.


## Mac and iOS Simulator continuation

These are observations from the available Mac, not compatibility guarantees for
other hardware. Environment: Apple M5 Max, arm64, 36 GiB RAM; macOS 27.3
(26C5047e), Safari 27.3 (22626.1.17), Xcode 27.2 (27B52); disposable iPhone 18 Pro
Simulator running iOS 27.3 (24C5037i). SDK 11.0.100, workload set 11.0.100.1,
Mono 11.0.0, Node 26.0.0, portable PowerShell 7.7.0. The Simulator user agent
reported iPhone OS 18_7 despite that installed runtime: use the recorded runtime,
not only the user agent, to identify a test environment.

The original temporary checkout had been removed between sessions. The continuation
reconstructed source `47195a1f` plus the verified handoff patch/overlay in an isolated
clone; all 134 bundle SHA-256 values matched. Source/dependencies, full trim,
fixtures, fixed version/revision, three clean-output publish rounds, and interleaved
orders match the Windows protocol. SDK/NuGet/OS caches stayed warm. Browser work
started after publishing completed; native persistent-cache measurements and fresh
Playwright contexts remain separate. Do not pool Windows/Mac or earlier-session
Mac timings; browser/OS versions differed.

Tailwind 4.0.11's macOS-arm64 vendor executable had an invalid ad-hoc signature
and exited 137. Its original SHA-256 was
`48c64dc76f79a229e731b1e2851281c94c639189488b6699775014315525dd81`;
the locally repaired executable was
`2cfe75a9f82774a811aef797a7477eda59fd9a5de4233642f18387b898ff5c0b`.
This was a local tool-signing issue, not evidence against a WASM flag. Sandboxed
NuGet audit/network failures and loopback EPERM were likewise environmental;
accepted runs followed successful unrestricted restore/build. No permanent
`SkipTailwind` workaround or production code fix was used for those failures.

### Publish and throughput results

Decimal MB; framework totals include recursive satellite assets and are not navigation transfer sizes. Native browser workload cells are medians of the three per-run means. Keep Safari, Simulator and Playwright WebKit separate.

| Measurement | Neither | SIMD | AOT | AOT + SIMD |
| --- | ---: | ---: | ---: | ---: |
| Clean publish runs, s | 48.30, 36.97, 36.67 | 37.86, 37.85, 37.45 | 126.23, 129.66, 128.35 | 148.36, 140.91, 138.14 |
| Median publish, s | 36.97 | 37.85 | 128.35 | 140.91 |
| Framework Brotli, MB | 7.90 | 7.93 | 14.81 | 15.12 |
| Native WASM raw, MB | 3.07 | 3.08 | 47.31 | 49.27 |
| Native WASM Brotli, MB | 0.96 | 0.96 | 8.91 | 9.19 |
| Actual Safari: legality, ms | 3.5000 | 3.5400 | 0.5300 | 0.5000 |
| Actual Safari: encryption, ms | 0.0740 | 0.0740 | 0.0144 | 0.0152 |
| Actual Safari: search, ms | 1.4300 | 1.4100 | 0.3250 | 0.3250 |
| Actual Safari: encounter-generation, ms | 5.8000 | 5.8000 | 3.9000 | 3.9000 |
| Actual Safari: batch-lookup, ms | 0.0040 | 0.0040 | 0.0010 | 0.0010 |
| Simulator Safari: legality, ms | 3.1000 | 3.1400 | 0.5200 | 0.5200 |
| Simulator Safari: encryption, ms | 0.0530 | 0.0534 | 0.0148 | 0.0148 |
| Simulator Safari: search, ms | 0.9800 | 0.9800 | 0.3200 | 0.3200 |
| Simulator Safari: encounter-generation, ms | 3.3000 | 3.3000 | 3.3000 | 3.3000 |
| Simulator Safari: batch-lookup, ms | 0.0040 | 0.0030 | 0.0010 | 0.0010 |
| Playwright WebKit: bank-add-range, ms | 58.667 | 55.000 | 32.000 | 32.667 |
| Playwright WebKit: bank-json-import, ms | 43.667 | 44.667 | 28.333 | 28.000 |
| Playwright WebKit: bank-read, ms | 154.000 | 123.333 | 52.333 | 51.333 |
| Playwright WebKit: description-first-load, ms | 1249.333 | 1255.000 | 1256.667 | 1238.333 |
| Playwright WebKit: generic-zip-load, ms | 0.800 | 0.600 | 0.600 | 0.600 |
| Playwright WebKit: generic-zip-export, ms | 0.800 | 1.000 | 1.000 | 1.200 |
| Playwright WebKit: manic-load, ms | 0.800 | 0.600 | 0.200 | 0.400 |
| Playwright WebKit: manic-export, ms | 0.400 | 0.200 | 0.600 | 0.200 |

SIMD adds 36,425 framework Brotli bytes (0.46%). It shows no broad gain in the five native-browser hot paths; Playwright WebKit Bank-read median fell 19.9%. AOT improves Safari legality about 6.6x, with substantially larger payload and publish cost. These are local synthetic workload results, not mobile network, startup-through-interactive, or total-memory findings.

All three publish rounds produced identical size/native hashes within each configuration. Resolved packages match across all twelve publishes. CSS, description datasets, Bank JS and backup JS match across the four final builds (`resume-matched-assets.json`). Every final manifest passed 3,470 integrity checks. The first baseline publish was slower and remains in the reported range. Normal OS/agent/UI setup activity was not eliminated; treat small publish-time differences cautiously.

### Native functional checks

| Check | Safari, all four | Simulator, all four |
| --- | --- | --- |
| Welcome, normal Blazor file handlers | PASS | PASS |
| Raw save load/edit/export/reload | PASS | PASS |
| Generic ZIP and Manic load/edit/export/reload | PASS | PASS |
| 1,000-row Bank import, exact read-back, selection, scroll, export | PASS | PASS |
| Master Ball and Tackle full descriptions | PASS | PASS |
| Synthetic PK7 selection and expected invalid-legality presentation | PASS | PASS |
| Controlling worker, expected cached native file, server-stopped app reload | PASS | PASS |
| Backup restore, Bank rehydration/export online and offline | PASS | PASS |
| Same-origin none -> SIMD -> AOT -> both data preservation | PASS | PASS |

Native timing protocol: three alternating rounds on ports 8910-8913, original warm-up/iteration counts, normal persistent SW/cache history. Desktop Inspector was closed before navigation and opened after visible `State: done`. Simulator navigation was scheduled 1.5 seconds ahead; Inspector closed before navigation and reopened after workload completion (at least six seconds after scheduling). All native raw records report WebDriver=false. No app build or other timed browser benchmark ran concurrently. These are workload timings, not cold/cached startup timings.

Functional files were supplied to the normal production file-input change handlers with inspector-created File/DataTransfer objects. Production exports were captured as Blobs, then fed through the same load path. Money was verified as 12345 before edits and 23456 / 34567 / 45678 after raw/generic/Manic reload. Python independently verified all 24 export records, archive entry paths, timestamps and unchanged metadata; no original fixture was overwritten. These are synthetic fixtures, not playable game progress.

Desktop Safari native pickers opened for all four configurations; the baseline save was selected through the native picker. Completed desktop raw/generic/Manic downloads matched captured production-export hashes. On Simulator, the combined build additionally opened the native Files chooser with its app server stopped and completed a real Bank download through Safari's confirmation prompt. The downloaded 635,894-byte / 1,000-row JSON SHA-256 was `8fce1df2cfbe844c460416e558ce371d01fee69a43e898e8e9c8c18a83044591`, matching every production Bank export. Native iOS picker/download gestures were not separately repeated on all four variants.

### Offline and update preservation

Each platform used the same origin `http://127.0.0.1:8910/` throughout its none -> SIMD -> AOT -> both sequence, including its initially empty Bank. Manual backups were created through the normal Backups UI before freezing the comparison. The server was stopped and curl independently confirmed connection refusal before every offline reload. The evidence collector on a separate loopback port remained available only for saving reports; external fonts/sprites could still use the Internet.

Each update used the app's Check for updates action, retained a waiting worker, then used the app's explicit Update available — reload handoff. Waiting records show old/new native assets coexisting; post-handoff Resource Timing names prove which native file actually loaded. Every offline reload later confirmed the worker fully activated and its native bytes/hash matched the publish report. One desktop AOT online snapshot caught the transient `activating` state; its subsequent offline snapshot was `activated`. This transient is preserved in raw records.

| Platform | Bank entries | Backups, including manual | Full stored-data SHA-256, unchanged in all eight online/offline checks |
| --- | ---: | ---: | --- |
| Actual Safari | 1000 | 10 | `66289c4f59b6b2bb5dbd6c517b89e9eda4da5a7574a3e40fecf2a6060a9e8d65` |
| Simulator Safari | 1000 | 8 | `6bc7f2a3efe3fd01e1adef6e7faaa0346813d116bb3319c3902575b9f7e1a76c` |

The full hash covers every raw Bank/backup record, including IDs, metadata, timestamps and bytes. Compact evidence also retains per-record byte hashes. Every restore/read/export operation was checked against its own before hash. All six waiting-worker snapshots and all sixteen online/offline records passed the final validator. No origin data was cleared, no database was deleted, and no existing simulator was erased. Retention of setup-created synthetic backups happened before the frozen baseline.

### Playwright WebKit and instrumentation results

Playwright WebKit 27.2 on macOS completed all eight application workloads in each of twelve fresh-context runs; fixture dictionaries match across configurations/repetitions. The Windows JS/IndexedDB insertion timeout was not reproduced by this Mac engine, actual Safari or Simulator. This does not establish the root cause of the Windows automation defect.

The separate final WebKit pointer test passed all four configurations: one Pikachu moved from box slot 1 to slot 2, exactly one occupied slot remained, title unchanged, no page exceptions. It uses a fresh context and injected synthetic file, then real Playwright mouse dragging. It is not released Safari or a real touch-gesture/latency measurement.

Retained unsuccessful instrumentation/preflight evidence:

- Sandbox NuGet audit/network failure (NU1900), stalled sandbox build, and initial loopback-server EPERM. Unrestricted restore/build/benchmarks passed. The stale sandbox build was stopped before timed publishing.
- Initial Safari console top-level-await SyntaxError and repeated Inspector preview evaluation opened overlapping load dialogs. The first baseline functional probe timed out at upload readiness. A per-name one-run guard fixed the driver; accepted baseline result is `mac883-resume-safari-none-functional-v2.json`.
- The initial Simulator AOT UI probe read the asynchronous description placeholder. A second probe captured Tackle before full content arrived. The accepted record is `mac883-resume-simulator-aot-ui-v3.json`, requiring full item and move text. Other accepted UI records use the unsuffixed names.
- One evidence submission exceeded the collector limit by duplicating complete base64 backups. Subsequent records keep the full aggregate storage hash plus compact per-record hashes. The oversized submission was not an application failure.
- The initial update driver waited for incorrect toast wording. The waiting worker installed successfully; the driver was corrected to use the actual app menu. The first Simulator transition records the manual correction before explicit reload.
- Native Safari pointer automation failed twice with CUA `noWindowsAvailable` before reaching the app. Native Safari drag remains unverified.
- The additional Playwright chooser-event test timed out in all four builds; those four raw failures/screenshots remain in `resume-webkit-ui.*`. Native Safari pickers passed separately. The first separate drag script had ambiguous held-item sprite selectors and waited for intentionally hidden fallback images. Final `resume-webkit-drag-v2.json` corrects those selectors and passes all four.
- Inspector source-map 404s and expected offline connection errors are retained. Canceling iOS prepared-download overlays after Blob capture logs AbortError as designed; the separate real Bank download completed successfully.

## Browser requirements: SIMD and exceptions are separate

At the start of the investigation, the project's Safari 16.4 comment described
SIMD availability and `index.html` checked baseline WebAssembly only. Neither
established the .NET 11 runtime floor. The implementation now checks final
exception handling and SIMD before starting Blazor; the corrected browser policy
is linked above.

The installed `Microsoft.NET.Runtime.WebAssembly.Sdk/11.0.0/Sdk/BrowserWasmApp.targets`
defaults `WasmEnableExceptionHandling` to true and uses
`-fwasm-exceptions -s WASM_LEGACY_EXCEPTIONS=0`. The published **AOT off/SIMD off**
native WASM was disassembled with the installed `wasm-dis`; it contains `exnref`
locals and `try_table (catch_ref ...)`. This confirms use of the final exception
format in the baseline, independently of SIMD or CoreCLR.

| Required feature | Chrome | Firefox | Safari |
| --- | ---: | ---: | ---: |
| Fixed-width SIMD | 91 | 89 | 16.4 |
| Legacy Wasm exception handling | 95 | 100 | 15.2 |
| Final Wasm exception handling (`exnref`) | 137 | 131 | 18.4 |

Feature versions come from the [WebAssembly project's feature data](https://github.com/WebAssembly/website/blob/main/features.json).
WebKit separately documents [SIMD in Safari 16.4](https://webkit.org/blog/13966/webkit-features-in-safari-16-4/)
and [the new exception specification in Safari 18.4](https://webkit.org/blog/16574/webkit-features-in-safari-18-4/#webassembly),
distinguishing it from legacy exceptions. Microsoft lists
[current browsers as supported](https://learn.microsoft.com/en-us/aspnet/core/blazor/supported-platforms),
which is a support policy rather than an enumeration of every required Wasm feature.
The same .NET 11 browser-floor change is reported upstream in
[dotnet/runtime#137477](https://github.com/dotnet/runtime/issues/137477); that report
does not substitute for the local artifact evidence above.

Inference for this configuration: final exception handling imposes a later feature
floor than fixed-width SIMD in these three browser families. Adding SIMD therefore
does not further raise those documented feature floors. This does **not** prove
all devices above the floor execute the app correctly. Chromium-based Edge needs
its corresponding engine capability checked; Chrome's version number is not an
independently tested Edge minimum.

The final exception format excludes WebKit on iOS/iPadOS below 18.4, including
the iPhone 8 Plus/iOS 16 failure class in #910. Safari 16.4–18.3 can support SIMD
and still fail this runtime requirement. Devices unable to reach a compatible
OS/browser need a different runtime configuration to regain support; merely
keeping SIMD off cannot supply it. A non-SIMD second build would consequently
not solve that compatibility problem. Assess any fallback against the whole
runtime feature set, payload, storage, and maintenance cost.

## What broke around the previous rollback?

The history identifies several distinct failure mechanisms. It does **not**
establish that an AOT-generated SIMD instruction caused the reported iPhone crash.

| Failure | Evidence | Attribution |
| --- | --- | --- |
| Older engines rejected before startup | #893 enabled both flags and a real SIMD feature gate; #914 removed it. Safari below 16.4 lacks the required SIMD feature. | Confirmed support-boundary change. It does not explain the reported iOS 16.7.7 device. |
| iPhone 8 Plus on iOS 16.7.7 reached 100% then showed the error bar | #910's corrected report says the probe passed. Its proposed AOT/WebKit diagnosis has no exception stack or controlled flag comparison in the public thread. | Symptom confirmed; specific AOT/SIMD runtime defect unproven. |
| Bootstrap fetched uncached configuration and received HTTP 429 | Commit `8d308943` records GitHub Pages rate limiting through shared iCloud Private Relay IPs, surfaced by startup error capture and linked to #910. It adds static `appsettings.json` to the service-worker cache. | Concrete recorded network/bootstrap cause near the rollback; not a SIMD instruction requirement. The public issue lacks the raw trace needed to assign every reporter's crash to it. |
| Large AOT native file and offline installation | #893/`ce0f15fb` excluded the roughly 44 MB native WASM after an apparent Safari cache-quota failure. #899 separately reproduced offline failure on production without AOT. | Cache/storage concern was recorded; the quota explanation has no device/quota trace in these records. Offline failure was not unique to AOT. |
| Stale clients later requested removed runtime fingerprints | #1162 documents a live reproduction: old cached application requested an excluded `dotnet.native.*.wasm` after deployment removed it; Pages returned HTML/404 and integrity fetch failed. | Confirmed later caching defect, inherited from the large-AOT workaround even after AOT was disabled. Not proof of the original May iPhone root cause. |
| Chromebook failed to load | #911 has no confirmed failing Chromebook version or console/network trace; the supplied Android/Chrome UA may describe another environment. | Root cause unresolved. Both a missing SIMD feature and a stale-worker explanation were assumptions. |

The sequence is relevant because the fixes were bundled close together:

1. [#893](https://github.com/codemonkey85/PKMDS-Blazor/pull/893) enabled AOT and SIMD
   together on .NET 10. Its test plan records a real-iOS golden-path pass, without
   an exact device/OS version; it cannot establish the entire Safari 16.4+ range.
2. On May 17, startup diagnostics landed in
   [`d077eed5`](https://github.com/codemonkey85/PKMDS-Blazor/commit/d077eed5e0f6629b25e0a9f24ea7f86c7ddddf7b)
   at 17:53 Eastern. The commit describes the iOS and Chromebook reports as the
   motivation; that description itself is not proof of an AOT runtime defect.
3. At 18:14 Eastern,
   [`8d308943`](https://github.com/codemonkey85/PKMDS-Blazor/commit/8d308943b6e4ec306ef0c89b692377e61c02d22d)
   added configuration caching to address the recorded 429 failure.
4. At 18:56 Eastern,
   [`844e9c9b` / #914](https://github.com/codemonkey85/PKMDS-Blazor/pull/914)
   disabled **both** flags and removed the SIMD gate. It retained the configuration
   caching fix and full trimming. Its description says compatibility below iOS
   16.4 was restored and closes #910, despite the corrected report being iOS 16.7.7.
   This is not an AOT-only versus SIMD-only experiment.
5. [#1162](https://github.com/codemonkey85/PKMDS-Blazor/pull/1162) later repaired the
   independently reproduced missing-runtime cache/update problem. Current cache
   coherence and native-runtime pre-caching must be preserved when revisiting AOT.

Therefore, a report that the app worked after this rollout cannot isolate which
change fixed a given device: the flags, gate, configuration caching, and later
cache repairs changed different parts of startup. The initial iOS-below-16.4
diagnosis was contradicted by
[#910's corrected report](https://github.com/codemonkey85/PKMDS-Blazor/issues/910#issuecomment-4472585965).
The documented network/cache causes are stronger evidence than the unverified
WebKit-vector hypothesis, but do not resolve every historical symptom.

A definitive test of the remaining iOS 16.7.7 failure requires an affected real
device and a **.NET 10** historical matrix, with configuration/network and
service-worker state controlled, recording the actual error/network trace for
each flag combination. The maintainer confirmed that an affected device is not
available for this evaluation. Today's .NET 11 builds cannot stand in for that experiment:
their final-exception requirement already excludes that engine. Windows WebKit's
hot-path passes also cannot retrospectively validate iOS 16's JavaScriptCore.
The December 2024 `eaffd2d5` AOT revert contains no recorded diagnostic rationale;
it supplies no additional causal evidence.

Use the corrected data-preserving advice on #911. Never follow the old suggestion
to wipe origin storage, which would remove backups and Bank records.

## Windows startup through one verified action and offline updates

A separate Chromium probe ran three interleaved rounds, each in a new browser
process and fresh nonpersistent context with service workers **allowed**. After
welcome visibility it clicked the welcome upload button and awaited Playwright's
file-chooser event, proving the Blazor click handler reached the picker. It then
waited for worker readiness, went offline, reloaded under the controlling worker,
and repeated that action. No file was selected; canceling an empty picker returns
without loading a save. The local server served published Brotli siblings.

| Observation | No AOT/SIMD | SIMD only | AOT only | AOT + SIMD |
| --- | ---: | ---: | ---: | ---: |
| Fresh navigation → observed file chooser, ms | 892.0–962.6 | 879.0–1004.4 | 906.0–909.0 | 919.8–1060.8 |
| Offline controlled reload → observed file chooser, ms | 463.4–467.4 | 464.4–498.1 | 437.6–454.1 | 435.8–484.3 |

These observations include harness visibility/action waits, interprocess messages,
and picker-event detection. Worker installation/cache writes can run during the
first navigation. They are a verified-action endpoint, not a precise time-to-
interactive or paint metric. Ranges overlap; this probe establishes no startup
speedup. OS/browser-binary/filesystem caches remain warm, and loopback delivery
does not model a mobile network. Raw records and script:
`measurements/eval883-startup.runtime.jsonl` and `eval883-startup.mjs`.

A separate **single-origin update** probe installed the baseline in a fresh
temporary context, seeded one synthetic Bank canary and one backup canary through
the production JavaScript APIs, then updated none → SIMD → AOT → both using the
application's explicit update handoff. Every variant cached its native runtime
and 189 total precache entries, booted offline, and preserved an identical SHA-256
of the complete stored canary snapshots (including IDs/metadata/timestamps).
Native cached response sizes were 3,065,233 / 3,077,078 / 47,216,974 / 49,174,166
bytes. No origin storage was wiped. The canaries test storage preservation,
not playable save/PKM validity or arbitrary existing-profile migrations.
Records/script: `eval883-pwa.json`, `eval883-pwa.log`, `eval883-pwa.mjs`.

## Windows harness diagnostics and WebKit gap

Early application runs flagged occasional Chromium `net::ERR_ABORTED` on the
description JSON responses even though all eight workloads/validators completed.
A diagnostic wrapper captured `ReadableStreamDefaultReader.cancel` called by
the runtime's `httpAbort`. The production description loader disposes its stream
after deserialization; [.NET 11's HTTP controller disposal](https://github.com/dotnet/runtime/blob/v11.0.0/src/libraries/System.Net.Http/src/System/Net/Http/BrowserHttpHandler/BrowserHttpHandler.cs)
aborts the response. The final harness narrowly records these HTTP-200 description
stream cancellations for an already completed, validated application suite. It
still fails startup/asset errors and incomplete/failed suites. Actual injected
HTTP 503 and truncated-JSON responses both failed the measurement, with no
successful-looking record. Raw diagnostics include `eval883-debug-reader-cancel.log`
and `eval883-rejected-{http503,truncated-json}.runtime.jsonl`.

Preflight records are retained separately from the complete final protocol:
`eval883-chromium-*`, `eval883-final-*`, `eval883-smoke-*`, and
`eval883-debug-*`. The initial unprimed baseline welcome observation was
3,414 ms; it is retained, not substituted into the warmed final comparison.
Do not interpret the final welcome ranges as machine-cache-cold startup.

Windows WebKit 27.2 passed the five hot paths in all configurations, but all four
application invocations timed out after 90 seconds. Later failure-state capture
shows all four description-loading cycles completed and Bank JavaScript loaded.
A transaction diagnostic reached the initial Bank read and then a readwrite
transaction without its completion event. Separate fresh-context probes confirmed
plain IndexedDB open and the production empty-Bank read work; a **JavaScript-only**
production `addRange` of 1,000 synthetic rows timed out after 10 seconds with
**no .NET/WASM loaded**. This reproduces the blocking boundary independently of
AOT/SIMD. The exact transaction/backend defect and its relevance to released
Safari remain unresolved; do not classify this as a Safari pass or an AOT bug.
Evidence: `eval883-webkit-bank-debug.log`, `eval883-webkit-idb.json`, and
`eval883-webkit-idb-bulk.log`, with their local diagnostic scripts.

## Independent decisions

- **SIMD: enable with the browser gate and support policy.** The Windows framework
  Brotli increase is 37,367 bytes (0.47%); Mac is 36,425 bytes (0.46%). Publish times
  are similar. Windows median Bank read improved about 17.1% and bulk add about
  11.7%; Mac Playwright WebKit Bank read improved 19.9%. The five native hot paths
  show no broad SIMD-only gain. Description loading and JSON import are mostly
  unchanged. Modern native Safari/Simulator application and update checks passed;
  the Windows JavaScript-only Bank timeout did not reproduce on Mac. These
  observations support the focused change, not universal device certification.
- **AOT: defer default adoption.** Windows median publish rises from 46.60 s to
  224.38 s without SIMD and 242.05 s with it; framework Brotli grows 86.8%/90.8%
  and observed local startup bytes nearly double. Mac median publish rises from
  36.97 s to 128.35/140.91 s with roughly 7.90 MB to 14.81/15.12 MB framework
  Brotli. Legality, encryption, search, and Bank read can improve substantially,
  but descriptions/import do not broadly improve. Encounter-generation behavior
  differs by engine: slower with AOT in Windows Chromium, faster in desktop Safari,
  unchanged in Simulator. Do not generalize a microbenchmark to every browser.
  Mobile startup, peak memory, storage pressure, and physical-device updates still
  need evidence. The historical .NET 10 iPhone runtime hypothesis remains unproven.
- **CoreCLR: separate investigation.** No CoreCLR, Runtime Async, algorithm, full-trim
  root, IndexedDB format, or persisted user-data change is part of this rollout.

## Startup implementation lessons

The gate compiles a baseline WASM module and validates small final-EH and SIMD
modules. A legacy exception probe is insufficient. Missing/invalid build metadata
fails closed with reload guidance; missing capabilities get feature-specific
unsupported-browser guidance. There is no user-agent version gate or automatic
storage reset. SIMD is optional only for builds that explicitly disable it.

Build metadata is generated from resolved MSBuild feature flags, including CLI
overrides, rather than a second hand-maintained JavaScript switch. It is a normal
static asset and receives service-worker integrity verification. Published
`.boot.js` assets use an explicit compound-extension fingerprint pattern. Exact
filename patterns put fingerprints in the wrong place; use the SDK's supported
`*.boot.js` form. The generated file needs an explicit content root matching its
physical intermediate-directory path. Development uses the SDK's default asset
naming/mapping; the required published pattern is scoped to publishing. A Debug
build alone did not catch the initial incorrect serving path: actual development
server startup did. Verify both development and published loading.

Initial gate validation with SIMD disabled passed 51 JavaScript tests (15 for the
gate), zero-warning Debug build, trimmed Release publish, all 3,472 published
manifest hashes, development startup, published online/offline startup, and
simulated missing WASM/final EH/SIMD/metadata. Required-feature failures made zero
WASM requests in that probe; a synthetic IndexedDB sentinel survived each case.
The host-bytecode test needs Node 26+ final EH; simulated gate tests also run on
older CI Node versions. These simulations exercise the gate, not real old engines.

Final SIMD-enabled validation on the updated PR base is recorded below; the
original four-way matrix is not silently rerun or relabeled by the default flip.

## Windows evaluation verification (historical snapshot)

- `dotnet format --verify-no-changes --no-restore` passed.
- Solution `dotnet build -c Debug` passed with zero warnings/errors; tests compile
  but were not executed locally, per repository policy.
- All four published asset manifests passed integrity verification: 3,470 assets
  each. JavaScript syntax and `git diff --check` passed.
- Chromium and Firefox suites, guarded HTTP/JSON fault cases, verified picker
  actions, and Chromium PWA/canary checks are recorded above. Windows WebKit's
  application failures remain explicit. No real user profile or save was used.


## Remaining rollout checks

Fresh-context welcome arrival is not paint or interactive readiness. The separate
verified-action probe covers one local offline startup action. Loopback Brotli
traffic is not a production network simulation. Neither probe measures total
browser/WASM/JS/native peak memory, real scrolling/selection/drag latency, or
actual mobile hardware behavior. Application
managed counters, heap snapshots, and timer lateness retain the scopes described
in [the application baseline](DOTNET_11_APPLICATION_BENCHMARKS.md).

Keep these remaining checks open after the SIMD rollout:

- Physical iPhone/iPad hardware, modern mobile behavior, and exact feature-floor
  engines. Current native Safari and Simulator passes close the available local
  Mac/Simulator matrix, not those hardware/version gaps.
- Historical .NET 10/iOS 16.7.7 four-way reproduction with actual error/network
  traces. Today's .NET 11 runtime cannot substitute for that older runtime matrix.
- Branded Chrome/Edge/Firefox devices, native Safari drag, quantified real
  scrolling/touch/selection/drag latency, and AES/MD5 known-answer bridge checks
  (#1390). Synthetic encryption/archive checks are narrower evidence.
- Total browser/WASM/JS/native peak memory and real responsiveness (#1387), plus
  production-network first load/cached startup through verified interaction.
- Deployed-origin existing-profile updates and storage-pressure/failed-install
  behavior preserving all backups and Bank. Local full-record hashes and Windows
  canaries do not prove every mobile storage limit. Never clear origin site data.
- Windows Playwright WebKit's IndexedDB completion defect and chooser-event
  instrumentation failures remain tool/engine-specific open diagnoses.

The browser policy and matching startup gate are implemented with this rollout.
SIMD and AOT remain separate decisions; this work does not close #883, #1387, or
#1390, or claim physical/legacy-device support.

## Evidence retention and reproducibility

The committed document preserves methods, complete reported timing/size tables,
functional/update outcomes, historical attribution, unsuccessful instrumentation,
and open gates. It is a report, not a substitute for raw traces. `measurements/`
is ignored by design: do not commit built sites, synthetic databases, screenshots,
large generated fixture payloads, or machine logs merely to publish this report.

The Mac continuation retains `mac883-resume-results.md`,
`mac883-resume-final-validation.json`, `artifact-sha256.json`, the verified
`original-mac-handoff.zip` (Windows report/source/harness overlay), scripts,
fixtures, raw native/Playwright reports, and four final sites under the local
`measurements/mac883-resume-session/`. The original Windows artifact names above
are provenance references, not promises that the files exist in a new checkout.

Use [the benchmark README](../tools/bench/README.md) for clean publishes and the
isolated runtime/application suite, retaining exact source, fixed version metadata,
resolved packages, hardware/engine versions, compression, cache state, iteration
counts, every run, and manifest/native hashes. A rerun after dependency changes is
a new experiment. Keep native Safari, Simulator, Playwright WebKit, and other
engines separate; preserve failed attempts rather than selecting favorable runs.

## SIMD rollout verification

On source base `397aee4d`, with `WasmEnableSIMD=true` and AOT disabled:

- `dotnet format --no-restore` passed.
- Solution `dotnet build -c Debug --no-restore` passed with zero warnings/errors;
  test assemblies compiled but were not executed locally.
- `node --test tools/tests/*.test.mjs`: 51 passed on Node 26, including all 15
  compatibility tests, with no skips.
- Trimmed Release publish passed. All 3,472 service-worker asset hashes verified;
  manifest `1FYf2vup`. Both compatibility scripts have real fingerprinted URLs
  present in the manifest. Generated metadata requires final EH and SIMD.
- Disassembly of `dotnet.native.7jlwqrj1oe.wasm` confirms final EH (`exnref`,
  `try_table`) and SIMD instructions. Its SHA-256 is
  `034bbd041d6f58120f673a7ff1c90e85a741ab30ef4602ca1b2a0943c1b2b443`,
  matching the earlier Mac SIMD-only native runtime despite the application
  dependency updates. This does not make the whole application build identical.
- Fresh Playwright Chromium 155.0.8059.40 contexts passed development-server
  startup and published online startup, worker activation/control, and offline
  reload/startup with no page exceptions.
- In the actual SIMD-enabled publish, simulated unavailable WASM, final EH, SIMD,
  and missing build metadata each produced the matching fallback before any
  observed WASM request. A synthetic IndexedDB sentinel survived every rejected
  case and the successful online/offline case. No real user data was used.
- .NET/xUnit execution is delegated to the PR's Build and Test workflow under
  `AGENTS.md`; local browser probes do not substitute for that suite.

CodeQL subsequently caught case-sensitive script-tag extraction in the regression
test fixtures. The shared extractor now handles mixed-case names, whitespace, and
closing-tag attributes, with a regression case. All 52 JavaScript tests pass after
that correction; this changes fixture extraction, not the production startup gate.

These checks validate the selected rollout build. They do not repeat the four-way
performance experiment on the newer dependency graph or close the physical-device,
minimum-version, memory, or historical-runtime gaps above.
