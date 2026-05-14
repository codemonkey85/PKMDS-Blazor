# Embedding PKMDS in a host application

Guide for developers integrating PKMDS into another application (iOS / iPadOS app, desktop wrapper, any other JS-capable container). Tracks the contract shipped in [#787](https://github.com/codemonkey85/PKMDS-Blazor/issues/787).

PKMDS today ships an **embedded host mode**: a runtime flag that adapts the published `wwwroot/` so it can be hosted inside a `WKWebView` (or any other JS-capable host environment), driven by a small JS interop bridge instead of the file picker and File System Access API. This document describes what the host has to deliver to drive it.

---

## 1. What PKMDS gives you

A static Blazor WebAssembly site (`wwwroot/` produced by `dotnet publish Pkmds.Web -c Release`) that, when navigated to with `?host=<name>` in the URL, transforms itself into a host-driven editor:

- Drawer, hamburger, title, theme buttons, GitHub link, in-app-browser warning, service worker registration → **all suppressed**
- Pokémon Bank tab, Trade tab, PokeAPI sprite overlay, theme picker, sprite-style picker, haptics toggle, Advanced settings tab → **all hidden**
- Theme is forced to `System` and live-follows `prefers-color-scheme`
- A JS bridge (`window.PKMDS.host`) appears on the page, ready to receive `loadSave` / `requestExport` calls and post `ready` / `saveExport` messages back

You get an editor surface; you don't get load/save UI. Those are your job.

## 2. What the host has to provide (iOS / iPadOS)

### a) WKWebView with `WKURLSchemeHandler`

The published site uses `<base href="/" />`, so it must be served from the root of *some* origin. The cleanest fit on iOS is a custom URL scheme — e.g. `app://pkmds/` — backed by a `WKURLSchemeHandler` that streams files out of your app bundle.

- Bundle the entire `wwwroot/` under a known resource directory (e.g. `Resources/PKMDS/`).
- Register the scheme on the `WKWebViewConfiguration` *before* creating the `WKWebView` (`setURLSchemeHandler:forURLScheme:` — Apple disallows `http`/`https` here).
- For each request: map URL path → file in your bundle, set `Content-Type` correctly. The MIME types that matter and that iOS will *not* infer for you:
  - `.wasm` → `application/wasm`
  - `.dll` / `.dat` → `application/octet-stream`
  - `.br` (Brotli) → original type + `Content-Encoding: br`
  - `.json`, `.js`, `.css`, `.html`, `.png`, `.svg`, `.woff2` → standard
- Reference: .NET MAUI's `BlazorWebView` iOS implementation (`SchemeHandler.cs` in the maui repo) is the canonical example of this pattern.

Then `webView.load(URLRequest(url: URL(string: "app://pkmds/?host=delta")!))`. Load the **root path**, not `/index.html` — Blazor's router only registers `@page "/"`, so `/index.html` falls through to PKMDS's `NotFound` component. Map empty paths in your scheme handler to `index.html` server-side; let the URL Blazor sees stay at `/`.

### b) `WKScriptMessageHandler` for outbound messages

`host.js` posts to `window.webkit.messageHandlers.pkmds.postMessage(...)`. You must register a handler under that exact name:

```swift
config.userContentController.add(self, name: "pkmds")
```

Then handle two `kind`s in `userContentController(_:didReceive:)`:

| `kind`        | Payload                                  | What you do                                                                       |
|---------------|------------------------------------------|-----------------------------------------------------------------------------------|
| `ready`       | `{}`                                     | Now safe to call `loadSave`. Don't call before this — Blazor may not have booted. |
| `saveExport`  | `{ data: "<base64>", fileName: "..." }`  | Decode `data`, write back to whatever storage your app owns, dismiss your sheet.  |

Body arrives as `[String: Any]` (NSDictionary in objc). Defensive cast `message.body as? [String: Any]`; treat unknown `kind` values as no-ops so PKMDS can extend the protocol later.

### c) Inbound calls into PKMDS (Swift → JS)

Two methods on `window.PKMDS.host` — both async, both invoked via `evaluateJavaScript`:

| Call                                | When                                     | Returns                                                                       |
|-------------------------------------|------------------------------------------|-------------------------------------------------------------------------------|
| `loadSave(base64Bytes, fileName)`   | After `ready`. Hands the active save in. | `Promise<bool>` — `true` on success                                           |
| `requestExport()`                   | When user taps your "Done" button.       | `Promise<bool>`; the actual bytes arrive *later* via the `saveExport` message |

`bytesBase64` is **base64-encoded raw save bytes** (or a Manic EMU `.zip` wrapper, which PKMDS detects from `fileName`). Not a path, not a `Data` reference. From Swift:

```swift
let b64 = saveData.base64EncodedString()
let escapedName = fileName.replacingOccurrences(of: "'", with: "\\'")
let js = "window.PKMDS.host.loadSave('\(b64)', '\(escapedName)')"
webView.evaluateJavaScript(js, completionHandler: nil)
```

`requestExport` does **not** return the bytes from its promise. PKMDS posts them back asynchronously via the `saveExport` message handler. Your "Done" handler should call `requestExport()`, then *wait for* `saveExport` before dismissing. Use a reasonable timeout — typical Gen 9 save round-trip is well under a second, but a hung Blazor side shouldn't strand your UI forever.

### d) Host chrome you must supply yourself

PKMDS deliberately renders **no save/discard buttons** in embed mode. The host owns:

- The navigation bar with **Done** and **Cancel** (or your native equivalent).
  - **Done** → call `requestExport()`, await `saveExport`, persist bytes, dismiss.
  - **Cancel** → just dismiss; don't call `requestExport`.
- The visible app title / breadcrumb.
- Any "share to other app", "rename", "delete save" affordances — out of scope for PKMDS.

If you render two "Done" buttons (yours + PKMDS's), users get confused — that's why PKMDS hides its export UI when `?host=` is present.

## 3. The exact bridge contract

### Native → Web

```js
// Promise<bool>; true on success
await window.PKMDS.host.loadSave(bytesBase64, fileName)

// Promise<bool>; bytes arrive later via the 'saveExport' outbound message
await window.PKMDS.host.requestExport()
```

### Web → Native (posted to `webkit.messageHandlers.pkmds`)

```js
{ kind: "ready" }
{ kind: "saveExport", data: "<base64>", fileName: "Black.sav" }
```

That's the entire surface area today. The contract is intentionally tiny so it can grow without breaking existing hosts.

## 4. End-to-end lifecycle

1. User opens a save in your app → you stage `Data` + filename in memory.
2. Push/sheet a view controller hosting your `WKWebView`.
3. Web view loads `app://pkmds/index.html?host=<your-host-name>`.
4. PKMDS boots, settings load, theme initializes → posts `ready`.
5. Your handler receives `ready` → calls `loadSave(b64, fileName)`.
6. PKMDS renders boxes/party. User edits.
7. User taps your **Done** → you call `requestExport()`.
8. PKMDS posts `saveExport` with new bytes → you decode, write back to disk, dismiss.

Cancel skips steps 7-8 entirely.

## 5. Constraints to know going in

- **iOS 16.4+ minimum (WKWebView WASM SIMD).** PKMDS publishes with `WasmEnableSIMD=true` + `RunAOTCompilation=true` (see #883). WKWebView in iOS / iPadOS &lt; 16.4 ships a WebKit without WASM SIMD support — `WebAssembly.compileStreaming` fails on the `v128.const` opcode, and PKMDS's static feature test in `index.html` redirects to `BrowserNotSupported.html`. From the user's perspective inside your app this looks like "we opened the editor and got a 'browser not supported' page", which is confusing UX. **Set your iOS app's `IPHONEOS_DEPLOYMENT_TARGET` to `16.4` or later** if you depend on the standard PKMDS bundle. If you must support earlier iOS, the project may publish a compat-only mirror on a separate URL (no AOT, no SIMD) — open an issue to discuss before integrating against it; otherwise point your host at the standard URL and require iOS 16.4+.
- **WASM cold boot cost.** Several hundred ms to a couple of seconds depending on device — design your transition (loading shimmer, etc.) accordingly. AOT-compiled WASM (12+ MB brotli) is larger than pre-AOT builds, so first launch from a cold cache is noticeably slower; subsequent loads from the WKWebView HTTP cache are fast.
- **No service worker, no IndexedDB persistence.** PKMDS clears its own SW registration in embed mode, and its IndexedDB-backed features (Pokémon Bank, save backups) are hidden. Each session is one-shot: load → edit → export. State doesn't persist between embeds; if you want backups, that's your app's responsibility.
- **No external network.** PKMDS suppresses PokeAPI sprite fetches in embed mode, so all sprites come from the bundled low-res set. Six form-picker dialogs (Alcremie, Vivillon, Furfrou, Minior, Pumpkaboo, FlowerColor) lose preview images and degrade to text dropdowns. Acceptable per the issue scoping; bundling HOME sprites is a future-tense improvement.
- **Manic EMU `.zip` wrappers** are accepted on input (PKMDS detects from the filename), but `requestExport` returns **raw save bytes only** — never the rebuilt ZIP. If your host wraps saves in any container format, unwrap before `loadSave` and re-wrap after `saveExport`.
- **iOS WebKit doesn't implement `navigator.vibrate`**, so the haptics toggle is hidden; if you want haptic feedback in your host UI, do it natively.
- **Theme follows `prefers-color-scheme`.** To override, set `UIUserInterfaceStyle` on your view controller (`WKWebView` inherits it).
- **Programmatic file pickers won't work** inside the embedded site (no user gesture), but you shouldn't be using them anyway — `loadSave` replaces that path entirely.

## 6. How to develop and test before you have native hosting

The same code path works in any browser. Run `./watch.ps1` and visit `http://localhost:5283/?host=test`:

- All the embed-mode UI gating activates.
- `window.webkit.messageHandlers.pkmds` is absent, so outbound messages fall back to `console.log("[PKMDS.host] →", kind, payload)`. You can watch the protocol live in DevTools.
- A built-in dev-only file picker appears under `?host=test` specifically — pick a `.sav` from disk and it round-trips through `loadSave` exactly as a real host would.
- Drive `requestExport()` from the console and watch the `saveExport` message print.

Build your Swift bridge against this observable shim before you have a working `WKURLSchemeHandler` — the contract is identical.

### Browser-based mock host page (PoC)

For a more complete end-to-end demonstration — including mock Done/Cancel chrome, a file picker outside PKMDS, and a live message log — see [`tools/embedded-host-poc/`](tools/embedded-host-poc/README.md).

The mock host page embeds PKMDS in an `<iframe src="/?host=poc">` and drives it entirely through `window.postMessage`, simulating exactly what a native host app must do. It runs without Xcode, a device, or a real `WKWebView`. Open `tools/embedded-host-poc/index.html` directly in a browser while the dev server is running (`./watch.ps1`).

### iOS / iPadOS PoC (real `WKWebView` + `WKURLSchemeHandler`)

For the native-app version of the same round-trip — a minimal SwiftUI app that hosts PKMDS via `WKWebView`, bundles `wwwroot/` under a custom URL scheme, and exercises the bridge end-to-end on the iOS Simulator — see [`tools/embedded-host-ios-poc/`](tools/embedded-host-ios-poc/README.md). That PoC is where to look for working Swift implementations of `WKURLSchemeHandler` (Brotli + WASM MIME handling) and `WKScriptMessageHandler` for `ready` / `saveExport`.

## 7. Getting a publishable bundle

```bash
dotnet publish Pkmds.Web/Pkmds.Web.csproj -c Release -o release --nologo
```

The deployable tree is `release/wwwroot/`. Copy that into your iOS app bundle (or a downloaded asset pack — the Brotli-compressed payload is several MB).

You can drop the PWA-only files (`manifest.webmanifest`, `service-worker.js`, `service-worker.published.js`, `BrowserNotSupported.html`, `staticwebapp.config.json`) — none of them are reachable from embed mode.

## 8. Source pointers (for when something unexpected happens)

- Bridge entry points: `Pkmds.Rcl/wwwroot/js/host.js`
- Managed `[JSInvokable]` side: `Pkmds.Rcl/Services/EmbeddedHostBridge.cs`
- Host detection: `Pkmds.Rcl/Services/HostService.cs` — reads `?host=` from `NavigationManager`
- Pre-Blazor SW gate: `Pkmds.Web/wwwroot/js/app.js`
- Pre-Blazor theme gate: `Pkmds.Web/wwwroot/js/theme-init.js`
- `ready` signal fire site: `Pkmds.Rcl/Components/Layout/MainLayout.razor.cs` (in `OnAfterRenderAsync`)

## 9. Summary of host obligations

Build a `WKURLSchemeHandler` to serve `wwwroot/` from a custom scheme, register a `WKScriptMessageHandler` named `pkmds`, call `loadSave` after `ready`, decode `saveExport` after `requestExport`, supply your own Done/Cancel chrome — that is the whole contract.
