# .NET 10/11 baseline follow-up (#1387)

Measured November 12, 2026, after #1384 merged. These are observations from one
Windows desktop, not a claim about every supported browser/device. This follow-up
changes the measurement harness and documentation, without an application
algorithm, runtime-default, persisted-format, or C# model refactor.

## Method

- Reference: `1ce1b2fe90eb0285d9b5615e7fcce16fbf7034dd`, SDK 10.0.403,
  Mono/WASM pack 10.0.14, existing ASP.NET Core packages 10.0.15.
- Migration: `326729f1ec581489a1a145bdf7252ed0aab55e0f`, SDK 11.0.100,
  Mono/WASM pack 11.0.0, ASP.NET Core packages 11.0.0.
  This includes #1384's documented UAT compatibility/UI fixes. The benchmark
  implementation/configuration is unchanged, but these figures compare the
  delivered migration; every startup/size difference cannot be isolated to the
  runtime alone.
- Existing PKHeX.Core 26.10.18, MudBlazor 9.12.0, and other application dependencies
  retained. The framework dependency graph changes as part of the migration;
  resolved Web package lists are retained in the local publish JSON.
- Both: `UseMonoRuntime=true`, full trimming, compression enabled, AOT/SIMD false.
  Release Web/RCL/Core `bin` and `obj` and publish output removed before each run;
  package/Tailwind downloads primed beforehand. Implicit restore is timed.
  SDK/NuGet/build-server/OS caches remain warm. No concurrent local builds or
  browser workloads ran during the timed publishes.
- Three sequential alternating publishes per framework (10, 11, 10, 11, 10, 11).
  Source reports mark dirty state after Tailwind regenerates its tracked minified
  CSS; migration reports also include the harness/documentation edits. Application
  C# and Razor source remains at the recorded commits. Git status is retained by
  the final harness rather than interpreting every dirty tree as changed logic.
- Windows 10.0.26200 x64, Ryzen 7 9800X3D, 16 logical processors, approximately
  61.6 GiB physical memory. Browser: Playwright 1.65.0 / headless Chromium
  158.0.8127.18; Node 24.14.0, 1280×720 viewport.

See [harness instructions](../tools/bench/README.md) for reproducible commands and
measurement definitions. Raw reports remain in gitignored `measurements/`.

## Clean publish results

| Measurement | .NET 10 | .NET 11 |
| --- | ---: | ---: |
| Publish run 1, seconds | 74.38 | 71.46 |
| Publish run 2, seconds | 66.03 | 59.20 |
| Publish run 3, seconds | 64.01 | 60.29 |
| Median, seconds | 66.03 | 60.29 |
| Total `wwwroot` bytes, runs 2–3 | 69,824,999–69,825,211 | 72,865,132–72,865,346 |
| Recursive framework Brotli bytes, runs 2–3 | 7,327,129–7,327,368 | 7,878,578–7,878,752 |

The local .NET 11 median was approximately 8.7% shorter, but the ranges overlap
and both first runs were slower. This does not establish a portable build-time
improvement. Runtime/native-link toolchains differ, and build-server/OS caches
were not reset.

Framework Brotli grew approximately 551 KB (7.5%). Small run-to-run byte changes
are expected with timestamp-based assembly versioning. Total output counts raw
files and `.br`/`.gz` siblings; it is disk usage, not download size. The first
publish pair used an earlier size collector that omitted framework subdirectories;
retain its timings, but use runs 2–3 for the recursive size comparison. No
historical size value has been silently substituted with a later build's value.

Local records: `net10-clean-{1,2,3}.publish.json` and
`net11-clean-{1,2,3}.publish.json`, corresponding Markdown and logs where captured.
Both final published outputs passed manifest integrity verification: 3,465
reference assets and 3,469 migration assets.

## Browser results

The same harness installation drove both published outputs. Three interleaved
rounds exercised both identity and Brotli HTTP modes, reversing framework order
in the middle round. Each invocation used `--runs 1`, a new browser process, and
separate fresh contexts for the welcome screen and benchmark. Service workers
were blocked and responses used no-store. OS/server filesystem caches remained
warm. No build, integrity-check, or other local verification ran during the final
comparison; earlier harness smoke runs are retained separately.

| Measurement, three-run range | .NET 10 | .NET 11 |
| --- | ---: | ---: |
| Welcome DOM, identity HTTP, ms | 551.1–653.7 | 622.6–674.8 |
| Welcome DOM, Brotli HTTP, ms | 628.3–637.7 | 661.7–737.4 |
| Benchmark complete, identity HTTP, ms | 1,931–2,138 | 2,043–2,208 |
| Benchmark complete, Brotli HTTP, ms | 2,057–2,139 | 2,067–2,172 |
| Local resource encoded-body bytes at welcome visibility, identity | 31,639,410 | 32,790,203 |
| Local resource encoded-body bytes at welcome visibility, Brotli | 6,796,570 | 7,218,093 |

Welcome DOM is the navigation-relative time when the welcome component first
appears in a MutationObserver. The harness then waits for visibility. This is not
paint or time to interactive. Benchmark completion includes fixture construction,
five warm-up iterations, workloads, serialization, and polling; it is not startup.
Resource lists omit in-flight/later requests and do not represent the entire site.
Loopback compression adds decompression without production network savings, so
these figures do not imply that uncompressed production traffic would be faster.

| Workload mean, ms/iteration, Brotli runs | .NET 10 | .NET 11 |
| --- | ---: | ---: |
| Legality, 30 Pokémon | 4.346–4.569 | 4.270–4.453 |
| Encryption, 30 roundtrips | 0.0633–0.0676 | 0.0580–0.0645 |
| Search, 960 slots | 1.158–1.249 | 1.049–1.132 |
| Encounter generation, 1,502 encounters | 4.740–4.850 | 4.650–5.610 |
| Batch property lookup, 6 lookups | 0.0040–0.0052 | 0.0037–0.0051 |

The raw reports also retain identity-mode workload timings and per-iteration
summary statistics. Search was lower in these three Brotli runs; startup was
generally higher, and other ranges overlap or vary. There is no consistent
across-workload throughput gain. The batch operation is too short for useful
conclusions. Generated encounter Pokémon can vary, so the synthetic workloads
are matched configurations rather than byte-identical fixtures.

Raw comparison records: `matched-net{10,11}-{identity,br}.runtime.jsonl` and their
Markdown counterparts. Each JSON line includes environment, cache/compression
policy, resource sizes, and the original C# results. Initial `net*-identity/br`
and `net10-harness-check` records are development smoke checks, not part of the
final comparison. The first harness smoke invocation records a missing-browser
installation error; the browser was installed before the successful runs.

## Decisions and remaining work

Keep the merged Mono/full-trim/no-AOT/no-SIMD configuration as the baseline.
No optimization is justified solely by retargeting or adopting shorter syntax.
Use these results to compare focused future changes, reporting startup/size
alongside steady-state behavior.

This closes the missing clean-publish comparison and adds a separate welcome-DOM
measurement. The subsequent [application suite](DOTNET_11_APPLICATION_BENCHMARKS.md)
adds representative description loading, synthetic Bank bulk import/read, archive
operations, managed counters/heap snapshots, and a scheduling diagnostic. Total
peak memory and real UI responsiveness remain follow-up work in #1387/#1374.
Browser/device breadth, existing-profile update validation, and
other deployment checks remain in #1390. Larger experiments are linked from
#1374; no allocation or memory improvement is claimed here.
