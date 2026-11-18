# Side-by-side box alignment

Implementation and investigation notes for [issue #769](https://github.com/codemonkey85/PKMDS-Blazor/issues/769), recorded November 18, 2026.

## Changes

- At the desktop breakpoint (1280px), pinned boxes share grid rows with the main storage view. The party and export controls occupy the first row; both box selectors share the next row; box actions and legalization progress occupy another row; both box grids start together below them.
- Paired Trade panes share rows for the Slot B actions, save titles, parties, box selectors, box grids, and selected-Pokémon summaries. Wrapped save titles can increase the shared title row without moving only one box down.
- The desktop columns have matching widths. The existing four-column, 20-slot layout for older saves remains smaller than the six-column, 30-slot layout; their top edges still align when paired.
- Below 1280px, the panes retain a vertical layout without empty alignment rows.
- Party / Box and Trade use top padding inside their tab content instead of an external top margin. With `height: 100%`, the previous 12px margin placed the bottom of the desktop scroll area below its clipping parent, preventing the last pixels of the bottom row from being reached.

## Validation performed

- `dotnet format --no-restore` completed successfully.
- `dotnet build Pkmds.Web/Pkmds.Web.csproj -c Debug` passed with zero warnings and zero errors.
- Playwright checks against the local Debug app compared rendered bounding rectangles, rather than relying only on screenshots. Same-format Trade grids measured 472px wide by approximately 392.67px high; both pinned grids measured 480px by 400px.
- Checked desktop widths of 1280px, 1424px, and 1920px, including a 700px-high viewport and scrolling to the bottom row. Checked the stacked layout at 390px by 844px, including the last slot and horizontal overflow.
- Used `TestFiles/violet_main` and `TestFiles/Test-Save-Scarlet.sav`, a copy of the Scarlet save with a long filename that wrapped, and `TestFiles/POKEMON BLUE-0.sav` for mixed-generation alignment. Also checked the selected-Pokémon summary.
- Local screenshots and the temporary renamed fixture are under the gitignored `measurements/769/` directory. They are not required at runtime.
- Did not run `dotnet test` locally, in accordance with `AGENTS.md`; the PR's GitHub Actions workflow runs the test suite.

## Lessons for future layout work

- Use shared, content-sized rows instead of fixed top offsets. Export controls can wrap, legalization adds progress UI, and save filenames can span multiple lines.
- MudBlazor's `MudItem` renders `.mud-grid-item`. `MudStack` renders utility classes such as `.d-flex`, which can override a normal `display` rule. The Trade wrappers use native divs so desktop `display: contents` and the mobile flex layout can both apply predictably.
- Define the mobile layout explicitly when replacing a `MudStack`. Newly introduced Tailwind utilities may be absent from the committed stylesheet if the incremental Tailwind target has not regenerated it.
- Verify the scroll area's bounds against its clipping parent and the viewport, not only against its own last child. A scroll area can extend outside its parent even when its own scroll position reaches the end.

## Development-server observations

The first `dotnet watch` session could not apply a Razor update because a referenced assembly's generated version had changed (`CS7038`). Stopping the command session with Ctrl+C left watcher processes running in this environment. Starting another watcher led to a port conflict and startup requests for incompatible fingerprinted WASM assets, which surfaced as `Failed to fetch`.

Stopping the identified watcher process trees and running one stable server with `dotnet run --no-build -c Debug --launch-profile http` from `Pkmds.Web` restored startup. That command serves the last build; rebuild and restart it to review further managed-code changes. This was a local development-session problem, and this change does not alter the application's startup or caching logic.

For future recovery, first check which process owns the development port and stop only the relevant development-server processes. Refresh after the server is stable. If a targeted app-cache repair is needed, preserve IndexedDB: save backups and the Pokémon Bank must not be erased.
