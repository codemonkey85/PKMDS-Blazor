# C# 15 legalization outcomes

Investigation for [#1388](https://github.com/codemonkey85/PKMDS-Blazor/issues/1388),
following the [.NET 11 migration](DOTNET_11_UPGRADE.md). The original prototype
landed in #1409; the production follow-up adopts its closed-hierarchy design.
`tools/legalization-outcomes/` retains the historical model and union experiment
and now links the production hierarchy for continued comparison.

## Decision

Adopt a **closed record hierarchy** for the production model.
It supports exhaustive switch expressions and success-specific change summaries,
while retaining a common `Pokemon` property for callers that intentionally use
best-effort results. A generated union offers the same case checking, but adds
default-value handling and repetitive matching to access the common Pokémon.

The engine, editor, report/box batches, and worker boundary are updated together.
This is a correctness/maintenance change: invalid status/payload combinations
are no longer representable through the normal typed API. The original small
CoreCLR model-allocation observation does not establish a browser speedup.

| Representation | Invalid combinations prevented | Caller cost | Decision |
| --- | --- | --- | --- |
| Existing status record | Nullable annotations only; permits success with a failure reason or non-success with changes | Shared properties; enum switches need a fallback for unnamed values | Frozen historical baseline in tool |
| Generated union with sealed record cases | Each case carries only its relevant payload; required reason/warning parameters; exhaustive case switches | No shared `Pokemon`; default union contents are null and require a guard | Prototype only |
| Closed record hierarchy with sealed cases | Same case-specific payloads and exhaustiveness | Shared `Pokemon`; ordinary reference nullability; success-only `Changes` | Adopt in app |

Required non-nullable parameters provide compile-time nullability checks, not
runtime validation against `null!`, reflection, or untrusted input. Neither
candidate proves that a supplied Pokémon is legal; the engine still owns that.
The hierarchy is closed within its assembly; sealed leaves prevent external
subclassing. Exhaustiveness applies to switch **expressions**, so a mechanical
replacement inside the editor's switch statement would miss that benefit.

## Production adoption

`LegalizationOutcome` is a C# 15 `closed record` with four sealed cases:

| Case | Payload and behavior |
| --- | --- |
| `LegalizationSuccess` | Pokémon plus `Changes`, defaulting to the existing empty summary. Editor replacement and success notification; accepted by batches. |
| `LegalizationFailure` | Best-effort Pokémon plus required reason. Editor warning without replacement; rejected by batches. |
| `LegalizationTimeout` | Best-effort Pokémon plus required timeout reason. Editor warning without replacement; rejected by batches. |
| `LegalizationHaX` | Populated illegal Pokémon plus required warning. Editor replacement with warning; rejected by batches. |

The engine's preserve/rebuild passes compute diffs only on a typed success. Its
encounter search, timeout budgets, clone ownership, HaX conversion scope, and
cooperative yields are unchanged. Cancellation still returns `Failure` with
`Cancelled.` at the existing checks or propagates `OperationCanceledException`
from an interrupted delay; no new cancellation case or behavior is introduced.

The editor uses an exhaustive switch **expression** that awaits the appropriate
case handler, with no discard arm. Both batches require `LegalizationSuccess`
before any save write and retain their stored-byte legality checks. Generation
continues to use the common `Pokemon` property, accepting any populated
best-effort result and returning null for a blank result.

The isolated worker uses an exhaustive projection to its existing numeric wire
status and optional reason. That enum belongs only to its serialization boundary;
the app model has no assignable status or failure reason. Its request/reply JSON
shape and values remain unchanged. There is no production worker integration.

CI regression coverage exercises editor replacements/severities/change-summary
text across all four cases, cancellation warnings, best-effort/blank generation,
non-success batch-write rejection (including HaX), and the engine's empty-set
and two existing cancellation paths. The compiler probe now deliberately omits
`LegalizationHaX` from a switch over the linked production hierarchy.

## Production paths inspected

- `LegalizationService`: preserve-details pass, rebuild fallback, success-only
  `with { Changes = ... }`, best-effort failures/timeouts, cancellation as either
  `Failed("Cancelled.")` or a propagated `OperationCanceledException`, and HaX retry.
- `LegalityTab`: legal success replaces the editor Pokémon and shows changes;
  HaX also replaces it but shows a warning; failure/timeout only show warnings.
- `PokemonStorageComponent` and `LegalityReportTab`: only legal success is written
  during a batch. HaX is counted as failure. Stored bytes are reanalyzed afterward.
- `AppService.ConvertShowdownSetToPkm`: accepts any populated result, including a
  best-effort failure/timeout. Restricting it to success would change behavior.

`Callers.cs` expresses those decisions without UI or save-writing side effects.
For example, the editor's pre-adoption status switch read shared nullable data:

```csharp
LegalizationStatus.Success => new(result.Pokemon, result.Changes, null),
LegalizationStatus.SuccessHaX => new(result.Pokemon, null, result.FailureReason ?? HaXWarning),
```

The closed candidate exposes only the appropriate payload in each arm:

```csharp
LegalizationSuccess success => new(success.Pokemon, success.Changes, null),
LegalizationHaX hax => new(hax.Pokemon, null, hax.Warning),
```

Batch checks become `result is LegalizationSuccess success`. Generation retains
`result.Pokemon` with the hierarchy, while the union must match all four cases.
The service's success diff can use the typed record:

```csharp
if (outcome is LegalizationSuccess success)
{
    return success with { Changes = PkmDiffer.Diff(original, success.Pokemon) };
}
```

Failure, timeout and HaX require their own reason/warning at construction. There
is no independently assignable status and no change-summary property on those
cases. Existing nullable reasons are normalized to the editor's existing fallback
text by the prototype factories. Production constructors already supply explicit
reasons; stored Pokémon/save/IndexedDB formats are untouched.

## Matched allocation observation

Observed November 17, 2026, SDK **11.0.100**, stable `LangVersion=15.0`, Release,
**CoreCLR .NET 11.0.0, Windows 10.0.26200, x64**. No preview language switch.
At that point the production `LegalizationResult.cs` and `LegalizationChanges.cs`
were compiled directly into the tool as linked source, using the centrally pinned
PKHeX package. The old outcome is now frozen as `BaselineOutcome.cs` from
`272a491e`; the closed candidate links the current production outcome instead.

All representations receive the same PKM, string and summary references. Ten
fixtures cover success with/without changes, explained failure, cancellation
represented as failure, timeout, HaX, absent reasons for each warning case, and
an empty best-effort Pokémon. Editor decisions, batch acceptance and generated
Pokémon equivalence are checked outside allocation measurement. The engine and
its actual async cancellation paths are not executed.

Each representation is warmed with 1,000 constructions per fixture/repetition.
Five repetitions allocate 100,000 outcomes per sample with rotated representation
ordering. Non-inlined factories and escaping static sinks prevent elimination.
The counter is `GC.GetAllocatedBytesForCurrentThread`; no GC is forced. All 150
samples are retained in JSON; the figures below include the small first-sample
noise rather than dropping it.

| Representation | Samples | Allocated bytes per constructed outcome, range |
| --- | ---: | ---: |
| Existing production record | 50 | 48.00000–48.00128 |
| Union with sealed record cases | 50 | 32.00000–32.00000 |
| Closed record hierarchy | 50 | 32.00000–32.00000 |

Both candidates allocate a case object. The union's struct wrapper does not make
construction allocation-free. The 16-byte difference excludes Pokémon creation,
strings, computing diffs, the service's additional success `with` clone, Tasks,
encounter search, UI dispatch, JS/native memory and GC work on other threads.
There are no timing results, peak-memory measurements, or Mono WASM allocation
claims. This is too narrow to establish an app performance improvement.

## Production follow-up validation — November 18, 2026

On Windows with SDK 11.0.100 and stable C# 15, solution formatting and Debug
builds compiled the app and added regressions. The standalone worker Debug build
also passed. Builds used `-p:NuGetAudit=false` for the invocation because the
NuGet vulnerability service was unreachable; repository audit settings are intact.

The full-trim Release app publish passed integrity verification for all 3,418
service-worker assets. In fresh isolated Chromium 156.0.8078.4, the Moon fixture's
illegal Clefable legalized successfully, replaced the editor Pokémon, showed
the success notification and opened its 18-change summary. The editor remained
explicit about saving the result. The standalone worker Release publish passed
its browser lifecycle/data/crypto checks with one observation run, including
generation success and empty-set failure through the unchanged wire contract.

The model-only tool again checked all ten fixture projections and retained 150
allocation samples: baseline 48.00000–48.00128 bytes, union 32 bytes, production
closed hierarchy 32 bytes per outcome on CoreCLR. These measurements retain the
original narrow scope and make no browser throughput/allocation claim.

Local diagnostic files are `measurements/1388-production-*` and
`measurements/1388-worker-*`. These checks do not establish every HaX/timeout/UI
path, Safari/mobile compatibility, or native behavior. The added application
regressions are compiled locally and await execution in CI; no local
`dotnet test` ran. No user profile, save, or IndexedDB content was modified.

## Reproduce and validate

From the repository root:

```powershell
dotnet format --no-restore
dotnet build -c Debug
dotnet run --project tools/legalization-outcomes/LegalizationOutcomes.csproj -c Release > measurements/legalization-outcomes-coreclr.json
```

Create the gitignored `measurements` directory first if it does not exist. The
JSON records runtime/OS/architecture, fixture count, equivalence result, sampling
parameters and every allocation sample. Performance values are observations,
not thresholds. No real save, browser profile or IndexedDB data is accessed.

An additional compiler probe deliberately omits the HaX arm in both candidates:

```powershell
dotnet build tools/legalization-outcomes/LegalizationOutcomes.csproj -c Debug -p:DefineConstants=VERIFY_EXHAUSTIVENESS
```

That command must fail with **CS8509** naming `ImportedViaHaX` and `LegalizationHaX`.
The normal build excludes the probe. CI checks both expected diagnostics and
runs the normal Release tool, including the caller-equivalence observations,
and uploads its JSON as the `legalization-outcomes` artifact.
Locally the normal prototype Debug build and negative compiler probe passed
their respective expectations; allocation observations are recorded above.
No `dotnet test` was run locally; application regression execution stays in CI.

## Language references

- [C# 15 union types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union)
- [Closed hierarchies and exhaustiveness](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/closed)

The compiler probe verifies the behavior on the selected SDK, including the
union null arm preserving the missing-case diagnostic.
