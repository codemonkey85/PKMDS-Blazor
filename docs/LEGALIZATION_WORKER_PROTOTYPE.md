# Legalization worker prototype and crypto bridge checks

Bounded investigation for [#702](https://github.com/codemonkey85/PKMDS-Blazor/issues/702),
following the [priorities recorded on #1374](https://github.com/codemonkey85/PKMDS-Blazor/issues/1374#issuecomment-6963712645).
The independent host and reproduction commands are in
[`tools/legalization-worker`](../tools/legalization-worker/README.md).

## Decision

The .NET 11 Mono worker is a viable candidate for keeping expensive legalization
off the UI thread. Continue toward an engine/context extraction, but **do not
replace the production legalization service in this change**. The prototype
establishes a working generation boundary and hard cancellation mechanism; it
does not establish an acceptable production memory/lifetime policy or broad
save/device coverage. Keep AOT off. CoreCLR and Runtime Async remain deferred
until .NET 12, as requested by the maintainer.

The production change justified by this investigation is the AES-CBC IV fix
and repeatable checks of both browser crypto providers. MD5 required no fix.

## Browser observations

Recorded November 17, 2026, on macOS Arm64, SDK 11.0.100 / Mono WASM 11.0.0,
PKHeX.Core 26.10.18, full trim, SIMD on, AOT off, Node 26.13.1 and Playwright
Chromium 158.0.8127.18. Three sequential fresh-context runs alternate main/worker
ordering. Both sides use the same assembly, engine, fixture and validation code.
Builds finished before measurement. HTTP is loopback, Brotli where available,
no-store, service workers blocked, with warm OS/server caches. These are local
desktop observations, not production-network or physical-mobile measurements.

| Observation across three runs | Main-thread control | Worker |
| --- | ---: | ---: |
| Valid Pikachu generation wall time | 4,980.9–5,152.6 ms | 5,361.5–5,466.8 ms |
| Largest animation-frame interval during generation | 183.3–216.7 ms | 16.8 ms |
| Largest 16 ms timer interval during generation | 186.9–213.7 ms | 17.7–17.9 ms |
| Largest frame interval around separate 500 ms synchronous control | 483.3–500.0 ms | 16.7–16.8 ms |

Worker initialization was **83.2–85.2 ms**. This includes worker creation,
runtime/script loading, export resolution and provider registration, and ends
before crypto known-answer/generation checks. It is not total app interactive
startup. The worker did not improve generation throughput in these samples;
the observed benefit is main-thread scheduling. Actual editor interaction,
scrolling, touch, drag and rendered UI still need integration testing.

The valid input is a **589,238-byte** JSON request containing a full owned
synthetic Gen 7 save snapshot, a Showdown set and the HaX flag. Its SHA-256 was
`7b78a5062b4a3ed44cd33d70def75dc9acaea7ee49a4035de113f7f8c01ec8af`
in every final Chromium run. The empty-set request is checked separately. Both
paths rehydrate exact result bytes, preserve input/save snapshots and agree on
the requested result semantics. RNG-dependent Pokémon fields are not compared
for byte identity across independent generations.

The independently published host has **5,276,162 bytes** of framework Brotli
files and **26,543,134 bytes** of uncompressed framework files. This excludes
app static files and is not incremental production download cost: production
integration may share some cached assets, require different exports, and must
account for worker/runtime packaging. Final size accounting uses a fresh publish
directory; repeated prior publishes accumulated unused fingerprinted files.

After the crypto/signing/checksum checks but before generation, each runtime's
`GC.GetTotalMemory(false)` reported 20,649,720 bytes. The worker reported about
33,848,000 bytes after generation. These non-forcing snapshots include garbage
awaiting collection and diagnostic fixture buffers. They are not retained-memory,
total WASM/JS/native/browser-memory or peak measurements. Allocation counters
are retained as exploratory raw data only; the initial zero readings do not
mean initialization allocated nothing, and no per-operation allocation claim
is made. A second runtime/PKHeX instance has a real cost that still needs a
production lifetime and total-memory evaluation.

## Cancellation and failures

The client rejects concurrent requests, because `ParseSettings` and `GameInfo`
are global within a runtime. Each generation reconstructs its trainer context
and calls `InitFromSaveFileData`; it does not globally disable cartridge-era
settings. Ordinary method errors reject the request while leaving the worker
usable. Cancellation or timeout terminates the worker, rejects pending work and
requires explicit recreation. No work is automatically replayed.

All final runs aborted a bounded synchronous two-second control after receiving
its started message. Abort-to-request-rejection was 0.1–0.2 ms in Chromium; this
is **not** a measurement of complete runtime/resource teardown. A separately
started synchronous control timed out at 150.9–152.0 ms with a 150 ms setting.
Recreation, subsequent generation, pre-aborted creation and error recovery passed.
Termination can interrupt uncooperative work, but discards its current best
attempt; it does not turn synchronous PKHeX methods into cooperative async work.

One Playwright WebKit 27.2 run also passed generation, crypto and lifecycle
checks. This is engine coverage, not native Safari, WKWebView or physical iOS
certification. Its result and environment remain in the raw report.

## AES and MD5

The first synthetic Gen 7 export used the .NET default AES provider before the
standalone host had initialized the app's JS providers. It failed with the
expected browser unsupported-algorithm error. That did not establish a defect
in the app's configured AES-ECB bridge. The final prototype registers the real
`BlazorAesProvider`, `BlazorMd5Provider` and `JsService` in both runtimes.

The maintainer requested checking the other crypto bridge too. Independent
known answers exposed a separate production defect: `BlazorAesProvider` accepted
an IV but discarded it, and `crypto.js` omitted CryptoJS's CBC `iv` option.
AES-ECB and MD5 passed; AES-CBC failed. PKHeX's `HomeCrypto` uses CBC with an
explicit IV. The fix preserves and passes that IV through C# and JavaScript,
rejects missing/invalid CBC IVs, and refuses unsupported padding/modes rather
than silently applying no padding. Existing ECB calls remain compatible.
The JS functions use `globalThis`, allowing the same implementation in a window
and a worker. The CryptoJS bundle is used unchanged.

Seven known-answer checks now pass:

- AES-128 ECB and CBC encryption/decryption, each using four blocks from
  [NIST SP 800-38A F.1/F.2](https://nvlpubs.nist.gov/nistpubs/legacy/sp/nistspecialpublication800-38a.pdf).
  Encryption and decryption use independent expected values, rather than only
  checking a round trip that could hide matching defects.
- MD5 of empty input, `abc` and an 80-byte message from
  [RFC 1321 A.5](https://www.rfc-editor.org/rfc/rfc1321.html#appendix-A.5).

The published production app's guarded `crypto` benchmark suite checks these
through its normal DI-configured providers and actual Blazor interop. The
standalone main/worker checks use the same C#/JS sources with a narrow synchronous
`[JSImport]` adapter, plus actual synthetic Gen 7 signing and BDSP MD5 checksum
generation/verification. The standalone checks passed in Chromium and Playwright
WebKit; the actual app bridge check passed in Chromium. No user
save, browser profile, IndexedDB or data cleanup was involved. Deployed PWA and
native/physical-device checks remain separate rollout gates in #1390.

## Validation and retained evidence

Local solution formatting and Debug build passed with zero warnings/errors;
the independent host is formatted/built separately. Twelve focused JavaScript
crypto/lifecycle regressions passed. Published-app crypto checks passed and all
3,472 service-worker asset hashes verified. CI publishes/checks the worker and
the app's crypto suite, uploads observations and runs .NET/JS regressions. No
`dotnet test` ran locally.

Final local reports are gitignored:

- `measurements/worker-clean-chromium.json`: three final repeated runs.
- `measurements/worker-clean-webkit.json`: the final WebKit check.
- `measurements/crypto-bridge-verified.runtime.jsonl`: the actual Blazor bridge check.

Earlier probes remain separate, including missing-provider/blank-trainer failures
and a crypto-harness result-shape failure (the known answers had returned, but
the harness initially required nonempty timing results). Those are not counted
as passing runs. Earlier successful runs against reused publish output are also
retained but are not the final size-accounting source.

The next integration needs a reviewed trainer-context contract, lazy worker and
idle/restart policy, total-memory/asset/PWA evidence, progress and partial-result
semantics, additional outcome/save-generation fixtures, and actual UI/mobile
validation. This change does not close #702 or wire editor save writes to a worker.
