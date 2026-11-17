# Description and Chromium memory profiling

Follow-up to [#1387](https://github.com/codemonkey85/PKMDS-Blazor/issues/1387),
November 17, 2026. The preceding C# prototype and Bank changes are independent
PRs [#1409](https://github.com/codemonkey85/PKMDS-Blazor/pull/1409) and
[#1410](https://github.com/codemonkey85/PKMDS-Blazor/pull/1410). Build/Test and
CodeQL passed for both; Bank UAT deployment also passed.

## Description results and tuning

The focused `descriptions` suite separately measures moves, abilities, items,
normalized item lookup and a fresh service loading all three datasets. Each has
one warm-up and three measured iterations. It uses the real service, source-gen
context, generated files and version-aware queries. Dataset and output SHA-256
signatures match across the comparison invocations, including the final
completion-turn implementation. Output validation
and signature construction are included in timing/counters; repeated-output
comparison and fingerprinting are outside. No Bank API is called.

Matched Release/full-trim .NET 11 Mono WASM publishes retain SIMD on/AOT off,
the same packages and instrumentation, Windows 10.0.26200 x64, Ryzen 7 9800X3D,
Chromium 156.0.8078.4 and Node 24.14.0. Contexts are fresh, nonpersistent and
guarded loopback contexts; service workers are blocked, HTTP is no-store with
published Brotli siblings, and OS/browser/server caches remain warm. No builds
or other benchmarks ran alongside measurements. The first three pairs alternate
default-streaming and buffered runs; 128 KiB observations follow them.

| Read strategy | Three-dataset means across runs, ms | Managed bytes per three-dataset load | Whole-batch max timer delay, range | Browser-run status |
| --- | --- | ---: | --- | --- |
| Existing streaming buffer | 816.37 / 780.13 / 788.93 | 7,398,840 | 3.1–6.7 ms | Rejected for asset-request aborts |
| Buffer complete UTF-8 responses, then synchronous parse | 131.23 / 130.73 / 126.00 | 17,256,616 | 31.1–43.7 ms | Passed |
| Stream with 128 KiB JSON buffer | 124.53 / 119.60 / 127.17 | 7,292,624 | 6.7–7.3 ms | Rejected for asset-request aborts |
| 128 KiB stream plus browser completion turn | 108.87 / 108.33 / 109.77 | 7,292,352 | 6.6–7.2 ms | Passed |

The buffered moves-only workload also recorded 55.6–84.3 ms maximum timer
lateness, compared with small delays in the streaming observations. The
normalized item index did not explain the bulk of initialization time.

The production change sets `DescriptionJsonContext.DefaultBufferSize`
to `128 * 1024` and allows one browser timer turn before disposing a successfully
parsed fetch stream, retaining streaming, source generation, lazy cached Tasks,
generation-appropriate flavor and error fallback. The diagnostic results suggest
read/continuation overhead is a substantial part of the original cost; this is
an inference from changing buffer size, not a native CPU attribution profile.
The larger pooled read buffer has its own retained-memory cost, and Safari/mobile
behavior has not been established. Added regressions cover concurrent queries
sharing one download, Unicode results, and cached HTTP/JSON failure fallback.

**Adopt streaming with the larger buffer and completion turn.** The two
streaming variants without a completion turn returned all expected data/output signatures, but the
harness observed `net::ERR_ABORTED` on some description requests. Those invocations
still fail and their completed results are kept under `FailedObservation`, not
the successful `Runs` array. Adding an extra stream-to-EOF drain did not remove
the diagnostic and was removed; its single exploratory observation is retained
separately. Complete-response
buffering avoids the diagnostic but more than doubles managed allocation and
increases scheduling delays, so it is not adopted here.

One initial completion-turn invocation (109.27 ms combined load), all three
subsequent focused runs in the table, and three full application-suite runs
passed without asset failures. The latter measured description initialization
at 122.77 / 123.10 / 120.60 ms and retained all Bank/archive correctness checks.
The browser-only `Task.Delay(1)` occurs after parsing, before stream disposal;
`Task.Yield` alone does not release the current WASM macrotask. This is a tested
mitigation for the observed disposal/completion interaction, not proof of the
underlying .NET/Chromium implementation cause. Buffer tuning alone did not fix
the abort. Safari/mobile validation remains open.

Input sizes/SHA-256:

| Dataset | Bytes | SHA-256 |
| --- | ---: | --- |
| Moves | 1,219,747 | `acb903653b7cbd11ced6ad1388477635beb8bcf78ceb80b56d704e2bfd833f81` |
| Abilities | 198,527 | `249b80677f7ee92acbcd1feca5c2bfbd662eaa54f208ecb42c419d6f1caea381` |
| Items | 1,087,053 | `f4d22ff7fe492d9537d1a954cbc61fa08107865fb3dde92d8494a80aba7aaefa` |

Raw local files are `measurements/description-{streaming,buffered,stream128k}.runtime.jsonl`
and `description-drained.runtime.jsonl`. The latter is an unsuccessful extra-EOF
experiment, not an adopted implementation. Final successful observations are
`description-yield.runtime.jsonl`, `description-yield-repeat.runtime.jsonl` and
`description-yield-application.runtime.jsonl`. No generated data was changed.

## Sampled browser-process memory

The harness now has a separate opt-in Chromium memory-infra tracing pass. It
records background dumps at a requested 250 ms cadence, a final dump without
forced GC, every reported process counter, process names, sample timestamps,
coverage and raw trace paths. Ordinary trace categories are explicitly excluded.
A fixed trace buffer plus data-loss/final-dump checks reject incomplete captures.
Raw traces use unique names and cannot overwrite prior captures. Allocator
events are not added to process totals, avoiding duplicate accounting.

A known touched 32 MiB buffer in a fresh renderer produced a **34,103,296-byte**
renderer private-footprint range with the final filtered configuration. That is
a sensitivity check, not exact attribution: ordinary process activity and trace
overhead remain included. Its raw trace/summary are retained as
`measurements/memory-filtered-calibration.trace.json` and `.json`.

Three alternating pairs exercised the real 8,000-row Bank UI workflow on the
same pre-optimization/UTF-8 publishes used in the Bank comparison. Traced timings
are not used as performance baselines. Each workflow includes bootstrap, fixture
seeding, rehydration, export-hash checks, selection/pagination/filtering and a
60-frame programmatic scroll. All six UI/data checks passed and complete export
hashes stayed unchanged.

| Pair | Control peak reported private footprint, bytes | UTF-8 peak reported private footprint, bytes |
| --- | ---: | ---: |
| 1 | 481,505,280 | 442,773,504 |
| 2 | 488,247,296 | 458,498,048 |
| 3 | 495,439,872 | 445,997,056 |

These are maxima of **11–13 samples per workflow**, summing only the process
totals actually reported by Chromium: browser, renderer, GPU, network and tracing
service. The lower UTF-8 observations are consistent with the prior managed
allocation reduction, but do not isolate its contribution from runtime/JS/native
activity and diagnostic verification. These whole-process figures must not be
added to managed or JS-heap counters, which overlap them.

On this Windows build, resident-set counters were absent; the report preserves
`null` rather than reporting zero. Chromium's Windows counters distinguish
private usage from working set ([source](https://raw.githubusercontent.com/chromium/chromium/main/base/process/process_metrics_win.cc)).
Windows defines private usage as committed private memory
([counter documentation](https://learn.microsoft.com/en-us/windows/win32/api/psapi/ns-psapi-process_memory_counters_ex)).
Private footprint is not total physical RAM: shared pages, OS/GPU-driver memory,
unreported processes and peaks between dumps are outside the result. This is
sampled private process memory, not an exact resident/total browser peak or a
complete managed/WASM/JS/native partition. #1387 remains open for those gaps,
broader views, physical interactions and other platforms.

Earlier unfiltered light traces captured unrelated browser categories and
produced excessive output/observer overhead; those exploratory observations are
not used in the table. A subsequently bounded background attempt filled its
trace buffer and was correctly rejected for data loss. All remain local diagnostic
artifacts. The final filtered traces are approximately 1.5–2.0 MB per Bank flow.

Method references: [Chromium MemoryInfra](https://chromium.googlesource.com/chromium/src/+/main/docs/memory-infra/README.md)
and [CDP tracing protocol](https://chromedevtools.github.io/devtools-protocol/tot/Tracing/).
The protocol's deterministic dump option can force GC; this harness explicitly
sets it to false. Actual interval jitter and process coverage remain in raw data.

## Reproduce

Install the [benchmark dependencies](../tools/bench/README.md), finish publishes,
and run on an otherwise idle machine. Use separate trace-enabled passes:

For a matched description baseline, keep this PR's instrumentation and use the
parent `DescriptionService.cs` without the buffer-size option. The full-response
experiment instead uses `GetByteArrayAsync` followed by source-generated
`JsonSerializer.Deserialize`, with the original context options. Keep all other
sources/settings fixed. The committed candidate retains asynchronous streaming
and adds the larger context buffer plus browser-only completion turn.

```powershell
node tools/bench/run-bench.mjs --label descriptions --site release/wwwroot --suite descriptions --runs 3 --compression br
node tools/bench/calibrate-memory.mjs measurements/new-calibration.trace.json > measurements/new-calibration.json
node tools/bench/run-bench.mjs --label bank-memory --site release/wwwroot --suite bank-ui --runs 3 --compression br --memory-sampling-ms 250
```

The description command exits unsuccessfully if a browser failure is observed,
retaining `FailedObservation`. Failed invocations are never appended
as successful Markdown measurements. Tracing affects time and memory; normal runs
have tracing disabled. The memory helper requires Chromium/CDP and does not claim
Firefox/Safari support. Guard checks run before Bank writes or UI mounting; no
real user storage or profile is used, and no site-data cleanup is performed.

Final memory records are `measurements/bank-filtered-{control,utf8}.runtime.jsonl`;
each points to uniquely named raw trace JSON. Counter parser, cache/Unicode/error
and guard regressions run in CI. A new published-Chromium description smoke step
checks real downloads/output and fails on asset errors without timing thresholds.
Local validation uses formatting, Debug builds and published-host
observations; `dotnet test` is never run locally.
