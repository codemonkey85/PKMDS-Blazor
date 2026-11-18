# Isolated legalization worker prototype

This is the bounded first step of [#702](https://github.com/codemonkey85/PKMDS-Blazor/issues/702).
It is a separate browser application, never referenced by `Pkmds.Web` or deployed
by the production workflow. It compiles the existing legalization engine,
contracts, crypto providers and helpers as linked source. It does not fork the
engine algorithm or replace the production `ILegalizationService` registration.

The same trimmed Mono WASM assembly runs on the main thread and in a worker.
SIMD is on, AOT is off, and CoreCLR/Runtime Async remain deferred to .NET 12.
The installed .NET 11 `blazorwebworker` template provided the starting lifecycle
and runtime-loading pattern. The prototype uses a classic worker bootstrap so
`importScripts` can load the production UMD CryptoJS bundle without rewriting it;
the .NET runtime still loads through dynamic ES module import. The worker is
single-flight because PKHeX has runtime-global trainer/legality settings.

## Run

From the repository root, after the [benchmark dependency setup](../bench/README.md):

```sh
dotnet format tools/legalization-worker/LegalizationWorker.csproj --no-restore
dotnet build tools/legalization-worker/LegalizationWorker.csproj -c Debug
dotnet publish tools/legalization-worker/LegalizationWorker.csproj -c Release -o measurements/worker-prototype
node tools/bench/run-worker-prototype.mjs --site measurements/worker-prototype/wwwroot --runs 3 --output measurements/worker-new-run.json
```

Finish all builds before measuring. Use a new output path each time: the runner
refuses to overwrite observations. `--browser webkit` or `--browser firefox`
selects another installed Playwright browser. Playwright WebKit is not native
Safari or a physical iOS-device check. This tool is not in the solution, so build
and format it explicitly. Do not run `dotnet test` locally; CI runs .NET tests.

The runner creates a loopback server and fresh nonpersistent browser contexts,
blocks service workers, and serves published Brotli files when available with
`no-store`. The page refuses ordinary navigation before creating a runtime or
worker: it requires loopback, WebDriver and the explicit harness query marker.
There is no file picker, user profile, user-save write, IndexedDB access or storage
cleanup. Every save/request is synthetic and data stays on the local machine.

## Checks and scope

- Generate from an empty set and from a level-100 Hardy Pikachu/Thunderbolt set,
  using the real `GenerateFromSetAsync` path and an owned Gen 7 trainer snapshot.
- Reconstruct trainer context and call `ParseSettings.InitFromSaveFileData` in
  each runtime. Preserve cartridge/VC semantics instead of overriding settings.
- Compare requested result semantics, including failure status/reason, species,
  nature, level, first move and legality. Rehydrate result bytes and verify their
  exact round trip. Hash each caller request before/after, and verify that the
  engine did not change its save snapshot. PID/IV/EC/date values are randomized,
  so output byte identity across independent generations is not expected.
- Record cold runtime/worker initialization, generation wall time, 16 ms timer
  intervals and animation-frame intervals. No timing thresholds determine success.
- Use a separate 500 ms synthetic synchronous loop to demonstrate event-loop
  blocking. This is a scheduling/lifecycle control, not a PKHeX speed benchmark.
- Abort after the worker reports that a bounded synchronous operation started;
  also exercise an invocation timeout, which includes dispatch time and can
  expire before work starts on a busy host. Both terminate the worker and reject future
  calls until it is explicitly recreated. Normal method errors preserve it.
  Concurrent operations, pre-aborted creation, and recreation are checked too.
- Run NIST AES-128 ECB/CBC encryption/decryption and RFC 1321 MD5 known answers
  through the actual C# providers and production JavaScript functions. Real
  synthetic Gen 7 signing and BDSP checksum generation also execute in both
  runtimes. The standalone host uses a narrow `IJSInProcessRuntime` adapter via
  `[JSImport]`; the production app's `crypto` suite separately uses real Blazor
  DI/interop. No cryptographic fallback is substituted.

## Limits before integration

The full save snapshot is deliberately conservative. Its JSON/Base64 encoding,
copies and reconstructed save may be too expensive for production; a future
trainer-context contract needs generation-specific validation. Keeping a second
runtime and PKHeX tables alive has a memory cost. Reported GC snapshots/counters
are neither total WASM/browser memory nor peaks, and the independent host's
deployment size is not incremental production download overhead. OS/server
caches remain warm and the current fixture set is narrow.

Hard termination discards that runtime's current best attempt and costs another
startup. It does not make a synchronous PKHeX call cooperatively cancellable.
Progress, recoverable partial outcomes, actual UI save writes, HaX presentation,
crypto-dependent encounters, multiple save generations, physical mobile devices,
offline/PWA updates and worker lifetime/idle policy require further work before
production adoption. No worker pool or parallel legalization is introduced.

Production integration also needs the agreed cancellable progress UI: an
indeterminate bar/status/elapsed time for a single Pokémon, completed/total
progress for batches, protection against conflicting edits and duplicate starts,
and clear final outcomes. Forward existing engine status messages with request
IDs. See the [progress UI requirements](../../docs/LEGALIZATION_WORKER_PROTOTYPE.md#progress-ui-for-production-integration).

See [results and decisions](../../docs/LEGALIZATION_WORKER_PROTOTYPE.md).
