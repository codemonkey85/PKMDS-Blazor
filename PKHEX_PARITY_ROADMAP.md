# PKHeX Feature Parity Roadmap

This roadmap outlines the path to achieving 100% feature parity with PKHeX. Tasks are broken down into actionable items organized by feature category and priority.

**Last Updated:** 2026-02-28

---

## Current State Summary

### ✅ Already Implemented in PKMDS
- **Pokemon Editor** - Full individual Pokemon editing (species, nickname, gender, level, experience, abilities, held items)
- **Stats Editing** - IVs, EVs, AVs (LGPE), GVs (LA), base stats, calculated battle stats, stats chart, CP (LGPE), Stat Nature (Gen 8+), Dynamax Level, Can Gigantamax, Tera Type Original/Override (SV), Is Alpha (LA/ZA), Is Noble (LA)
- **Moves Editing** - 4-move slots with search, type indicators, PP/PP Ups editing
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
- **OT/Misc Data** - Original trainer info, TID/SID (16-bit and 6-digit), handling trainer name/gender/language (Gen 8+), memory editing (Gen 6+), affection/friendship, Geo Locations (Gen 6–7), Home Tracker (Gen 8+) *(Country/Sub-Region/Console Region and Affixed Ribbon not yet implemented — see §1.1)*
- **Cosmetic Features** - Markings, height/weight scalars, scale rating, origin mark display (Gen 6+), Favorite toggle (Gen 7b+)
- **Pokerus** - Infection status display/editing
- **Hidden Power** - Type selection
- **Showdown Export** - Export Pokemon to Showdown format
- **Multi-Save Support** - Load/save multiple save files
- **PWA Support** - Offline functionality, installable web app
- **Legality Checker** - Full `LegalityAnalysis` integration: per-check detail view, color-coded results, full verbose report, slot-level valid/warn icon overlays, one-click fix buttons (ball, met location, moves, TechRecord), and a batch "Legality Report" tab that sweeps all party and box Pokémon with a sortable/filterable table, aggregate Legal/Fishy/Illegal counts, and jump-to-slot navigation

---

## Priority 1: Critical Missing Features

### 1.1 Pokemon Editor Enhancements

#### Ribbon Editor
**Status:** ⚠️ Partial (ribbon legality checking and search/filter not yet implemented)  
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
**Status:** ⚠️ Partial (basic form support exists)  
**Complexity:** Medium  
**Tasks:**
- [ ] Enhance form selection UI with visual previews
- [ ] Add Alcremie decoration editor (Gen 8)
- [ ] Add Vivillon pattern editor
- [ ] Add Furfrou trim editor
- [ ] Add Pumpkaboo/Gourgeist size editor
- [ ] Add Minior core color editor
- [ ] Support all Pokemon with form variations
- [ ] Validate form legality based on origin/capture method

#### Ability Slot Editor
**Status:** ⚠️ Partial (ability display exists; unrestricted slot selection not yet supported)
**Complexity:** Low
**Tracks:** #176
**Tasks:**
- [ ] Allow freely selecting any ability slot (Ability 1 / Ability 2 / Hidden Ability) regardless of species legality
- [ ] Display all three slots even when a species lacks a Hidden Ability (show as "None")
- [ ] Let legality checker report illegal ability slots rather than preventing selection

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
**Status:** ❌ Not Implemented (generation-specific)
**Complexity:** Low
**Tracks:** #417
**PKHeX Reference:** `PKMEditor.cs` OT/Misc tab
**Note:** Handler Language (`IHandlerLanguage`), Home Tracker (`IHomeTrack`), Handling Trainer name/gender, memories, Geo Locations (`IGeoTrack`), and TID/SID format handling are all ✅ already implemented in `OtMiscTab.razor`.
**Tasks:**
- [ ] **Country / Sub-Region / Console Region** (`IRegionOrigin`: `Country`, `Region`, `ConsoleRegion`, Gen 6–7 only) — the Pokémon's native 3DS geographic origin; three separate dropdowns; distinct from the five IGeoTrack visited-country slots which are already implemented
- [ ] **Affixed Ribbon / Mark** (`PKM.AffixedRibbon`, Gen 8+) — selects which ribbon or mark is displayed on the Pokémon's summary screen; stored as a `RibbonIndex` enum value

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
**Status:** ❌ Not Implemented
**Complexity:** Low
**Tracks:** #419
**PKHeX Reference:** `PKMEditor.cs` Cosmetic tab + generation-specific controls
**Note:** Stat Nature, Tera Types, Is Alpha, Is Noble, Can Gigantamax, Dynamax Level, AVs, GVs, CP, and N's Sparkle (PK5) are all ✅ already implemented. The items below are the remaining gaps.
**Tasks:**
- [ ] **Gen 4 HGSS National Park Sparkle** (`PK4.NSparkle`) — boolean flag for Bug-Catching Contest winner; HG/SS only *(N's Sparkle for PK5 is already implemented; PK4's distinct use of the same property is not)*
- [ ] **Gen 4 HGSS Shiny Leaves** (`PK4.ShinyLeaf`) — 6-part panel: 5 leaf type checkboxes + crown flag; HG/SS only
- [ ] **Gen 4 HGSS Walking Mood** (`G4PKM.WalkingMood`) — walking partner mood value; HG/SS only
- [ ] **Gen 5 B2W2 PokéStar Fame** (`PK5.PokeStarFame`) — PokéStar Studios fame value; B2W2 only
- [ ] **Gen 7b LGPE Spirit** (`PB7.Spirit`) — Go Park spirit value; LGPE only
- [ ] **Gen 7b LGPE Mood** (`PB7.Mood`) — partner Pokémon mood; LGPE only
- [ ] **Gen 7b LGPE Received Date/Time** (`PB7.ReceivedYear/Month/Day/Hour/Minute/Second`) — full timestamp of when the Pokémon was received from GO Park; LGPE only
- [ ] **Gen 3 Colosseum/XD Shadow fields** (`IShadowCapture`: `ShadowID`, `Purification`, `IsShadow`) — Shadow Pokémon identification and purification counter; Colo/XD only

#### Extra Bytes Editor
**Status:** ❌ Not Implemented
**Complexity:** Low
**Tracks:** #420
**PKHeX Reference:** `CB_ExtraBytes` + `TB_ExtraByte` in OT/Misc tab
**Tasks:**
- [ ] Add raw byte editor: offset selector (hex) + value field (0–255) for any byte in the Pokémon's data that is not exposed by a named field; useful for accessing undocumented generation-specific bytes

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
- [x] Show legality warnings on Pokémon slot display (valid/warn icon overlay)
- [x] Add inline per-field legality indicators throughout Pokémon editor tabs (#411)
  - [x] Reusable `LegalityIndicator` component (MudTooltip + severity icon, renders nothing when valid)
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
**Status:** ❌ Not Implemented  
**Complexity:** Very High  
**Priority:** High  
**Tasks:**
- [ ] Design Batch Editor UI with script input
- [ ] Implement batch editor scripting engine:
  - [ ] Property filtering (`.Property=Value`)
  - [ ] Property setting (`=Property=Value`)
  - [ ] Comparison operators (`>`, `<`, `>=`, `<=`, `!=`)
  - [ ] Logical operators (`&`, `|`)
  - [ ] Special filters (`.IsShiny`, `.IsEgg`, etc.)
- [ ] Add batch editor presets/templates
- [ ] Implement dry-run mode (preview changes)
- [ ] Add undo functionality
- [ ] Support filtering by:
  - [ ] Species/forms
  - [ ] Levels
  - [ ] OT/TID
  - [ ] Moves
  - [ ] Ribbons
  - [ ] Stats
  - [ ] Met data
- [ ] Create batch editor tutorial/documentation
- [ ] Add example scripts library
- [ ] Implement batch export/import
- [ ] Create unit tests

---

## Priority 2: Important Secondary Features

### 2.1 Database/Bank System
**Status:** ⚠️ Partial (Mystery Gift DB exists)  
**Complexity:** Very High  
**Tasks:**
- [ ] Design Pokemon Database/Bank UI
- [ ] Implement personal Pokemon database storage (IndexedDB)
- [ ] Add database import/export functionality
- [ ] Create database search with advanced filters:
  - [ ] Species/forms
  - [ ] Moves
  - [ ] Abilities
  - [ ] Ribbons
  - [ ] Stats/IVs
  - [ ] Shiny status
  - [ ] Ball type
  - [ ] OT/TID
  - [ ] Generation/origin
- [ ] Implement batch operations on database:
  - [ ] Mass import from save
  - [ ] Mass export to save
  - [ ] Batch delete
  - [ ] Batch tag/organize
- [ ] Add folder/tag organization system
- [ ] Support drag-and-drop from database to save
- [ ] Implement database backup/restore
- [ ] Create database statistics view
- [ ] Add duplicate detection
- [ ] Support multiple database profiles

### 2.2 Encounter Database
**Status:** ❌ Not Implemented  
**Complexity:** High  
**Tasks:**
- [ ] Design Encounter Database UI
- [ ] Integrate PKHeX.Core encounter data
- [ ] Implement encounter search/filter by:
  - [ ] Species
  - [ ] Game version
  - [ ] Location
  - [ ] Level range
  - [ ] Encounter type (grass, surf, etc.)
  - [ ] Shiny locked status
- [ ] Add "Generate Legal Pokemon" from encounter
- [ ] Display encounter details (location, level, moves)
- [ ] Support Mystery Gift encounter generation
- [ ] Add Living Dex builder using encounter database
- [ ] Create unit tests

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
**Status:** ⚠️ Partial (basic search exists in some areas)  
**Complexity:** Medium  
**Tasks:**
- [ ] Create Advanced Search UI component
- [ ] Implement search across all boxes:
  - [ ] Species/forms
  - [ ] Shiny status
  - [ ] IVs (exact/range)
  - [ ] EVs
  - [ ] Nature
  - [ ] Ability
  - [ ] Moves (any/all)
  - [ ] Hidden Power type
  - [ ] Gender
  - [ ] Ball type
  - [ ] Level range
  - [ ] OT name/TID
  - [ ] Ribbons
  - [ ] Marks (Gen 8+)
  - [ ] Origin game
  - [ ] Egg/not egg
  - [ ] Legal/illegal status
- [ ] Add search result highlighting
- [ ] Implement saved search filters
- [ ] Support batch operations on search results
- [ ] Add search export functionality

### 2.5 Pokédex Editor
**Tracks:** #414
**Status:** ⚠️ Partial (Gen 1–3 seen/caught counts and bulk fill implemented; all per-generation advanced fields absent)
**Complexity:** High
**PKHeX Reference:** `SAV_SimplePokedex.cs`, `SAV_Pokedex4.cs`, `SAV_Pokedex5.cs`, `SAV_PokedexXY.cs`, `SAV_PokedexORAS.cs`, `SAV_PokedexSM.cs`, `SAV_PokedexGG.cs`, `SAV_PokedexSWSH.cs`, `SAV_PokedexBDSP.cs`, `SAV_PokedexLA.cs`, `SAV_PokedexSV.cs`, `SAV_PokedexSVKitakami.cs`, `SAV_Pokedex9a.cs`
**Core API:** `ZukanBase` / `Zukan4` / `Zukan5` / `Zukan6` / `Zukan7` / `Zukan7b` / `Zukan8` / `Zukan8b` / `PokedexSave8a` / `Zukan9Paldea` / `Zukan9Kitakami` / `Zukan9a` in `PKHeX.Core/Saves/Substructures/PokeDex/`

**Common API (all gens):**
- `GetSeen(ushort species)` / `SetSeen(...)` / `GetCaught(ushort species)` / `SetCaught(...)`
- `SeenAll(bool shinyToo)` / `CaughtAll(bool shinyToo)` / `CompleteDex(bool shinyToo)`
- `SeenNone()` / `CaughtNone()` / `ClearDexEntryAll(ushort species)`
- `SetDex(PKM pk)` — auto-update entry from a Pokémon object

#### Gen 1–3 (SAV1 / SAV2 / SAV3)
**Status:** ✅ Implemented (simple seen/caught bitflags)
- [x] Seen flag per species
- [x] Caught flag per species
- [x] Bulk fill / clear
- [ ] Per-species edit UI (currently only aggregate counts shown)

#### Gen 4 (Zukan4) — Diamond / Pearl / Platinum / HeartGold / SoulSilver
**Status:** ❌ Not Implemented
- [ ] Seen flag (basic)
- [ ] Caught flag
- [ ] Gender-seen tracking: male-first and female-first regions (2 separate bitfields)
- [ ] Form tracking (species-specific; varies by game):
  - Unown ×28, Deoxys ×4, Shellos/Gastrodon ×2
  - Rotom ×6, Shaymin ×2, Giratina ×2, Pichu ×3 (HG/SS only)
- [ ] Spinda: store first-seen Spinda's PID (32-bit, offset `0x0104`)
- [ ] Language flags (HG/SS only — DP/Pt lack language data)
- [ ] Per-species edit UI

#### Gen 5 (Zukan5BW / Zukan5B2W2) — Black / White / Black 2 / White 2
**Status:** ❌ Not Implemented
- [ ] 4-region gender×shiny seen tracking (male, female, male-shiny, female-shiny)
- [ ] Display variant selection per species (which gender/shiny combo shows in Pokédex)
- [ ] Form tracking (Unown ×28, Castform ×4; B2W2 adds more species)
- [ ] Language flags (7 languages: JPN, ENG, FRE, ITA, GER, SPA, KOR)
- [ ] Spinda PID storage
- [ ] National Dex unlock flag and mode tracking
- [ ] Per-species edit UI

#### Gen 6 XY (Zukan6XY) — X / Y
**Status:** ❌ Not Implemented
- [ ] Seen / caught flags
- [ ] 4-region gender×shiny seen tracking
- [ ] Per-form seen flags (form bits alongside species bits)
- [ ] Display form, display gender, display shiny selection per species
- [ ] Language flags (7 languages; slot 6 unused/skipped)
- [ ] "Foreign" flag per species (tracks Pokémon migrated from Gen 5)
- [ ] Spinda: store first-seen Spinda's encryption constant
- [ ] National Dex unlock / mode flags
- [ ] Per-species edit UI

#### Gen 6 ORAS (Zukan6AO) — Omega Ruby / Alpha Sapphire
**Status:** ❌ Not Implemented
- [ ] All XY features above
- [ ] Encounter count per species (`u16`, running total of wild encounters)
- [ ] Obtained count per species (`u16`, running total of catches)
- [ ] Per-species edit UI

#### Gen 7 SM/USUM (Zukan7) — Sun / Moon / Ultra Sun / Ultra Moon
**Status:** ❌ Not Implemented
- [ ] Seen / caught flags
- [ ] 4-region gender×shiny seen tracking
- [ ] Per-form seen flags
- [ ] Display form / display gender / display shiny selection per species
- [ ] Language flags (9 languages: adds Simplified Chinese and Traditional Chinese)
- [ ] Spinda ×4 storage (separate first-seen per gender×shiny combination)
- [ ] Current-viewed-dex tracking (Alola regional vs National)
- [ ] Per-species edit UI

#### Gen 7b LGPE (Zukan7b) — Let's Go Pikachu / Eevee
**Status:** ❌ Not Implemented
- [ ] Seen / caught flags for the limited (153-species) Let's Go Pokédex
- [ ] Per-species edit UI

#### Gen 8 SWSH (Zukan8) — Sword / Shield
**Status:** ❌ Not Implemented
- [ ] **3 independent regional dex blocks** (each a separate `Zukan8` instance):
  - Galar (400 species)
  - Isle of Armor DLC (211 species)
  - Crown Tundra DLC (210 species)
- [ ] Per-form seen tracking (up to 63 forms as bits in a `u64` field)
- [ ] Gigantamax seen / caught flag (bit 63 of the form `u64`)
- [ ] Display options per species: form ID, gender, shiny, Gigantamax/Dynamax preference
- [ ] Language flags (9 languages)
- [ ] Battled count per species (`u32`)
- [ ] Per-species edit UI

#### Gen 8 BDSP (Zukan8b) — Brilliant Diamond / Shining Pearl
**Status:** ❌ Not Implemented
- [ ] `ZukanState8b` state per species: `None` → `HeardOf` → `Seen` → `Captured`
- [ ] Gender × shiny seen flags (4 separate `u32` arrays: male, female, male-shiny, female-shiny)
- [ ] Form tracking per species (Unown ×28, Castform ×4, Deoxys ×4, Rotom ×6, Giratina ×2, Shaymin ×2, Arceus ×18)
- [ ] Regional (Sinnoh) dex obtained flag vs National Dex obtained flag
- [ ] Language flags (9 languages)
- [ ] Per-species edit UI

#### Gen 8 LA (PokedexSave8a) — Legends: Arceus
**Status:** ❌ Not Implemented
**Note:** Completely different architecture — no simple seen/caught flags; tracked via per-task research counters. See also §3.3.
- [ ] Research task progress (22+ task types per species):
  - Catch, Defeat, Evolve, Use Move (×4 move slots), Defeat With Move Type
  - Catch Alpha, Catch Large, Catch Small, Catch Heavy, Catch Light
  - Catch At Time Of Day, Catch Sleeping, Catch In Air, Catch Not Spotted
  - Give Food, Stun With Items, Scare With Scatter Bang, Lure With Pokéshi Doll
  - Use Strong Style Move, Use Agile Style Move
  - Leap From Trees, Leap From Leaves, Leap From Snow, Leap From Ore, Leap From Tussocks
- [ ] Research rate % (0–100) per species
- [ ] Perfect research (100%) completion flag per species
- [ ] Display selection: form, gender, shiny, alpha per species
- [ ] 5 local-area dex completion flags (Hisui + Local 1–5)
- [ ] Per-species research edit UI (see `SAV_PokedexResearchEditorLA` for PKHeX reference)

#### Gen 9 SV (Zukan9Paldea / Zukan9Kitakami) — Scarlet / Violet
**Status:** ❌ Not Implemented
**Note:** Simple stubs existed in roadmap as "Advanced Pokédex (Kitakami, etc.)"; consolidated here.
- [ ] 3-tier DLC dex system (separate `Zukan9` instances per save revision):
  - Paldea (base game)
  - Kitakami (DLC 1 — The Teal Mask)
  - Blueberry (DLC 2 — The Indigo Disk)
- [ ] State system per species: Unknown → Known (adjacent species) → Seen → Caught
- [ ] Per-form seen and caught flags
- [ ] Gender seen flags, shiny seen flags, shiny caught flags
- [ ] Display options: form, gender, shiny, "New" flag
- [ ] Language flags (9 languages)
- [ ] Per-species edit UI

#### Gen 9 ZA (Zukan9a) — Legends: Z-A
**Status:** ❌ Not Implemented
- [ ] Per-species state tracking
- [ ] Mega / Mega X / Mega Y form seen flags
- [ ] Alpha form tracking
- [ ] Language flags (9 languages)
- [ ] Per-species edit UI

#### Cross-generation bulk operations
- [x] Fill all seen — Gen 1–3
- [x] Fill all caught — Gen 1–3
- [ ] Fill all seen with gender/shiny variants — Gen 4+
- [ ] Fill all caught with all form variants — Gen 4+
- [ ] Set all language flags — Gen 4+
- [ ] Clear all entries (full reset)
- [ ] Complete a single species entirely (all forms, genders, shinies, languages)
- [ ] Complete all research tasks at 100% — Gen 8 LA only

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
- [ ] Join Avenue editor
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
- [ ] Pokéathlon stats
- [ ] Battle Frontier progress
- [ ] Seal/Ball Capsule editor
- [ ] Villa furniture (Platinum)
- [ ] GTS deposits
- [ ] Battle Video editor

### 3.8 Generation 3 (Ruby/Sapphire/Emerald, FR/LG, Colosseum/XD)
**Tasks:**
- [ ] **WC3 Wonder Card import** (`IGen3Wonder`: write `WonderCard3` from `.wc3` file to save's wonder card slot; Emerald and FR/LG only) — see #423
- [ ] Secret Base editor (SAV_SecretBase3)
  - [ ] Location
  - [ ] Decorations
  - [ ] Registry
- [ ] Berry Master/Berry trees
- [ ] Roamer Pokemon editor (SAV_Roamer3)
- [ ] RTC (Real Time Clock) editor (SAV_RTC3)
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
- [ ] Add keyboard shortcuts for common operations
- [ ] Implement undo/redo functionality
- [ ] Add theme customization (dark/light modes already exist, add more)
- [ ] Create customizable hotkeys
- [ ] Add tooltip help system
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
- [ ] Add bulk Pokemon import from files
- [ ] Support .pk* file drag-and-drop
- [ ] Add team import/export (multiple Pokemon at once)
- [ ] Support Battle Video Pokemon extraction
- [ ] Add .wc* (Wonder Card) file import
- [ ] Implement save file conversion between formats
- [ ] Add CSV export for Pokemon data
- [ ] Support JSON export/import
- [ ] Add Showdown team import (already have export)
- [ ] Create backup management system

### 4.3 Box Management Enhancements
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
**Tasks:**
- [ ] Add in-app tutorials
- [ ] Create feature tooltips
- [ ] Implement contextual help
- [ ] Add FAQ section
- [ ] Create video tutorial links
- [ ] Add quick start guide
- [ ] Implement onboarding for new users
- [ ] Create generation-specific guides
- [ ] Add legality checker explanations
- [ ] Create glossary of terms

---

## Priority 5: Advanced/Plugin-Level Features

### 5.1 Auto-Legality Mod (ALM)
**Status:** ❌ Not Implemented  
**Complexity:** Very High  
**Tasks:**
- [ ] Research ALM functionality
- [ ] Implement auto-generation of legal Pokemon
- [ ] Add showdown import with auto-legality
- [ ] Create legal move set generator
- [ ] Implement legal ribbon/memory assignment
- [ ] Add smart PID/EC generation
- [ ] Create legal met data assignment
- [ ] Implement form/ability validation and auto-correction

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
**Tasks:**
- [ ] Add save file backup manager with auto-backup
- [ ] Implement save file comparison tool
- [ ] Create save file info viewer (metadata, size, checksums)
- [ ] Add save file repair tool
- [ ] Implement save file format converter
- [ ] Create save file splitter (for multi-save files)
- [ ] Add save file merger
- [ ] Implement save file encryption/decryption status

### 5.6 PKHaX / Illegal Mode
**Status:** ❌ Not Implemented
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
- [ ] Add `IsHaXEnabled` bool to `IAppState` (persisted in local storage)
- [ ] Add toggle to Settings panel with clear warning: _"Enables unrestricted editing. May create illegal/untradable Pokémon."_
- [ ] Show persistent warning chip/banner in app header while HaX is active
- [ ] Display a dismissable confirmation dialog on first enable
- [ ] **Hacked Stats** — unlock the six calculated-stat inputs (HP/Atk/Def/SpA/SpD/Spe) in `StatsTab` when HaX is on; direct writes to `PKM.Stat_HP`, `Stat_ATK`, `Stat_DEF`, `Stat_SPA`, `Stat_SPD`, `Stat_SPE`; revert to read-only calculated display when HaX is off
- [ ] **Suppress legality overlays** — gate legality icon rendering in `PokemonSlotComponent` on `!AppState.IsHaXEnabled`
- [ ] **Unrestricted item quantities** — allow quantities up to `ushort.MaxValue` (65,535) in Bag/Inventory editor; Gen 1–2 bags cap at `byte.MaxValue` (255)
- [ ] **Unrestricted item lists** — show full item list regardless of pouch type when HaX is on
- [ ] **Unrestricted ability selection (Gen 3 only)** — Gen 3 ability dropdown uses `GetAbilityList(PersonalInfo)` and is restricted to the species' legal abilities; in HaX mode it should show all abilities (matching the `DEV_Ability` behaviour PKHeX provides in HaX mode). **Gen 4+ is already unrestricted** — `MudAutocomplete` in `MainTab.razor` searches `GameInfo.FilteredSources.Abilities` (all abilities), which already matches PKHaX behaviour regardless of mode; no Gen 4+ change needed. *Note: ability slot selection (Ability 1 / Ability 2 / Hidden Ability) is a separate concern tracked in §1.1 / #176 and is not HaX-specific.*
- [ ] Add unit tests for HaX-gated stat editing path

---

## Priority 6: Minor Features & Polishing

### 6.1 Trainer Customization Enhancements
**Tracks:** #41
**Tasks:**
- [ ] Add trainer appearance/avatar editor
- [ ] Implement trainer card customization
- [ ] Add money/BP/currency editors for all types
- [ ] Create playtime editor
- [ ] Add language selection
- [ ] Implement game sync ID editor
- [ ] Add trainer memo/notes

### 6.2 Box Viewer Enhancements
**Tasks:**
- [ ] Add multi-save box viewer (SAV_BoxViewer)
- [ ] Implement box group viewer (SAV_GroupViewer)
- [ ] Create box list view (SAV_BoxList)
- [ ] Add box preview on hover
- [ ] Implement box quick-peek

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
**Tasks:**
- [ ] Create app settings editor (SettingsEditor equivalent)
- [ ] Add preference for:
  - [ ] Default OT name
  - [ ] Default TID/SID
  - [ ] Default language
  - [ ] Auto-backup frequency
  - [ ] Database location
  - [ ] Export folders
  - [ ] UI preferences
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
**Tasks:**
- [ ] Improve error messages
- [ ] Add validation error tooltips
- [ ] Implement comprehensive input validation
- [ ] Add save file corruption detection
- [ ] Create error recovery mechanisms
- [ ] Implement error logging/telemetry (opt-in)
- [ ] Add user-friendly error dialogs
- [ ] Create error report export

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

**Last Updated:** 2026-02-28
**Next Review:** 2026-03-27
<!-- Legality Checker (§1.2): fix buttons (#401) + batch report (#402) done 2026-02-27; comprehensive unit tests still pending -->
<!-- Legality Checker (§1.2): per-field inline indicators (#411) implemented 2026-02-27; covers Moves/Stats/Main/Met/OT-Misc/Ribbons tabs -->
<!-- PKHeX bug filed 2026-02-27: SAV1.IsVirtualConsole filename heuristic causes false cart-era detection for renamed VC saves → kwsch/PKHeX#4734 -->
<!-- PKHaX / Illegal Mode (§5.6): added 2026-02-28; issue #422 created; supersedes #103 (incorrectly closed as duplicate of Batch Editor #329) -->
