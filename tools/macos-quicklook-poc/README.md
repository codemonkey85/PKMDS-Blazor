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
│   ├── project.yml              # xcodegen spec — host app + preview + thumbnail extensions
│   ├── PkmdsHost/
│   │   └── PkmdsHostApp.swift   # Minimal SwiftUI placeholder
│   ├── PkmdsQuickLook/          # Preview extension (com.apple.quicklook.preview)
│   │   ├── PreviewViewController.swift   # QLPreviewingController + WKWebView
│   │   └── PkmdsQuickLook.entitlements
│   └── PkmdsQuickLookThumbnail/ # Thumbnail extension (com.apple.quicklook.thumbnail)
│       ├── ThumbnailProvider.swift       # QLThumbnailProvider (sprite + trainer card)
│       └── PkmdsQuickLookThumbnail.entitlements
├── build-and-run.sh             # CLI smoke test (no Xcode involved)
└── build-extension.sh           # Full pipeline: dotnet → xcode → sprites → install → qlmanage
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

### Full Quick Look extension (preview + thumbnail)

Builds the dylib, generates the Xcode project, builds + signs both extensions, copies bundled sprites into the thumbnail extension, deploys to `/Applications`, registers with Launch Services + PluginKit, and smoke-tests both.

```sh
./build-extension.sh
# Then in Finder, press Space on a .pk*/.sav file to preview.
# Thumbnail icons appear in icon/gallery view (may need qlmanage -r cache to flush).
```

After the first install, you can verify individually:

```sh
# Preview (full card in Quick Look panel)
qlmanage -p ../../TestFiles/Lucario_B06DDFAD.pk5

# Thumbnail (Finder icon-view thumbnail, 256px square → /tmp)
qlmanage -t -s 256 -o /tmp ../../TestFiles/Lucario_B06DDFAD.pk5
qlmanage -t -s 256 -o /tmp ../../TestFiles/Test-Save-Scarlet.sav
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

### 6. Prohibited extensions block ThumbnailsAgent dispatch for the _entire_ UTI

**Symptom:** `.gci` files (which are declared in `com.bondcodes.pkmds.save-file` alongside `.sav`/`.dat`/`.fla`) never get thumbnails. `qlmanage -t` hangs silently. pkd logs show the extension candidate but it is never dispatched.

**Cause:** macOS has a hard-coded list of "prohibited" filename extensions (`.sav`, `.dat`, `.fla` are three of them). If **any** extension in a UTI's `UTTypeTagSpecification` is prohibited, ThumbnailsAgent refuses to dispatch that UTI to a sandboxed extension — blocking every extension in the UTI, not just the prohibited ones.

**Fix:** Split into two UTIs. Keep non-prohibited extensions (`.gci`, `.dsv`, `.srm`) in `com.bondcodes.pkmds.save-file` and declare it in both the preview _and_ thumbnail extensions. Add a separate `com.bondcodes.pkmds.save-file-restricted` UTI for `.sav`/`.dat`/`.fla` and register it **only** with the preview extension — never with the thumbnail extension. ThumbnailsAgent never sees the prohibited extensions; quicklookd dispatches both UTIs fine.

**Verification:** `pluginkit -mAvvv -p com.apple.quicklook.thumbnail` — the thumbnail extension should list only UTIs without prohibited extensions.

### 7. Spotlight MDImporter dispatch path doesn't grant sandbox file access

**Background:** We tried adding a Spotlight metadata importer (`PkmdsSpotlight.mdimporter`) that wrote `kMDItemContentType = com.bondcodes.pkmds.save-file` into the Spotlight database for `.sav`/`.dat`/`.fla` files, hoping Finder would use the database value (our UTI) instead of the raw extension when dispatching Quick Look.

**What actually happened:** quicklookd did dispatch our preview extension via the Spotlight metadata path, but `com.apple.security.files.user-selected.read-only` does **not** receive a security-scoped access grant on this dispatch path. `Data(contentsOf: url)` threw a permissions error, producing a blank page-curl animation instead of our HTML preview. The UTI-database dispatch path (used for `.gci` etc.) grants file access correctly.

**Conclusion:** The MDImporter adds complexity and breaks the preview for the files it was supposed to help. The split-UTI approach (finding #6) is the correct solution. MDImporter files were removed.

### 8. Finder doesn't request thumbnails at the default 64 px icon size

**Symptom:** After a clean install, `.pk*` and `.gci` files show the generic document icon in Finder's icon view even though `qlmanage -t -x` produces correct thumbnails.

**Cause:** Finder's default icon size is 64 × 64 px. At that size the system renders the file icon itself without asking Quick Look extensions for a thumbnail. The threshold where Finder starts dispatching thumbnail requests is around 100 px.

**Fix:** Drag the Finder icon size slider to ≥ 100 px, or switch to Gallery view. After the first request, the result is cached and persists across size changes. If thumbnails still don't appear after increasing the size: `qlmanage -r && qlmanage -r cache`.

**Note on `qlmanage -t`:** Without the `-x` flag, `qlmanage -t` drives ThumbnailsAgent directly — it hung indefinitely in all our tests. Use `qlmanage -t -x <file>` to drive via quicklookd instead, which works reliably and writes the PNG to the output directory.

### 9. `QLThumbnailMinimumDimension` must be a plain integer, not a size dict

**Symptom:** `qlmanage -t` always returns "no matching extension" regardless of icon size, even though PluginKit reports the extension as registered.

**Cause:** iOS Quick Look uses `QLThumbnailMinimumSize: {width: N, height: N}` (a dict). macOS uses `QLThumbnailMinimumDimension: 0` (a plain integer). In xcodegen `project.yml`, using the dict form generates a malformed `Info.plist` value that the system either rejects or interprets as a very large minimum size, causing the extension to be filtered out for all practical thumbnail sizes.

**Fix:** In `project.yml` under the thumbnail extension's `NSExtensionAttributes`:
```yaml
QLThumbnailMinimumDimension: 0
```
Not `QLThumbnailMinimumSize`, not a dict. Confirm the generated `Info.plist` contains `<integer>0</integer>`.

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
| `qlmanage -p -x <file>` | Preview via quicklookd (remote mode) — needed for prohibited-extension files |
| `qlmanage -t -x -s 256 -o /tmp <file>` | Thumbnail via quicklookd — use `-x`; plain `-t` drives ThumbnailsAgent and tends to hang |
| `qlmanage -r && qlmanage -r cache` | Reset quicklookd and clear thumbnail cache |
| `killall pkd quicklookd` | Force PluginKit + Quick Look daemons to restart |
| `killall -9 "com.apple.quicklook.ThumbnailsAgent"` | Force ThumbnailsAgent restart (re-reads UTI→extension mapping from LS database) |
| `pluginkit -mAvvv -p com.apple.quicklook.thumbnail` | All thumbnail extensions and their registered UTIs |
| `mdls -name kMDItemContentType <file>` | What content type Spotlight has indexed for a file |
| `ls -t ~/Library/Logs/DiagnosticReports/PkmdsQuickLook*` | Extension crash reports (`.ips` files) |
| `/usr/bin/log show --last 30s --predicate 'process == "pkd"'` | PluginKit dispatch decisions in real time |
| `/usr/bin/log show --last 30s --predicate 'process == "ThumbnailsAgent"'` | ThumbnailsAgent UTI-lookup and dispatch decisions |

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
- **Trim warnings from PKHeX.Core** — IL2070 on `EntityBlank.GetBlank` and `ReflectUtil.GetAllProperties`, plus three "always throw" notes on `SaveBlock3*.PrintMembers`. None affect the read-only decode path.
- **POC outputs only the host display name** — `PkmdsHost` (literally), not branded. Cosmetic; address before any user-facing release.
- **dylib is 17 MB per extension** — each of the two extensions embeds its own copy (~34 MB total). Production would share a single copy from the host app's `Contents/Frameworks/` via `@loader_path/../../../../Frameworks`. Drop `cs.disable-library-validation` too once both are signed with the same Team ID.
- **Thumbnail trainer card border is single-colour** — `RB` (Red/Blue) and `GS` (Gold/Silver) get a gradient version code (per-character colour interpolation) but still a single-accent border. `SaveCard.cs` on Windows draws a true `LinearGradientBrush` border; replicating that in CoreGraphics requires clipping to the stroke region and filling with a gradient — doable but skipped for the POC.
- **Sprites are copied post-build by the shell script** — `build-extension.sh` runs `cp -r` after `xcodebuild` and re-signs. An Xcode Run Script build phase would be cleaner but adds xcodegen complexity.

## Macros for next session

If this POC graduates to a real `tools/macos-quicklook/` (no `-poc` suffix):

1. **Shared dylib** — move `PkmdsNative.dylib` to `PkmdsHost.app/Contents/Frameworks/` and update both extensions' `LD_RUNPATH_SEARCH_PATHS` to `@loader_path/../../../../Frameworks`. Drop the per-extension copy (saves ~17 MB).
2. **Developer ID signing + notarization** — once both extensions and the dylib share the same Team ID, drop `cs.disable-library-validation`. Add notarization to CI.
3. **Distribution channel** — DMG / Homebrew cask / Mac App Store.
4. **Gradient border for RB/GS trainer cards** — clip CGContext to the stroke region, then fill with a `CGGradient` diagonal. Mirrors the `LinearGradientBrush` approach in Windows `SaveCard.cs`.
5. **iOS share extension** — same dylib + `HtmlRenderer`; `QLPreviewingController` on iOS uses the same `QLPreviewingController` protocol. `ThumbnailProvider.swift` won't compile for iOS directly (uses `NSColor`/`NSBezierPath`) but the logic ports cleanly to `UIColor`/`UIBezierPath`.
