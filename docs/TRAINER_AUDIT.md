# Trainer control audit (#1376, #1378)

Reference: **PKHeX 26.10.18**, commit
[`bfe89c74a3f5265805e364b79bc04ad38ebbc5ea`](https://github.com/kwsch/PKHeX/tree/bfe89c74a3f5265805e364b79bc04ad38ebbc5ea).
This matches `Directory.Packages.props`. The reference was exported from the local
`C:\Code\PKHeX` tag into a separate directory; newer master code was not used.
The app baseline is `ebe5e414`. This work is separate from #1375.

## Reading the matrix

[trainer-controls.csv](trainer-controls.csv) enumerates **every concrete input,
button, tab, grid column, and progress control declaration** reachable from Trainer,
including all five dialogs. It records the source line, exact binding/action,
bounds, formatting, and disabled/read-only expressions. Rebuild it with
`node tools/audit-trainer-controls.mjs` after changing markup. Repeated controls
(badges, sayings, card Pokémon, berries, perks, clothing rows, raids) are recorded
once per declaration; their multiplicities and format conditions are below.

The format table and field tables together form the control-by-format matrix:
each field applies **only** to the listed formats, and each format inherits the
corresponding detailed field rows. A dash means absent, never a writable base
`SaveFile` placeholder. Bounds are inclusive; numeric inputs reject values outside
their type/range. Values already outside the editing range are not normalized on
load. No table requires private saves or contains trainer details.

## Save formats

`I` = stored name/TID; `S` = stored SID and read-only TSV; `G` = editable gender;
`L` = stored language; `P` = hours/minutes/seconds/reset; `M` = money;
`C` = Game Corner coins. IDs are 16-bit through Gen 6, display TID/SID from Gen 7.

| Save implementation / versions | Common controls | Start / Fame / Saved | Additional Trainer controls |
| --- | --- | --- | --- |
| SAV1: Red, Green/Blue, Yellow; Japanese/international/VC | I P M C | — / — / — | Rival name/starter, HoF count, battle style/effects/text speed, 8 badges |
| SAV2: Gold/Silver; Japanese/international/Korean/VC | I P M C | — / — / — | Rival, style/effects/text/sound, RTC, 16 badges |
| SAV2: Crystal; Japanese/international/VC | I G P M C | — / — / — | Same, plus GS Ball event |
| SAV1StadiumJ: Pocket Monsters Stadium | — | — / — / — | No persistent trainer profile |
| SAV1Stadium: Stadium (both regions) | — | — / — / — | No persistent trainer profile |
| SAV2Stadium: Stadium 2 (both regions) | — | — / — / — | No persistent trainer profile |
| SAV3RS: Ruby/Sapphire | I S G P M C | — / — / — | 8 badges |
| SAV3E: Emerald | I S G P M C | — / — / — | BP, earned BP, 8 badges |
| SAV3FRLG: FireRed/LeafGreen | I S G P M C | — / — / — | Rival name, 6 trainer-card species, 8 badges |
| SAV3Colosseum | I S G L P M | — / — / — | No Game Corner coin control |
| SAV3XD | I S G L P M | — / — / — | No Game Corner coin control |
| SAV3RSBox | — | — / — / — | No persistent trainer profile |
| SAV4DP: Diamond/Pearl | I S G L P M C | seconds / seconds / — | Geography, map/X/Z/Y, 8 badges |
| SAV4Pt: Platinum | I S G L P M C | seconds / seconds / — | Same |
| SAV4HGSS: HeartGold/SoulSilver | I S G L P M C | seconds / seconds / — | Same, Pokéathlon points, 16 badges |
| SAV4BR: Battle Revolution | I S L P M | — / — / — | `CurrentOT`, connected DS TID/SID; active profile, no generic gender/coins |
| SAV4Ranch: DP and Platinum revisions | P | — / — / — | No writable identity, currency, or geography placeholders |
| SAV5BW: Black/White | I S G L P M | seconds / seconds / — | Geography, uint Game Sync ID, Subway BP |
| SAV5B2W2: Black 2/White 2 | I S G L P M | seconds / seconds / — | Same |
| SAV6XY: X/Y | I S G L P M | seconds / seconds / minutes | 3DS geography, hex Game Sync ID, BP, Poké Miles, sayings/nickname viewer, 8 badges |
| SAV6AO: Omega Ruby/Alpha Sapphire | I S G L P M | seconds / seconds / minutes | Same; no separate nickname |
| SAV6AODemo: ORAS demo | I S G L P M | seconds / — / minutes | Common backed fields; unused BP, HoF and Game Sync excluded; no badge UI |
| SAV7SM: Sun/Moon | I S G L P M | seconds / seconds / minutes | 3DS geography, Game Sync/Nex IDs, BP/FC, plaza name, Mega unlock |
| SAV7USUM: Ultra Sun/Ultra Moon | I S G L P M | seconds / seconds / minutes | Same |
| SAV7b: Let's Go Pikachu/Eevee | I S G L P M | seconds / — / minutes | `PlayerGeoLocation.AdventureBegin`, rival; no unused Game Sync editor |
| SAV8SWSH: Sword/Shield | I S G L P M | date / — / minutes | BP, Watts, Trainer Card dialog, 8 badges |
| SAV8BS: BD/SP, all revisions | I S G L P M | seconds* / — / seconds* | BP, 8 badges |
| SAV8LA: Legends: Arceus | I S G L P M | seconds / — / minutes | Merit current/total |
| SAV9SV: Scarlet/Violet, revisions 0/1/2 | I S G L P M | date / — / seconds | SV section below; DLC controls gated by revision |
| SAV9ZA: Legends: Z-A, revisions 0/1 | I S G L P M | — / — / seconds | ZA section below; unused enrollment block excluded |
| Bank3: PokéStock GST | — | — / — / — | Storage only |
| Bank4: PokéStock STK | — | — / — / — | Storage only |
| Bank7: original and EOL Bank formats | — | — / — / — | Storage only; original format upgraded by Core on import |

*BDSP displays seconds and preserves the original subsecond FILETIME ticks.*
GameCube GCI and memory-card wrappers, Manic ZIPs, and VC containers retain their
normal loader/export handling; Trainer capabilities depend on the resulting save
implementation. Gen 1/2/3 language is derived by the loader (`ILangDeviantSave`),
not a persistent language selector. Colosseum/XD language is stored.

## Common fields: WinForms and backing storage

References below are under `PKHeX.WinForms/Subforms/Save Editors/` at the pinned
revision. `Simple` means `SAV_SimpleTrainer`; `6/7/7GG/8/8b/8a/9/9a` mean
`GenN/SAV_Trainer*.cs` (Gen 6 is `Gen6/SAV_Trainer.cs`). Inspect the matching
`.Designer.cs` for masks/ranges. The `SaveFile` implementations delegate to the
named Core blocks; the CSV gives the exact PKMDS getter and writer.

| PKMDS field/action | WinForms control/handler | Read/write storage, formatting, conditions, result |
| --- | --- | --- |
| Name | `TB_OTName`, `SaveTrainerInfo` / Simple save | `SaveFile.OT`, format string converter and `MaxStringLengthTrainer`; only writes on a changed name. Colosseum also updates OT2 through Core. |
| BR Name | `Gen4/SAV_Trainer4BR.TB_OTName` | `CurrentOT`, 7 chars, active `CurrentSlot` bytes; never base OT. No Pokémon name propagation for BR. |
| ID / SID (16-bit) | shared `TrainerID`/`TrainerTID`/`TrainerSID`, BR `MT_TID/MT_SID` | `TID16/SID16`, 0–65535, five digits. SID absent Gen 1/2. BR IDs describe the connected DS game. |
| ID / SID (display) | shared `TrainerID.LoadTrainer` | `TrainerTID7/TrainerSID7` split ID32 at 1,000,000. TID max min(999999, remaining ID32); SID max (uint.MaxValue−TID)/1,000,000. Prevent arithmetic wrap, retain other component. |
| TSV | shared `TrainerID.GetTSV` | `(TID16 ^ SID16) >> 3` through Gen 5, `>> 4` later; read-only. |
| Gender | `CB_Gender`; ZA `GenderToggle` / `SaveTrainerInfo` | Real stored `Gender` only; hide Gold/Silver's male-only no-op. SM/USUM skin parity, SWSH gender appearance/reset and ZA `PlayerFashion.Reset` follow reference. |
| Language | `CB_Language`, `GameInfo.LanguageDataSource` | `SaveFile.Language`; LGPE Core synchronizes status and config. Derived-language formats have no selector. |
| Country / Region (4,5) | Simple `CB_Country/CB_Region`, `UpdateCountry` | `SAV4/5.Country/Region`; generation-specific resources, default subdivision list if absent. Country changes reset subdivision; rendering never changes it. |
| Country / Region / 3DS Region (6,7) | `CB_Country/CB_Region/CB_3DSReg` | `IRegionOrigin`; byte fields, filtered dependent lists. Clear old region choices when switching saves/country zero. No subdivisions are invented. |
| Game Sync ID (5) | stored extension to Simple editor | `PlayerData5.GameSyncID` at 0x18, uint decimal 0–4294967295. |
| Game Sync ID (6,7) | stored extension; no Trainer control in reference | `IGameSync.GameSyncID`, 16 hexadecimal characters (64 bits); require exact length and hex before writing. LGPE's legacy bytes are not a functional Game Sync service. |
| Nex Unique ID (7) | stored extension; no Trainer control in reference | `MyStatus7.NexUniqueID`, 32 hex characters (128 bits); same validation. Not an event unlock. |
| Playtime readout, Hours, Minutes, Seconds, Reset | `MT_Hours/MT_Minutes/MT_Seconds`; reset is a PKMDS convenience | 0–65535 hours (Gen 1 0–255), 0–59 minutes/seconds. Most blocks use ushort+two bytes; Colosseum float, XD float/double, BR double, Ranch uint seconds, ZA double. Hours preserve remaining components/fractions; Reset clears Gen 1 maximum/frames and ZA fractions. |
| Game Start | `CAL_AdventureStartDate/Time`; LGPE `CAL_AdventureBeginDate/Time` | `TrainerTimestamps`: uint seconds since 2000 (4–7); epoch 1970 (LGPE/LA); SWSH card Y/M/D; BDSP `System.LocalTimestampStart`; SV `EnrollmentDate`. ZA block explicitly unused. |
| Hall of Fame | `CAL_HoFDate/Time` (4–7) | `SecondsToFame`, uint epoch 2000. Zero displays blank; entering a value writes date **and time**. No stale separate time state or base-property fallbacks. |
| Last Saved | `CAL_LastSavedDate/Time` (6+) | 6/7 `Played` packed epoch-zero; LGPE/SWSH packed 1900; BDSP `System.LocalTimestampLatest` UTC FILETIME via local accessor; LA/SV/ZA packed 1900. Step is 60 or 1 according to actual storage, not generation. |
| Festival Plaza Name | `7.TB_PlazaName` | `JoinFesta7.FestivalPlazaName`, 20 chars at 0x510. |
| Rival names | Simple/7GG rival data, Gen 3 misc editor | `SAV1/2.RivalName`, `SAV3FRLG.RivalName`, `SAV7b.Misc.RivalName`; per-save maximum length. Other rivals are tracked below. |
| Rival Starter | Gen 1 stored extension | `SAV1.RivalStarter`; national ↔ internal species conversion; save-filtered species search. |
| FRLG Trainer Card 1…6 | Gen 3 trainer-card/misc data | Event work 0x43…0x48; national ↔ internal Gen 3 conversion; unrelated work untouched. |
| Current Map / X / Z / Y | Simple `NUD_M/X/Z/Y` | `SAV4.M/X/Z/Y`; native map/grid units, no float scaling or swapped axes. Other generations' missing maps tracked below. |
| Hall of Fame Count | Gen 1 stored extension | `SAV1.HallOfFameCount`, byte 0–255. |
| Battle Style / Text Speed / Use Battle Effects (1,2) | Simple `CB_BattleStyle/CB_TextSpeed/CHK_BattleEffects` | Boolean switch/set, text 0–7, battle-effects flag; preserve other option bits. |
| Sound Type (2) | Simple `CB_SoundType` | `SAV2.Sound`, **0 mono / 2 stereo**, not selection index 1. Fixes stereo displaying as an unknown option. |
| Sync overlay / spinner / message | PKMDS extension | Progress display during party/box identity propagation; no save writes while rendered. |

Dates accept 2000–2099, matching the reference pickers. Blank/invalid timestamps
display blank without initializing storage. Supported controls remain editable
when blank (intentional difference from WinForms hiding/disabling some invalid
Last Saved controls). Clearing a picker does not erase stored data. Editing uses
native precision; no load-time date normalization or timezone writes occur.

## Currencies

All currency controls have minimum 0. `Money` uses `SaveFile.Money/MaxMoney` and
WinForms `MT_Money`; `Coins` uses the concrete `Coin/MaxCoins` accessors, never
`Generation <= 4` alone. The loop-rendered extra currency field in the CSV expands
to every row here.

| Format / label | Matching reference | Core read/write, maximum, coupled fields |
| --- | --- | --- |
| Emerald BP / Earned BP | Gen 3 misc/frontier data | `SmallBlock.BP` 9999 / `BPEarned` 65535 |
| HGSS Pokéathlon Points | Pokéathlon editor | `Pokeathlon.Points`, `Pokeathlon4.MaxPoints` (99999) |
| BW/B2W2 Battle Subway BP | Simple `MT_Coins` relabeled BP | `BattleSubway.BP`, 9999 |
| XY/ORAS BP | `6.TB_BP` | `SAV6.BP`, 9999; exclude unused demo BP |
| XY/ORAS Poké Miles | `6.TB_PM` | Record 63, `GetRecordMax(63)`; **also write record 64** like WinForms |
| SM/USUM BP | `7.NUD_BP` | `Misc.BP`, 9999 |
| SM/USUM Festival Coins | `7.NUD_FC` | `Festa.FestaCoins`, 9999999; Core updates total to used record 38 + balance |
| SWSH BP / Watt | `8.NUD_BP/MT_Watt` | `Misc.BP` 9999; `MyStatus.Watt` 9999999; raise `Record8.WattTotal` when needed |
| BDSP BP | `8b.NUD_BP` | `BattleTower.BP`, **9999**, not uint.MaxValue |
| LA Merit Points / Merit Earned (Total) | `8a.NUD_MeritCurrent/Earned` | `KMeritCurrent/KMeritEarnedTotal`, 999999999 each |
| SV League Points / Blueberry Points | `9.MT_LP/MT_BP` | `LeaguePoints/BlueberryPoints`, **9999999** (seven-digit masks); BP only revision >= 2 |
| ZA Royale Points / Infinite | `9a.MT_RoyaleRegular/Infinite` | `TicketPointsRoyale/Infinite`, 310000 / 50000 |
| ZA Hyperspace Survey Points | `9a.MT_HyperspaceSurveyPoints` | uint block `KHyperspaceSurveyPoints`, 100000; DLC only |

## Game sections and actions

| Control(s) | Format / reference | Binding, behavior, side effects, disposition |
| --- | --- | --- |
| Mega Evolution Unlocked | SM/USUM `7.CHK_UnlockMega`, `SaveFlags` | `MyStatus.MegaUnlocked`, bit 0 at 0x78. Changes only that bit; leaves Z-Move/unrelated bits and inventory intact. Both enabling and disabling persist. Implements #1378. |
| Reset Clock (RTC), password, Reset/Cancel | GSC `SAVEditor` RTC prompt | Japanese omits password; otherwise `ResetKey:00000`. Confirm then `ResetRTC`, mark edited, snackbar. Cancel has no writes. |
| Enable GS Ball Event | Crystal `Gen2/SAV_Misc2.B_VirtualConsoleGSBall` | `EnableGSBallMobileEvent`; disabled once enabled. Physical and VC both supported; does not alter global GB legality settings. |
| Birth Month / Birth Day | SV stored extension | `MyStatus.BirthMonth/Day`, bytes 1–12 / 1–31; render does not fill zero dates. No matching WinForms Trainer control. |
| Throw Style | SV revision >= 2, `9.CB_ThrowStyle` | `ThrowStyle9` 1–9, explicit enum values, not zero-based selected index |
| BBQ Solo / Group Quests Completed | SV revision >= 2, `9.NUD_BBQSolo/Group` | `BlueberryQuestRecord.QuestsDoneSolo/Group`, uint range |
| Map X / Y / Z / Rotation | SV `9.LoadMap/SaveMap` | `X/Y/Z`, float native units; rotation degrees = atan2(RZ,RW) × 360/π; writer places sine in **RZ**, cosine in RW; keeps position unchanged |
| Map X / Y / Z / Rotation / Map Name | ZA `9a.LoadMap/SaveMap` | `Coordinates`, map ASCII 32 bytes, float coordinates, quaternion rotation as above. Work around Core's wrong-axis overload. |
| Street Name | ZA DLC `9a.GetDLC/SaveDLC` | `KStreetName`, format string converter, 18 chars; absent base revision |
| Unlock All Fly Locations | SV `9.B_UnlockFlyLocations_Click` | Explicit same base/Kitakami/Blueberry block-name list; existing bool blocks only, no creation of absent DLC blocks |
| Collect All Shrine Stakes | SV `9.B_CollectAllStakes_Click` | Core `CollectAllStakes`: stake flags, shrine state=1, removed-chain counts; not just map markers |
| Unlock All TM Recipes | SV `9.B_UnlockTMRecipes_Click` | Core `UnlockAllTMRecipes`, only existing recipe blocks |
| Activate Snacksworth Legendaries | SV DLC2 `9.B_ActivateSnacksworthLegendaries_Click` | Core event states=1 (appeared, not captured); hidden before revision 2 |
| Unlock All Coaches | SV DLC2 `9.B_UnlockCoaches_Click` | Core coach bool blocks; hidden before revision 2 |
| Unlock All Ride Upgrades | SV `9.B_UnlockBikeUpgrades_Click` | dash/swim/high jump/glide/climb and optional DLC flight flag |
| Unlock All Throw Styles | SV DLC2 `9.B_UnlockThrowStyles_Click` | Core also marks three support-board purchases and clears their unread flags |
| Unlock Base Clothing | SV `9.B_UnlockClothing_Click` | `PlayerFashionUnlock9.UnlockBase`, uses trainer gender; no automatic gender change |
| Collect All Technical Machines | ZA `9a.B_CollectTechnicalMachines_Click` | `TechnicalMachine9a.SetAllTechnicalMachines(save,true)`; reports collected count/no-op |
| Collect All Colorful Screws | ZA `9a.B_CollectScrews_Click` normal action | `ColorfulScrew9a.CollectScrews`; updates collection and inventory via Core, reports count. Modifier-key reset/location clipboard alternatives are deferred. |
| Gym Badges, each badge image/name or fallback checkbox | 1–6, SWSH, BDSP; Simple / per-game badge controls | `Badges` bits; GSC 16 bits; HGSS Johto byte + Kanto byte (`Badges16 << 8`); BDSP system flags 124–131. Only requested bit changes. LGPE/SV show existing explanatory issue links (#59/#60), not invented flag bindings. |

Bulk actions are explicit live-save operations with success/no-op feedback; they
mark the save edited and refresh. Unlike WinForms' cloned parent editor, the whole
Trainer tab has no Save/Cancel transaction. The individual dialogs below do.
No bulk action adds unsupported blocks. Opening an editor or a tab is read-only.

## Every reachable dialog

| Dialog / every field or action family | Reference and storage | Commit/cancel and bounds |
| --- | --- | --- |
| Sayings: XY nickname; Saying 1…5; Close | `6.TB_TRNick/TB_Saying1…5`, `MyStatus6` (XY subtype for nickname) | Explicit read-only viewer; intentional difference from editable WinForms text. Close changes nothing. |
| Trainer Card: card name; number; card TID; Roto Rally score; Confirm/Cancel | `8.TB_TrainerCardName/Number`, `MT_TrainerCardID/MT_RotoRally`, `TrainerCard8` | Buffered until Confirm; name uses save max, number 0–999 mirrors MyStatus only if changed, TID 0–999999, score `RotoRallyScoreMax` and Core record mirror. Confirm does not rewrite untouched card values/records; Cancel/X/Escape discard. |
| Fashion SV: 8 section tabs; index/ID; New per slot; Save/Cancel | `Gen9/SAV_Fashion9` property grid; `FashionItem9` arrays | All slots shown, sentinel 65535 disables flag edits. Rows are buffered; only materialized sections saved. |
| Fashion ZA: 10 clothing tabs; index/ID; New, New Shop, New Group, Equipped, Owned; Set All Owned (tab/all tabs) | Same generic WinForms fashion editor; `FashionItem9a` arrays | Bulk owned skips sentinel slots, all-tabs loads all clothing arrays. ID/index read-only; editable-ID parity gap tracked below. |
| Fashion ZA: 15 hair/makeup tabs; index/ID; New; Save/Cancel | `HairMakeItem9a` arrays | Hair ID/index read-only; same buffering. Cancel/X/Escape never write underlying arrays. |
| Raid: Paldea/Kitakami/Blueberry tabs; Today/Tomorrow seeds; raid selector | `Gen9/SAV_Raid9`, `RaidSpawnList9` | Paldea global seeds 16 hex; Kitakami revision >=1, Blueberry >=2; per-list selection is transient. |
| Raid: Enabled; LP Claimed; Area ID; Lottery Group; Spawn Point ID; Seed; Content; scene-point name | `TeraRaidDetail` (`SAV_Raid9` property grid) | Native uint fields, seed 8 hex; content enum base/black/distribution/mighty; scene-point name read-only. Repeated per supported region. |
| Raid: Copy to All / Copy to All (incl. Seed) | `RaidSpawnList9.Propagate` | Explicit propagation in selected list; seed inclusion matches button. Save commits; snapshots restore every edited raid block on Cancel, X, Escape or disposal, leaving unrelated save blocks intact. |
| Raid: 7-Star index; Identifier; Captured; Defeated; Save/Cancel | `SevenStarRaidDetail` captured/defeated blocks | Existing nonzero identifiers only; ID 8 hex; captured/defeated booleans. Creating new entries is deferred. |
| Donut: slot; previous/next; active count; empty-slot message | `Gen9/SAV_Donut9a` slot list | DLC only, raw slot 0–998; navigation chooses occupied neighbors, no write on selection. |
| Donut: Berry 1…8; Perk 1…3 | `DonutEditor9a`; `Donut9a.Berry1…8/Flavor0…2` | Berry item IDs; perk hashes; native choice lists, None=0 |
| Donut: Stars; Calories; Level Boost; Donut ID; Berry Name ID; Created | `DonutEditor9a` fields / `Donut9a` | 0–5 / 0–9999 / byte / 0–202 / ushort; creation time read-only milliseconds since 1970 |
| Donut: Recalculate Stats; Shiny Template All; Random Lv3 All; Clone Selected to All; Save/Cancel | `DonutInfo.RecalculateDonutStats`, Core pocket methods as in `SAV_Donut9a` | Clone requires occupied selection; all=999 slots. Snapshot restores on every unsaved dismissal, not just Cancel. Other blocks are preserved. |

Dialog launch buttons are also included individually in the CSV. The list above
includes their tabs, navigation, readouts, bulk operations and closing actions;
none is treated as being outside the Trainer audit just because it is a dialog.

## Cross-cutting findings and remaining parity work

Fixed discrepancies: unsupported storage identity/money/coins/dates; GS gender;
incorrect LA epoch; missing LGPE adventure-start binding; missing Last Saved
editors and precision distinctions; invalid SWSH dates throwing; initialization
writing timestamps; BDSP fractional ticks; wrong currency/card-ID bounds; missing
Poké Miles total mirror; stale subdivision lists; Gen 2 stereo display; wrong-axis
SV/ZA rotation; missing ZA gender appearance reset; DLC actions without DLC;
dialog X/Escape committing direct edits; fashion bulk writes into empty slots.

Name, gender and ID changes intentionally propagate to matching resident Pokémon,
an app behavior beyond WinForms. Matching uses species != 0, original name, ID32,
and gender for **Gen 3+** (PK3 does store gender). Handling-trainer matching uses
name, optional old gender and current handler because HT has no ID. BR bypasses
this propagation. Slot writes now use `EntityImportSettings.None` so a trainer
edit does not count as a catch/trade or update the Pokédex. Unreadable LGPE party
pointers are skipped via the existing safe accessor. The overlay clears in
`finally`; slot/Trainer refresh follows successful edits. Scalar user changes
mark the save edited through the form's change event; reading never does.

**Remaining functionality is consolidated in
[#1381](https://github.com/codemonkey85/PKMDS-Blazor/issues/1381) and roadmap §6.1;
closing the audit issue #1376 does not declare full WinForms feature parity.**
The consolidated issue includes a checklist to close fully redundant issues or
edit broader issues to retain their unrelated scope, preserving discussion and
linking the trackers in both directions. It identifies overlaps including
appearance #873, fashion #882/#895, Gen 9 follow-ups #889, generation trackers,
and badge research #59/#60. Existing issues have not been closed or edited as
part of this consolidation; reconciliation is recorded as follow-up work there.

| Follow-up group | Exact missing or intentionally limited functionality |
| --- | --- |
| Identity/profile customization | Game/version switching where WinForms permits it, trainer string trash editor, BR country/region, birth strings, self-introduction, Player ID, battle/Colosseum records/unlocks (`SAV_Trainer4BR`); appearance/avatar/skin editors (#873) |
| Gen 1–3 options and misc | Yellow sound choices, Pikachu friendship/beach score; Gen 3 sound/text/style/scene controls; Gen 3 rival/card features absent for other games; playtime frame/maximum flag controls beyond reset |
| Gen 4–5 | Rival editors, Gen 4 BP/frontier and additional HGSS progress, Gen 5 player position and records; Gen 5 LastSaved bits exist in Core but are not in the reference Simple Trainer UI |
| Gen 6 | Editable sayings/nickname (currently explicitly read-only); Vivillon, style, multiplayer sprite, fashion, Maison/unlocks, XY Chateau, Mega/Rayquaza flags and other record editing |
| Gen 7 | Z-Move toggle, Alola offset, map coordinates scaled by 60 and rotation, skin/Vivillon, refresh days, throw styles and learned/unlocked lists, stamps, Battle Tree streaks/super flags, Poké Finder version/gyro/snaps/thumbs, map/fly lists, USUM surf scores/Rotom/Roto Loto fields |
| LGPE/SWSH/BDSP/LA | Missing map/scaling/rotation editors for these formats, LGPE partner/GO Park workflows, additional SWSH trainer appearance, BDSP rival, LA rank/satchel fields and trainer records |
| SV/ZA | Profile/icon/photo import/export, additional trainer records, ZA overworld/day clocks and fractional playtime fields, screw modifier-key actions; existing birth fields do not add a full calendar editor |
| Nested editors | Editable fashion IDs; additional property-grid fields and new 7-Star entries; donut import/export, creation-time editor, generator/flavor visualization and additional donut fields present in WinForms |

The Core workarounds are documented at their call sites: the released XD/PBR/Ranch
hour setters subtract fractional `TotalHours`, erasing smaller units; ZA's
rotation overload writes RY while its getter reads RZ. Remove workarounds only
after a package upgrade and the corresponding regression checks pass. This
document records the upstream limitations for the issue/PR; no upstream report
has been posted automatically.

## Validation

`TrainerAuditTests` adds format/control/read-only-render checks, Mega bit
isolation and export/reload for SM/USUM, native timestamp round trips, epoch and
FILETIME regressions, storage distinctions, playtime fraction preservation,
rotation, stereo, Poké Miles mirror, resident-name synchronization/record
preservation, and dialog dismissal rollback. These are compiled locally;
**execution is left to GitHub Actions**, per AGENTS.md.

Local validation uses .NET 10.0.403 from `C:\Code\.dotnet-10`, `dotnet format`,
and Debug builds of Web and Tests. Browser checks use only public repository
fixtures; fixture changes are local and do not publish exports or trainer details.

Observed browser checks:

| Fixture format | Result |
| --- | --- |
| Sun / Ultra Sun | Toggle off, export/reload false; toggle on, export/reload true. Both game families pass. |
| Z-A | Only Last Saved; seconds supported; no unused start or HoF editor |
| Arceus | Start year matches the original 2022 fixture; Last Saved has minute precision |
| Let's Go | Working adventure-start and Last Saved inputs, no unsupported HoF |
| Sword/Shield | Date-only start, minute Last Saved |
| Battle Revolution | Name and playtime/currency present; no timestamps or coins |
| Ruby/Sapphire Box | No placeholder trainer fields or timestamps |
| Silver | Real identity/playtime/coins/options, no timestamps |
| BDSP | Start/Last Saved both editable with second precision |
| Scarlet raid dialog | Escape restores seed; Save retains changed seed when reopened |

The export checks used the app's download fallback because Playwright intercepts
the native save picker. Original repository fixtures were not overwritten.

Repository sync was attempted for all eight `.sync-repos` entries. SSH fetches
did not complete in the sandbox and were interrupted; no branch was advanced.
All were initially clean except PokeAPI's two locally modified move-flag CSVs,
which were left intact. The pinned reference tag was already available locally,
so this did not prevent comparison against the required released source.

| Resolved repository | Branch | Before = after (no update) |
| --- | --- | --- |
| C:\Code\PKHeX | master | 14223bbac964ac06a68e32e3e6e569457b49ff4d |
| C:\Code\pokemon-showdown | master | 16db150abae0829180b8721da3bff0e51896a223 |
| C:\Code\pokeapi | master | e5b79b2e15d7c50735a945936afd4cd1cfd94eae |
| C:\Code\sprites | master | a7a3d62bb80a654ab5e01b488659929af0077c31 |
| C:\Code\PKHeX.Everywhere | main | 14a0831435b0ab8f4d004f855edfc2aba5125b32 |
| C:\Code\PKHeXFeebasLocatorPlugin | master | 9b9fc32fc9c725b1a6ea515cc300356614083b7c |
| C:\Code\PKHeXWC3Plugin | master | 85f8852c4ce2c43fb33411e45f847bcb8d5f6cd0 |
| C:\Code\PKHeXPluginPile | master | 8b354105c545a6d7d6f391488e3e24b9a67a2624 |

Core findings were reported on
[#1376](https://github.com/codemonkey85/PKMDS-Blazor/issues/1376#issuecomment-6809355655)
as required by AGENTS.md. No upstream issue was created.
