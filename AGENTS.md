# AGENTS.md

Guidance for WARP (warp.dev) when working in this repository.

## Quick commands

Prereqs
- Install the .NET SDK from `global.json`: `dotnet --list-sdks` to verify.
- Install Blazor WASM tooling once: `dotnet workload install wasm-tools`.
- IDE users can open `Pkmds.slnx`. CLI builds target individual projects (e.g., `Pkmds.Web.csproj`).

Local dev (Blazor WASM)
- From repo root (PowerShell): `./watch.ps1`
  - Equivalent from `Pkmds.Web`: `dotnet watch run -c Debug -v n --no-hot-reload`
  - Launch profiles expose: http `http://localhost:5283`, https `https://localhost:7267` (see `Pkmds.Web/Properties/launchSettings.json`).

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
- DI: `IAppState`, `IRefreshService`, `IAppService`, drag/drop, logging service, File System Access, MudBlazor, JS interop.
- PKHeX crypto bridged to JS: `RuntimeCryptographyProvider.Aes/Md5` set to Blazor providers at startup.

## CI/CD (GitHub Actions)

- `.github/workflows/buildandtest.yml` (dev branches; PRs to main):
  - Setup .NET (from `global.json`), install wasm tools, restore/build Web (Release), run `dotnet test`.
- `.github/workflows/main.yml` (main):
  - Setup .NET + wasm tools, restore, publish Web to `release/`, copy `index.html` → `404.html`, add `.nojekyll`, replace `%%CACHE_VERSION%%` in `service-worker.published.js`, deploy `release/wwwroot` to `gh-pages`.
- `.github/workflows/codeql.yml`: CodeQL for C# and JS/TS (manual C# build step).

## Notes

- Use `watch.ps1` for a consistent local dev experience.
- If WASM crypto errors occur, ensure `libman restore` has brought down `crypto-js` (or run LibMan in your IDE). CI publishes without requiring LibMan on the runner because the published output contains required assets.
