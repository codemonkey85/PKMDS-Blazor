# Publish and browser measurements

Used by #883 and #1387. Run on an otherwise idle machine, with no concurrent
builds or browser benchmarks. Keep the application inputs, dependencies,
publish settings, hardware, and browser version matched. Retain every run;
report ranges and outliers rather than selecting the fastest result.

## Setup

Use the SDK/workloads selected by the source checkout's `global.json`. Restore
both source checkouts and provision the Tailwind CLI before timing, so network
downloads are not included in only one side of a comparison.

```powershell
cd tools/bench
npm ci
npx playwright install chromium
cd ../..
```

## Clean publish measurements

```powershell
./measure-publish.ps1 -Label net11-clean-1

# Use the same measurement script against an unchanged reference worktree.
# An explicit executable allows an isolated SDK installation when required.
./measure-publish.ps1 -Label net10-clean-1 `
    -ProjectRoot C:\Code\PKMDS-net10-baseline `
    -DotNetPath C:\Code\.dotnet-10\dotnet.exe
```

Run sequentially in alternating framework order, at least three times each,
with a new label for every publish. The script rejects existing publish labels.
It changes to `ProjectRoot` for SDK selection and publishing. Reports always
go in the measurement script checkout's gitignored `measurements/` directory.

The script removes the chosen publish directory and each Web/RCL/Core project's
Release `bin` and `obj`. `OutputDir` must be `release` (the default), a directory
under `release`, or a subdirectory of `measurements`, within `ProjectRoot`.
It rejects cleanup through junctions/symlinks. SDK/NuGet packages, Tailwind's CLI,
build servers, and OS/filesystem caches remain warm. The stopwatch includes
implicit restore, compile, trimming, native linking, and publish compression;
this is a **clean project-output publish**, not a machine-cache-cold build.

Each label produces Markdown and `.publish.json` with exact byte counts,
elapsed seconds, source commit/status, executable/SDK/workload information,
resolved publish properties, and resolved Web package versions. Framework size
includes subdirectories. Total `wwwroot` counts raw files and compressed siblings;
it is disk usage, not download size. Brotli totals cover every framework asset,
including culture-specific assemblies, rather than just startup requests.

## Repeated browser measurements

```powershell
node tools/bench/run-bench.mjs --label net11 --site release/wwwroot `
    --runs 3 --compression br --output measurements/net11-browser.md

# Or publish and then benchmark (three repetitions by default):
./measure-publish.ps1 -Label net11-clean-2 -RunBenchmark -BenchmarkRuns 3
```

Use the same harness installation for both published builds. Alternate reference
and migration invocations with `--runs 1` to reduce order effects. The default
compression mode is `identity`, preserving the older harness's uncompressed
server behavior. `--compression br` serves precompressed `.br` siblings when the
browser accepts them and falls back to raw files where siblings are absent.
This loopback server does not simulate a production CDN, latency, or bandwidth.

Each repetition loads `/` and `/bench` in **separate fresh, nonpersistent browser
contexts**, with service workers blocked, a 1280×720 viewport, and no-store HTTP
responses. Real user profiles, backups, and Bank records are never used. The
browser process is reused; engine, OS, and server filesystem caches are not reset.
This is not a PWA-update test or a machine-cache-cold startup measurement.
The app may log a failed service-worker registration because Playwright blocks
registration in these contexts; that expected diagnostic is printed. Uncaught
page exceptions and failed local asset requests fail the measurement. Validate
real service-worker activation separately with workers enabled.

- **Navigation → welcome DOM** is the navigation-relative timestamp when a
  MutationObserver first sees `.welcome-empty-state` on `/`. The harness also
  waits for that element to become visible. The timestamp measures DOM arrival,
  not paint or time to interactive. It does not include benchmark workloads.
- **Navigation → benchmark complete** includes fixture construction, warm-up,
  workloads, serialization, rendering, and harness polling. It is not startup.
- Workload statistics come from the existing C# runner: legality, encryption,
  search, encounter generation, and batch lookup. Preserve identical workloads
  and iteration counts between comparisons. Encounter data can contain randomly
  generated Pokémon properties, so these are synthetic representative inputs,
  not byte-identical fixtures. Very short operations may hit timer resolution.

Markdown includes each repetition. A sibling `.runtime.jsonl` appends a complete
JSON record for each invocation, including every raw C# result, per-resource
startup transfer/encoded/decoded sizes, browser/Node versions, machine details,
and cache/compression policy. These resource lists are captured at welcome
visibility; requests still in flight are not included. Do not treat them as the
entire site's eventual traffic. Failed invocations retain completed repetitions
and an error in JSON; failures do not append successful-looking Markdown.

The default hot-path timings do not establish managed allocations, GC cost,
peak memory, or UI responsiveness. The opt-in application suite below adds
separate diagnostics; large-list interactions remain outside this harness.

Run `node tools/verify-service-worker-assets.mjs <site>` after publishing as a
separate integrity check. Browser harness validation is not a substitute for CI
tests or platform/data-preservation smoke checks.

## Opt-in application suite (#1387)

```powershell
node tools/bench/run-bench.mjs --label application-net11 --site release/wwwroot `
    --suite application --runs 3 --compression br `
    --output measurements/application-net11.md
```

The normal `/bench` route and the default CLI suite still run the five existing
hot paths. `--suite application` requests `/bench?suite=application` and installs
an explicit harness marker in each fresh context. Before any Bank API is called,
the application bridge requires that marker, WebDriver automation, a loopback
hostname, and **no existing IndexedDB database**. It refuses existing Bank data
even in an automated local context. This is an accidental-use guard, not an
authentication mechanism. Never run this suite with a real user profile.

The suite uses the production service/interop/IndexedDB paths and appends
synthetic rows. The runner issues no explicit clear/delete operation. Production
Bank reads retain their normal malformed-record cleanup behavior; the fresh
context guard is required before any Bank read too. Each context is discarded
after the run. Appending is deliberate: repeated measured imports
must preserve existing rows, just as the real import contract does.

| Workload | Input and timing boundary |
| --- | --- |
| `description-first-load` | Fresh `DescriptionService`, three HTTP/JSON loads and first Tackle/Overgrow/Master Ball queries. Service caches start empty; HTTP/server cache and compression follow harness policy. |
| `bank-add-range` | 1,000 fixed PK9 records; stored-data extraction, Base64, source-generated JSON, Blazor interop, and one committed IndexedDB transaction. |
| `bank-json-import` | Fixed synthetic 1,000-record JSON export; byte-array interop, JS decoding/parsing, and one committed transaction. |
| `bank-read` | All 8,000 synthetic rows, including JSON/Base64/PKM rehydration through `BankService`. |
| `generic-zip-load` / `manic-load` | Production save loader, wrapper detection, and parsing of a loadable synthetic Gen 7 save. |
| `generic-zip-export` / `manic-export` | Production archive writer replacing edited save bytes while retaining metadata, paths, and timestamps. |

There is one warm-up per workload, then three measured iterations (five for each
archive workload). Bank starts empty: warm-up plus measured add-range calls
produce 4,000 rows; warm-up plus measured JSON imports produce another 4,000.
The initial database/module open is outside timing. Verification reads happen
between iterations and can influence subsequent GC/cache state; no forced GC is
used. These are representative boundary workloads, not isolated engine timings.

Every Bank step verifies row count, distinct IDs, all Pokémon bytes, tag/source,
and imported timestamps. The real export path is checked outside the timed
operations. Archive export checks edited bytes, reload/edited money, wrapper
kind, every entry/path, ZIP clock timestamps, metadata, and unchanged input
buffers. ZIP timestamps have no timezone offset. The synthetic save initializes
the footer marker omitted by PKHeX.Core 26.10.18's blank constructor; production
loading/export code is unchanged. Fixtures are loadable test data, not playable
game progress. Fixture SHA-256 values and Bank JSON length are retained in raw
reports to validate matched comparisons.

Application diagnostics are separate fields in each workload result:

- `ManagedAllocatedBytes`: sum of process-wide allocation-counter deltas around
  timed operations, after probing that a known allocation advances the counter.
  Null means unavailable/failed calibration. The precise reads occur outside the
  stopwatch. They include managed harness/other managed activity and exclude
  JavaScript/native allocations; dividing by iterations gives a per-call estimate.
- `CollectionsByGeneration`: runtime-reported process-wide collection-count
  deltas around timed operations. These counts are not GC pause durations.
- `ManagedHeapBeforeBytes` / `ManagedHeapAfterBytes`: `GC.GetTotalMemory(false)`
  snapshots before/after the measured batch, including validation activity. They
  exclude fragmentation and do not force a collection. They are not peak or
  retained-memory measurements and need not match allocation deltas.
- `MaxEventLoopDelayMs` / `EventLoopSamples`: maximum lateness of a 16 ms JS timer
  during the whole measured batch, including validation and counter overhead.
  A batch that blocks the event loop can produce a large delay with few samples.
  This is an instrumentation-dependent scheduling diagnostic, not scrolling,
  interaction latency, or a per-operation responsiveness guarantee.

Total browser/WASM/JS/native peak memory is **not measured** by these APIs.
Use separate profiling passes for that question; do not infer a memory improvement
from one heap snapshot, a zero collection count, or managed-only import costs.
References: [process allocation counter](https://learn.microsoft.com/dotnet/api/system.gc.gettotalallocatedbytes),
[managed heap snapshots](https://learn.microsoft.com/en-us/dotnet/api/system.gc.gettotalmemory).
