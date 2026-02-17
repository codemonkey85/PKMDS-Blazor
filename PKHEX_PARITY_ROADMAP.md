# PKHeX Feature Parity Roadmap

This roadmap outlines the path to achieving 100% feature parity with PKHeX. Tasks are broken down into actionable items organized by feature category and priority.

**Last Updated:** 2026-02-17

---

## Current State Summary

### ✅ Already Implemented in PKMDS
- **Pokemon Editor** - Full individual Pokemon editing (species, nickname, gender, level, experience, abilities, held items)
- **Stats Editing** - IVs, EVs, base stats, calculated battle stats, stats chart visualization
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
- **Met Data** - Met location, ball, level, origin game, generation-specific fields
- **OT/Misc Data** - Original trainer info, generation-specific format support
- **Cosmetic Features** - Markings, height/weight scalars, scale rating
- **Pokerus** - Infection status display/editing
- **Hidden Power** - Type selection
- **Showdown Export** - Export Pokemon to Showdown format
- **Multi-Save Support** - Load/save multiple save files
- **PWA Support** - Offline functionality, installable web app

---

## Priority 1: Critical Missing Features

### 1.1 Pokemon Editor Enhancements

#### Ribbon Editor
**Status:** ❌ Not Implemented  
**Complexity:** High  
**Tasks:**
- [ ] Design Ribbon UI component with tabs for different ribbon categories
- [ ] Implement ribbon data binding to PKHeX.Core ribbon properties
- [ ] Add support for all ribbon types:
  - [ ] Contest ribbons (Cool, Beauty, Cute, Smart, Tough, etc.)
  - [ ] Battle ribbons (Battle Tower, Battle Tree, etc.)
  - [ ] Event ribbons (Classic, Premier, etc.)
  - [ ] Memorial ribbons
  - [ ] Mark ribbons (Gen 8+)
  - [ ] Generation-specific ribbons
- [ ] Display ribbon icons/images
- [ ] Implement ribbon legality checking (prevent illegal combinations)
- [ ] Add search/filter for ribbons
- [ ] Create unit tests for ribbon editing

#### Memory Editor
**Status:** ❌ Not Implemented  
**Complexity:** Medium  
**Tasks:**
- [ ] Create Memory editing UI component
- [ ] Implement OT Memory editing (Gen 6+)
- [ ] Implement HT (Handling Trainer) Memory editing (Gen 6+)
- [ ] Add memory intensity/feeling/text selection
- [ ] Support memory location selection
- [ ] Add memory validation based on game version
- [ ] Implement affection/friendship display and editing
- [ ] Create unit tests for memory editing

#### Contest Stats Editor
**Status:** ❌ Not Implemented  
**Complexity:** Low  
**Tasks:**
- [ ] Create Contest Stats UI component
- [ ] Add fields for Cool, Beauty, Cute, Smart, Tough stats
- [ ] Add Sheen field (Gen 3-4)
- [ ] Implement contest stat validation (max values per generation)
- [ ] Support generation-specific contest stat limits
- [ ] Create unit tests for contest stats

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

#### PID/EC Generator
**Status:** ⚠️ Partial (basic PID editing exists)  
**Complexity:** Medium  
**Tasks:**
- [ ] Create PID generator with options:
  - [ ] Method 1/2/4 (Gen 3-4)
  - [ ] Shiny PID generation
  - [ ] Gender-locked PID generation
  - [ ] Nature-locked PID generation
- [ ] Add Encryption Constant (EC) generator (Gen 6+)
- [ ] Implement shiny type selection (Star/Square for Gen 8+)
- [ ] Add PID-to-IV correlation for Gen 3-4
- [ ] Create PID reroller for legal PIDs

#### Catch Rate / Status Condition Editor
**Status:** ❌ Not Implemented  
**Complexity:** Low  
**Tasks:**
- [ ] Add Catch Rate field (Gen 1-2)
- [ ] Create Status Condition editor (Gen 1-2)
- [ ] Add held item slot for Gen 1-2 (linked to catch rate byte)
- [ ] Implement generation-specific validation

### 1.2 Legality Checker
**Status:** ❌ Not Implemented  
**Complexity:** Very High  
**Priority:** Critical  
**Tasks:**
- [ ] Design Legality Checker UI component
- [ ] Integrate PKHeX.Core LegalityAnalysis
- [ ] Display legality results with color coding (legal/illegal/suspicious)
- [ ] Show detailed legality report with:
  - [ ] Encounter type validation
  - [ ] Relearn moves validation
  - [ ] PID/EC validation
  - [ ] Ribbon validation
  - [ ] Memory validation
  - [ ] Met location validation
  - [ ] Ball legality
  - [ ] Ability legality
  - [ ] Level/experience validation
- [ ] Add "Fix" buttons for common legality issues
- [ ] Implement batch legality checking
- [ ] Show legality warnings on Pokemon slot display
- [ ] Create comprehensive unit tests

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

### 2.5 Event Flags & Story Progress
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
- [ ] Advanced Pokedex (Kitakami, etc.)
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
  - [ ] Pokedex Research Tasks (SAV_PokedexResearchEditorLA)
  - [ ] Research level editor
  - [ ] Alpha Pokemon caught tracker
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

---

## Priority 6: Minor Features & Polishing

### 6.1 Trainer Customization Enhancements
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
- **Currently Implemented:** ~50 tasks (20%)
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

**Last Updated:** 2026-02-17  
**Next Review:** 2026-03-17
