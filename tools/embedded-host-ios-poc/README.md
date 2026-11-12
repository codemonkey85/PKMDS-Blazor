# iOS Embedded Host PoC

Proof-of-concept for issue [#799](https://github.com/codemonkey85/PKMDS-Blazor/issues/799): a minimal iOS / iPadOS app that hosts PKMDS via `WKWebView` + `WKURLSchemeHandler` and round-trips a `.sav` through the [embedded host bridge](../../EMBEDDED_HOST_GUIDE.md).

The goal is an end-to-end existence proof — PKMDS really can be embedded in a native iOS app driven entirely by the JS bridge, not just the browser-based mock host in [`tools/embedded-host-poc/`](../embedded-host-poc/).

## What's in here

```
tools/embedded-host-ios-poc/
├── refresh-pkmds.sh                    # dotnet publish → strip PWA files → stage bundle
├── build-and-run.sh                    # refresh + xcodegen + xcodebuild + simulator
└── xcode/
    ├── project.yml                     # xcodegen spec
    └── PkmdsHost/
        ├── PkmdsHostApp.swift          # @main SwiftUI App
        ├── ContentView.swift           # Files-picker entry screen
        ├── EditorSheet.swift           # Sheet wrapping WebView + Done/Cancel toolbar
        ├── PkmdsWebView.swift          # UIViewRepresentable for WKWebView
        ├── PkmdsSchemeHandler.swift    # app://pkmds/ → bundle file lookup
        ├── PkmdsBridge.swift           # WKScriptMessageHandler + ready/saveExport
        ├── PickedSave.swift            # Plain-data structs
        └── Resources/PKMDS/            # ← populated by refresh-pkmds.sh (gitignored)
```

## Prerequisites

- **Xcode** with the iOS Simulator runtime installed (`xcode-select -p` must point at `/Applications/Xcode.app/Contents/Developer`).
- **.NET SDK** matching `global.json` (net11.0).
- **xcodegen** — `brew install xcodegen`.
- **wasm tools workload** — `dotnet workload install wasm-tools` (one-time).

## Build & run

```sh
./build-and-run.sh
```

Generates the Xcode project, builds the app for the iOS Simulator, and launches it. On first run (or with `--refresh`) it also runs `refresh-pkmds.sh` to publish PKMDS and stage the WASM bundle into the app's resources.

```sh
# Re-publish PKMDS after changing Web/Rcl/Core code
./refresh-pkmds.sh

# Pick a different simulator
SIM_NAME="iPhone 15 Pro" ./build-and-run.sh
```

To get a `.sav` into the simulator, drag one from Finder onto the Simulator window — it lands in **Files → On My iPhone**, where the in-app picker can reach it.

## How it works

```
┌─────────────────────────────────────────────────────────────┐
│  iOS app (SwiftUI)                                          │
│                                                              │
│  ┌─── ContentView ───────────────────────────────────────┐ │
│  │  [ Pick a save… ]   ← .fileImporter([.data])          │ │
│  └────────────────────────────────────────────────────────┘ │
│                          │                                   │
│                          ▼ (PickedSave: Data + filename)    │
│  ┌─── EditorSheet ───────────────────────────────────────┐ │
│  │  [Cancel]   <save filename>           [Done]           │ │
│  │  ┌── WKWebView ───────────────────────────────────┐  │ │
│  │  │  app://pkmds/index.html?host=poc-ios           │  │ │
│  │  │                                                 │  │ │
│  │  │  PkmdsSchemeHandler  →  bundle file lookup     │  │ │
│  │  │  PkmdsBridge         →  ready / saveExport     │  │ │
│  │  └─────────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────┘ │
│                          │                                   │
│                          ▼ (Done → requestExport → bytes)   │
│              UIActivityViewController (share sheet)          │
└─────────────────────────────────────────────────────────────┘
```

The share sheet stands in for the real-world handoff: in a native host integration, the exported bytes would be returned to the host app's storage (emulator save slot, file manager, whatever) instead of going through the share sheet.

## Architecture decisions

### Why xcodegen

Same reasons as `tools/macos-quicklook-poc/`: `project.yml` is reviewable in PRs, doesn't merge-conflict the way `.pbxproj` does, and `wwwroot/` can be added with `type: folder` so it becomes a **folder reference** (blue folder in Xcode). That preserves the published bundle's directory structure and file extensions through Xcode's resource processing — directly addresses the "Xcode resource processing" gotcha called out in #799.

### Why iOS 14.0 deployment target

Matches the lowest target of the canonical downstream consumer class (iOS retro emulators with native save management). `WKURLSchemeHandler` (iOS 11+), `WKScriptMessageHandler` (iOS 8+), SwiftUI App lifecycle (iOS 14+), and `.fileImporter` (iOS 14+) all work there.

### Why Files-picker only (no bundled sample save)

Keeps the PoC honest about what the integration actually exercises. Bundling a sample `.sav` would skip the security-scoped URL access dance — which a real host integration handles via its own storage layer, not via `.fileImporter`, but the file I/O surface is closer to reality this way.

### Why `app://pkmds/` and not `pkmds://`

WebKit refuses to install a `WKURLSchemeHandler` for any HTTP-like scheme it considers built-in. `app` is short, descriptive, and free of conflicts; the handler keys off the scheme prefix and ignores the host/path beyond mapping to bundle files.

## The not-yet-broke-us-but-likely-to gotchas

These are the things we know from the issue + `EMBEDDED_HOST_GUIDE.md` that this PoC has to get right:

### 1. Brotli `.br` compressed assets

Blazor publishes `*.dll.br` and `*.wasm.br` and the runtime references them via integrity-guarded loaders. The scheme handler must:
- Strip the `.br` suffix when computing the `Content-Type` (so `App.dll.br` returns `application/octet-stream`, not `application/x-brotli`).
- Set `Content-Encoding: br` on the response so WebKit decompresses transparently.

Same pattern for `.gz`. Reference: .NET MAUI `BlazorWebView/iOS/SchemeHandler.cs`.

### 2. WASM MIME type

`.wasm` must be served as `application/wasm`. Anything else and modern WebKit refuses to instantiate the module — the failure surface is a generic JS error from inside Blazor's loader, with the actual MIME-type check buried in `Microsoft.AspNetCore.Components.WebAssembly`.

### 3. Folder reference vs group

Xcode silently reformats certain files (`.json`, `.xml`, asset catalogs) when copied into a bundle as part of a regular group ("yellow folder"). `project.yml` adds the PKMDS bundle with `type: folder` so it's a folder reference ("blue folder") — Xcode treats the directory as opaque and copies it byte-for-byte.

### 4. Pre-Blazor service-worker gate

`Pkmds.Web/wwwroot/js/app.js` already skips SW registration when `?host=` is present, so we shouldn't see any SW registration attempts. Verify in Web Inspector → Application → Service Workers (empty).

### 5. Fingerprinted asset paths

Blazor 9/10 emits filenames like `blazor.webassembly.[hash].js`. Published `index.html` already embeds the fingerprinted path, so the scheme handler's straightforward path-to-file mapping just works — no regex needed. If a future release changes that, the handler is the place to add the rewrite.

### 6. Theme follows `prefers-color-scheme`

PKMDS forces theme to `System` in embed mode (per `EMBEDDED_HOST_GUIDE.md`), so the WebView inherits the simulator's light/dark setting through `traitCollection.userInterfaceStyle` automatically. No Swift-side wiring needed.

## Diagnostic toolbox

Useful while iterating:

| Command | Purpose |
|---|---|
| `xcrun simctl list devices available` | Find a simulator UUID |
| `xcrun simctl io booted recordVideo demo.mp4` | Screen-record the simulator |
| `xcrun simctl spawn booted log stream --level debug --predicate 'process == "PkmdsHost"'` | Stream app logs from the simulator |
| Web Inspector (Safari → Develop → Simulator → PKMDS Host) | DOM, JS console, network panel for `WKWebView` |
| `xcrun simctl uninstall booted com.bondcodes.pkmds.host.ios` | Force-clean install state |

Web Inspector is the load-bearing tool here — that's where you verify (a) `ready` posted, (b) no HTTP requests fire, (c) `saveExport` payload looks right.

## Out of scope

- App Store / signing / TestFlight — this is a developer demo.
- Multi-save management UI — single round-trip only.
- Persisting saves between launches — the share sheet hands bytes off and we forget.
- Wiring up to a real emulator — that's a downstream consumer's job.

## Macros for next session

If this PoC graduates to a real `tools/embedded-host-ios/` (no `-poc` suffix):

1. Real Apple Developer signing instead of `CODE_SIGNING_ALLOWED=NO`.
2. Replace the share sheet with a host-app-style storage handoff.
3. Decide whether to ship the PKMDS bundle baked into the app or as a downloadable asset pack (the published bundle is several MB).
4. Extract the `PkmdsSchemeHandler` + `PkmdsBridge` into a Swift package so downstream consumers can drop the bridge in without copying source.
