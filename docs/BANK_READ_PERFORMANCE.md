# Bank read and large-list observations

Follow-up to [#1387](https://github.com/codemonkey85/PKMDS-Blazor/issues/1387),
November 17, 2026. The app now reuses `bank.js`'s existing `exportAll` function
when reading Bank entries. It returns the complete JSON as a UTF-8 `Uint8Array`;
Blazor's byte-array interop and source-generated deserializer consume it directly.
The previous path marshaled the full document as an escaped JSON string and
deserialized the resulting managed string.

## Decision and compatibility

Adopt the byte-array read. The matched Mono WASM observations show less managed
allocation and shorter Bank reads. The existing IndexedDB schema, JSON export
format, PKM parsing, metadata, ordering, ownership, and invalid-record cleanup
are unchanged. No buffer protecting Pokémon data was removed or shared.
No JavaScript export was added, so the primary path uses an already deployed
function. The old string and raw-object read contracts remain as fallbacks for
missing exports. Storage errors and malformed JSON still propagate without
falling back or deleting records at those failure boundaries.

Regression coverage compiles for UTF-8 and both fallback paths, mixed Gen 1/5/9
Pokémon bytes/types, IDs/order, Unicode tags, source-save metadata and timestamp
offsets. Storage and malformed-JSON errors are covered separately. Tests execute
only in CI. The actual published-browser Bank suite verifies every synthetic
Pokémon's bytes, IDs, tags/source, imported timestamps, row counts and exports.

## Matched Bank measurements

Both publishes use SDK **11.0.100 / Mono 11.0.0**, Release/full trim, SIMD on,
AOT off, PKHeX.Core **26.10.18**, and MudBlazor **9.14.0**. The comparison changes
only `BankService` in the application path. The focused measurement runner,
fixtures and UI remain the same. Windows **10.0.26200 x64**, AMD Ryzen 7 9800X3D,
16 logical processors, Chromium **156.0.8078.4**, Node **24.14.0**. Builds finished
before browser measurements; no concurrent benchmark was run.

The focused `bank` suite reuses the application suite's exact Bank workloads and
correctness checks. It omits descriptions and archives. Each fresh nonpersistent
automated loopback context must pass the existing isolation guard before any
Bank API. The suite refuses any existing IndexedDB database. Service workers are
blocked; HTTP is no-store with published Brotli siblings. Browser/OS/filesystem
caches remain warm. The published runtime and all 3,472 service-worker assets
were verified on both sides. This does not validate PWA update behavior.

The synthetic 1,000-row import is 641,439 bytes with SHA-256
`64374bedbf6d261516a0b6c1d7101d415b83e3c1f7a6e853815ec3d5f7683603`
in every run. Warm-up plus measured add-range/import calls produce 8,000 rows.
Pokémon/input bytes and imported timestamps are fixed; add-range timestamps are
generated during each run, as in production. The full serialized databases are
therefore not byte-identical across contexts.

Every reported read result contains one warm-up and three measured iterations.
Validation is outside the timing/allocation counters, but can affect later GC.
Three initial runs per variant were followed by three alternating
control/optimized pairs. All successful read means are retained below:

| Sequence | Control mean, ms | UTF-8 mean, ms |
| --- | ---: | ---: |
| Initial run 1 | 184.73 | 150.43 |
| Initial run 2 | 183.60 | 147.13 |
| Initial run 3 | 185.63 | 149.53 |
| Alternating pair 1 | 183.57 | 147.60 |
| Alternating pair 2 | 184.97 | 146.90 |
| Alternating pair 3 | 183.87 | 148.93 |

| Per 8,000-row read | Control | UTF-8 |
| --- | ---: | ---: |
| Managed allocation delta | 47,828,624 bytes | 26,825,056 bytes |
| Range of run means | 183.57–185.63 ms | 146.90–150.43 ms |

Allocation dropped **21,003,568 bytes (43.9%)** per read. Alternating-pair median
read time dropped from **183.87 ms to 147.60 ms (19.7%)** on this machine.
Allocation values were identical across all six runs per variant. These are
calibrated process-wide managed counter deltas, including interop/runner activity;
they exclude JavaScript/native allocations and are not retained or peak memory.
This is a .NET 11 control/refactor comparison, not a .NET 10/11 runtime comparison.
No broad application, startup, or browser-memory speedup is inferred.

### Failed full-suite observations

Before isolating Bank, four full-suite/diagnostic invocations were rejected by
the harness for `item-info.json: net::ERR_ABORTED` (three Brotli, one identity).
The diagnostic invocation completed its application data checks before the
harness rejected it. These are retained as failures; their numbers are excluded
from the comparison above. The description-request abort remains unexplained.
The focused suite avoids that unrelated workload and keeps the harness's asset
failure checks intact. No successful-looking full-suite result is claimed.

## Real Bank component observations

The `bank-ui` suite seeds 8,000 deterministic synthetic records, then mounts the
production `PokemonBankTab` in the diagnostic layout. It uses the same fresh
context/database guard; it never loads a user save or profile. The component is
not mounted when the guard fails. No production UI layout was redesigned.

On both published variants, the harness checks:

- 20 initial cards and 100 cards after changing page size, independent of the
  8,000 records held by the service/component.
- Selection survives a round trip to the next page and back.
- Searching `BENCH0999` shows exactly the eight expected records.
- The grid scrolls to its bottom over 60 animation frames at a 1280×720 viewport.
- The SHA-256 of the complete exported database is unchanged after interactions.

Three initial UI runs per variant passed. An additional three per variant record
attached Bank element counts alongside the CDP counters. The harness retains all
frame intervals, selection/paging/filter timings, and point-in-time DOM/JS-heap
snapshots. Interaction timings include Playwright auto-waiting/round trips and
render observation; they are not isolated event-handler durations. Navigation to
cards also includes bootstrap and fixture seeding, not just opening the Bank.

Each of the additional runs had **839 attached Bank elements at 20 cards** and
**3,734 at 100 cards**, identical on control and UTF-8 builds. All twelve UI runs
retained selection and unchanged database hashes. The optimization changes
rehydration cost, not the number of rendered cards/elements.

The initial UI passes had maximum frame intervals of **18.0–18.4 ms** during
programmatic scrolling, with overlapping selection/paging/filter ranges. That
supports the functional observation; it does not establish a scrolling speedup.
The Bank already bounds rendered cards with pagination. These results do not
justify replacing its pagination with virtualization.

CDP DOM counters include documents and nodes awaiting GC. The separate attached
Bank element count reflects the actual component tree. JS heap snapshots vary
with collection timing and exclude WASM/native/browser memory. None of these
measure total browser peak memory. The snapshots bracket export-hash validation
as well as interactions; no collection is forced. Touch/physical drag behavior, a loaded-save
send operation, other large views, Safari, and mobile devices remain unmeasured.
Issue #1387 stays open for total peak-memory and broader interaction profiling.

## Reproduce

Use the [benchmark setup](../tools/bench/README.md) and matched publishes. The
control uses `BankService.cs` from `e92f781f30b1c74ffe8c134e11190a99468c1b7f`;
all fixture/instrumentation sources
must match. Publish sequentially, finish both builds, then alternate runs:

```powershell
dotnet publish Pkmds.Web/Pkmds.Web.csproj -c Release -o measurements/bank-utf8
node tools/bench/run-bench.mjs --label bank-utf8 --site measurements/bank-utf8/wwwroot --suite bank --runs 3 --compression br
node tools/bench/run-bench.mjs --label bank-ui-utf8 --site measurements/bank-utf8/wwwroot --suite bank-ui --runs 3 --compression br
```

Local raw observations are in the gitignored `measurements/` directory:
`bank-control.runtime.jsonl`, `bank-utf8.runtime.jsonl`,
`bank-control-pairs.runtime.jsonl`, `bank-utf8-pairs.runtime.jsonl`,
`bank-ui-control.runtime.jsonl`, `bank-ui-utf8.runtime.jsonl`, and
`bank-ui-elements-{control,utf8}.runtime.jsonl`. Failed full-suite observations
remain in `bank-interop-before*.runtime.jsonl` and `bank-diagnostic.runtime.jsonl`.

Formatting, the solution Debug build (including tests), and JavaScript syntax
checks passed locally. No `dotnet test` was run locally. No deployment or GitHub
issue closure is implied by these observations.
