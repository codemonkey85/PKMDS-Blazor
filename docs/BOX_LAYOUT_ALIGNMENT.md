# Side-by-side box alignment

Implementation and investigation notes for [issue #769](https://github.com/codemonkey85/PKMDS-Blazor/issues/769), recorded November 18, 2026.

## Changes

- At the desktop breakpoint (1280px), pinned boxes share grid rows with the main storage view. The party and export controls occupy the first row; selected/pinned labels share the next row; both box selectors share another row; box actions and legalization progress occupy the following row; both box grids start together below them. A 24px gap and divider separate the columns. The stacked view separates the pinned section with a label and top border.
- Paired Trade panes share rows for the Slot B actions, save titles, parties, box selectors, box grids, and selected-Pokémon summaries. Wrapped save titles can increase the shared title row without moving only one box down.
- The desktop columns have matching widths. The existing four-column, 20-slot layout for older saves remains smaller than the six-column, 30-slot layout; their top edges still align when paired.
- Below 1280px, the panes retain a vertical layout without empty alignment rows.
- Party / Box and Trade use top padding inside their tab content instead of an external top margin. With `height: 100%`, the previous 12px margin placed the bottom of the desktop scroll area below its clipping parent, preventing the last pixels of the bottom row from being reached.
- Trade also reserves 16px of bottom content padding at every width. Reaching the last card's border is insufficient: fractional layout can spill slightly past the scroll boundary, and elevation shadows extend several pixels beyond the border box. The gutter leaves the complete bottom edge visible for both slot cards and selected-Pokémon summaries.
- Both storage layouts reserve 8px of horizontal pane padding so party rows and box slots have room for their left-edge shadows. Party / Box selector text also has 8px of leading padding, separating it from the previous-box button in both selected and pinned boxes. Storage columns can shrink and party export buttons can wrap, preserving the gutter at 320px rather than letting a button's minimum width expand the selected pane.
- Trade uses the same 24px desktop gap and divider as pinned boxes, extending through selected-Pokémon summaries. Stacked Trade panes have a horizontal separator.
- Game sprite scaling is shared by `PokemonSlotComponent` and `TradeSlot` through `ImageHelper.GetPokemonHighResSpriteSizeClass`. Trade previously omitted the size modifier, so tightly cropped X/Y and OR/AS sprites appeared larger than in Party / Box. Each Trade pane now scales using its owning save's game; Gen I/II sprites fill their slots, Gen VI sprites use half the slot width, and HOME keeps its existing sizing.

## Validation performed

- `dotnet format --no-restore` completed successfully.
- `dotnet build Pkmds.Web/Pkmds.Web.csproj -c Debug` passed with zero warnings and zero errors.
- `dotnet build Pkmds.Tests/Pkmds.Tests.csproj -c Debug` also passed with zero warnings and zero errors after adding sprite-scale coverage; this compiles the tests without running them.
- Playwright checks against the local Debug app compared rendered bounding rectangles, rather than relying only on screenshots. Same-format Trade grids measured 472px wide by approximately 392.67px high; both pinned grids measured 480px by 400px.
- Checked desktop widths of 1280px, 1424px, and 1920px, including a 700px-high viewport and scrolling to the bottom row. Checked the stacked layout at 390px by 844px, including the last slot and horizontal overflow.
- Used `TestFiles/violet_main` and `TestFiles/Test-Save-Scarlet.sav`, a copy of the Scarlet save with a long filename that wrapped, and `TestFiles/POKEMON BLUE-0.sav` for mixed-generation alignment. Also checked the selected-Pokémon summary.
- Local screenshots and the temporary renamed fixture are under the gitignored `measurements/769/` directory. They are not required at runtime.
- Did not run `dotnet test` locally, in accordance with `AGENTS.md`; the PR's GitHub Actions workflow runs the test suite.
- Added bUnit coverage for Trade's owning-game sprite scale (including a different primary-save generation) and resetting the scale when switching to HOME. The tests run in CI.

## Independent review

The separate review found that Trade panes still fit side by side at 1100px and 1279px: their inline 480px maximum widths let both `MudItem` elements occupy one flex row despite `xs="12"`. Because shared alignment starts at 1280px, Slot B remained 30px lower at those intermediate widths. The narrow-screen rule now explicitly stacks the Trade columns until the desktop shared-row layout applies. It uses `width < 1280px` so fractional CSS viewport widths do not fall into a gap between 1279px and 1280px.

The reviewer independently checked pinned alignment and bottom scrolling at 1280px by 600px, Trade at widths from 320px through 1920px, and closing Slot B in Chromium and WebKit. No other actionable findings were reported. Firefox crashed before page creation in the review environment, so it was not validated.

After the correction, the reviewer rechecked Chromium and WebKit at 320px, 768px, 1100px, 1279px, 1280px, and 1920px. Stacking, desktop alignment and equal sizes, bottom-slot access, and horizontal overflow checks passed. No actionable findings remained.

Subsequent user screenshots exposed a gap in the bottom-edge checks: accepting a border position within 1px of the clipping boundary missed clipped shadows. Independent investigation confirmed zero clearance at full scroll, with approximately 0.06–0.17px of fractional border overflow. In Chromium and WebKit, the 16px bottom gutter left approximately 15.83–15.94px of actual clearance at 1280px by 600px and 390px by 600px, with and without a selected-Pokémon summary. Future checks must verify visible clearance, not just that the last card's border reaches the viewport.

The reviewer checked the final gutter and pinned-box separation in Chromium and WebKit, including dark mode. The 24px desktop gap, divider, aligned labels, equal grid widths, mobile section separation, and bottom-row access passed; no actionable findings remained.

The Trade-divider and sprite-scale follow-up also passed independent Chromium/WebKit review: X and Alpha Sapphire use the same half-width scale as Party / Box, mixed Blue/X panes independently use their owning games' scales, HOME removes the game-specific modifiers, desktop/mobile separators remain visible, and bottom clearance remains positive. No actionable findings were reported.

The horizontal-gutter follow-up passed independent Chromium/WebKit checks at 1280px, 390px, and 320px. Party rows and box grids retain visible edge clearance, paired desktop widths and alignment remain unchanged, selector navigation works, and wrapping export buttons prevents narrow-screen overflow. The sprite-scale tests and full CI suite passed for commit `bc1ec8ff` before this CSS-only follow-up.

## Lessons for future layout work

- Use shared, content-sized rows instead of fixed top offsets. Export controls can wrap, legalization adds progress UI, and save filenames can span multiple lines.
- MudBlazor's `MudItem` renders `.mud-grid-item`. `MudStack` renders utility classes such as `.d-flex`, which can override a normal `display` rule. The Trade wrappers use native divs so desktop `display: contents` and the mobile flex layout can both apply predictably.
- Define the mobile layout explicitly when replacing a `MudStack`. Newly introduced Tailwind utilities may be absent from the committed stylesheet if the incremental Tailwind target has not regenerated it.
- Verify the scroll area's bounds against its clipping parent and the viewport, not only against its own last child. A scroll area can extend outside its parent even when its own scroll position reaches the end. Check the last card's shadow and a positive bottom gutter as well; `scroll-padding-bottom` alone does not create scrollable content space.

## Development-server observations

The first `dotnet watch` session could not apply a Razor update because a referenced assembly's generated version had changed (`CS7038`). Stopping the command session with Ctrl+C left watcher processes running in this environment. Starting another watcher led to a port conflict and startup requests for incompatible fingerprinted WASM assets, which surfaced as `Failed to fetch`.

Stopping the identified watcher process trees and running one stable server with `dotnet run --no-build -c Debug --launch-profile http` from `Pkmds.Web` restored startup. That command serves the last build; rebuild and restart it to review further managed-code changes. This was a local development-session problem, and this change does not alter the application's startup or caching logic.

For future recovery, first check which process owns the development port and stop only the relevant development-server processes. Refresh after the server is stable. If a targeted app-cache repair is needed, preserve IndexedDB: save backups and the Pokémon Bank must not be erased.
