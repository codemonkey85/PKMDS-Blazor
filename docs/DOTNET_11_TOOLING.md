# .NET 11 developer tooling (#1389)

Observed on Windows x64 with stable SDK 11.0.100, Windows PowerShell and
headless Chromium, November 17, 2026. The app baseline was merged main
`a9fa4c3495f12b2d8a0b92d14dd10add169b2cdf`.

## Gateway and watch

From the repository root, `./watch.ps1` starts the HTTP profile at
`http://localhost:5283`. To select HTTPS explicitly, run from `Pkmds.Web`:

```powershell
dotnet watch --non-interactive run -c Debug -v n --launch-profile https
```

This profile listens on `https://localhost:7267` and `http://localhost:5283`.
Fresh browser contexts displayed the welcome UI on both profiles, with no page
exceptions. `/bench` returned the SPA document and
`/_framework/blazor.webassembly.js` returned HTTP 200 with JavaScript content.
The HTTPS probe ignored certificate errors, so it does not establish certificate
trust. No save files or existing browser profiles were used.

The probes set `DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER=1` and opened Chromium
explicitly. Windows PowerShell's execution policy blocked direct script invocation
in this environment; a child PowerShell process with process-scoped
`-ExecutionPolicy Bypass -File ./watch.ps1` ran the script without changing the
machine or user policy. Restricted NuGet access initially produced NU1900 during
restore; running with network access succeeded. Neither failure was a Gateway
hosting failure.

`watch-local-pkhex.ps1` previously failed before building: `dotnet watch` parsed
`-p:UseLocalPKHeX=true` as the deprecated `--project` option and looked for a
project named `UseLocalPKHeX=true`. The script now uses the explicit
`--property:UseLocalPKHeX=true` and `--property:PKHeXSourcePath=...` spellings.
Its MSBuild guard also used a property to inspect `@(PackageReference)` before
items were evaluated, so even a correctly supplied flag retained the NuGet
package. Selecting matching packages during item evaluation fixes that second
failure. CI verifies the package-to-project swap, an explicit path containing a
space and unchanged references in projects without a direct PKHeX package.
The default-path script built PKHeX.Core from synchronized checkout
`C:\Code\PKHeX`, commit `d6a3cfe872dd99db6627c0711f129a847181b5b9`, then
served the welcome UI, route fallback and framework JavaScript successfully.
No upstream API incompatibility appeared in this build. The HTTP-only profile
logged a missing HTTPS-redirection-port warning but served these requests.
The restore assets identify PKHeX.Core as `type: project`, and the build log
shows `C:\Code\PKHeX\PKHeX.Core\PKHeX.Core.csproj` being built; a successful
startup alone would not establish that the source override took effect.

Keep Gateway and the existing profiles. IDE attachment, a C# breakpoint hit,
edit/hot-reload behavior, certificate trust and native-platform debugging remain
unverified. These CLI/browser observations do not close those parts of #1389.

## File-based tools

Adopt stable [`#:include`](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk)
for one concrete duplicate: walking upward from the working directory to find
the `Pkmds.Rcl` marker and default data directory. Four tools include
`tools/shared/ToolPaths.cs`; each keeps its own argument handling, error messages,
parsing and serialization. The description-overrides search remains separate
because it searches for an overrides file rather than the repository marker.
CSV and HTML parsing have different contracts and remain in their respective
scripts. A shared project or package adds no benefit for this small helper.

Build the scripts independently; the solution build does not compile these apps:

```powershell
dotnet build tools/generate-descriptions.cs -c Debug
dotnet build tools/generate-tm-data.cs -c Debug
dotnet build tools/report-missing-descriptions.cs -c Debug
dotnet build tools/scrape-pokemondb-descriptions.cs -c Debug
dotnet format tools/generate-descriptions.cs --no-restore --verify-no-changes --include tools/shared/ToolPaths.cs
```

All four builds passed. File-based `dotnet format` resolved included source:
a scratch helper with malformed spacing failed verification, formatting fixed it,
and verification then passed. The command above checks the shared helper without
reformatting the existing generator bodies. CI builds all four apps and checks
the helper's formatting. The scraper was compiled without making HTTP requests.

## Output comparison

The original scripts and refactored scripts ran against the same frozen inputs:

- 191 input files, including PokeAPI CSVs, Showdown move/item text, description
  overrides and an offline TM HTML fixture with 12 round tables. Each table
  contained TM01 Tackle and TM02 Thunderbolt; the normal hardcoded TR map also ran.
- PokeAPI checkout `e4f981e5cb03c1fe33d436b1ee8e1197fbb0af84` had pre-existing
  local changes to `move_flag_map.csv` and `move_flags.csv`. Those files were
  preserved and snapshotted, not reset or silently treated as pristine upstream.
- Showdown checkout `f827a1d41064e0dec4c19c6ea0394fcf0c23479c`.

The three description JSON files, TM JSON and missing-description report matched
byte for byte before/after. A second comparison ran both versions from a nested
directory under a temporary `Pkmds.Rcl` marker, without output/data overrides;
all five outputs and their default locations matched. The nearest-marker search
therefore retained its working-directory behavior. Explicit overrides continued
to work. The TM fixture establishes equivalence for these inputs, not coverage
of every live Bulbapedia layout.

Input SHA-256 manifest, output hashes and logs are local diagnostics under
gitignored `measurements/1389/`. Generated application data is unchanged in this
PR. The input snapshots differ from the inputs used for some committed JSON, so
these runs establish refactor equivalence rather than an upstream data refresh.
The solution Debug build passed with zero warnings/errors and solution formatting
passed. No `dotnet test` ran locally; test execution remains in CI.

CLI references: [`dotnet watch`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-watch),
[`dotnet run` properties](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run).
