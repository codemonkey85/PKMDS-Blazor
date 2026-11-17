# Browser support

PKMDS targets the latest stable Chrome, Edge, Firefox, and Safari on supported
desktop and mobile operating systems. A browser must provide the WebAssembly
features required by the published runtime. Passing those checks is necessary;
it does not certify every application feature or every device.

## Current runtime requirements

The .NET 11 Mono runtime uses **final WebAssembly exception handling** (`exnref`
and `try_table`), including when AOT and SIMD are disabled. Legacy WebAssembly
exception handling is a different feature and does not satisfy this requirement.

| WebAssembly feature | Chrome | Firefox | Safari / iOS Safari | Current default |
| --- | --- | --- | --- | --- |
| Final exception handling | 137+ | 131+ | 18.4+ | Required |
| Fixed-width SIMD | 91+ | 89+ | 16.4+ | Required |

These are feature introduction versions from the
[WebAssembly feature data](https://github.com/WebAssembly/website/blob/main/features.json),
not a list of exact browser versions certified by PKMDS. Chromium-based browsers
such as Edge also need an engine with the required features; PKMDS checks
capabilities rather than browser names or version strings. WebKit documents the
distinction between final and legacy exception handling in its
[Safari 18.4 release notes](https://webkit.org/blog/16574/webkit-features-in-safari-18-4/#webassembly).

**Safari on iOS/iPadOS 16 and 17 cannot run the current runtime.** Disabling SIMD
does not restore that support. On iPhone or iPad, update the operating system to
18.4 or later and use its latest available browser updates. Another browser using
the same system WebKit engine has the same limitation. A device that cannot
update needs a separately supported runtime build or another supported device;
PKMDS currently does not ship a legacy runtime fallback.

SIMD is enabled; AOT remains disabled. The [AOT/SIMD evaluation](WASM_AOT_SIMD_EVALUATION.md)
records the independent decisions, matched measurements, historical failures, and
remaining checks under [issue #883](https://github.com/codemonkey85/PKMDS-Blazor/issues/883). AOT is not
itself a separate browser capability to probe. A future runtime change, including
CoreCLR, needs its own requirements review.

## Startup behavior

Before `Blazor.start`, small self-contained modules check baseline WebAssembly
and the enabled exception-handling and SIMD features. The exception probe uses
final `exnref` / `try_table`, not legacy `try` / `catch`. AOT and SIMD command-line
build overrides do not bypass these checks: `Pkmds.Web.csproj` generates
`js/browser-requirements.boot.js` from the resolved `WasmEnableExceptionHandling` and
`WasmEnableSIMD` settings. Published requirements and probe scripts use
fingerprinted URLs and participate in the service-worker manifest. Development
uses the SDK's default asset naming and an explicit content root for generated files.

Missing capabilities lead to `BrowserNotSupported.html` with an explanation and
update guidance, before the runtime is started. Missing or invalid build metadata
also prevents startup, with a separate reload message rather than incorrectly
claiming that the browser is too old. The checks do not cover all runtime errors,
available memory, storage access, or application behavior.

The reason is carried in a URL fragment (`#reason=...`). The worker ignores
fragments when matching navigations to manifest assets, so controlled clients
receive the cached fallback. A reason query would trigger SPA-shell routing.

The fallback does not change IndexedDB, caches, or service-worker registration.
For startup troubleshooting, use a hard refresh, unregister only the service
worker, or diagnose in a fresh private session. Never clear the origin's site
data: it contains save backups and Pokémon Bank. Those records remain in the
original browser profile and do not automatically follow to another profile,
browser, or device.

## Validation boundaries

The [evaluation record](WASM_AOT_SIMD_EVALUATION.md) consolidates measurements of
the four Mono configurations (neither flag, SIMD, AOT, both), including application
and PWA behavior. Machine-specific results are labeled observations; raw artifacts
remain local diagnostics. See [the measurement workflow](../tools/bench/README.md).

Modern-browser and Simulator passes do **not** establish behavior on physical
older iPhones or the exact feature-floor browser versions above. The historical
.NET 10 / iOS 16.7.7 startup
failure in [#910](https://github.com/codemonkey85/PKMDS-Blazor/issues/910) remains a
separate unresolved cause; the newer runtime requirement does not explain it
retroactively. Keep real-device and floor-version validation open before claiming
broader certification.
