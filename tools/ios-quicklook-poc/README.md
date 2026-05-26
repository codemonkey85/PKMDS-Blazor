# iOS / iPadOS Quick Look POC

Proof-of-concept for issue [#796](https://github.com/codemonkey85/PKMDS-Blazor/issues/796): a native iOS / iPadOS Quick Look extension that previews PKHeX-compatible files (`.pk1`–`.pk9`, `.pa8`, `.pb7`, `.pb8`, `.sav`) by embedding PKHeX.Core compiled with NativeAOT.

This is the iOS companion to [`tools/macos-quicklook-poc/`](../macos-quicklook-poc/) (issue [#549](https://github.com/codemonkey85/PKMDS-Blazor/issues/549) / PR [#795](https://github.com/codemonkey85/PKMDS-Blazor/pull/795)). The two POCs share `HtmlRenderer.cs` and the rendered HTML; the platform shell — and the build pipeline — diverges.

## What's in here

```
tools/ios-quicklook-poc/
├── PkmdsQuickLook/                           # C# iOS app extension (Microsoft.iOS.Sdk)
│   ├── PreviewViewController.cs              # UIViewController + IQLPreviewingController + WKWebView
│   ├── Info.plist                            # NSExtension dict + QL principal class
│   └── PkmdsQuickLook.csproj                 # net10.0-ios + IsAppExtension=true + PublishAot=true
├── xcode/
│   ├── project.yml                           # xcodegen — host SwiftUI app only
│   └── PkmdsHost/PkmdsHostApp.swift          # Minimal placeholder
├── build-extension.sh                        # dotnet → xcodegen → xcodebuild → embed .appex into PlugIns/
└── README.md
```

`HtmlRenderer` lives in `tools/preview-shared/` and is shared with the macOS (and future Windows) PoC — see [`../preview-shared/`](../preview-shared/).

## Prerequisites

- **Xcode** (full install, not just Command Line Tools) with the iOS Simulator runtime.
- **.NET SDK** matching `global.json` (net10.0).
- **`ios` .NET workload** — `sudo dotnet workload install ios` (one-time).
- **xcodegen** — `brew install xcodegen`.

## Build & run

### iOS Simulator (default)

```sh
./build-extension.sh
SIM_NAME="iPhone 15 Pro" ./build-extension.sh
```

`dotnet build -r iossimulator-arm64` produces a Mono-runtime `.appex` (no AOT — Microsoft.iOS.Sdk's Publish target rejects simulator RIDs). xcodebuild builds the SwiftUI host for the simulator. The script copies the `.appex` into `PkmdsHost.app/PlugIns/`, installs, launches.

Drag a `.pk*`/`.sav` from Finder onto the Simulator window — it lands in **Files → On My iPhone**. Long-press the file to trigger Quick Look.

### iOS device (NativeAOT)

```sh
./build-extension.sh --device
```

`dotnet publish -r ios-arm64` produces an AOT-compiled `arm64` `.appex` (~24 MB). xcodebuild builds the host app for `generic/platform=iOS`. The host app is left **unsigned** — open the project in Xcode, set your Team / signing identity, and run on a connected device.

## Why the architecture diverges from the macOS POC

The macOS POC uses Swift `PreviewViewController` calling into a standalone NativeAOT C# `.dylib` via `dlopen`/`dlsym`. The first attempt at this iOS POC tried to mirror that. **It can't be done in .NET 10.** The five things that forced the pivot:

1. **`Microsoft.NET.Sdk` rejects iOS RIDs for AOT.** `<RuntimeIdentifier>iossimulator-arm64</RuntimeIdentifier>` + `<PublishAot>true</PublishAot>` errors with `NETSDK1203: Ahead-of-time compilation is not supported for the target runtime identifier 'iossimulator-arm64'`. Same for `ios-arm64`. iOS / Android RIDs aren't on the supported AOT list — only `osx-*`, `linux-*`, `win-*`, `browser-wasm`.
2. **`Microsoft.iOS.Sdk` + `OutputType=Library` skips AOT silently.** Setting `<TargetFramework>net10.0-ios</TargetFramework>` and publishing with `<NativeLib>Shared</NativeLib>` produces only managed `.dll`s — no native compilation. The iOS SDK classifies an `OutputType=Library` project as "iOS class library" and runs the AOT compiler only when the project is `OutputType=Exe` or `IsAppExtension=true`. (See `Xamarin.Shared.Sdk.targets:42-44` in the iOS workload pack.)
3. **`Microsoft.iOS.Sdk`'s Publish target rejects simulator RIDs.** `Xamarin.Shared.Sdk.Publish.targets:14` errors with "A runtime identifier for a device architecture must be specified" for any `iossimulator-*` RID. Simulator validation has to use `dotnet build` (not publish) — which doesn't run NativeAOT but does produce a runnable `.appex` via Mono.
4. **Apple's Xcode-version pin is strict.** The 26.0 SDK pack requires Xcode 26.0; the 26.2 pack requires Xcode 26.3. With Xcode 26.4.1 installed, both fail with `error E0191: This version of .NET for iOS requires Xcode <X>` until `<ValidateXcodeVersion>false</ValidateXcodeVersion>` is set.
5. **Bundle metadata is mandatory.** `ApplicationId` (→ `CFBundleIdentifier`) is required; missing it errors with "A bundle identifier is required."

### What the pivot looks like

Instead of two languages and an FFI:
- The extension is a single `Microsoft.iOS.Sdk` project with `IsAppExtension=true` and `PublishAot=true`.
- `PreviewViewController` is C# — `UIViewController` + `IQLPreviewingController` + `WKWebView` from the `Microsoft.iOS` bindings.
- `HtmlRenderer` is in `tools/preview-shared/Pkmds.Preview` — pure C#, no platform dependencies, shared with the macOS POC.
- The Xcode project is host-only (SwiftUI). `build-extension.sh` runs `dotnet publish`, then `xcodebuild`, then copies the `.appex` into `PkmdsHost.app/PlugIns/`.

It's actually a *cleaner* iOS architecture (one toolchain, no FFI ABI to maintain) — but the deviation from the macOS POC was driven by .NET 10's iOS SDK behaviour, not preference.

## Verified findings

These were established with `dotnet workload version 26.2.10233` + Xcode 26.4.1 on macOS 26.4.

- ✅ `dotnet publish -c Release -r ios-arm64` produces a 24 MB `arm64` Mach-O `.appex` with PKHeX.Core + Pkmds.Core under NativeAOT.
- ✅ `Microsoft.iOS` bindings cover everything the extension needs: `IQLPreviewingController` (in `QuickLook` namespace, not `QuickLookUI`), `UIViewController`, `WKWebView`, `NSData`, `NSError`.
- ✅ Trim warnings carry over from macOS POC: `IL2104` on `PKHeX.Core`, three "always throw" notes on `SaveBlock3*.PrintMembers`. The decode path we exercise isn't affected.
- ✅ `Info.plist`'s `NSExtension` keys round-trip cleanly through Microsoft.iOS.Sdk's plist merge — the SDK auto-fills `CFBundleIdentifier`, `MinimumOSVersion`, `CFBundleSupportedPlatforms` from MSBuild properties without overwriting the hand-authored extension keys.
- ✅ End-to-end on the iOS Simulator: tap a `.pk*`/`.sav` in Files.app and the extension renders the styled HTML preview — confirmed against full-Box B/W save fixtures and a `.pk5` entity.
- ⚠️ Simulator builds use Mono (no NativeAOT) — `.appex` is ~77 MB with managed assemblies + AOT data files, vs 24 MB for the AOT'd device build.

### The two non-obvious gotchas that broke us during simulator iteration

1. **`SIGKILL (Code Signature Invalid)` on extension launch.** iOS extensions are amfid-validated even on the simulator — the host app gets a pass without a signature, but a plain `cp` of the `.appex` from `dotnet build` into `PkmdsHost.app/PlugIns/` produces a binary that crashes during dyld's `__LINKEDIT` mapping. `build-extension.sh` ad-hoc signs the `.appex` (and every embedded `.dylib`) after embedding.
2. **Files.app's `contentTypesToPreviewTypes` whitelist.** Even with the extension correctly registered for our custom UTI (`pluginkit -m` confirms it), Files refused to dispatch to it and logged `Could not determine preview item: ... not part of contentTypesToPreviewTypes`. Files routes preview based on the UTI's *abstract conformance* — adding `public.composite-content` to `UTTypeConformsTo` (alongside `public.data`) lands the type in the whitelist and Files dispatches correctly.

## Open work past this POC

- **Real device verification.** The build script produces an unsigned host app for `ios-arm64`. Deploying requires opening the project in Xcode and setting a signing Team. The 120 MB extension memory ceiling can only be verified on hardware (the simulator doesn't enforce it).
- **Signing the embedded `.appex`.** `Microsoft.iOS.Sdk` defaults to `EnableCodeSigning=false` here; the build script ad-hoc signs the `.appex` post-embed for simulator runs. Real distribution will need a real Developer ID — set `CodesignKey` / `CodesignProvision` in the csproj or replace the post-embed `codesign --sign -` step. App Store submission requires the extension and host signed with the same Team ID (no `cs.disable-library-validation` escape hatch).
- **Xcode version pin.** `<ValidateXcodeVersion>false</ValidateXcodeVersion>` is a soft bypass for E0191. The right fix is to track the Microsoft.iOS.Sdk version that recommends the locally-installed Xcode.
- **Resolve the orientations warning** in the simulator path (`warning : Supported iPhone orientations have not been set`). Cosmetic; comes from Microsoft.iOS.Sdk wanting `UISupportedInterfaceOrientations` in the *extension's* Info.plist, not the host's.

## Diagnostic toolbox

| Command | What it tells you |
|---|---|
| `dotnet publish -r ios-arm64 -v normal \| grep IL` | Trim/AOT warnings from PKHeX.Core |
| `file <appex>/PkmdsQuickLook` | Confirm Mach-O arch — `arm64` for both device (AOT) and simulator (Mono) on Apple Silicon |
| `/usr/libexec/PlistBuddy -c "Print" <appex>/Info.plist` | Verify NSExtension keys made it through |
| `xcrun simctl spawn booted log stream --level debug --predicate 'process == "PkmdsQuickLook"'` | Stream extension logs from the simulator |
| `xcrun simctl uninstall booted com.bondcodes.pkmds.host.ios` | Force a clean re-install |
| Web Inspector (Safari → Develop → Simulator → "PkmdsQuickLook") | DOM / console / network for the `WKWebView` (only visible while a preview is active) |
| `xcrun simctl spawn booted pluginkit -mAvvv -p com.apple.quicklook.preview` | Confirm the extension is registered with PluginKit |
| `codesign -dv <appex>` | Verify the embedded `.appex` has the expected ad-hoc signature |
| `ls -t ~/Library/Logs/DiagnosticReports/PkmdsQuickLook*.ips` | Extension crash reports (`SIGKILL (Code Signature Invalid)` etc.) |

To copy a test fixture into Files.app's "On My iPhone":

```sh
cp /path/to/fixture.pk5 \
   ~/Library/Developer/CoreSimulator/Devices/<SIM-UUID>/data/Containers/Shared/AppGroup/*/File\ Provider\ Storage/
```

Drag-drop into the simulator window targets whichever app claims the file's UTI, so a `.pk*` drop opens PkmdsHost (since it's the only registered handler) instead of landing in Files. The `cp` route avoids that.

## Out of scope

- App Store / TestFlight distribution.
- iCloud / sync (Quick Look is read-only).
- Bundling sample fixtures.
- Wiring up to a real iOS host app for actual end-user use — that's downstream work after the POC validates the architecture.
