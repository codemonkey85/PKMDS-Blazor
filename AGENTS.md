# AGENTS.md

Guidance for AI agents and coding assistants working in this repository.

## Quick commands

Prereqs
- Install the .NET SDK from `global.json`: `dotnet --list-sdks` to verify.
- Install Blazor WASM tooling once: `dotnet workload install wasm-tools`.
- IDE users can open `Pkmds.slnx`. CLI builds target individual projects (e.g., `Pkmds.Web.csproj`).

Local dev (Blazor WASM)
- From repo root (PowerShell): `./watch.ps1`
  - Equivalent from `Pkmds.Web`: `dotnet watch --non-interactive run -c Debug -v n`
  - Launch profiles expose: http `http://localhost:5283`, https `https://localhost:7267` (see `Pkmds.Web/Properties/launchSettings.json`).

Local dev with a PKHeX.Core source checkout (manual UI testing)
- From repo root (PowerShell): `./watch-local-pkhex.ps1`
  - Optional custom path: `./watch-local-pkhex.ps1 -PKHeXSourcePath C:\Code\PKHeX-dev\PKHeX.Core\PKHeX.Core.csproj`
  - See **Local dev override (`UseLocalPKHeX`)** under the PKHeX.Core section for full details.

Restore and build
- Restore: `dotnet restore`
- Build Web (Debug/Release):
  - `dotnet build Pkmds.Web/Pkmds.Web.csproj -c Debug`
  - `dotnet build Pkmds.Web/Pkmds.Web.csproj -c Release`

Format/lint
- Uses `.editorconfig`; Debug builds treat warnings as errors.
- Run: `dotnet format` then `dotnet build -c Debug`.

Tests
- Test project: `Pkmds.Tests` (xUnit + bUnit; coverage via coverlet collector).
- Run all: `dotnet test -c Release`
- Filter: `dotnet test --filter "FullyQualifiedName~Namespace.Class.TestMethod"`

Publish locally (static site)
- `dotnet publish Pkmds.Web/Pkmds.Web.csproj -c Release -o release --nologo`
- `Copy-Item release/wwwroot/index.html release/wwwroot/404.html -Force`
- `New-Item -ItemType File release/wwwroot/.nojekyll -Force | Out-Null`
- Deployable output: `release/wwwroot/`.

## Architecture overview

- Single route `/`; all UI lives inside `SaveFileComponent` as a `MudTabs`-based layout.
- `RefreshAwareComponent` base class (in `Pkmds.Rcl`) auto-subscribes to `IRefreshService.OnAppStateChanged` — prefer it over `ComponentBase` for any component that reacts to save-file state changes.
- `GlobalUsings.cs` in `Pkmds.Rcl` imports: `MudBlazor`, `PKHeX.Core`, `Pkmds.Rcl.Components`, `Pkmds.Rcl.Services`, and aliases `Severity = MudBlazor.Severity` (avoids ambiguity with PKHeX's own `Severity`).
- New tabs: add a `<MudTabPanel>` entry in `SaveFileComponent.razor`; put the tab component under `Pkmds.Rcl/Components/MainTabPages/`; use namespace `Pkmds.Rcl.Components.MainTabPages` in the code-behind.
- Tab navigation: bind `@bind-ActivePanelIndex` on `MudTabs` and pass an `EventCallback` parameter to child tabs that need to trigger navigation.

## Project layout and build

- `Directory.Build.props`: sets global `TargetFramework` to `net10.0`, repo metadata, nullable + implicit usings, and `TreatWarningsAsErrors` in Debug.
- `Directory.Packages.props`: central package management (PKHeX.Core, MudBlazor, Serilog, FileSystemAccess, etc.).
- Projects
  - `Pkmds.Core/` (Class Library): PKHeX utilities and extensions. Reusable, UI-independent logic for working with PKHeX.Core (species validation, shiny handling, markings, etc.).
  - `Pkmds.Rcl/` (Razor Class Library): shared UI/components and services. Tailwind integrated via `Tailwind.targets` (input at `Pkmds.Rcl/wwwroot/css/tailwind.input.css`). References `Pkmds.Core`.
  - `Pkmds.Web/` (Blazor WebAssembly): PWA host. Linking and compression enabled; service worker assets generated on publish. JS libs via `libman.json` (crypto-js).
  - `Pkmds.Tests/` (tests): `net10.0`, references RCL and Core.

Runtime wiring (Web)
- Serilog to Browser Console with adjustable level (via `LoggingLevelSwitch`).
- DI: `IAppState`, `IRefreshService`, `IAppService`, `ILegalizationService` (Auto-Legality engine; singleton), `IBackupService` (IndexedDB save backups), `IBankService` (IndexedDB Pokémon bank), drag/drop, logging service, File System Access, MudBlazor, JS interop.
- PKHeX crypto bridged to JS: `RuntimeCryptographyProvider.Aes/Md5` set to Blazor providers at startup.

## CI/CD (GitHub Actions)

- `.github/workflows/buildandtest.yml` (dev branches; PRs to main):
  - Setup .NET (from `global.json`), install wasm tools, restore/build Web (Release), run `dotnet test`.
- `.github/workflows/main.yml` (main):
  - Setup .NET + wasm tools, restore, publish Web to `release/`, copy `index.html` → `404.html`, add `.nojekyll`, replace `%%CACHE_VERSION%%` in `service-worker.published.js`, deploy `release/wwwroot` to `gh-pages`.
- `.github/workflows/codeql.yml`: CodeQL for C# and JS/TS (manual C# build step).

## PKHeX.Core

This app depends heavily on [PKHeX.Core](https://github.com/kwsch/PKHeX). When implementing features, use the PKHeX WinForms app as a reference for how to leverage PKHeX.Core — both for UI/UX patterns and for understanding the correct API usage. The first place you should look when referencing PKHeX is `~/Code/codemonkey85/PKHeX` (macOS) or `C:\Code\PKHeX` (Windows), which contains the PKHeX WinForms app source code. The `PKHeX.Core` project within that solution is the library we consume here, and the WinForms app is a separate project that references it. The PKHeX WinForms app is a great reference for how to use PKHeX.Core effectively, as it demonstrates real-world usage of the library's APIs in a production application. If you can't find the source there, the PKHeX Wiki and source code are also valuable resources for understanding how to work with PKHeX.Core.

If you encounter bugs or limitations in PKHeX.Core while working on an issue or PR, note them in a code comment at the relevant site and report them on the GitHub issue or PR you are working on.

### Local dev override (`UseLocalPKHeX`)

`Directory.Build.targets` at the repo root supports a `UseLocalPKHeX` MSBuild flag that swaps the NuGet `PKHeX.Core` package for a `ProjectReference` to a local source checkout. Use this when you want to:

- Manually test the UI against a PKHeX dev build
- Verify whether a PKHeX bug/regression affects this app
- Test a PKHeX fix before it is released on NuGet

**Manual UI testing (watch)**

```powershell
# Default paths: C:\Code\PKHeX (Windows), ~/Code/codemonkey85/PKHeX (macOS/Linux)
./watch-local-pkhex.ps1

# Custom path (e.g. a specific branch checkout)
./watch-local-pkhex.ps1 -PKHeXSourcePath C:\Code\PKHeX-dev\PKHeX.Core\PKHeX.Core.csproj
```

**Build / automated tests**

```sh
# Build
dotnet build Pkmds.Web/Pkmds.Web.csproj -c Debug -p:UseLocalPKHeX=true

# Test
dotnet test -c Release -p:UseLocalPKHeX=true

# Custom path
dotnet build Pkmds.Web/Pkmds.Web.csproj -c Debug -p:UseLocalPKHeX=true -p:PKHeXSourcePath=/path/to/PKHeX.Core.csproj
```

Default `PKHeXSourcePath` values (set in `Directory.Build.targets`):
- **Windows**: `C:\Code\PKHeX\PKHeX.Core\PKHeX.Core.csproj`
- **macOS/Linux**: `~/Code/codemonkey85/PKHeX/PKHeX.Core/PKHeX.Core.csproj`

The default build (no flag) is completely unaffected.

### PKHeX.Core API notes

- `CheckResult` is a **struct** (value type) — never use `FirstOrDefault()` or other null-returning LINQ on collections of it.
- Spelling: `result.Judgement` (British English), not `Judgment`.
- Human-readable legality messages: `var ctx = LegalityLocalizationContext.Create(la); ctx.Humanize(in result, verbose: false)`.
- Box iteration: `saveFile.BoxCount`, `saveFile.BoxSlotCount`, `saveFile.GetBoxSlotAtIndex(box, slot)`.
- Party iteration: `saveFile.PartyCount`, `saveFile.GetPartySlotAtIndex(i)`.
- `ParseSettings.ActiveTrainer` is internal and set by `InitFromSaveFileData(sav)` — this enables the handler check in `HistoryVerifier.VerifyHandlerState`.
- `ParseSettings.AllowGBCartEra` is set by `InitFromSaveFileData` — `true` for physical Gen 1/2 saves (enables GB era events), `false` for VC saves. Do **not** override it globally to `false`; that breaks legitimate events (e.g. Nintendo Event Mew, GS Ball Celebi) on physical cartridge saves.

## Data generation tools

Static JSON data files consumed by `DescriptionService` are generated from external sources using .NET 10 file-based apps in `tools/`. Run them whenever the upstream data changes.

### `tools/generate-descriptions.cs`

Generates `ability-info.json`, `move-info.json`, and `item-info.json` from PokeAPI CSV data, with optional supplements from Pokémon Showdown.

- **Source**: PokeAPI repo (CSV files under `data/v2/csv/`)
- **Source (optional)**: Pokémon Showdown repo — supplies two fallbacks when PokeAPI is incomplete:
  - `data/moves.ts` — secondary effects (stat changes, status, flinch, drain, multi-hit, crit rate) for Gen 8+ moves that PokeAPI's `move_meta` CSV doesn't cover yet
  - `data/text/items.ts` — `shortDesc` used as a fallback description for items whose PokeAPI `item_prose.csv` row is empty (most Gen 9 held items, plus Ability Shield, Booster Energy, etc.)
- **Output**: `Pkmds.Rcl/wwwroot/data/`

```sh
# Without Showdown (PokeAPI data only)
# macOS:
dotnet run tools/generate-descriptions.cs -- --pokeapi ~/Code/codemonkey85/pokeapi
# Windows:
dotnet run tools/generate-descriptions.cs -- --pokeapi C:\Code\pokeapi

# With Showdown supplement (recommended — fills Gen 8+ move secondary effects and Gen 9 item descriptions)
# macOS:
dotnet run tools/generate-descriptions.cs -- --pokeapi ~/Code/codemonkey85/pokeapi --showdown ~/Code/codemonkey85/pokemon-showdown
# Windows:
dotnet run tools/generate-descriptions.cs -- --pokeapi C:\Code\pokeapi --showdown C:\Code\pokemon-showdown
```

### `tools/generate-tm-data.cs`

Generates `tm-data.json` from the Bulbapedia "List of TMs" page. Also merges Sword/Shield TR data (TR00–TR99) from hardcoded PKHeX.Core move IDs. Requires `move-info.json` to already exist (run `generate-descriptions.cs` first).

- **Source**: Fetched directly from https://bulbapedia.bulbagarden.net/wiki/List_of_TMs (or supply a saved HTML file with `--input`)
- **Output**: `Pkmds.Rcl/wwwroot/data/tm-data.json`

```sh
# Fetch live from Bulbapedia (default)
dotnet run tools/generate-tm-data.cs

# Use a previously saved HTML file
dotnet run tools/generate-tm-data.cs -- --input "path/to/List of TMs - Bulbapedia.html"
```

Both scripts default output to `Pkmds.Rcl/wwwroot/data/` by walking up from the working directory to find the repo root. Pass `--output /path` to override.

### `tools/report-missing-descriptions.cs`

Diagnostic script that scans the generated `ability-info.json` / `move-info.json` / `item-info.json` and produces a plain-text report of entries with missing descriptions, missing per-gen flavor, or both. Useful for tracking coverage as upstream data sources fill in.

- **Input**: `Pkmds.Rcl/wwwroot/data/` (override with `--data`)
- **Output**: `missing-flavor-report.txt` at the repo root (override with `--output`; pass `--output -` to write to stdout). The report file itself is gitignored — the script is the source of truth.

```sh
dotnet run tools/report-missing-descriptions.cs
dotnet run tools/report-missing-descriptions.cs -- --output -   # print to stdout
```

Categorizes entries as:
- **Runtime UI gaps** — no description AND no flavor; tooltip shows "No description available". Priority list.
- **Data completeness gaps** — missing description OR flavor but not both. `DescriptionService` renders gen-appropriate flavor, so these look fine in the UI today; chase them for 100% data parity.

### `tools/scrape-pokemondb-descriptions.cs`

Scrapes pokemondb.net for item and move descriptions that are missing from both PokeAPI and Showdown. Populates `tools/data/description-overrides.json`, which `generate-descriptions.cs` reads as a last-resort fallback (applied after PokeAPI `short_effect` and Showdown `shortDesc`).

- **Source**: https://pokemondb.net/item/\<slug> and https://pokemondb.net/move/\<slug>. Primary extraction target is the Effects section; falls back to the first row of the Game descriptions table when Effects is empty.
- **Output**: `tools/data/description-overrides.json` (committed — the scrape is slow, so the cache persists).
- **Rate limit**: pokemondb.net's `robots.txt` requires `Crawl-delay: 2`; the script defaults to 2500ms between requests. A full scrape of the gap list (~540 entries) takes ~22 minutes.

```sh
dotnet run tools/scrape-pokemondb-descriptions.cs                       # scrape all remaining gaps
dotnet run tools/scrape-pokemondb-descriptions.cs -- --limit 10         # smoke test
dotnet run tools/scrape-pokemondb-descriptions.cs -- --retry-notfound   # re-try prior 404s
dotnet run tools/scrape-pokemondb-descriptions.cs -- --force            # re-scrape everything
```

The scraper is incremental — re-running it only fetches entries that aren't already in the cache (or that were previously 404, unless `--retry-notfound` is passed). Ctrl+C saves partial progress. After running the scraper, rerun `generate-descriptions.cs` to pick up the new overrides; passing `--overrides` is optional because it auto-discovers `tools/data/description-overrides.json` from the repo root.

## Performance measurement

Used to compare WASM build configs (AOT, SIMD, trim, ...) on build time, deploy size, and runtime perf. See issue #883 for the ongoing investigation.

### `measure-publish.ps1`

Publishes `Pkmds.Web` in Release with optional MSBuild args, measures publish time and output size, and writes a labeled markdown report to `measurements/<Label>.md`. Cleans `release/` AND `obj/Release` for each project before publishing so build-time numbers reflect a true cold build.

```powershell
# Baseline (current settings)
./measure-publish.ps1 -Label baseline

# Flip one or more knobs
./measure-publish.ps1 -Label simd-on  -MSBuildArgs '-p:WasmEnableSIMD=true'
./measure-publish.ps1 -Label aot-on   -MSBuildArgs '-p:RunAOTCompilation=true'
./measure-publish.ps1 -Label aot-simd -MSBuildArgs '-p:RunAOTCompilation=true','-p:WasmEnableSIMD=true'

# Also run the runtime benchmark via Playwright (see below)
./measure-publish.ps1 -Label baseline -RunBenchmark
```

The `measurements/` directory is gitignored — these reports are local diagnostics, not artifacts to commit.

### `tools/bench/` — Playwright runtime harness

Drives the hidden `/bench` route in headless Chromium against a published build and appends runtime numbers to the same `measurements/<Label>.md` report.

- `Pkmds.Rcl/Components/Pages/Benchmark.razor` — unlinked `/bench` page that runs four PKHeX hot-path workloads (legality analysis, encryption roundtrip, search filter over a full populated SAV, encounter generation) and reports JSON via `#bench-result` + `console.log`.
- `Pkmds.Rcl/Services/BenchmarkRunner.cs` — synthetic SAV9SV factory + per-workload iteration + summary stats (mean/min/max/stddev/ops-per-sec).
- `tools/bench/run-bench.mjs` — Node script that serves the published `wwwroot`, launches Chromium, navigates to `/bench`, waits for `data-bench-state="done"`, scrapes the JSON, and appends a markdown section.

One-time setup:
```powershell
Push-Location tools/bench
npm install
npx playwright install chromium
Pop-Location
```

The bench page is hidden — no nav link, just reachable by typing `/bench`. Triggered automatically by `measure-publish.ps1 -RunBenchmark`.

## MudBlazor and Razor gotchas

- `ComboItem` (PKHeX) is a **sealed record** (reference type). Using `ComboItem?` in a Razor `@bind-Value` triggers CS8669 in the Razor-generated code — use `int?` with `.Value` for select bindings instead.
- `MudExpansionPanel` in MudBlazor 9 has no `IsInitiallyExpanded` or `IsExpanded` parameters (triggers MUD0002 analyzer error) — omit them; panels start collapsed by default.
- Razor integer literals in attributes must be parenthesised: `Value="@((int?)0)"`, not `Value="@0"`.
- Nullable reference type casts in Razor: `(string?)null` triggers CS8669 — use `default(string)` instead.
- `MudTable` `RowStyleFunc` signature is `Func<T, int, string>` (item + row index), not `Func<T, string>`.

## Local source references

Prefer reading local source over fetching from GitHub or relying solely on docs:

- **PKHeX**: macOS `~/Code/codemonkey85/PKHeX`, Windows `C:\Code\PKHeX`
- **MudBlazor**: macOS `~/Code/codemonkey85/MudBlazor`, Windows `C:\Code\MudBlazor`
- **Pokémon Showdown**: macOS `~/Code/codemonkey85/pokemon-showdown`, Windows `C:\Code\pokemon-showdown`
- **PokeAPI**: macOS `~/Code/codemonkey85/pokeapi`, Windows `C:\Code\pokeapi`

## Workflow

- **Tests**: Do not run `dotnet test` locally — leave it to the CI GitHub Actions workflow (`.github/workflows/buildandtest.yml`). Run only `dotnet format` and `dotnet build -c Debug` to verify changes locally.
- **PR review feedback**: (1) Review all comments and plan the response; (2) reply to each individual comment on the PR explaining what you're doing and why; (3) make code changes, commit, and push; (4) mark all addressed comments as resolved on the PR.

## Notes

- Respect the existing code style. Reference `.editorconfig` for formatting rules; Debug builds treat warnings as errors.
- Use `watch.ps1` for a consistent local dev experience.
- If WASM crypto errors occur, ensure `libman restore` has brought down `crypto-js` (or run LibMan in your IDE). CI publishes without requiring LibMan on the runner because the published output contains required assets.
