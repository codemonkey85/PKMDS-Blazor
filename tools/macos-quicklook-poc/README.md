# macOS Quick Look POC

Proof-of-concept for issue [#549](https://github.com/codemonkey85/PKMDS-Blazor/issues/549): a native macOS Quick Look extension that previews PKHeX-compatible files (`.pk1`–`.pk9`, `.pa8`, `.pb7`, `.pb8`, `.sav`) by calling PKHeX.Core through a NativeAOT-compiled dylib.

This POC validates that the planned approach is viable end-to-end — from binary parsing in C# all the way to a styled HTML preview rendered in Finder's Quick Look UI.

## What's in here

```
tools/macos-quicklook-poc/
├── PkmdsNative/                 # C# NativeAOT library (PKHeX.Core wrapper)
│   ├── Exports.cs               # C-exported pkmds_* entry points
│   └── PkmdsNative.csproj       # PublishAot=true, references Pkmds.Preview + Pkmds.Core
├── swift-cli/                   # Standalone CLI driver (kept for quick smoke-tests)
│   └── main.swift               # dlopen + dlsym invocation of the dylib
├── xcode/
│   ├── project.yml              # xcodegen spec — host app + .appex extension
│   ├── PkmdsHost/
│   │   └── PkmdsHostApp.swift   # Minimal SwiftUI placeholder
│   └── PkmdsQuickLook/
│       ├── PreviewViewController.swift   # QLPreviewingController + WKWebView
│       └── PkmdsQuickLook.entitlements   # Sandbox + library validation off
├── build-and-run.sh             # CLI smoke test (no Xcode involved)
└── build-extension.sh           # Full pipeline: dotnet → xcode → install → qlmanage
```

`HtmlRenderer` lives in `tools/preview-shared/` and is shared with the iOS (and future Windows) PoC — see [`../preview-shared/`](../preview-shared/).

## Prerequisites

- **Full Xcode** (not just Command Line Tools) — `xcode-select -p` must point at `/Applications/Xcode.app/Contents/Developer`. If it doesn't:
  ```sh
  sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
  ```
- **.NET SDK** matching `global.json` (net10.0)
- **xcodegen** — `brew install xcodegen`

## Build & run

### CLI smoke test (no extension involved)

Builds the AOT dylib and a Swift CLI that loads it via `dlopen`. Useful when iterating on `HtmlRenderer.cs` without the Xcode/codesign cycle.

```sh
./build-and-run.sh
# Outputs JSON + writes /tmp/pkmds-poc-pkm.html and /tmp/pkmds-poc-save.html
```

### Full Quick Look extension

Builds the dylib, generates the Xcode project, builds + signs the host app, deploys to `/Applications`, registers with Launch Services + PluginKit, and runs `qlmanage -p` as a smoke test.

```sh
./build-extension.sh
# Then in Finder, press Space on a .pk5/.sav file to preview.
```

After the first install, you can verify with:

```sh
qlmanage -p ../../TestFiles/Lucario_B06DDFAD.pk5
```

## Architecture decisions

### Why NativeAOT, not a subprocess

Tested both. NativeAOT wins for Quick Look's latency budget — Finder expects a preview within ~1s, and CLR cold-start eats most of that. Calling through `dlsym` with NativeAOT measures in single-digit milliseconds.

### Why we don't reuse the Razor components

The existing `Pkmds.Rcl` components require the Blazor runtime, MudBlazor's JS/CSS, and DI services (`IAppState`, `IRefreshService`). Even with `HtmlRenderer` for static SSR, the components are interactive editors wired to app state — not self-contained read-only previews. We hand-roll a small HTML template in `HtmlRenderer.cs` and pull in pure-C# helpers from `Pkmds.Core` (sprite URL resolution, `IsValidSpecies`, `GetMaxPP`, etc.).

### Why `Pkmds.Core` and not `Pkmds.Rcl`

`Pkmds.Core` only references `PKHeX.Core` and is AOT-clean. `Pkmds.Rcl` is a Razor Class Library that drags in MudBlazor, Tailwind, and the Blazor stack — none of which AOT-compiles cleanly. The PokeAPI sprite URL helpers (with full Mega/regional/gender/Alcremie/Vivillon coverage) live in `Pkmds.Core/Utilities/PokeApiSpriteUrls.cs` so both the web app and the QL extension share them.

## The five non-obvious things that broke us (and how to spot them)

These are in roughly the order we hit them.

### 1. `xcode-select` pointing at Command Line Tools

**Symptom:** `xcodebuild` fails with `tool 'xcodebuild' requires Xcode, but active developer directory '/Library/Developer/CommandLineTools' is a command line tools instance`.

**Fix:** `sudo xcode-select -s /Applications/Xcode.app/Contents/Developer`. The build script can also set `DEVELOPER_DIR=...` per-invocation to avoid the global switch.

### 2. Missing `QuickLookUI.framework` link

**Symptom:** Quick Look silently falls back to the default file-icon preview. No crash, no log message — the extension just doesn't run.

**Cause:** Swift's `import Quartz` brings in the module for typechecking but the linker only includes referenced symbols. Conformance to the `QLPreviewingController` protocol alone doesn't generate a strong reference, so the linker dead-strips the framework. At runtime the system tries to instantiate our class, can't resolve the protocol's symbols, gives up.

**Fix:** Explicit `sdk: QuickLookUI.framework` (and `WebKit.framework`) in `project.yml`'s `dependencies`. Verify with `otool -L`.

### 3. Build-directory registration leak

**Symptom:** `pluginkit -mAvvv -i <bundle.id>` shows the extension at the build dir path even after `cp` to `/Applications`. Quick Look says "Extension <id> not found" because it tries to load from a now-deleted or unprivileged path.

**Cause:** `lsregister -f $BUILD_DIR_APP` was creating the registration first; the later `lsregister -f /Applications/...` didn't supersede it.

**Fix:** Build script now does `lsregister -u` on both old paths *before* installing, then registers only the `/Applications` copy.

### 4. `lsregister` alone isn't enough — need `pluginkit -a`

**Symptom:** Extension is in Launch Services (visible via `lsregister -dump`) and BTM marks it `disposition=[enabled, allowed, notified]`, but `pluginkit -mAvvv -i <id>` returns `(no matches)` and Quick Look doesn't dispatch.

**Cause:** PluginKit's plugin database is separate from Launch Services. `lsregister -f` updates LS but doesn't always trigger pkd to pick up the extension.

**Fix:** Build script explicitly runs `pluginkit -a /Applications/PkmdsHost.app/Contents/PlugIns/PkmdsQuickLook.appex` followed by `pluginkit -e use -i com.bondcodes.pkmds.host.quicklook`.

### 5. Library validation rejects the AOT dylib

**Symptom:** Extension crashes immediately (`~/Library/Logs/DiagnosticReports/PkmdsQuickLook-*.ips`) with:
```
Library not loaded: @rpath/PkmdsNative.dylib
Reason: ... mapping process and mapped file (non-platform) have different Team IDs
```

**Cause:** Hardened runtime + ad-hoc-signed extension + ad-hoc-signed dylib. Each ad-hoc signature counts as its own "team", so amfid (Apple Mobile File Integrity Daemon) rejects loading the dylib into the extension's process.

**Fix:** Add `com.apple.security.cs.disable-library-validation` to the extension's entitlements. **For Mac App Store / Developer ID distribution this entitlement isn't needed** — sign both the extension and the dylib with the same Team ID and library validation passes.

## Diagnostic toolbox

Commands that earned their keep during this POC.

| Command | What it tells you |
|---|---|
| `pluginkit -mAvvv -i <bundle.id>` | Is PluginKit aware of our extension? At what path? |
| `pluginkit -mAvvv -p com.apple.quicklook.preview` | All registered Quick Look extensions |
| `/System/.../lsregister -dump \| grep <bundle.id>` | Launch Services view: claimed UTIs, plugin identifiers, version |
| `codesign -d --entitlements - <path>` | What entitlements does the binary actually have? |
| `codesign -d -v <path>` | Code signature flags (`adhoc`, `runtime`, etc.) |
| `otool -L <executable>` | Linked frameworks/dylibs and their install names |
| `otool -D <dylib>` | The dylib's own install name (e.g. `@rpath/...`) |
| `qlmanage -p <file>` | Trigger a preview without Finder; useful for repeatable tests |
| `qlmanage -r && qlmanage -r cache` | Reset quicklookd and clear thumbnail cache |
| `killall pkd quicklookd` | Force PluginKit + Quick Look daemons to restart |
| `ls -t ~/Library/Logs/DiagnosticReports/PkmdsQuickLook*` | Extension crash reports (`.ips` files) |
| `/usr/bin/log show --last 30s --predicate 'process == "pkd"'` | PluginKit dispatch decisions in real time |

The two log filters that broke this POC open:

```sh
# Why is the extension being filtered out?
/usr/bin/log show --last 30s --predicate \
  'process == "pkd" AND (composedMessage CONTAINS "Final plugin" OR composedMessage CONTAINS "Candidate plugin")'

# Why did the extension crash on load?
/usr/bin/log show --last 30s --predicate \
  'composedMessage CONTAINS[c] "PkmdsNative.dylib" OR composedMessage CONTAINS[c] "PkmdsQuickLook"'
```

Note: `/usr/bin/log` rather than bare `log` — zsh has a builtin that intercepts the bare invocation.

## Known POC limitations

- **Ad-hoc signing only** — works for local install. Distribution (Mac App Store or Developer ID + notarization) requires a real signing identity and Team ID; once that's in place, drop `cs.disable-library-validation`.
- **Trim warnings from PKHeX.Core** — IL2070 on `EntityBlank.GetBlank` and `ReflectUtil.GetAllProperties`, plus three "always throw" notes on `SaveBlock3*.PrintMembers`. Carried over from the original AOT POC findings; none affect the read-only decode path we exercise.
- **POC outputs only the host display name** — `PkmdsHost` (literally), not branded. Cosmetic; address before any user-facing release.
- **dylib is 17 MB** — includes resource string tables for all PKHeX-supported games. Acceptable for an extension bundled with a desktop app; would benefit from string-table trimming if size matters.

## Macros for next session

If this POC graduates to a real `tools/macos-quicklook/` (no `-poc` suffix):

1. Set up Developer ID signing (drop `disable-library-validation`).
2. Set up notarization in CI.
3. Decide distribution channel (DMG / Homebrew cask / Mac App Store).
4. Consider an iOS share extension using the same dylib and `HtmlRenderer`.
