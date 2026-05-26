# PKHeX Feature Parity Roadmap

This roadmap outlines the path to achieving 100% feature parity with PKHeX. Tasks are broken down into actionable items organized by feature category and priority.

**Last Updated:** 2026-05-11 (Gen 3 RTC editor implemented — §3.8, closes #865; Wonder Card in-app viewer + Gen 3 WC3 import — §3.8, closes #811; Feebas tile locator/seed editor (Gen 3 RSE + Gen 4 DPPt/BDSP) — §3.7/§3.8, closes #815; Pokémon Bank import of `.pk*`/`.zip` + export as `.pk*`/ZIP — §2.1b, closes #780/#779; Legalization change report on legalise — §5.1, closes #833; HaX retry path for failed Showdown imports + blank-PKM guard — §5.1, closes #859/#858; Showdown import trash bytes seeding for Gen 8+/7b — closes #860; Cross-save trade phases 1–4 implemented (phase 3 multi-select still open #712) — §5.8; Advanced Search type/origin/flags/ribbons/marks filters — §2.4, closes #737; All dropdowns → type-to-filter autocomplete; Trainer customization phases 1–5 (trainer card, currency editors, game sync ID, Gen 6 sayings, game start/HoF time, Game Corner Joyful records, language dropdown hidden for Gen 3 ILangDeviantSave saves, Gen 4 HGSS NSparkle flag) — §6.1, tracks #361/#870; PKHeX.Core updated to 26.5.5, closes #851; Mobile full-screen dialogs — closes #740; Embedded host mode (iOS WKWebView) — closes #787/#799; Drag-and-drop to open saves; New app layout (left app bar replaces side drawer); New branding (logo, adaptive favicon, icon set); Theme palette switcher (preview); Import warnings for illegal Pokémon snackbar; Smarter legalization fallback handling; Gen 4 badge toggling fixed; iOS PWA status-bar/safe-area fixes; Dark mode slot borders. Earlier — Auto-Legality Mod Phase 1 implemented — §5.1, tracks #344; Legalize button on Legality tab; "Legalize All in Box" toolbar action scoped to Illegal-only entries — tracks #693; Batch legalize from Legality Report tab with cancel, per-Pokémon timeout, and in-place report updates — tracks #690; Showdown import now delegates to legalization engine — tracks #691; Mystery Gift event properties preserved during legalization — tracks #695; Original details (OT, met data, ribbons) preserved when legalizing existing PKM — tracks #705; Tri-state legality indicators with per-state visibility toggles + Fishy reasons in Legality Report; MudToggleGroup filter UI on Legality Report; Gen 2 trade evolutions handled with beauty/item conditions — tracks #688. Earlier — Teams Tab implemented — §1.4, tracks #661; Showdown / PokePaste import implemented — §4.2, tracks #656; Backup management system implemented — §4.2/§5.5; Save file info viewer + repair tool implemented — §5.5; Display party stats implemented — tracks #658; In-app browser detection + warning implemented — §4.1, tracks #663; Non-exportable save detection implemented — tracks #675/#679; Save File Utilities §5.5 partially complete — backup manager, info viewer, repair tool, and encryption status done; remaining tasks tracked in #657. Earlier — Pokémon Bank implemented — §2.1b, tracks #330; One-Touch Evolve implemented — §1.1, tracks #448; Gen-Specific editor tab implemented — §1.1 #419; Box pop-out dialogs implemented — §6.2, tracks #352; Pokédex per-species grid + LA research editor implemented — §2.5, tracks #414/#437/#438/#439; Fix Load Pokémon/Gift File slot behaviour implemented — §4.1, tracks #445; Trash bytes auto-fixer added to legality tab — tracks #542; Spinda spot preview implemented — tracks #554; Manic EMU `.3ds.sav` ZIP round-trip support added — tracks #537; §6.6 Settings & Preferences mostly complete — `AppSettingsDialog` covers UI, trainer defaults, and auto-backup; settings import/export + reset remain. §7.2a Bag virtualization corrected — virtualization was reverted (commit 48ce4f15) and remains blocked on MudBlazor/MudBlazor#12799; Info popovers for moves/items/abilities/balls added — §4.1, tracks #579)

---

## Current State Summary

### ✅ Already Implemented in PKMDS
- **Pokemon Editor** - Full individual Pokemon editing (species, nickname, gender, level, experience, abilities, held items)
- **Stats Editing** - IVs, EVs, AVs (LGPE), GVs (LA), base stats, calculated battle stats, stats chart, CP (LGPE), Stat Nature (Gen 8+), Dynamax Level, Can Gigantamax, Tera Type Original/Override (SV), Is Alpha (LA/ZA), Is Noble (LA)
- **Moves Editing** - 4-move slots with search, type + category icons (Physical/Special/Status, generation-accurate including Gen 1–3 type-based split and Water Shuriken Gen 6 special-case), PP/PP Ups editing, inline stat summary panel (Ability, Nature with modifier, Atk/Sp. Atk with nature arrows; generation-aware visibility)
  - **Relearn Moves** (Gen 6+) - 4 relearn move slots with search functionality
  - **Plus Move Checkboxes** (PA9/Legends Z-A) - Mark moves as Plus-enabled inline
  - **Mastered Move Checkboxes** (PA8/Legends Arceus) - Mark moves as mastered inline
  - **Alpha Move Selector** (PA8/Legends Arceus) - Dropdown for Alpha Move selection
  - **TR Relearn Flags Dialog** (Gen 8+) - Technical Record flag editor with Give All/Remove All
  - **Move Shop Dialog** (PA8/Legends Arceus) - Purchased and Mastered flags in single dialog
  - **Plus Flags Dialog** (PA9/Legends Z-A) - Plus Move flag editor
- **Box Management** - Visual box grid, drag-and-drop, copy/paste Pokemon
- **Party Management** - Party grid with visual display
- **Bag/Inventory** - Multi-pouch item editing with counts, favorites, sorting
- **Trainer Info** - Name, TID/SID (16-bit and 6-digit), money, game version
- **Pokedex** - View seen/caught counts, bulk fill functionality
- **Records** - Game statistics, Hall of Fame timing
- **Mystery Gift Database** - Browse, search, filter, import mystery gifts
- **Met Data** - Met location, ball, level, origin game, Battle Version (Gen 8+), Obedience Level (Gen 9+), Ground Tile (Gen 4), Fateful Encounter, Met Date (Gen 4+), Egg Location + Egg Date (Gen 4+), Met Time of Day (Gen 2)
- **OT/Misc Data** - Original trainer info, TID/SID (16-bit and 6-digit), handling trainer name/gender/language (Gen 8+), memory editing (Gen 6+), affection/friendship, Geo Locations (Gen 6–7), Home Tracker (Gen 8+), Country/Sub-Region/Console Region (IRegionOrigin, Gen 6–7), Affixed Ribbon/Mark (IRibbonSetAffixed, Gen 8+)
- **Cosmetic Features** - Markings, height/weight scalars, scale rating, origin mark display (Gen 6+), Favorite toggle (Gen 7b+)
- **Pokerus** - Infection status display/editing
- **Hidden Power** - Type selection
- **Showdown Export** - Export Pokemon to Showdown format
- **Multi-Save Support** - Load/save multiple save files
- **PWA Support** - Offline functionality, installable web app
- **Legality Checker** - Full `LegalityAnalysis` integration: per-check detail view, color-coded results, full verbose report, slot-level valid/warn icon overlays, one-click fix buttons (ball, met location, moves, TechRecord), and a batch "Legality Report" tab that sweeps all party and box Pokémon with a sortable/filterable table, aggregate Legal/Fishy/Illegal counts, and jump-to-slot navigation
- **Advanced Search** - Multi-criteria search tab sweeping all party and box slots with filters for species, shiny, nature, ability, held item, ball, origin game, gender, level range, IV/EV floors, moves (any/all), Hidden Power type, OT name/TID, language, ribbons/marks, and legal status; saved filters via localStorage; batch Showdown text export
- **Encounter Database** - Encounter DB tab backed by PKHeX.Core's `EncounterMovesetGenerator`; filter by species, game version, level range, shiny lock, and encounter type (Wild, Static, Mystery Gift, Trade, Egg); sortable results table with type-coloured chips and location display; per-encounter detail panel with "Generate Legal Pokémon" action that places a legal PKM into the selected slot
- **Batch Editor** - Scripted batch property editor wrapping PKHeX.Core's `BatchEditing` engine; multi-line script input with filter (`.Property=Value`) and mutation (`=Property=Value`) instructions; comparison operators; named presets with localStorage persistence; dry-run preview mode showing matched Pokémon and proposed changes before applying; scope selector (party, current box, all boxes, or all)
- **Pokémon Bank** - Persistent client-side Pokémon storage using browser IndexedDB; card grid UI with species sprite, shiny indicator, nickname, source save label, and added date; "Add from Save" bulk-imports all party + box Pokémon with duplicate detection (O(N+M) hash set); single and multi-select "Send to Save" with cross-format conversion; JSON file import/export via File System Access API; species/nickname search and shiny-only filter; configurable pagination; dismissible backup reminder; automatic cleanup of corrupt records
- **Gen-Specific Editor Tab** — Generation-specific Pokémon fields consolidated into a dedicated tab that appears only when applicable: Gen 3 Colo/XD Shadow fields (`IsShadow` read-only, `ShadowID`, `Purification`), Gen 4 HGSS ShinyLeaf (5 leaf checkboxes + Crown flag) and `WalkingMood`, Gen 5 `NSparkle` and `PokeStarFame`, LGPE `Spirit`, `Mood`, and full received timestamp; Gen 8 SwSh `DynamaxLevel`/`CanGigantamax`, Gen 8 LA `IsNoble`/`IsAlpha`, and Gen 9 `TeraTypeOriginal`/`TeraTypeOverride` consolidated here from StatsTab
- **One-Touch Evolve** — Evolve button on Main tab; single-path evolutions apply instantly; branching evolutions (Eevee, Slowpoke, etc.) open a picker dialog with sprite, localized name, and method label; Nincada→Ninjask offers Shedinja placement; Gen 2 trade evolutions show the required held item; nickname sync on evolve
- **Box Pop-Out Dialogs** — `BoxViewerDialog` (single-box pop-out with independent prev/next navigation, "View All Boxes" button) and `BoxListDialog` (all-boxes responsive grid with per-box ⇄ swap buttons); both subscribe to live box state; triggered from box nav bar; `SwapBoxes` added to `IAppService`
- **Pokédex Per-Species Grid** — Searchable, paginated per-species Seen/Caught checkbox grid (replacing count-only view) with full per-game dex filtering (LGPE 153-species, SWSH Galar-only, LA Hisui-only, SV regional dexes, ZA personal table); search by name or Pokédex number; Gen 8 LA per-species research task editor dialog (all task types, Clear All Research)
- **Trash Bytes Auto-Fixer** — One-click trash-bytes cleaner in the legality tab (clears junk bytes in string fields to avoid legality flags)
- **Manic EMU `.3ds.sav` Round-Trip** — Auto-detects and unpacks Manic EMU's ZIP-based `.3ds.sav` format on load; repacks to the same ZIP structure on export so saves can be re-imported directly
- **Teams Tab** — Dedicated tab for battle box (Gen 7+) plus battle teams and rental teams (Gen 8/9): per-team Showdown export, lock/unlock, clear, and battle-team indicator badges on box grid sprites; backed by `IAppService` battle/rental team operations and unit-tested service layer
- **Showdown / PokePaste Import** — `ShowdownImportDialog` parses Showdown text into legal PKM via `EncounterMovesetGenerator`, with HOME tracker / Scale fix-ups and overwrite-party confirmation; `PokePasteExportDialog` exports party as a PokePaste paste; PokePaste URLs can also be imported directly
- **Backup Management System** — `BackupService` (browser IndexedDB) + `BackupManagerDialog` for client-side save backups with retention enforcement, restore, and per-entry delete; backup restore is guarded against non-exportable saves
- **Save File Info Viewer** — `SaveFileInfoDialog` displays save type/generation/game/revision/size, header/footer presence, checksum status (valid/invalid + detail), encryption status, exportable + edited flags, and storage summary (boxes, party, seen/caught)
- **Save File Repair Tool** — `SaveFileRepairDialog` exposes save file repair operations from the main menu
- **Non-Exportable Save Detection** — Saves loaded from blank/template files are flagged as non-exportable at load time so the user is warned before attempting to export
- **In-App Browser Detection** — Detects when the app is loaded inside an in-app browser (Telegram, WhatsApp, WeChat, Instagram, Facebook, etc.) and shows a warning so users open the site in a real browser where File System Access works
- **Display Party Stats** — Party slots show current HP and status condition, with stat-recalculation skipped for PB7 entities that already have party stats persisted
- **Auto-Legality Engine (Phase 1)** — `ILegalizationService` / `LegalizationService` wraps PKHeX.Core's encounter/criteria pipeline to generate legal PKM from a template or Showdown set; supports Gen 3–5 Method-1 PID↔IV correlation via `ClassicEraRNG`, Gen 8 SwSh overworld PID/IV, Gen 8b BDSP, and Gen 9 Tera raids; preserves original OT/met/ribbon details when legalizing an existing PKM and preserves event properties when legalizing a Mystery Gift
- **Legalize Actions** — "Legalize" button on the Legality tab; **"Legalize All in Box"** toolbar action (scoped to Illegal-only entries, disabled when the box is empty); **batch legalize from the Legality Report tab** with a cancel button, per-Pokémon timeout, and in-place report entry updates; Showdown / PokePaste import now routes through the legalization engine instead of ad-hoc fix-ups; **HaX retry path** for Showdown imports that fail the first pass; **blank-PKM guard** prevents importing empty Pokémon when legalization fails entirely; **legalization change report** shows exactly what was modified when a Pokémon is legalized
- **Tri-State Legality Indicators** — Legality Report tab shows Legal / Fishy / Illegal with independent visibility toggles (MudToggleGroup); Fishy reasons surface in the "First issue" column; visually marks the active filter
- **Gen 3 RTC Editor** — view and adjust the Real-Time Clock for Ruby, Sapphire, and Emerald saves; supports battery-dead clock repair (#865)
- **Feebas Tile Locator** — finds which Route 119 (Gen 3) / Mt. Coronet (Gen 4/BDSP) water tiles contain Feebas for the save's RNG seed; includes a Feebas seed editor (#815)
- **Wonder Card In-App Viewer** — browse Wonder Cards / Mystery Gifts stored in the save file without leaving the app; Gen 3 `.wc3` import supported (#811)
- **Cross-Save Pokémon Transfer** — load two saves in a single session; transfer Pokémon between them via drag-and-drop with compatibility checks; held items returned to source bag on transfer; phases 1–4 done (move-mode multi-select #712 still open)
- **Pokémon Bank Import/Export** — import `.pk*` and `.zip` (containing `.pk*`) files directly into the Bank; export individual or multi-selected Pokémon as `.pk*` files or a ZIP (#780/#779)
- **Embedded Host Mode** — PKMDS can run embedded inside a native iOS/iPadOS WKWebView host (#787/#799)
- **Trainer Customization (Phases 1–5)** — expanded Trainer tab with per-gen currency editors (money, coins, BP, Poké Miles, Watts, Festival Coins, Roto Tokens, League Points); Game Sync ID (Gen 5/6/7/7b); Gen 8 SwSh Trainer Card dialog (card name, card number, trainer ID, Roto Rally score); Gen 6 Sayings dialog (read-only view); game start and Hall of Fame timestamps; Gen 3 Game Corner Joyful Records; Gen 4 HGSS NSparkle flag; language dropdown hidden for `ILangDeviantSave` saves (Gen 1–3, Stadium) (#361, #870)
- **Drag-and-Drop Save Open** — drop save files onto the welcome screen or load dialog to open immediately
- **New App Layout** — left app bar menu replaces the side drawer; welcoming empty state on no-save
- **New Branding** — fresh logo, adaptive favicon, and full icon set
- **Theme Palette Switcher** — segmented-control palette switcher in the app bar (preview)
- **Import Warning Snackbar** — notifies when imported Pokémon are illegal after a Showdown import

---

## Priority 1: Critical Missing Features

### 1.1 Pokemon Editor Enhancements

#### Ribbon Editor
**Status:** ⚠️ Partial (ribbon legality checking and search/filter — #650)  
**Complexity:** High  
**Tasks:**
- [x] Design Ribbon UI component with tabs for different ribbon categories
- [x] Implement ribbon data binding to PKHeX.Core ribbon properties
- [x] Add support for all ribbon types:
  - [x] Contest ribbons (Cool, Beauty, Cute, Smart, Tough, etc.)
  - [x] Battle ribbons (Battle Tower, Battle Tree, etc.)
  - [x] Event ribbons (Classic, Premier, etc.)
  - [x] Memorial ribbons
  - [x] Mark ribbons (Gen 8+)
  - [x] Generation-specific ribbons
- [x] Display ribbon icons/images
- [ ] Implement ribbon legality checking (prevent illegal combinations)
- [ ] Add search/filter for ribbons
- [x] Create unit tests for ribbon editing

#### Memory Editor
**Status:** ✅ Implemented  
**Complexity:** Medium  
**Tasks:**
- [x] Create Memory editing UI component
- [x] Implement OT Memory editing (Gen 6+)
- [x] Implement HT (Handling Trainer) Memory editing (Gen 6+)
- [x] Add memory intensity/feeling/text selection
- [x] Support memory variable selection
- [x] Add memory validation based on game version
- [x] Implement affection/friendship display and editing
- [x] Create unit tests for memory editing

#### Contest Stats Editor
**Status:** ✅ Implemented  
**Complexity:** Low  
**Tasks:**
- [x] Create Contest Stats UI component
- [x] Add fields for Cool, Beauty, Cute, Smart, Tough stats
- [x] Add Sheen field (Gen 3-4)
- [x] Implement contest stat validation (max values per generation)
- [x] Support generation-specific contest stat limits
- [x] Create unit tests for contest stats

#### Form/Appearance Editor
**Status:** ⚠️ Partial (Spinda spot preview + species-specific editors for Alcremie/Vivillon/Furfrou/Pumpkaboo/Minior implemented; general form preview UI and form-legality validation remain — #651)
**Complexity:** Medium
**Tasks:**
- [ ] Enhance form selection UI with visual previews (general species)
- [x] **Spinda spot pattern preview** — renders Spinda's 4 unique spot positions from its PID using bundled sprite assets; shown inline in the Pokémon editor (closes #554)
- [x] **Alcremie decoration editor** (Gen 8) — `AlcremieEditorDialog` covers all 63 cream × sweet combinations
- [x] **Vivillon pattern editor** — `VivillonEditorDialog`; also covers Scatterbug and Spewpa via the Vivillon species substitution
- [x] **Furfrou trim editor** — `FurfrouEditorDialog`; auto-sets days-remaining to maximum when switching to a trim form
- [x] **Pumpkaboo/Gourgeist size editor** — `PumpkabooSizeDialog`
- [x] **Minior core color editor** — `MiniorColorDialog`
- [ ] Support all Pokemon with form variations
- [ ] Validate form legality based on origin/capture method

#### Ability Slot Editor
**Status:** ✅ Implemented
**Complexity:** Low
**Tracks:** #176
**Tasks:**
- [x] Allow freely selecting any ability slot (Ability 1 / Ability 2 / Hidden Ability) regardless of species legality
- [x] Display all three slots even when a species lacks a Hidden Ability (show as "None")
- [x] Let legality checker report illegal ability slots rather than preventing selection

#### PID/EC Generator
**Status:** ✅ Implemented
**Complexity:** Medium
**Tasks:**
- [x] Create PID generator with options:
  - [x] Method 1/2/4 (Gen 3-4)
  - [x] Shiny PID generation
  - [x] Gender-locked PID generation
  - [x] Nature-locked PID generation
- [x] Add Encryption Constant (EC) generator (Gen 6+)
- [x] Implement shiny type selection (Star/Square for Gen 8+)
- [ ] Add PID-to-IV correlation for Gen 3-4
- [ ] Create PID reroller for legal PIDs

#### Catch Rate / Status Condition Editor
**Status:** ✅ Implemented
**Complexity:** Low
**Tasks:**
- [x] Add Catch Rate field (Gen 1-2)
- [x] Create Status Condition editor (Gen 1-2)
- [x] Add held item slot for Gen 1-2 (linked to catch rate byte)
- [x] Implement generation-specific validation

#### Met Tab — Missing Fields
**Status:** ✅ Implemented (#416)
**Complexity:** Low
**Tracks:** #416
**PKHeX Reference:** `PKMEditor.cs` Met tab — `CB_BattleVersion`, `TB_ObedienceLevel`
**Note:** Met Date, Egg Met Date, Egg Met Location, Ground Tile, Met Time of Day (Gen 2), and Fateful Encounter are all ✅ already implemented in `MetTab.razor`.
**Tasks:**
- [x] **Battle Version** (`PKM.BattleVersion`, Gen 8+) — dropdown selecting the game version whose move/move-legality rules apply; important for VGC transferred Pokémon
- [x] **Obedience Level** (`PKM.ObedienceLevel`, Gen 9+ / PK9/PA9) — level cap below which a traded Pokémon will obey; shown alongside Met Level in PKHeX

#### OT/Misc Tab — Missing Fields
**Status:** ✅ Implemented (#417)
**Complexity:** Low
**Tracks:** #417
**PKHeX Reference:** `PKMEditor.cs` OT/Misc tab
**Note:** Handler Language (`IHandlerLanguage`), Home Tracker (`IHomeTrack`), Handling Trainer name/gender, memories, Geo Locations (`IGeoTrack`), and TID/SID format handling are all ✅ already implemented in `OtMiscTab.razor`.
**Tasks:**
- [x] **Country / Sub-Region / Console Region** (`IRegionOrigin`: `Country`, `Region`, `ConsoleRegion`, Gen 6–7 only) — the Pokémon's native 3DS geographic origin; three separate dropdowns; distinct from the five IGeoTrack visited-country slots which are already implemented
- [x] **Affixed Ribbon / Mark** (`PKM.AffixedRibbon`, Gen 8+) — selects which ribbon or mark is displayed on the Pokémon's summary screen; stored as a `RibbonIndex` enum value

#### Cosmetic Tab — Origin Mark Display and Favorite
**Status:** ✅ Implemented (#418)
**Complexity:** Low
**Tracks:** #418
**PKHeX Reference:** `PB_Origin` and `PB_Favorite` in `PKMEditor.cs`; `OriginMarkUtil.GetOriginMark(pk)` in `PKHeX.Core/PKM/Enums/OriginMark.cs`
**Tasks:**
- [x] **Origin Mark display** (`OriginMarkUtil.GetOriginMark(pk)`, Gen 6+) — read-only icon shown in the Cosmetic tab indicating which generation group the Pokémon originated from; derived from the entity's version, not editable directly:
  - Gen 6 (X/Y, ORAS) → Pentagon mark
  - Gen 7 (SM, USUM) → Clover/flower mark
  - Gen 8 SWSH → Galar mark
  - Gen 8 BDSP → Trio mark
  - Gen 8 LA → Arc/triangle mark
  - Gen 9 SV → Paldea mark
  - Gen 9 ZA → ZA mark
  - Virtual Console (Gen 1–2 VC) → Game Boy mark
  - Pokémon GO transfers → GO mark
  - LGPE (Let's Go) → Let's Go mark
- [x] **Favorite toggle** (`IFavorite.IsFavorite`) — clickable toggle; shown whenever the entity implements `IFavorite`: `PB7` (LGPE), `G8PKM` base (PK8 SWSH + PB8 BDSP), `PA8` (LA), `PK9`/`PA9` (SV); marks the Pokémon as a favorite in the PC box; in PKHeX, `ClickFavorite` uses `Entity is IFavorite` so it works for all these formats, not just LGPE

#### Generation-Specific Fields Not Yet Implemented
**Status:** ⚠️ Partial (most fields implemented in Gen-Specific tab — #419; `PK4.NSparkle` still missing)
**Complexity:** Low
**Tracks:** #419
**PKHeX Reference:** `PKMEditor.cs` Cosmetic tab + generation-specific controls
**Note:** Stat Nature, Tera Types, Is Alpha, Is Noble, Can Gigantamax, Dynamax Level, AVs, GVs, CP, and N's Sparkle (PK5) are all ✅ already implemented. The items below are the remaining gaps.
**Tasks:**
- [ ] **Gen 4 HGSS National Park Sparkle** (`PK4.NSparkle`) — boolean flag for Bug-Catching Contest winner; HG/SS only *(N's Sparkle for PK5 is already implemented; PK4's distinct use of the same property is not)*
- [x] **Gen 4 HGSS Shiny Leaves** (`PK4.ShinyLeaf`) — 6-part panel: 5 leaf type checkboxes + crown flag; HG/SS only
- [x] **Gen 4 HGSS Walking Mood** (`G4PKM.WalkingMood`) — walking partner mood value; HG/SS only
- [x] **Gen 5 B2W2 PokéStar Fame** (`PK5.PokeStarFame`) — PokéStar Studios fame value; B2W2 only
- [x] **Gen 7b LGPE Spirit** (`PB7.Spirit`) — Go Park spirit value; LGPE only
- [x] **Gen 7b LGPE Mood** (`PB7.Mood`) — partner Pokémon mood; LGPE only
- [x] **Gen 7b LGPE Received Date/Time** (`PB7.ReceivedYear/Month/Day/Hour/Minute/Second`) — full timestamp of when the Pokémon was received from GO Park; LGPE only
- [x] **Gen 3 Colosseum/XD Shadow fields** (`IShadowCapture`: `ShadowID`, `Purification`, `IsShadow`) — Shadow Pokémon identification and purification counter; Colo/XD only

#### Extra Bytes Editor
**Status:** ❌ Not Implemented
**Complexity:** Low
**Tracks:** #420
**PKHeX Reference:** `CB_ExtraBytes` + `TB_ExtraByte` in OT/Misc tab
**Tasks:**
- [ ] Add raw byte editor: offset selector (hex) + value field (0–255) for any byte in the Pokémon's data that is not exposed by a named field; useful for accessing undocumented generation-specific bytes

#### One-Touch Evolve
**Status:** ✅ Implemented
**Complexity:** Medium
**Priority:** Medium
**Tracks:** #448
**PKHeX Reference:** `EvolutionTree.GetEvolutionTree(context)` / `tree.Forward.GetForward(species, form)` — no direct WinForms UI equivalent; this is a PKMDS quality-of-life addition
**Tasks:**
- [x] Add `IAppService.GetDirectEvolutions(PKM)` returning `IReadOnlyList<EvolutionMethod>` — queries `EvolutionTree.GetEvolutionTree(pkm.Context).Forward.GetForward(pkm.Species, pkm.Form)` and filters out zero-species entries
- [x] Add **Evolve** button to `MainTab.razor`, visible only when `GetDirectEvolutions` returns at least one result and `!Pokemon.IsEgg`
- [x] Single-evolution path (e.g., Caterpie → Metapod): evolve immediately on button click
- [x] Branching-evolution path (e.g., Eevee, Slowpoke, Tyrogue, Wurmple): open `EvolvePickerDialog` showing each branch with sprite, localized species name, and method label (e.g., "Level 36", "Use Fire Stone")
- [x] `ApplyEvolution(EvolutionMethod)` logic:
  - Set `pkm.Species` and `pkm.Form` via `method.GetDestinationForm(pkm.Form)`
  - Bump `pkm.CurrentLevel` to `method.Level` if currently below minimum
  - Adjust `pkm.Gender` via `GetSaneGender()`
  - Sync nickname: call `pkm.ClearNickname()` when `!pkm.IsNicknamed`
  - Wurmple special case: in Gen 6+ set `pkm.EncryptionConstant` via `WurmpleUtil.GetWurmpleEncryptionConstant(evoVal)`; in Gen 3–5 the EC setter is a no-op (`EncryptionConstant { get => PID; set { } }`), so set `pkm.PID` directly to a value satisfying `WurmpleUtil.GetWurmpleEvoVal(pid) == evoVal`
  - Call `AppService.LoadPokemonStats(pkm)` and `RefreshService.Refresh()`
- [x] Create `EvolvePickerDialog.razor[.cs]` — MudBlazor dialog listing evolution choices, closes with the selected `EvolutionMethod`
- [x] **Shedinja special case** — when evolving Nincada → Ninjask, offer to also place a Shedinja in the next empty box slot
- [x] **Gen 2 trade evolution** — show required held item in the evolution picker dialog
- [x] Unit tests: no-evo guard, single-evo direct apply, Eevee multi-branch, Wurmple EC correction, level-bump, nickname sync (nicknamed + un-nicknamed)

### 1.2 Legality Checker
**Status:** ⚠️ Partial (core analysis, UI, fix buttons, and batch report implemented; comprehensive unit tests remain)
**Complexity:** Very High
**Priority:** Critical
**Tasks:**
- [x] Design Legality Checker UI component (`LegalityTab`)
- [x] Integrate PKHeX.Core `LegalityAnalysis` (`IAppService.GetLegalityAnalysis`)
- [x] Display legality results with color coding (legal/illegal/suspicious)
- [x] Show detailed legality report with:
  - [x] Encounter type validation (`CheckIdentifier.Encounter`)
  - [x] Relearn moves validation (`CheckIdentifier.RelearnMove`)
  - [x] PID/EC validation (`CheckIdentifier.PID` / `CheckIdentifier.EC`)
  - [x] Ribbon validation (`CheckIdentifier.Ribbon` / `CheckIdentifier.RibbonMark`)
  - [x] Memory validation (`CheckIdentifier.Memory`)
  - [x] Met location validation (part of encounter analysis)
  - [x] Ball legality (`CheckIdentifier.Ball`)
  - [x] Ability legality (`CheckIdentifier.Ability`)
  - [x] Level/experience validation (`CheckIdentifier.Level`)
- [x] Add "Fix" buttons for common legality issues (ball, met location, moves, TechRecord) — closes #401
- [x] Implement batch legality checking — "Legality Report" tab sweeps all party/box slots, sortable/filterable table, Legal/Fishy/Illegal counts, click-to-jump-to-slot — closes #402
- [x] **Tri-state indicators with per-state visibility toggles** — MudToggleGroup filter surfaces Legal / Fishy / Illegal independently; active filter is visually marked; "First issue" column surfaces Fishy reasons
- [x] **Batch legalize from the report** — wraps the Auto-Legality engine (see §5.1); cancel button, per-Pokémon timeout, and in-place entry updates; adapts PKM to the save file before analysis so results match what will be written
- [x] Show legality warnings on Pokémon slot display (valid/warn icon overlay)
- [x] Add inline per-field legality indicators throughout Pokémon editor tabs (#411)
  - [x] Reusable `LegalityPopover` component (clickable info button → popover with humanized message + optional quick-fix button, renders nothing when valid; #773)
  - [x] `LegalityAnalysis` computed once in `PokemonEditForm` and passed to all tabs
  - [x] Per-move-slot indicators (current moves + relearn moves) in **Moves tab**
  - [x] Group-level indicators for IVs/EVs/AVs/GVs in **Stats tab**
  - [x] Field-level indicators (PID, Ability, Gender, Shiny, Nature, Form, Held Item, Nickname, Language, Egg) in **Main tab**
  - [x] Field-level indicators (Ball, Encounter/Met Location, Level, Game Origin) in **Met tab**
  - [x] Field-level indicators (EC, Trainer/OT, Handler, Memory) in **OT/Misc tab**
  - [x] Per-ribbon indicators in **Ribbons tab**
- [ ] Create comprehensive unit tests
- **Note:** `ParseSettings.InitFromSaveFileData` is intentionally not called (see `MainLayout.razor.cs` comment); relies on default `AllowGBCartEra = false` so VC encounters are always checked regardless of filename. PKHeX bug filed: [kwsch/PKHeX#4734](https://github.com/kwsch/PKHeX/issues/4734).

### 1.3 Batch Editor
**Status:** ✅ Implemented
**Complexity:** Very High
**Priority:** High
**Tracks:** #329
**Tasks:**
- [x] Design Batch Editor UI with script input
- [x] Implement batch editor scripting engine (wraps PKHeX.Core `BatchEditing` / `StringInstructionSet`):
  - [x] Property filtering (`.Property=Value`)
  - [x] Property setting (`=Property=Value`)
  - [x] Comparison operators (`>`, `<`, `>=`, `<=`, `!=`)
  - [x] Logical operators (`&`, `|`)
  - [x] Special filters (`.IsShiny`, `.IsEgg`, etc.)
- [x] Add batch editor presets/templates
- [x] Implement dry-run mode (preview changes)
- [ ] Add undo functionality
- [x] Support filtering by all PKM properties (species, levels, OT/TID, moves, ribbons, stats, met data, etc.)
- [ ] Create batch editor tutorial/documentation
- [ ] Add example scripts library
- [ ] Implement batch export/import
- [ ] Create unit tests

### 1.4 Teams Tab (Battle Box, Battle Teams, Rental Teams)
**Status:** ✅ Implemented (slot assignment via drag-and-drop is a follow-up — #671)
**Complexity:** Very High
**Tracks:** #661
**PKHeX Reference:** `SAV_BattleBox`, `SAV_RentalTeam9`, `SAV_RentalTeam8`, `BoxLayout7/8/8b`, `TeamIndexes8`

A dedicated **Teams** tab in `SaveFileComponent.razor` for viewing and managing battle box, battle teams, and rental teams across all supporting generations. Implementation is split across `TeamsTab.razor[.cs]`, `IAppService` battle/rental team operations, and box grid indicator overlays.

**Tasks:**
- [x] **Service layer** — battle team and rental team get/set/clear/lock operations on `IAppService`
- [x] **Battle box editing operations** — clear, unlock, lock all teams
- [x] **Battle Teams section** — 6 team cards (SWSH/SV/ZA), team name display, lock toggle, 6 sprite slots resolved from box slot references, per-team Showdown export and clear
- [x] **Rental Teams section** — Gen 8 SWSH and Gen 9 SV rental teams with Showdown export and clear
- [x] **Battle team indicators on box grid** — small badge overlay on box-slot sprites showing which battle team a Pokémon is registered to (mirrors PKHeX WinForms `StorageSlotSource.BattleTeamN` flags)
- [x] **Unit tests** — battle team service layer (get, clear, lock, unlock, indicators)
- [ ] **Slot assignment via drag-and-drop / picker** — tracked separately in #671

---

## Priority 2: Important Secondary Features

### 2.1 Database/Bank System
**Status:** ⚠️ Partial (Pokémon Bank implemented — #330; Mystery Gift DB redesign tracked in #444)
**Complexity:** Very High
**Tasks:**

#### 2.1a Mystery Gift Database Redesign (#444)
**Status:** 🔲 Planned
**Complexity:** Medium
Bring the Mystery Gift Database tab to full parity with PKHeX's `SAV_MysteryGiftDB` design language.
- [ ] Replace card grid + pagination with virtualized scrollable row list (`<Virtualize>`)
- [ ] Add structured filter panel:
  - [ ] Species dropdown (filtered to species present in event DB)
  - [ ] Held item dropdown
  - [ ] Move 1–4 dropdowns (`mg.HasMove()`)
  - [ ] Generation filter with comparator (Any / >= / == / <=), preset to save generation
  - [ ] Shiny tri-state toggle (Any / Shiny / Not Shiny)
  - [ ] Egg tri-state toggle (Any / Egg / Not Egg)
  - [ ] Filter Unavailable Species toggle (existing logic, exposed as UI option)
  - [ ] Reset Filters button
  - [ ] Result count label
- [ ] Add detail panel (populated on row selection, shows `mg.GetTextLines()`)
- [ ] Add Export Gift File action (download `.wc*` blob via JS interop; disabled for WC3 until #423)
- [ ] Add Generate action (`mg.ConvertToPKM` → write to selected/first-empty box slot → navigate to Party/Box tab, same pattern as Encounter Database)
- [ ] Add Export All Results action (zip download of `DataMysteryGift` entries in current result set)
- [ ] Responsive layout: sidebar filter panel collapses to `MudDrawer` on mobile

#### 2.1b Pokémon Database/Bank (Personal Collection)
**Status:** ⚠️ Partial (core bank implemented — #330; advanced filters and organization remain)
**Complexity:** Very High
**Tracks:** #330

**Implemented:**
- [x] Design Pokémon Bank UI — card grid with species sprite, nickname, shiny indicator (same sprite as box slots), source save label, and added date
- [x] Implement personal Pokémon storage using browser IndexedDB (`bank.js` + `BankService`)
- [x] Add bank import/export functionality (JSON file round-trip via File System Access API)
- [x] Create basic search/filter: species/nickname text search, shiny-only toggle; filtered count shown only when active
- [x] Mass import from save — "Add from Save" imports all party + box Pokémon with source save tracking (trainer name, TID, game name)
- [x] Send to save — single and multi-select batch send with cross-format conversion (EntityConverter)
- [x] Batch delete — multi-select with confirmation dialog
- [x] Duplicate detection — O(N+M) bulk check via `PartitionDuplicatesAsync` using DecryptedBoxData hash set; skip-or-add-all prompt on import
- [x] Backup reminder — dismissible banner warning that browser storage clearing erases the bank
- [x] Pagination — configurable page size (20/50/100)
- [x] Uniform card height — nickname line always rendered (placeholder when absent)
- [x] Bulk insert performance — single IDB transaction + single JS interop call for batch imports
- [x] Invalid record cleanup — corrupt/unparseable IndexedDB entries auto-deleted on load

**Remaining:**
- [x] Import `.pk*` and `.zip` (containing `.pk*`) files directly into the Bank (#780)
- [x] Export individual or multi-selected Pokémon from Bank as `.pk*` files or a ZIP (#779)
- [ ] Create advanced search filters (#645): species/forms, moves, abilities, ribbons, stats/IVs, ball type, OT/TID, generation/origin, source save
- [ ] Add folder/tag organization system (#646)
- [ ] Support drag-and-drop from database to save (#647)
- [ ] Create database statistics view (#648)
- [ ] Support multiple database profiles

### 2.2 Encounter Database
**Status:** ✅ Implemented (Living Dex builder deferred — see §5.2)
**Complexity:** High
**Tasks:**
- [x] Design Encounter Database UI
- [x] Integrate PKHeX.Core encounter data
- [x] Implement encounter search/filter by:
  - [x] Species
  - [x] Game version
  - [x] Location
  - [x] Level range
  - [x] Encounter type (Wild, Static, Gift, Trade, Egg, Mystery Gift)
  - [x] Shiny locked status
- [x] Add "Generate Legal Pokemon" from encounter
- [x] Display encounter details (location, level, shiny status)
- [x] Support Mystery Gift encounter generation
- [ ] Add Living Dex builder using encounter database (deferred — §5.2)
- [x] Create unit tests

### 2.3 QR Code Support
**Status:** ❌ Not Implemented  
**Complexity:** Medium  
**Tasks:**
- [ ] Add QR code generation for Pokemon (Gen 7 format)
- [ ] Add QR code scanning/import
- [ ] Support Showdown QR codes
- [ ] Add QR code export for teams
- [ ] Implement web camera access for QR scanning
- [ ] Create QR code gallery view
- [ ] Add unit tests for QR encode/decode

### 2.4 Advanced Search
**Tracks:** #332
**Status:** ✅ Implemented
**Complexity:** Medium
**Tasks:**
- [x] Create Advanced Search UI component (`AdvancedSearchTab.razor` / `.razor.cs`)
- [x] Implement search across all party and box slots:
  - [x] Species/forms
  - [x] Shiny status
  - [x] IVs (floor per stat)
  - [x] EVs (floor per stat)
  - [x] Nature
  - [x] Ability
  - [x] Moves (any/all chip sets)
  - [x] Hidden Power type
  - [x] Gender
  - [x] Ball type
  - [x] Level range (min/max)
  - [x] OT name (case-insensitive substring) / TID
  - [x] Ribbons / Marks (reflection-based property name matching)
  - [x] Origin game
  - [x] Egg/not egg
  - [x] Legal/illegal status (evaluated last for performance)
- [ ] Add search result highlighting (future enhancement)
- [x] Implement saved search filters (localStorage via JS interop)
- [x] Support batch operations on search results (Showdown text copy)
- [x] Add search export functionality (Showdown clipboard export)
- [x] **Info popovers on filter fields and result rows (#579)** — ball/ability/held item filter fields show item sprites and description popovers; result rows show ball and held item sprites; explicit "Jump to Pokémon" button per row replaces implicit row-click navigation
- [x] **Extended filters (#737)** — type filter, origin game, flags (shiny, egg, nicknamed, etc.), ribbons, marks; all dropdowns converted to type-to-filter autocomplete inputs

### 2.5 Pokédex Editor
**Tracks:** #414, #654
**Status:** ⚠️ Partial (per-species Seen/Caught grid + LA research task editor implemented via #490/#437/#438/#439/#510; bulk operations and progress bars via #436; per-generation advanced fields — gender/shiny tracking, form tracking, language flags — #654)
**Complexity:** High
**PKHeX Reference:** `SAV_SimplePokedex.cs`, `SAV_Pokedex4.cs`, `SAV_Pokedex5.cs`, `SAV_PokedexXY.cs`, `SAV_PokedexORAS.cs`, `SAV_PokedexSM.cs`, `SAV_PokedexGG.cs`, `SAV_PokedexSWSH.cs`, `SAV_PokedexBDSP.cs`, `SAV_PokedexLA.cs`, `SAV_PokedexSV.cs`, `SAV_PokedexSVKitakami.cs`, `SAV_Pokedex9a.cs`
**Core API:** `ZukanBase` / `Zukan4` / `Zukan5` / `Zukan6` / `Zukan7` / `Zukan7b` / `Zukan8` / `Zukan8b` / `PokedexSave8a` / `Zukan9Paldea` / `Zukan9Kitakami` / `Zukan9a` in `PKHeX.Core/Saves/Substructures/PokeDex/`

**Common API (all gens):**
- `GetSeen(ushort species)` / `SetSeen(...)` / `GetCaught(ushort species)` / `SetCaught(...)`
- `SeenAll(bool shinyToo)` / `CaughtAll(bool shinyToo)` / `CompleteDex(bool shinyToo)`
- `SeenNone()` / `CaughtNone()` / `ClearDexEntryAll(ushort species)`
- `SetDex(PKM pk)` — auto-update entry from a Pokémon object

#### Gen 1–3 (SAV1 / SAV2 / SAV3)
**Status:** ✅ Implemented (simple seen/caught bitflags + per-species grid)
- [x] Seen flag per species
- [x] Caught flag per species
- [x] Bulk fill / clear
- [x] Per-species edit UI (searchable per-species Seen/Caught grid via #490)

#### Gen 4 (Zukan4) — Diamond / Pearl / Platinum / HeartGold / SoulSilver
**Status:** ⚠️ Partial (bulk fill/seen-all/clear via #436; per-species grid via #490; advanced fields absent)
- [x] Bulk fill (CompleteDex) / Seen All / Clear
- [x] Per-species seen flag UI (basic Seen/Caught grid via #490)
- [x] Per-species caught flag UI (basic Seen/Caught grid via #490)
- [ ] Gender-seen tracking: male-first and female-first regions (2 separate bitfields)
- [ ] Form tracking (species-specific; varies by game):
  - Unown ×28, Deoxys ×4, Shellos/Gastrodon ×2
  - Rotom ×6, Shaymin ×2, Giratina ×2, Pichu ×3 (HG/SS only)
- [ ] Spinda: store first-seen Spinda's PID (32-bit, offset `0x0104`)
- [ ] Language flags (HG/SS only — DP/Pt lack language data)
- [x] Per-species edit UI (basic Seen/Caught grid via #490)

#### Gen 5 (Zukan5BW / Zukan5B2W2) — Black / White / Black 2 / White 2
**Status:** ⚠️ Partial (bulk fill/seen-all/clear via #436; per-species grid via #490; advanced fields absent)
- [x] Bulk fill (via per-species GiveAll) / Seen All / Clear
- [ ] 4-region gender×shiny seen tracking (male, female, male-shiny, female-shiny)
- [ ] Display variant selection per species (which gender/shiny combo shows in Pokédex)
- [ ] Form tracking (Unown ×28, Castform ×4; B2W2 adds more species)
- [ ] Language flags (7 languages: JPN, ENG, FRE, ITA, GER, SPA, KOR)
- [ ] Spinda PID storage
- [ ] National Dex unlock flag and mode tracking
- [x] Per-species edit UI (basic Seen/Caught grid via #490)

#### Gen 6 XY (Zukan6XY) — X / Y
**Status:** ⚠️ Partial (bulk fill/seen-all/clear via #436; per-species grid via #490; advanced fields absent)
- [x] Bulk fill (via per-species GiveAll) / Seen All / Clear
- [x] Per-species seen / caught flags UI (basic grid via #490)
- [ ] 4-region gender×shiny seen tracking
- [ ] Per-form seen flags (form bits alongside species bits)
- [ ] Display form, display gender, display shiny selection per species
- [ ] Language flags (7 languages; slot 6 unused/skipped)
- [ ] "Foreign" flag per species (tracks Pokémon migrated from Gen 5)
- [ ] Spinda: store first-seen Spinda's encryption constant
- [ ] National Dex unlock / mode flags
- [x] Per-species edit UI (basic Seen/Caught grid via #490)

#### Gen 6 ORAS (Zukan6AO) — Omega Ruby / Alpha Sapphire
**Status:** ⚠️ Partial (bulk fill/seen-all/clear via #436; per-species grid via #490; advanced fields absent)
- [x] Bulk fill (via per-species GiveAll) / Seen All / Clear
- [ ] All XY features above (per-form, gender, language, foreign flags)
- [ ] Encounter count per species (`u16`, running total of wild encounters)
- [ ] Obtained count per species (`u16`, running total of catches)
- [x] Per-species edit UI (basic Seen/Caught grid via #490)

#### Gen 7 SM/USUM (Zukan7) — Sun / Moon / Ultra Sun / Ultra Moon
**Status:** ⚠️ Partial (bulk fill/seen-all/clear via #436; per-species grid via #490; advanced fields absent)
- [x] Bulk fill (CompleteDex) / Seen All / Clear
- [x] Per-species seen / caught flags UI (basic grid via #490)
- [ ] 4-region gender×shiny seen tracking
- [ ] Per-form seen flags
- [ ] Display form / display gender / display shiny selection per species
- [ ] Language flags (9 languages: adds Simplified Chinese and Traditional Chinese)
- [ ] Spinda ×4 storage (separate first-seen per gender×shiny combination)
- [ ] Current-viewed-dex tracking (Alola regional vs National)
- [x] Per-species edit UI (basic Seen/Caught grid via #490)

#### Gen 7b LGPE (Zukan7b) — Let's Go Pikachu / Eevee
**Status:** ⚠️ Partial (bulk fill/seen-all/clear via #436; per-species grid via #490 limited to 153-species)
- [x] Bulk fill (CompleteDex) / Seen All / Clear
- [x] Per-species seen / caught flags UI for the limited (153-species) Let's Go Pokédex (via #490)

#### Gen 8 SWSH (Zukan8) — Sword / Shield
**Status:** ⚠️ Partial (bulk fill/seen-all/clear via #436; per-species grid with Galar-only filtering via #490; advanced fields absent)
- [x] Bulk fill (CompleteDex) / Seen All / Clear
- [x] Per-species seen / caught flags UI (Galar-filtered grid via #490)
- [ ] **3 independent regional dex blocks** (each a separate `Zukan8` instance):
  - Galar (400 species)
  - Isle of Armor DLC (211 species)
  - Crown Tundra DLC (210 species)
- [ ] Per-form seen tracking (up to 63 forms as bits in a `u64` field)
- [ ] Gigantamax seen / caught flag (bit 63 of the form `u64`)
- [ ] Display options per species: form ID, gender, shiny, Gigantamax/Dynamax preference
- [ ] Language flags (9 languages)
- [ ] Battled count per species (`u32`)

#### Gen 8 BDSP (Zukan8b) — Brilliant Diamond / Shining Pearl
**Status:** ⚠️ Partial (bulk fill/seen-all/clear via #436; per-species grid via #490; advanced fields absent)
- [x] Bulk fill (CompleteDex) / Seen All / Clear
- [x] Per-species seen / caught flags UI (basic grid via #490)
- [ ] `ZukanState8b` state per species: `None` → `HeardOf` → `Seen` → `Captured`
- [ ] Gender × shiny seen flags (4 separate `u32` arrays: male, female, male-shiny, female-shiny)
- [ ] Form tracking per species (Unown ×28, Castform ×4, Deoxys ×4, Rotom ×6, Giratina ×2, Shaymin ×2, Arceus ×18)
- [ ] Regional (Sinnoh) dex obtained flag vs National Dex obtained flag
- [ ] Language flags (9 languages)

#### Gen 8 LA (PokedexSave8a) — Legends: Arceus
**Status:** ⚠️ Partial (bulk fill via #436; per-species research task editor + Clear All Research via #510/#439; display sub-editor via #438; advanced task types and 5-area flags remain)
**Note:** Completely different architecture — no simple seen/caught flags; tracked via per-task research counters. See also §3.3.
- [x] Bulk fill all research tasks at max threshold (via `SetResearchTaskProgressByForce` loop + `UpdateAllReportPoke`)
- [x] `SetSolitudeAll()` called as part of fill
- [x] Per-species research task progress edit UI — `PokedexLaResearchEditorDialog` opens on row click; shows task counters in tabbed layout; "Edit All Tasks…" button
- [x] Research rate % display and progress bar (capped at 100%) per species
- [x] "Complete All Research" and "Clear All Research" bulk buttons
- [x] 5 local-area dex labels (Fieldlands, Mirelands, Coastlands, Highlands, Icelands) shown in research panel
- [ ] Perfect research (100%) completion flag per species (individual toggle)
- [ ] Full 22+ research task types editing (all counters fully editable per task)
- [ ] Display selection: form, gender, shiny, alpha per species

#### Gen 9 SV (Zukan9Paldea / Zukan9Kitakami) — Scarlet / Violet
**Status:** ⚠️ Partial (bulk fill/seen-all/clear via #436; per-species grid via #490 with SV regional dex filtering; advanced fields absent)
**Note:** Simple stubs existed in roadmap as "Advanced Pokédex (Kitakami, etc.)"; consolidated here.
- [x] Bulk fill (CompleteDex — covers Paldea + Kitakami internally) / Seen All / Clear
- [x] Per-species seen / caught flags UI (SV regional dex filtering via #490)
- [ ] 3-tier DLC dex system (separate `Zukan9` instances per save revision):
  - Paldea (base game)
  - Kitakami (DLC 1 — The Teal Mask)
  - Blueberry (DLC 2 — The Indigo Disk)
- [ ] State system per species: Unknown → Known (adjacent species) → Seen → Caught
- [ ] Per-form seen and caught flags
- [ ] Gender seen flags, shiny seen flags, shiny caught flags
- [ ] Display options: form, gender, shiny, "New" flag
- [ ] Language flags (9 languages)

#### Gen 9 ZA (Zukan9a) — Legends: Z-A
**Status:** ⚠️ Partial (personal table filtering implemented in #490; advanced fields absent)
- [x] Per-species Seen/Caught grid (filtered to ZA personal table via #490)
- [ ] Per-species state tracking (ZA-specific state system)
- [ ] Mega / Mega X / Mega Y form seen flags
- [ ] Alpha form tracking
- [ ] Language flags (9 languages)

#### Cross-generation bulk operations
- [x] Fill all seen — Gen 1–3
- [x] Fill all caught — Gen 1–3
- [x] Fill Pokédex (CompleteDex) — Gen 4–9 (#436; Gen 5/6 via per-species GiveAll; Gen 8 LA via research task loop)
- [x] Seen All — Gen 1–9 except LA (#436)
- [x] Clear Pokédex (SeenNone + CaughtNone) — Gen 1–9 except LA (#436)
- [x] Progress bars (seen % and caught %) — all gens (#436)
- [ ] Fill all seen with gender/shiny variants — Gen 4+
- [ ] Fill all caught with all form variants — Gen 4+
- [ ] Set all language flags — Gen 4+
- [ ] Complete a single species entirely (all forms, genders, shinies, languages)
- [ ] Complete all research tasks at 100% — Gen 8 LA only (covered by Fill above, but no per-species UI)

### 2.6 Event Flags & Story Progress
**Status:** ❌ Not Implemented  
**Complexity:** Very High  
**Warning:** Highly game-specific and prone to save corruption  
**Tasks:**
- [ ] Design Event Flags editor UI with safety warnings
- [ ] Implement Event Flags viewing/editing
- [ ] Add Event Constants (Work) viewing/editing
- [ ] Create Flag/Work comparison tool (before/after saves)
- [ ] Add generation-specific flag editors:
  - [ ] Gen 1-2 event flags
  - [ ] Gen 3 event flags
  - [ ] Gen 4 event flags
  - [ ] Gen 5 event flags
  - [ ] Gen 6 event flags
  - [ ] Gen 7 event flags
  - [ ] Gen 8/8.5 flag/work system
  - [ ] Gen 9 flag/work system
- [ ] Implement flag search/filter
- [ ] Add flag descriptions (where documented)
- [ ] Create backup/restore for flag edits
- [ ] Add "Missing Flags Checker" functionality
- [ ] Implement story progress presets (if safe)
- [ ] Create comprehensive warnings about save corruption risks
- [ ] Add unit tests with known flag states

---

## Priority 3: Generation-Specific Features

### 3.1 Generation 9 (Scarlet/Violet)
**Status:** ⚠️ Partial (Plus Move flags implemented)  
**Tasks:**
- [x] **Plus Move Support** (Legends Z-A)
  - [x] Plus Move checkboxes (inline with current moves)
  - [x] Plus Flags dialog editor
  - [x] Give All / Remove All functionality
- [ ] Raid Editor (Tera Raids)
  - [ ] Raid seed editor
  - [ ] Raid Pokemon species/stats editor
  - [ ] Raid difficulty/star editor
  - [ ] Tera type editor
  - [ ] Raid rewards editor
- [ ] Fashion/Clothing editor (SAV_Fashion9)
- [ ] Sandwich/Picnic editor (Donut editor)
- [ ] Pokédex editor (Paldea / Kitakami / Blueberry) — see §2.5
- [ ] Tera Orb charge status
- [ ] Academy class progress
- [ ] Gym badges & Elite Four
- [ ] League Points (LP) editor
- [ ] Rotom Phone skin/upgrades

### 3.2 Generation 8.5 (BDSP)
**Tasks:**
- [x] **Feebas tile locator** (BDSP — Mt. Coronet B1F) — finds Feebas tiles for the save's RNG seed; includes Feebas seed editor — closes #815
- [ ] Underground editor (SAV_Underground8b)
- [ ] Poffin editor (SAV_Poffin8b)
- [ ] Ball Capsule/Seal Stickers editor (SAV_SealStickers8b)
- [ ] Contest stats and rankings
- [ ] Super Contest show editor
- [ ] Fashion/clothing editor
- [ ] Casual/Ranked battle restrictions

### 3.3 Generation 8 (Sword/Shield, Legends Arceus)
**Status:** ⚠️ Partial (Move Shop, TR Flags implemented)  
**Tasks:**
- [ ] Raid Den editor (SAV_Raid8)
  - [ ] Den seed manipulation
  - [ ] Active raid editor
  - [ ] Raid rewards
- [ ] Curry Dex completion
- [ ] Rotom Bike/Rotom Catalog
- [ ] Dynamax/Gigantamax data
- [ ] DLC progress (Isle of Armor, Crown Tundra)
- [x] **Technical Records (TR) Relearn Flags** (Gen 8+)
  - [x] TR flag editor dialog with checkbox grid
  - [x] Give All / Remove All functionality
  - [x] Support for PK8, PA8, PB8, PK9, PA9
- [ ] **Legends Arceus specific:**
  - [x] **Move Shop Editor**
    - [x] Purchased flags for unlocked moves
    - [x] Mastered flags for mastered moves
    - [x] Combined Purchased/Mastered dialog
  - [x] **Alpha Move** selector dropdown
  - [x] **Mastered Move** checkboxes (inline with current moves)
  - [ ] Pokédex Research Tasks editor — see §2.5 (Gen 8 LA)
  - [ ] Alpha Pokémon caught tracker
  - [ ] Mass Outbreak editor
  - [ ] Space-Time Distortion progress
  - [ ] Pastures management
  - [ ] Crafting recipes unlocked
  - [ ] Request completion

### 3.4 Generation 7 (Sun/Moon, Ultra Sun/Ultra Moon, Let's Go)
**Tasks:**
- [ ] Festival Plaza editor (SAV_FestivalPlaza)
  - [ ] Festival Plaza rank
  - [ ] Festival Coins (FC)
  - [ ] Facilities unlocked
- [ ] Poke Pelago editor (SAV_Pokebean)
  - [ ] Bean counts
  - [ ] Island development
- [ ] Zygarde Cell collection (SAV_ZygardeCell)
- [ ] Battle Agency progress
- [ ] Mantine Surf scores
- [ ] Ultra Wormhole progress
- [ ] **Let's Go specific:**
  - [ ] Capture Combo editor (SAV_Capture7GG)
  - [ ] GO Park transfer data
  - [ ] Partner Pikachu/Eevee customization
  - [ ] Candy counts by species

### 3.5 Generation 6 (X/Y, ORAS)
**Status:** ⚠️ Partial (Relearn Moves implemented)  
**Tasks:**
- [x] **Relearn Moves** (Gen 6+)
  - [x] 4 relearn move slots with search
  - [x] Type icon display
  - [x] Category icon display (Physical/Special/Status)
  - [x] Extension methods for relearn move access
- [ ] O-Powers editor
- [ ] Pokémon-Amie affection
- [ ] Super Training medals/stats
- [ ] Berry Field editor
- [ ] PSS (Player Search System) settings
- [ ] Secret Base editor (ORAS)
  - [ ] Base location
  - [ ] Base decoration
  - [ ] Base team
- [ ] Contest stats (ORAS)
- [ ] Soaring location unlocks (ORAS)
- [ ] Mirage spots (ORAS)
- [ ] Eon Ticket flag

### 3.6 Generation 5 (Black/White, B2/W2)
**Tasks:**
- [ ] Entralink editor
- [ ] Medals editor
  - **PKHeX.Core API** (landed upstream after the `26.5.5` tag — PKHeX `master`, May 2026; reaches PKMDS on the next `PKHeX.Core` NuGet bump): `SaveBlockAccessor5B2W2.Medals` → `MedalList5` (block 68), in `PKHeX.Core/Saves/Substructures/Gen5/MedalList5.cs` (+ `Medal5.cs`). WinForms UI reference: `SAV_Medals5`. Upstream commit `2fb38b368`.
- [ ] Join Avenue editor
  - **PKHeX.Core API** (landed upstream after the `26.5.5` tag — PKHeX `master`, May 2026; reaches PKMDS on the next `PKHeX.Core` NuGet bump): `SAV5B2W2.JoinAvenue` → `JoinAvenue5` (block 67), in `PKHeX.Core/Saves/Substructures/Gen5/Join Avenue/` (`JoinAvenue5`, `JoinAvenueVisitor5`, `JoinAvenueAssistant5`, `JoinAvenueFan5`, `JoinAvenueSettings5`, `IJoinAvenueEntity5`). WinForms UI reference: `SAV_JoinAvenue`. Upstream commit `934ec2afe`.
- [ ] Musical props editor
- [ ] C-Gear skin editor (SAV_CGearSkin, CGearImage)
- [ ] Dream World data
- [ ] Funfest Missions
- [ ] PWT (Pokemon World Tournament) progress
- [ ] Battle Subway progress
- [ ] DLC/Black City/White Forest editor (SAV_DLC5)
- [ ] Pass Powers

### 3.7 Generation 4 (Diamond/Pearl/Platinum, HGSS)
**Tasks:**
- [x] **Feebas tile locator** (DPPt — Mt. Coronet B1F) — finds Feebas tiles for the save's RNG seed; includes Feebas seed editor — closes #815
- [ ] Pokéwalker editor (SAV_Pokéwalker4)
  - [ ] Route unlocks
  - [ ] Steps counter
  - [ ] Watts
- [ ] Underground editor (SAV_Underground4)
  - [ ] Traps/goods
  - [ ] Secret base
  - [ ] Flags captured
- [ ] Pokétch app unlocks
- [ ] Safari Zone editor (HGSS)
- [x] **Pokéathlon Points** (currency) — already implemented in the trainer-tab currency section (PR #872); distinct from the performance records below
- [ ] **Pokéathlon stats** (performance records — best score per event, per-event course records, athlete medals; distinct from the Pokéathlon Points currency above)
  - **PKHeX.Core API** (landed upstream after the `26.5.5` tag — PKHeX `master`, May 2026; reaches PKMDS on the next `PKHeX.Core` NuGet bump): `SAV4HGSS.Pokeathlon` → `Pokeathlon4`, in `PKHeX.Core/Saves/Pokeathlon4.cs` — `GetBestScore`/`SetBestScore` over `PokeathlonEvent4` (10 events), `GetCourseRecord` over `PokeathlonStat4`, `Medals` (`PokeathlonMedalManager4`), and `GlobalCounters`. (`Pokeathlon4.Points` is the currency value already surfaced above.) WinForms UI reference: the `Pokeathlon*4Editor` controls. Upstream commits `300953128`, `5cbc796ad`.
- [ ] Battle Frontier progress
- [ ] Seal/Ball Capsule editor
- [ ] Villa furniture (Platinum)
- [ ] GTS deposits
- [ ] Battle Video editor

### 3.8 Generation 3 (Ruby/Sapphire/Emerald, FR/LG, Colosseum/XD)
**Tasks:**
- [x] **WC3 Wonder Card import** (`IGen3Wonder`: write `WonderCard3` from `.wc3` file to save's wonder card slot; Emerald and FR/LG only) — closes #811
- [x] **RTC (Real Time Clock) editor** (`SAV_RTC3`) — view/adjust in-game time; supports battery-dead clock repair — closes #865
- [x] **Feebas tile locator** (Gen 3 RSE) — finds Feebas tiles on Route 119 for the save's RNG seed; includes Feebas seed editor — closes #815
- [ ] Secret Base editor (SAV_SecretBase3)
  - [ ] Location
  - [ ] Decorations
  - [ ] Registry
- [ ] Berry Master/Berry trees
- [ ] Roamer Pokemon editor (SAV_Roamer3)
- [ ] Misc editor (SAV_Misc3)
- [ ] PokeBlock Case editor (PokeBlock3CaseEditor)
- [ ] Battle Frontier symbols
- [ ] **Colosseum/XD specific:**
  - [ ] Purification progress
  - [ ] Shadow Pokemon
  - [ ] Orre Colosseum progress
  - [ ] Mt. Battle progress
  - [ ] Strategy Memo

### 3.9 Generation 2 (Gold/Silver/Crystal)
**Tasks:**
- [ ] Decorations editor (SAV_Misc2)
- [ ] Unown Pokedex
- [ ] Bug Catching Contest
- [ ] Mobile Stadium data
- [ ] Mystery Gift data
- [ ] Time capsule data

### 3.10 Generation 1 (Red/Blue/Yellow)
**Tasks:**
- [ ] Misc editor (SAV_Misc1)
- [ ] Hall of Fame editor
- [ ] Pokemon Stadium data

---

## Priority 4: Quality of Life Features

### 4.1 UI/UX Improvements
**Tasks:**
- [x] **Fix "Load Pokémon File" and "Load Mystery Gift File" slot behaviour (#445)** — use currently selected slot (or first empty box slot as fallback), write via `AppService.EditFormPokemon`/`SavePokemon`, show snackbars instead of blocking dialogs, and navigate to Party/Box tab after placement.
- [ ] Add keyboard shortcuts for common operations
- [ ] Implement undo/redo functionality
- [x] **Theme-aware startup / loading screen + three-way theme toggle (#449)** — inline script in `index.html` stamps `data-theme` on `<html>` before WASM boots (no white flash); `app.css` adds `@media (prefers-color-scheme: dark)` + `[data-theme]` CSS rules for the loading screen; `MainLayout.razor` AppBar toggle redesigned from `MudSwitch` to a `MudButtonGroup` with Light / System / Dark icon buttons; `MainLayout.razor.cs` gains `ThemeMode` enum, persists choice to `pkmds_theme` in `localStorage`, and only follows live OS changes in System mode.
- [ ] Add theme customization (dark/light modes already exist, add more)
- [ ] Create customizable hotkeys
- [x] **Info popovers for moves, items, abilities, and balls (#579)** — `DescriptionService` fetches Bulbapedia-sourced descriptions from bundled JSON; reusable `InfoButton` and `BagItemInfoButton` components with lazy-loading `MudPopover`s; Moves tab shows type/category/power/PP/accuracy/description per slot; Main tab shows held item and ability descriptions; Met tab shows ball description; Bag tab shows item sprite + move details for TM/HM items; Advanced Search shows info buttons on filters and result rows with explicit jump-to-Pokémon button
- [x] **In-app browser detection and warning (#663)** — detects when the app is loaded inside an in-app browser (Telegram, WhatsApp, WeChat, Instagram, Facebook, etc.) and shows a warning so users open the site in a real browser where File System Access works
- [x] **Non-exportable save detection at load time (#675/#679)** — flags blank/template saves as non-exportable on load and warns the user before they attempt to export
- [ ] Add tooltip help system (wiki-link HelpButton — see §6.3)
- [ ] Implement context menus for Pokemon slots
- [ ] Add quick-edit mode (inline editing)
- [ ] Create Pokemon comparison view (side-by-side)
- [ ] Add recently edited Pokemon list
- [ ] Implement favorite Pokemon marking
- [ ] Add Pokemon sorting options in boxes
- [ ] Create box wallpaper customization
- [ ] Add grid size options
- [ ] Implement multi-select for batch operations

### 4.2 Import/Export Enhancements
**Tracks:** #61, #166
**Tasks:**
- [x] **Manic EMU `.3ds.sav` round-trip** — auto-detects the ZIP-based format on load, extracts the PKHeX-compatible save bytes, and repacks to the same ZIP structure on export so saves re-import directly into Manic EMU (closes #537)
- [x] **Showdown text import** — `ShowdownImportDialog` parses Showdown text into legal PKM and delegates generation to the Auto-Legality engine (see §5.1), with HOME tracker / Scale fix-ups and overwrite-party confirmation flow (closes #656 and #691; partial coverage of #61)
- [x] **PokePaste import / export** — `PokePasteExportDialog` exports the current party as a PokePaste paste; PokePaste URLs can be imported directly via the same Showdown import flow
- [x] **Backup management system** — `BackupService` (browser IndexedDB) + `BackupManagerDialog` for client-side save backups with retention enforcement, restore, and per-entry delete; backup restore is guarded against non-exportable saves (closes the §5.5 backup manager task as well)
- [x] Add bulk Pokémon import from .pk* files and .zip — via Bank import (#780)
- [x] Support .pk* file drag-and-drop — drop onto editor places in selected slot; drop onto Bank imports
- [x] Add Gen 3 `.wc3` Wonder Card file import — closes #811
- [ ] Support Battle Video Pokemon extraction
- [ ] Add .wc* Wonder Card import for other generations
- [ ] Implement save file conversion between formats
- [ ] Add CSV export for Pokemon data
- [ ] Support JSON export/import

### 4.3 Box Management Enhancements
**Tracks:** #352
**Tasks:**
- [ ] Add box cloning
- [ ] Implement box sorting (by species, level, shiny, etc.)
- [ ] Add box search/filter
- [ ] Create box import/export
- [ ] Add box name editing
- [ ] Implement box wallpaper selection
- [ ] Add "Jump to box" quick navigation
- [ ] Create box comparison tool
- [ ] Add empty box slots highlighting
- [ ] Implement auto-organize boxes

### 4.4 Data Visualization
**Tasks:**
- [ ] Add party/box statistics dashboard
- [ ] Create shiny collection gallery
- [ ] Add type distribution chart
- [ ] Implement IV/EV distribution graphs
- [ ] Create ribbon collection tracker
- [ ] Add completion percentage for:
  - [ ] Living Dex
  - [ ] Shiny Dex
  - [ ] Ribbon Master
  - [ ] Form Dex
- [ ] Implement achievement/milestone tracker
- [ ] Create collection value estimator (competitive viability)

### 4.5 Help & Documentation
**Tracks:** #110
**Status:** ❌ Not Implemented
**Complexity:** High
**Priority:** Medium

Three parallel tracks — wiki content authoring, in-app help links (code), and contextual onboarding/tooltips (code).

#### Track A — GitHub Wiki Documentation Site
- [ ] Scaffold all wiki pages with outline structure (Overview → Steps → Tips → See Also)
- [ ] **Getting Started** — browser support, PWA install, loading a save file
- [ ] **Box & Party Management** — visual grid, drag-and-drop, copy/paste, clone, delete, export slot
- [ ] **Pokémon Editor — Main Tab** — species, nickname, gender, level, ability, held item, shiny, PID/EC generator
- [ ] **Pokémon Editor — Stats Tab** — IVs, EVs, AVs/GVs, CP, Stat Nature, Dynamax Level, Tera Type, Is Alpha/Noble
- [ ] **Pokémon Editor — Moves Tab** — move slots, PP/PP Ups, Relearn Moves, TR flags, Move Shop, Mastered/Plus moves
- [ ] **Pokémon Editor — Met Tab** — met location, ball, origin game, Battle Version, Obedience Level, dates
- [ ] **Pokémon Editor — OT/Misc Tab** — OT/HT info, memories, Geo Locations, Country/Region, Affixed Ribbon
- [ ] **Pokémon Editor — Ribbons Tab** — ribbon categories, per-ribbon legality indicators
- [ ] **Pokémon Editor — Cosmetic Tab** — markings, height/weight, scale, origin mark, favorite, contest stats, Pokérus
- [ ] **Pokémon Editor — Legality Tab** — reading results, color coding, fix buttons, verbose report
- [ ] **Bag / Inventory** — pouch tabs, item counts, HaX mode items, sort/favorites
- [ ] **Trainer Info** — name, TID/SID, money, game version, badges
- [ ] **Pokédex** — view/edit seen/caught, bulk fill, progress bars
- [ ] **Records** — game statistics, Hall of Fame timing
- [ ] **Legality Checker** — per-check explanations, batch legality report, fix buttons
- [ ] **Advanced Search** — all filter fields, saved filters, batch Showdown export
- [ ] **Encounter Database** — filters, shiny lock, "Generate Legal Pokémon", encounter types
- [ ] **Mystery Gift Database** — search/filter, import, generate into slot
- [ ] **Showdown Export** — single and batch export
- [ ] **Settings** — theme toggle, PKHaX mode (with warning), verbose logging, Check for Updates
- [ ] **Glossary** — EC, PID, TID, SID, HT, OT, legality, shiny types, etc.
- [ ] **FAQ** — Why is my Pokémon illegal? What is HaX mode? What save files are supported?
- [ ] Add screenshots for every major feature (annotated where helpful)

#### Track B — In-App Help Links (Code)
- [ ] Create `HelpButton.razor` component in `Pkmds.Rcl/Components/` — `MudTooltip` wrapping `MudIconButton` (HelpOutline icon), `WikiUrl` parameter, opens `_blank`
- [ ] Add global Documentation button to `MainLayout.razor` AppBar (desktop: text+icon; mobile: icon-only) linking to wiki Home
- [ ] Add `<HelpButton WikiUrl="...">` to each major tab: Party/Box, Bag, Trainer Info, Pokédex, Records, Advanced Search, Encounter DB, Mystery Gift DB, Legality Report, Pokémon Edit Form

#### Track C — Contextual Onboarding & Field Tooltips (Code)
- [ ] Add `HasDismissedOnboarding` bool to `IAppState` (persisted in localStorage as `pkmds_onboarding_v1`)
- [ ] Add first-run onboarding card to `Home.razor` — shown when `!AppState.HasDismissedOnboarding`; card has Dismiss + Open Guide buttons
- [ ] Add/expand `MudTooltip` on non-obvious fields: EC, PID, Battle Version, Obedience Level, Home Tracker, Affixed Ribbon, Ground Tile, Fateful Encounter, TID/SID

---

## Priority 5: Advanced/Plugin-Level Features

### 5.1 Auto-Legality Mod (ALM)
**Status:** ⚠️ Partial (Phase 1 core engine + Legalize button + batch/box legalize + Showdown/Mystery Gift integration implemented — tracks #344, #690, #691, #693, #695, #705)
**Complexity:** Very High
**PKHeX Reference:** `C:\Code\PKHeX-Plugins\AutoLegalityMod` (ALM plugin source)
**Tasks:**
- [x] Research ALM functionality
- [x] Implement auto-generation of legal Pokémon — `ILegalizationService` / `LegalizationService` in `Pkmds.Rcl/Services/`, singleton-registered in `Program.cs`; `LegalizationResult` record models outcome + status
- [x] **Gen 3–5 PID↔IV correlation via ClassicEraRNG (Method 1)** — addresses discussion #629
- [x] **Gen 8 SwSh overworld PID/IV, Gen 8b BDSP, Gen 9 Tera raids**
- [x] Add Showdown import with auto-legality — `ShowdownImportDialog` delegates to the legalization engine (#691)
- [x] **"Legalize" button on LegalityTab** — single-PKM legalize with progress UI and `OnPokemonLegalized` callback
- [x] **"Legalize All in Box"** toolbar action — scoped to Illegal entries only, disabled when the box is empty (#693)
- [x] **Batch legalize from Legality Report tab** — cancel button, per-Pokémon timeout, skips save-file adapt-on-import when writing legalized PKMs, updates report entries in place; prefers Legal over Fishy outcomes; prioritises Illegal over Fishy when choosing candidates (#690)
- [x] **Preserve original details when legalizing an existing PKM** — OT, met data, and ribbons are retained where compatible (#705)
- [x] **Preserve Mystery Gift event properties during legalization** (#695)
- [x] **HaX retry path** — when a Showdown import fails legalization, automatically retries with HaX constraints relaxed (#859)
- [x] **Blank-PKM guard** — prevents importing empty Pokémon when legalization fails entirely (#858)
- [x] **Showdown import trash bytes seeding** — seeds expected nickname/OT trash bytes for Gen 8+/Gen 7b after legalization (#860)
- [x] **Legalization change report** — after legalizing a Pokémon, shows exactly which fields were changed (#833)
- [ ] Create legal move set generator (Phase 2 follow-ups)
- [ ] Implement legal ribbon/memory assignment (Phase 2 follow-ups)
- [x] Add smart PID/EC generation (covered by ClassicEraRNG path above; extended RNG sources remain)
- [ ] Create legal met data assignment (Phase 2 follow-ups)
- [ ] Implement form/ability validation and auto-correction (Phase 2 follow-ups)
- [ ] Undo support after legalize

### 5.2 Living Dex Builder
**Status:** ❌ Not Implemented  
**Complexity:** High  
**Tasks:**
- [ ] Design Living Dex builder UI
- [ ] Integrate with Encounter Database
- [ ] Implement auto-generation of all Pokemon for dex completion:
  - [ ] All species
  - [ ] All forms
  - [ ] All gender variants
  - [ ] Shiny variants (optional)
- [ ] Add customization options:
  - [ ] Language selection
  - [ ] OT name
  - [ ] Ball preference
  - [ ] Shiny/non-shiny
- [ ] Implement box auto-organization
- [ ] Add progress tracking
- [ ] Create backup before generation

### 5.3 Raid/RNG Tools
**Status:** ❌ Not Implemented  
**Complexity:** Very High  
**Tasks:**
- [ ] Implement RNG seed finder
- [ ] Add RNG tool for:
  - [ ] Wild encounters
  - [ ] Egg hatching
  - [ ] Raid Pokemon
  - [ ] Stationary legends
- [ ] Create seed-to-Pokemon calculator
- [ ] Add frame advancement calculator
- [ ] Implement shiny RNG predictor
- [ ] Create IV spread calculator

### 5.4 Block Data Viewer/Editor
**Status:** ❌ Not Implemented  
**Complexity:** Very High  
**Warning:** Advanced users only, risk of corruption  
**Tasks:**
- [ ] Implement raw block data viewer
- [ ] Add block editor with hex view
- [ ] Create block import/export
- [ ] Add block comparison tool
- [ ] Implement block search
- [ ] Create block documentation viewer
- [ ] Add block checksum calculator
- [ ] Implement backup before block editing

### 5.5 Save File Utilities
**Status:** ⚠️ Partial — backup manager, info viewer, repair tool, and encryption/decryption status are done; comparison/converter/splitter/merger remain (tracked in #657)
**Tracks:** #349 *(closed; superseded by #657)*, #657
**Tasks:**
- [x] **Save file backup manager** — `BackupService` + `BackupManagerDialog`: client-side IndexedDB backups with retention enforcement, restore, and per-entry delete; backup restore guarded against non-exportable saves
- [ ] Implement save file comparison tool *(#657)*
- [x] **Save file info viewer** — `SaveFileInfoDialog` shows save type, generation, game, revision, file size, header/footer presence, checksum status (with detail), encryption status, exportable + edited flags, box/party/dex storage summary
- [x] **Save file repair tool** — `SaveFileRepairDialog` exposed from the main menu
- [ ] Implement save file format converter *(#657)*
- [ ] Create save file splitter (for multi-save files) *(#657)*
- [ ] Add save file merger *(#657)*
- [x] **Save file encryption/decryption status** — surfaced in `SaveFileInfoDialog` (Format Encryption row)

### 5.6 PKHaX / Illegal Mode
**Status:** ✅ Implemented — PR #423 · closes #422
**Complexity:** Medium
**Priority:** Medium
**Tracks:** #422 *(supersedes #103, which was incorrectly closed as a duplicate of the Batch Editor #329)*
**PKHeX Reference:**
- `PKHeX.WinForms/Program.cs` — `public static bool HaX`
- `PKHeX.Core/Editing/Program/StartupUtil.cs` — `GetIsHaX()`
- `PKHeX.WinForms/Controls/PKM Editor/StatEditor.cs:45` — `HaX` property gates `CHK_HackedStats`
- `PKHeX.WinForms/Controls/SAV Editor/SAV_Inventory.cs:139,237` — item pouch HaX bypasses
- `PKHeX.Core/PKM/PlayerBag.cs:52–58` — `IsQuantitySane` → `MaxQuantityHaX` path

**Overview:** A toggleable "Illegal Mode" for ROM hackers, researchers, and advanced users who need unrestricted editing beyond what standard legality rules permit. In PKHeX this is activated via a command-line flag (`-HaX`), executable rename, or settings toggle. PKMDS surfaces it as a Settings toggle.

**Tasks:**
- [x] Add `IsHaXEnabled` bool to `IAppState` (persisted in local storage via `localStorage.setItem`)
- [x] Add toggle to Settings panel with clear warning: _"Enables unrestricted editing. May create illegal/untradable Pokémon."_
- [x] Show persistent warning chip/banner in app header while HaX is active
- [x] Display a dismissable confirmation dialog on first enable
- [x] **Hacked Stats** — unlock the six calculated-stat inputs (HP/Atk/Def/SpA/SpD/Spe) in `StatsTab` when HaX is on; direct writes to `PKM.Stat_HP`, `Stat_ATK`, `Stat_DEF`, `Stat_SPA`, `Stat_SPD`, `Stat_SPE`; revert to read-only calculated display when HaX is off
- [x] **Suppress legality overlays** — gate legality icon rendering in `PokemonSlotComponent` on `!AppState.IsHaXEnabled`
- [x] **Unrestricted item quantities** — allow quantities up to `ushort.MaxValue` (65,535) in Bag/Inventory editor when HaX is on
- [x] **Unrestricted item lists** — show full item list regardless of pouch type when HaX is on
- [x] **Unrestricted ability selection (HaX DEV mode)** — When HaX is on and format > 3, the slot-based selector is replaced by a `MudAutocomplete` over all ability IDs (`GameInfo.Strings.Ability`) plus a raw `AbilityNumber` slot picker (1/2/4), writing directly to `PKM.Ability`. Mirrors PKHeX `DEV_Ability` (StatEditor.cs:45). Gen 3 keeps the slot-based selector in both modes.
- [x] Add unit tests for HaX-gated stat editing path (`HaXModeTests.cs`, 10 tests)

### 5.7 Damage Calculator
**Status:** ❌ Not Implemented
**Complexity:** High
**Priority:** Low
**Tracks:** #443
**Note:** PKMDS-exclusive feature — PKHeX has no native damage calculator.

**Overview:** An interactive damage calculator opened as a dialog from the Pokémon editor. Pre-loads the selected Pokémon as the attacker; user configures a theoretical defender (species, level, nature, EVs/IVs), selects a move and field conditions (weather, terrain, stat stages), and sees a damage range, % of HP, and KO count. Modified attacker stats can optionally be written back to the Pokémon.

**PKHeX Reference:** None — this is PKMDS-exclusive. Reference external damage calculators (e.g., Smogon's calc) for formula accuracy.

**Tasks:**
- [ ] Create `DamagePokemon` data model in `Pkmds.Core/Calculators/`
- [ ] Create `DamageInput` record (attacker, defender, move, weather, terrain, context)
- [ ] Create `DamageResult` record (min/max damage, % HP, 16-roll table, KO count, summary)
- [ ] Implement `DamageCalculator` static class — Gen 3+ physical/special formula with modifier chain:
  - [ ] Base damage formula `((2L/5+2) × Power × Atk/Def / 50) + 2`
  - [ ] Stat stage multipliers (±6 steps, ignoring negative attacker/positive defender on crits)
  - [ ] STAB multiplier (×1.5)
  - [ ] Type effectiveness via `TypeChart` helper
  - [ ] Weather modifiers (sun/rain ×1.5/×0.5 for boosted/weakened types)
  - [ ] Terrain modifiers (electric/grassy/psychic ×1.3, misty ×0.5 on dragon)
  - [ ] Critical hit multiplier (×1.5 Gen 6+, ×2.0 Gen 4–5)
  - [ ] Burn halving (physical moves, attacker is burned)
  - [ ] Basic held item multipliers (Choice Band/Specs ×1.5, Life Orb ×1.3)
- [ ] Implement `TypeChart` helper — Gen 6+ 18-type effectiveness table
- [ ] Build 16-roll table (85%–100% of max damage) and derive KO count / summary string
- [ ] Design `DamageCalculatorDialog.razor` (MudDialog, large on desktop / full-screen on mobile):
  - [ ] Attacker panel: pre-populated from selected PKM, all stat fields editable
  - [ ] Defender panel: species picker with level, nature, EV/IV inputs, stat auto-compute
  - [ ] Move selector: attacker's 4 current moves + "Custom move…" search option
  - [ ] Field conditions: weather dropdown, terrain dropdown, stat stage spinners, burn toggle
  - [ ] Critical Hit checkbox
  - [ ] Results section: min–max range, % HP, roll table chips, KO summary
  - [ ] Role selector at open time — toggle/radio lets user choose whether the selected PKM opens as attacker or defender
  - [ ] "Swap Attacker / Defender" button — flips both panels; save target tracks the selected PKM regardless of role
  - [ ] "Save changes to Pokémon" button — label reflects current role ("Save attacker changes…" / "Save defender changes…"); only visible when the selected PKM's fields differ from the original
- [ ] Integrate calculator button into `PokemonEditForm.razor` toolbar
- [ ] Implement "Save changes to Pokémon" — write modified EVs/IVs/level back via `AppService.SavePokemon()`, targeting the selected PKM regardless of attacker/defender slot
- [ ] Write unit tests in `Pkmds.Tests/DamageCalculatorTests.cs` with known damage fixture values

**MVP Scope (Gen 3+ only):**
- Physical/special split; Gen 6+ 18-type chart; basic weather, terrain, items, and stat stages
- No Gen 1–2 mechanics, no complex ability interactions, no Z/Max/multi-hit/fixed-damage moves

**Deferred to follow-on issues:**
- Gen 1–2 mechanics (special stat, Gen 1 type chart)
- Complex abilities (Thick Fat, Levitate, Guts, Wonder Guard, Intimidate, etc.)
- Z-moves / Dynamax Max Moves
- Multi-hit and fixed-damage moves
- Screens, doubles/triples modifiers
- Tera type STAB (SV)

### 5.8 Cross-Save Pokémon Transfer
**Status:** ⚠️ Partial (phases 1–4 implemented — #710/#711/#713; move-mode multi-select + mobile layout #712 still open)
**Complexity:** High
**Priority:** Medium
**Tracks:** #14, #710, #711, #712, #713
**Note:** Browser sandboxing prevents the PKHeX model (two separate windows). PKMDS instead loads two save files within a single session.

**Overview:** Allow a second save file to be loaded into memory alongside the primary save. A transfer UI lets the user move or copy Pokémon between the two saves. The secondary save's file can be written back to disk independently.

**PKHeX Reference:** The PKHeX WinForms app supports opening a second save via `File > Open` in the `SAV_BoxViewer` context, allowing drag-and-drop between the two windows. PKMDS adapts this into a split-pane or dialog-based transfer UI within a single browser session.

**Tasks:**
- [x] Extend `IAppState` with `SaveFile? SecondaryFile` and corresponding DI plumbing — #710
- [x] Add "Open Second Save" button/menu item; loads a file into `SecondaryFile` without replacing `PrimaryFile` — #710
- [x] Build `CrossSaveTransferDialog` showing both saves' boxes side by side — #710
- [x] Implement drag-and-drop of Pokémon between saves with compatibility checks; held items returned to source bag on transfer — #711
- [x] Block transfers that PKHeX.Core would reject due to format incompatibility; surface user-friendly error — #711
- [x] Add "Save Secondary File" action to write the modified secondary save back to disk — #713
- [x] Clear `SecondaryFile` on explicit close or when primary file is replaced — #710
- [ ] Move-mode multi-select + mobile layout — #712

---

## Priority 6: Minor Features & Polishing

### 6.1 Trainer Customization Enhancements
**Status:** ⚠️ Partial (phases 1–5 implemented — #361, #870; trainer appearance/avatar editor remains — #873)
**Tracks:** #41, #361
**Tasks:**
- [ ] Add trainer appearance/avatar editor — #873
- [x] Implement trainer card customization — Gen 8 SwSh `TrainerCardDialog` (card name, card number 0–999, trainer ID, Roto Rally score)
- [x] Add money/BP/currency editors for all types — per-gen currency editors: Gen 3 (money, coins, BP), Gen 4 (money, coins, BP, Watts HGSS), Gen 5 (money, coins, BP), Gen 6 (money, coins, BP, Poké Miles), Gen 7 (money, coins, BP, Festival Coins), Gen 7b (money, coins, BP), Gen 8 SwSh (money, coins, BP, Watts, Roto Tokens), Gen 8 BDSP (money, coins, BP), Gen 8 LA (money, Poké Miles), Gen 9 (money, coins, BP, League Points)
- [x] Create playtime editor — hours/minutes/seconds numeric fields with reset button
- [x] Add language selection — shown for Gen 4+ saves; hidden for `ILangDeviantSave` saves (Gen 1–3 R/S/E/FR/LG, Stadium)
- [x] Implement game sync ID editor — Gen 5/6/7/7b with copy-to-clipboard
- [x] Game start timestamp editor — `DateTimeLocal` picker for Gen 4–8 LA; date-only for SwSh/SV; time portion correctly persisted
- [x] Hall of Fame timestamp editor — Gen 4–7
- [x] Gen 6 Trainer Card Sayings viewer — read-only display (free-form text is view-only to avoid abuse vectors)
- [x] Gen 3 Game Corner Joyful Records editor — closes #870
- [x] Gen 4 HGSS NSparkle (National Park Sparkle) flag
- [ ] Add trainer memo/notes

### 6.2 Box Viewer Enhancements
**Status:** ✅ Implemented (pop-out dialogs and swap — #352; follow-up items remain)
**Complexity:** Medium
**Tracks:** #352
**PKHeX Reference:** `SAVEditor.cs:503–528,1579–1594`, `SAV_BoxViewer.Designer.cs`, `SAV_BoxList.cs`
**Tasks:**
- [x] **`SwapBoxes(int boxA, int boxB)`** — add to `IAppService` / `AppService`; swaps all Pokémon between two boxes, triggers `RefreshAppState`
- [x] **`BoxViewerDialog`** (`Pkmds.Rcl/Components/Dialogs/BoxViewerDialog.razor[.cs]`) — single-box pop-out dialog (`SAV_BoxViewer` equivalent):
  - `[Parameter] int InitialBox` — box to show on open
  - Local `CurrentBox` for independent dialog navigation (prev/next + dropdown)
  - Renders existing `BoxGrid` component; slot clicks select Pokémon for editing in main form
  - "View All Boxes" button transitions to `BoxListDialog`
  - Responsive: `MaxWidth.Large` on `sm+`; full-screen on `xs`
  - Subscribes to `OnBoxStateChanged` to keep slots current
- [x] **`BoxListDialog`** (`Pkmds.Rcl/Components/Dialogs/BoxListDialog.razor[.cs]`) — all-boxes grid dialog (`SAV_BoxList` equivalent):
  - Renders all `SaveFile.BoxCount` boxes in `MudGrid`: `xs=12 sm=6 md=4 lg=3`
  - Each cell: box name header + `BoxGrid` + optional adjacent-box swap button (⇄)
  - Swap button calls `AppService.SwapBoxes(i, i+1)`
  - Full-screen on mobile; `MaxWidth.ExtraExtraLarge` + `FullWidth` + scrollable on desktop
  - Subscribes to both `OnAppStateChanged` and `OnBoxStateChanged`
- [x] **Add trigger buttons to `PokemonStorageComponent`** — "Pop Out Box" (`OpenInNew` icon) and "All Boxes" (`GridView` icon) buttons in the box nav bar; both open their respective dialogs via `IDialogService`
- [x] **Unit tests** — `SwapBoxes` correctness; bUnit render tests for both dialogs
- [ ] Implement box group viewer (SAV_GroupViewer) — follow-up (was #362, closed as duplicate; break out a new issue when ready)
- [ ] Add box preview on hover — follow-up
- [ ] Implement box quick-peek — follow-up

### 6.3 Mail System
**Status:** ❌ Not Implemented  
**Complexity:** Low  
**Relevant Gens:** Gen 2-4  
**Tasks:**
- [ ] Implement Mail Box editor (SAV_MailBox)
- [ ] Add mail message editing
- [ ] Support mail item attachment
- [ ] Add mail template selection
- [ ] Implement Gen 2/3/4 mail formats

### 6.4 Chatter (Chatot Cry)
**Status:** ❌ Not Implemented  
**Complexity:** Medium  
**Relevant Gens:** Gen 4-5  
**Tasks:**
- [ ] Implement Chatter editor (SAV_Chatter)
- [ ] Add audio playback of custom cries
- [ ] Support custom cry recording/import
- [ ] Add cry waveform display

### 6.5 Hall of Fame
**Status:** ⚠️ Partial (records tab has some HoF data)  
**Complexity:** Low  
**Tasks:**
- [ ] Enhance Hall of Fame editor (SAV_HallOfFame7, SAV_HallOfFame3)
- [ ] Add Hall of Fame party display
- [ ] Show Hall of Fame entry history
- [ ] Add entry editing
- [ ] Implement entry deletion
- [ ] Create Hall of Fame export

### 6.6 Settings & Preferences
**Status:** ⚠️ Partial — `AppSettingsDialog` covers UI / Trainer Defaults / Advanced tabs; settings import/export and reset remain.
**Tasks:**
- [x] Create app settings editor (SettingsEditor equivalent) — `Pkmds.Rcl/Components/Dialogs/AppSettingsDialog.razor`
- [x] Add preference for:
  - [x] Default OT name
  - [x] Default TID/SID
  - [x] Default language
  - [x] Auto-backup frequency (auto-backup toggle + max backup count)
  - [ ] Database location *(N/A in browser sandbox — saved via File System Access API per save)*
  - [ ] Export folders *(N/A in browser sandbox — see above)*
  - [x] UI preferences (theme, sprite style, HaX mode, verbose logging)
- [ ] Implement settings import/export
- [ ] Add settings reset

---

## Technical Debt & Infrastructure

### 7.1 Code Quality
**Tasks:**
- [ ] Increase unit test coverage to >80%
- [ ] Add integration tests for save file operations
- [ ] Implement E2E tests for critical workflows
- [ ] Add performance benchmarks
- [ ] Create automated UI tests (Playwright)
- [ ] Implement code coverage reporting
- [ ] Add mutation testing
- [ ] Refactor large components into smaller pieces
- [ ] Improve code documentation
- [ ] Create architecture documentation

### 7.2 Performance Optimization
**Tasks:**
- [ ] Optimize large box rendering with virtualization
- [ ] Implement lazy loading for sprites
- [ ] Add caching for computed values
- [ ] Optimize search algorithms
- [ ] Implement web worker for heavy computations
- [ ] Add progressive web app enhancements
- [ ] Optimize bundle size
- [ ] Implement code splitting
- [ ] Add performance monitoring

#### 7.2a Bag/Inventory Performance (#299)
**Status:** ⚠️ Partial (virtualization tried + reverted — #454; lazy-render attempted then removed — #511; blocked on upstream MudBlazor/MudBlazor#12799)
**Complexity:** Medium
**Priority:** High
**Tracks:** #299

The Bag tab is slow to load on large saves (SV, SwSh) because all pouches render eagerly, virtualization is off, and empty item rows are included.

**Root causes:**
- All `MudTabPanel`/`MudDataGrid` instances are initialized at once even though only one tab is visible
- `Virtualize` is disabled because `MudDataGrid`'s `DataGridVirtualizeRow` passes `SpacerElement="div"` inside `<tbody>`, which CSS collapses to 0 px and causes rows to jump on scroll (upstream MudBlazor/MudBlazor#12799); we tried enabling it via MudBlazor 9.2.0 in PR #500 but had to revert (commits `e3d76cbd` → `48ce4f15`)
- Most pouches contain many empty slots (`Index == 0`, `Count == 0`) that are still rendered
- No `CellTemplate` on the Item column — display mode falls back to raw integer rendering

**Tasks:**
- [ ] **Re-enable virtualization once MudBlazor/MudBlazor#12799 ships** — bump `MudBlazor` in `Directory.Packages.props` and set `Virtualize="true"` on the bag `MudDataGrid` (#454)
- [ ] **Lazy-render inactive pouches** — attempted in PR #511 then removed due to layout issues; revisit when a stable approach is found
- [ ] **Filter empty slots by default** — bind `MudDataGrid.Items` to `pouch.Items.Where(i => i.Index != 0)` when `ShowEmptySlots == false`; add a per-tab "Show empty slots" toggle (default off)
- [ ] **Add `CellTemplate` to Item column** — render `@ItemList[context.Item.Index]` as `MudText` in display mode so the grid has a lightweight non-edit render path
- [ ] *(stretch)* **Evaluate replacing `MudDataGrid` with `<Virtualize>` + lightweight row component** — only if profiling after the above steps shows `MudDataGrid` itself as the remaining bottleneck

**Files:**
- `Pkmds.Rcl/Components/MainTabPages/BagTab.razor`
- `Pkmds.Rcl/Components/MainTabPages/BagTab.razor.cs`
- `Pkmds.Rcl/wwwroot/css/app.css`

**Acceptance criteria:** Tab loads noticeably faster on large saves; pouch switching is instant after first activation; all existing sort/save/delete/HaX behaviour unchanged; build and tests pass.

### 7.3 Accessibility
**Tasks:**
- [ ] Add ARIA labels to all interactive elements
- [ ] Implement keyboard navigation throughout app
- [ ] Add screen reader support
- [ ] Improve color contrast ratios
- [ ] Add focus indicators
- [ ] Implement skip navigation links
- [ ] Add alt text to all images
- [ ] Create high contrast theme
- [ ] Add text size controls
- [ ] Implement reduced motion mode

### 7.4 Error Handling & Validation
**Status:** ⚠️ Partial — dialogs, GitHub-issue bug reports, browser-side logging, legality popovers, and backup-based recovery are shipped; comprehensive input validation, proactive save-checksum scanning, and opt-in telemetry remain. Tracks #360.

**Tasks:**
- [ ] Improve error messages _(ongoing — many incremental fixes; #485, #528, #600, #634, #679, #719, #751)_
- [x] Add validation error tooltips _(per-field legality indicators #412; `LegalityPopover` info-button popovers #785/#773; Showdown-import legality #735; moves/items/abilities popovers #579)_
- [ ] Implement comprehensive input validation _(legality covers Pokémon-side; free-text fields like trainer name/IDs not yet systematically validated)_
- [ ] Add save file corruption detection _(load-time and export-time guards exist via #679; no proactive block-checksum scanner with user-facing report)_
- [x] Create error recovery mechanisms _(`BackupService` + auto-backup #655; `BackupManagerDialog` for restore)_
- [ ] Implement error logging/telemetry (opt-in) _(Serilog → browser console with runtime verbosity toggle #228 done; Sentry was added #514/#521 then removed when trial ended #615/#616 — opt-in telemetry replacement still TBD)_
- [x] Add user-friendly error dialogs _(`ErrorBoundary` in `App.razor` → `BugReportDialog` with captured exception)_
- [x] Create error report export _(`BugReportService` posts to Azure Function that opens GitHub issues #514/#577/#619; attaches save bytes #521)_

### 7.5 Internationalization (i18n)
**Tasks:**
- [ ] Extract all UI strings to resource files
- [ ] Implement language selection
- [ ] Add translations for:
  - [ ] English (en-US) - default
  - [ ] Spanish (es-ES)
  - [ ] French (fr-FR)
  - [ ] German (de-DE)
  - [ ] Italian (it-IT)
  - [ ] Japanese (ja-JP)
  - [ ] Korean (ko-KR)
  - [ ] Chinese Simplified (zh-CN)
  - [ ] Chinese Traditional (zh-TW)
- [ ] Add date/time/number localization
- [ ] Create translation contribution guide

---

## Dependencies & Risks

### Critical Dependencies
- **PKHeX.Core**: Must stay updated to latest version
- **MudBlazor**: UI component library
- **Blazor WebAssembly**: Framework updates
- **.NET SDK**: Currently on .NET 10

### Known Risks
1. **PKHeX.Core API Changes**: Breaking changes could require significant refactoring
2. **Browser Limitations**: Some features (like direct file system access) may not work in all browsers
3. **Performance**: Large save files with many Pokemon may cause performance issues
4. **Legal/Cheating Concerns**: Must maintain legality checker to prevent obvious cheating
5. **Save Corruption**: Advanced editing features (event flags, block data) risk corrupting saves

### Mitigation Strategies
- Regular testing with each PKHeX.Core update
- Comprehensive backup system before any edits
- Clear warnings on risky features
- Robust validation and error handling
- Browser compatibility testing
- Performance benchmarking and optimization

---

## Roadmap Phases

### Phase 1: Foundation (Q1 2026)
**Goal:** Core editing parity
- 🎯 Complete Pokemon editor (ribbons, memories, contest stats, catch rate)
- 🎯 Implement legality checker
- 🎯 Add batch editor
- 🎯 Create database/bank system

### Phase 2: Generation-Specific Features (Q2-Q3 2026)
**Goal:** Support unique features for each generation
- 🎯 Implement Gen 9 specific features (raids, fashion, sandwiches)
- 🎯 Add Gen 8/8.5 features (raids, underground, poffins, research)
- 🎯 Complete Gen 7 features (festival plaza, pelago, zygarde)
- 🎯 Add Gen 6 features (secret bases, amie, super training)

### Phase 3: Advanced Tools (Q4 2026)
**Goal:** Power user features
- 🎯 Implement auto-legality mod
- 🎯 Add living dex builder
- 🎯 Create RNG tools
- 🎯 Add event flags editor
- 🎯 Implement block data editor

### Phase 4: Quality & Polish (Q1 2027)
**Goal:** Production-ready, user-friendly
- 🎯 Complete UI/UX improvements
- 🎯 Add comprehensive help/documentation
- 🎯 Implement accessibility features
- 🎯 Add internationalization
- 🎯 Optimize performance
- 🎯 Increase test coverage

### Phase 5: Maintenance & Beyond (Q2 2027+)
**Goal:** Stay current with Pokemon games and PKHeX
- ♻️ Support new Pokemon games as released
- ♻️ Update PKHeX.Core regularly
- ♻️ Add community-requested features
- ♻️ Fix bugs and improve stability
- ♻️ Maintain documentation

---

## Success Metrics

### Completion Tracking
- **Total Features Identified:** ~250+ individual tasks
- **Currently Implemented:** ~55 tasks (22%)
- **Target for Phase 1:** 80 tasks (32%)
- **Target for Full Parity:** 100%

### Quality Metrics
- **Test Coverage:** Current <50%, Target >80%
- **Performance:** Page load <3s, interactions <100ms
- **Accessibility:** WCAG 2.1 AA compliance
- **Browser Support:** Chrome, Firefox, Edge, Safari (latest 2 versions)
- **Mobile Support:** Responsive design for tablets and phones

### User Metrics
- **Active Users:** Track via analytics (privacy-friendly)
- **Feature Usage:** Most/least used features
- **Error Rates:** Track validation errors, crashes
- **Satisfaction:** User surveys, GitHub issues/feedback

---

## Contributing

This roadmap is a living document. Community contributions are welcome!

### How to Contribute
1. **Pick a task** from this roadmap that interests you
2. **Create an issue** on GitHub describing your implementation plan
3. **Submit a PR** with your implementation
4. **Add tests** for your feature
5. **Update documentation** as needed

### Priority Areas for Contributors
- Legality checker integration
- Ribbon editor UI
- Memory editor implementation
- Generation-specific editors
- Unit tests for existing features
- Documentation and tutorials

---

## Notes

- This roadmap focuses on **web-based** PKMDS implementation
- Some PKHeX features may not be feasible in a web environment
- Plugin support (like PKHeX has) is not planned initially but may be considered later
- Security and save file safety are paramount - all risky features will have warnings
- The goal is feature parity, not UI parity - PKMDS will maintain its own UI/UX design

---

**For questions, suggestions, or to discuss the roadmap, please open an issue on GitHub or contact the maintainer.**

**Last Updated:** 2026-04-11
**Next Review:** 2026-05-09
<!-- Legality Checker (§1.2): fix buttons (#401) + batch report (#402) done 2026-02-27; comprehensive unit tests still pending -->
<!-- Legality Checker (§1.2): per-field inline indicators (#411) implemented 2026-02-27; covers Moves/Stats/Main/Met/OT-Misc/Ribbons tabs -->
<!-- PKHeX bug filed 2026-02-27: SAV1.IsVirtualConsole filename heuristic causes false cart-era detection for renamed VC saves → kwsch/PKHeX#4734 -->
<!-- PKHaX / Illegal Mode (§5.6): added 2026-02-28; issue #422 created; supersedes #103 (incorrectly closed as duplicate of Batch Editor #329) -->
