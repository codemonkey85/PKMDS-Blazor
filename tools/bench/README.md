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

The timings do not establish managed allocations, GC cost, peak memory, or UI
responsiveness. Description loading, Bank bulk import, archive operations, and
large-list interaction measurements remain separate work in #1387/#1374.

Run `node tools/verify-service-worker-assets.mjs <site>` after publishing as a
separate integrity check. Browser harness validation is not a substitute for CI
tests or platform/data-preservation smoke checks.
