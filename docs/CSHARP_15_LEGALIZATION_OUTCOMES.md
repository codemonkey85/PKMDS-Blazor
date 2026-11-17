# C# 15 legalization outcome prototype

Investigation for [#1388](https://github.com/codemonkey85/PKMDS-Blazor/issues/1388),
following the [.NET 11 migration](DOTNET_11_UPGRADE.md). The prototype lives in
`tools/legalization-outcomes/`; the application still uses its existing model.

## Decision

Prefer a **closed record hierarchy** if the production model is refactored.
It supports exhaustive switch expressions and success-specific change summaries,
while retaining a common `Pokemon` property for callers that intentionally use
best-effort results. A generated union offers the same case checking, but adds
default-value handling and repetitive matching to access the common Pokémon.

Defer production adoption in this change. The existing callers already separate
HaX from legal success. This experiment establishes a maintainability option and
a small CoreCLR model-allocation difference, without establishing an observable
browser improvement. A production adoption should update engine and UI callers
together and add behavioral regressions in CI, including cancellation, timeout,
best-effort retention, change summaries, and the distinct HaX warning.

| Representation | Invalid combinations prevented | Caller cost | Decision |
| --- | --- | --- | --- |
| Existing status record | Nullable annotations only; permits success with a failure reason or non-success with changes | Shared properties; enum switches need a fallback for unnamed values | Retain in app for now |
| Generated union with sealed record cases | Each case carries only its relevant payload; required reason/warning parameters; exhaustive case switches | No shared `Pokemon`; default union contents are null and require a guard | Prototype only |
| Closed record hierarchy with sealed cases | Same case-specific payloads and exhaustiveness | Shared `Pokemon`; ordinary reference nullability; success-only `Changes` | Preferred future model |

Required non-nullable parameters provide compile-time nullability checks, not
runtime validation against `null!`, reflection, or untrusted input. Neither
candidate proves that a supplied Pokémon is legal; the engine still owns that.
The hierarchy is closed within its assembly; sealed leaves prevent external
subclassing. Exhaustiveness applies to switch **expressions**, so a mechanical
replacement inside the editor's switch statement would miss that benefit.

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
For example, the editor's current status switch reads shared nullable data:

```csharp
LegalizationStatus.Success => new(result.Pokemon, result.Changes, null),
LegalizationStatus.SuccessHaX => new(result.Pokemon, null, result.FailureReason ?? HaXWarning),
```

The closed candidate exposes only the appropriate payload in each arm:

```csharp
ClosedSuccess success => new(success.Pokemon, success.Changes, null),
ClosedHaX hax => new(hax.Pokemon, null, hax.Warning),
```

Batch checks become `result is ClosedSuccess success`. Generation retains
`result.Pokemon` with the hierarchy, while the union must match all four cases.
The service's success diff can use the typed record:

```csharp
if (outcome is ClosedSuccess success)
{
    return success with { Changes = PkmDiffer.Diff(original, success.Pokemon) };
}
```

Failure, timeout and HaX require their own reason/warning at construction. There
is no independently assignable status and no change-summary property on those
cases. Existing nullable reasons are normalized to the editor's existing fallback
text by the prototype factories; the app's stored formats and service are untouched.

## Matched allocation observation

Observed November 17, 2026, SDK **11.0.100**, stable `LangVersion=15.0`, Release,
**CoreCLR .NET 11.0.0, Windows 10.0.26200, x64**. No preview language switch.
The production `LegalizationResult.cs` and `LegalizationChanges.cs` are compiled
directly into the tool as linked source, using the centrally pinned PKHeX package.

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

That command must fail with **CS8509** naming `ImportedViaHaX` and `ClosedHaX`.
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
