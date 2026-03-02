# AGENTS.md

Guidance for AI agents and coding assistants working in this repository.

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

## PKHeX.Core

This app depends heavily on [PKHeX.Core](https://github.com/kwsch/PKHeX). When implementing features, use the PKHeX WinForms app as a reference for how to leverage PKHeX.Core — both for UI/UX patterns and for understanding the correct API usage. The first place you should look when referencing PKHeX is ../PKHeX/ (or probably ~/Code/PKHeX), which contains the PKHeX WinForms app source code. The `PKHeX.Core` project within that solution is the library we consume here, and the WinForms app is a separate project that references it. The PKHeX WinForms app is a great reference for how to use PKHeX.Core effectively, as it demonstrates real-world usage of the library's APIs in a production application. If you can't find the source there, the PKHeX Wiki and source code are also valuable resources for understanding how to work with PKHeX.Core.

If you encounter bugs or limitations in PKHeX.Core while working on an issue or PR, note them in a code comment at the relevant site and report them on the GitHub issue or PR you are working on.

## Notes

- Respect the existing code style. Reference `.editorconfig` for formatting rules; Debug builds treat warnings as errors.
- Use `watch.ps1` for a consistent local dev experience.
- If WASM crypto errors occur, ensure `libman restore` has brought down `crypto-js` (or run LibMan in your IDE). CI publishes without requiring LibMan on the runner because the published output contains required assets.
