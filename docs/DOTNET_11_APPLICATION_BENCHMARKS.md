# Application-boundary measurements (#1387)

Measured November 12, 2026. These extend the [framework baseline](DOTNET_11_BASELINES.md)
with production service/interop/archive boundaries. No runtime or persisted-data
format setting was changed, and no production service algorithm was refactored.

## Method and fixture equivalence

The same opt-in instrumentation was compiled into both builds:

- .NET 10 reference at `1ce1b2fe`, SDK 10.0.403, Mono/WASM 10.0.14.
- .NET 11 migration plus baseline tooling at `4d8f9537`, SDK 11.0.100,
  Mono/WASM 11.0.0, with the application suite added on this branch.
- Both use full trim, no AOT/SIMD, PKHeX.Core 26.10.18, and MudBlazor 9.12.0.
- Windows 10.0.26200 x64, Ryzen 7 9800X3D / 16 logical processors / 61.6 GiB RAM;
  Playwright 1.65.0, headless Chromium 158.0.8127.18, Node 24.14.0.
- Three sequential interleaved rounds, order 10/11, 11/10, 10/11, using the same
  harness installation and published Brotli responses over loopback. No builds
  or independent validation ran during the final measurements. Each invocation
  used a new browser process and separate fresh contexts for welcome and workload
  navigation. Service workers were blocked; OS/server filesystem caches stayed
  warm. This is not a production-network or cold-machine measurement.

Byte comparisons confirmed that the description datasets, description/Bank
service sources, Bank JavaScript, archive helpers, and all six instrumentation
files matched across checkouts. The raw reports also confirm identical generated
fixture SHA-256 values in all six final runs:

| Fixture | Bytes / SHA-256 |
| --- | --- |
| 1,000-row Bank JSON | 641,439 bytes; `64374bedbf6d261516a0b6c1d7101d415b83e3c1f7a6e853815ec3d5f7683603` |
| Original synthetic Gen 7 save | `2ece5a43a7b77660482c8af3f6918567aa25228b89a56452b8c9bbff69d5886e` |
| Generic ZIP | `61249e6b7dc41df7f906432fd120ba936c7b6ce38f0ea4c552ffef894225fc5d` |
| Manic archive | `ea82c57ed84563d13383b60152613364d037881323078f6a97278952a2477bdc` |

The 1,000 PK9 records have fixed species, PID/encryption constants, names, and
metadata. Import timestamps are fixed. The loadable synthetic Gen 7 save uses
fixed trainer/money values and no user fixture. PKHeX.Core 26.10.18's blank
constructor leaves its BEEF footer marker unset, so the fixture initializes it
before serialization; the production loader is unchanged. ZIP clocks are fixed
and compared without timezone offsets, which the format does not store.

## Results

Each value is the range of workload means across three browser invocations.
There is one warm-up and three timed iterations per workload, five for archives.
Verification is outside the stopwatch and managed counters; it can affect later
GC/cache state. Full operation boundaries are documented in the
[harness instructions](../tools/bench/README.md#opt-in-application-suite-1387).

| Workload / operations per iteration | .NET 10 mean, ms | .NET 11 mean, ms |
| --- | ---: | ---: |
| Fresh description service, three datasets/first queries | 814.67–824.77 | 820.67–826.57 |
| Bank add-range, 1,000 PK9 records | 49.43–50.40 | 51.13–51.73 |
| Bank JSON import, 1,000 records | 26.87–27.30 | 26.50–27.53 |
| Bank read/rehydrate, 8,000 records | 310.70–319.33 | 326.87–327.37 |
| Generic ZIP load | 0.74–1.24 | 0.80–0.92 |
| Generic ZIP export | 0.78–0.84 | 0.88–1.04 |
| Manic load | 0.54–0.66 | 0.62–0.64 |
| Manic export | 0.42–0.68 | 0.32–0.40 |

Bank starts empty. Warm-up plus timed add-range calls create 4,000 rows, and
warm-up plus timed imports append another 4,000. The module/database first open
is outside timing; imports retain every prior synthetic row. Read tests then
process all 8,000 records. These are growing-store boundary workloads, not a
constant-size database microbenchmark. Every row's stored Pokémon bytes and
metadata, imported timestamps, row count, and unique IDs were checked. The real
Bank export was also checked outside timing. Archive exports preserved edited
save bytes, metadata/path/clock values and original buffers, and reloaded the
edited money and correct wrapper kind.

There is no broad application speedup from this upgrade in these samples. The
description/import ranges overlap; Bank add/read were modestly slower, while
Manic export was faster here. Sub-millisecond archive timings are particularly
sensitive to timer resolution, runtime warming, and GC history. Do not generalize
these desktop results to Safari or mobile hardware.

## Managed allocations and scheduling diagnostics

These are process-wide managed counter deltas summed over **all timed iterations**
of the workload, not retained memory or total browser allocation. For example,
Bank read's approximately 143.5 MB over three iterations is approximately 47.8 MB
per call. Counters advanced during a known-allocation probe in both runtimes.
The deltas were identical across each framework's three final invocations.

| Workload | .NET 10 managed allocated bytes | .NET 11 managed allocated bytes |
| --- | ---: | ---: |
| Description first load (3 iterations) | 22,497,720 | 22,498,800 |
| Bank add-range (3 × 1,000 records) | 7,619,072 | 7,619,744 |
| Bank JSON import (3 × 1,000 records) | 13,224 | 13,584 |
| Bank read (3 × 8,000 records) | 143,486,760 | 143,486,808 |
| Generic ZIP load (5 iterations) | 4,902,040 | 4,863,280 |
| Generic ZIP export (5 iterations) | 2,251,600 | 2,232,680 |
| Manic load (5 iterations) | 4,874,960 | 4,855,720 |
| Manic export (5 iterations) | 9,284,000 | 9,266,680 |

Import's small managed delta excludes its JavaScript JSON decoding, objects,
IndexedDB, and native/browser storage costs. It is not evidence that the whole
operation allocates only a few KB. Validation/read-back also allocates outside
these deltas. Runtime-reported collection counts and before/after managed heap
snapshots are retained in raw reports, without forced GC. Collection counts are
not pause durations, and heap snapshots are not peaks or retained-memory proofs.

The JS timer diagnostic uses a 16 ms period over each whole measured batch,
including validation and counter overhead. The observed maximum lateness ranges:

| Whole batch | .NET 10 max delay, ms | .NET 11 max delay, ms |
| --- | ---: | ---: |
| Description first load | 1.7–2.9 | 2.7–3.6 |
| Bank add-range | 111.1–115.7 | 125.2–130.1 |
| Bank JSON import | 182.9–198.9 | 191.7–205.5 |
| Bank read | 186.6–197.8 | 201.0–209.7 |

Archive batches did not exceed their first timer deadline in these samples;
zero delay is not proof of zero blocking. Timer diagnostics are affected by the
untimed validators and instrumentation, and do not establish scroll/drag latency
or per-operation responsiveness. Total browser/WASM/JS/native peak memory remains
unmeasured and needs separate profiling rather than interpretation of one heap
snapshot. See the harness documentation for exact API scopes and limitations.

## Decisions and validation

Keep existing stream/JSON/Base64/runtime defaults. The small allocation changes
do not justify removing owned buffers or changing persisted formats. Profile
description initialization and Bank read/rehydration before selecting a specific
optimization: they are substantially larger costs than archive operations in
this dataset. Keep large-list/UI responsiveness and total peak-memory profiling
as explicit follow-ups in #1387/#1374.

The published suite passed all eight workloads on both frameworks. Both final
manifests passed integrity checks: 3,466 reference assets, 3,470 migration assets.
The normal five-workload suite still passed. Browser refusal checks confirmed
that missing-marker and non-WebDriver contexts created no IndexedDB data, and
that an existing Bank canary remained byte-for-byte unchanged. No existing user
profile or save was used. Formatting and Debug builds compiled the fixture and
guard regression tests; test execution belongs to CI.

Raw records are gitignored `measurements/app-final-net{10,11}.runtime.jsonl` and
their Markdown reports. `input-hashes.json` records matched source/dataset hashes;
`application-summary.json` retains computed ranges. Initial smoke failures are
also retained separately: a missing synthetic footer and a ZIP timezone assertion
were fixed before the final comparison. No failed sample was substituted into
the reported successful-run ranges.
