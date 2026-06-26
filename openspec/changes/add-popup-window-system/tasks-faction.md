# Tasks — Faction Window (HUD-003 slice 5) — BtnFaction

> Slice 5 of `add-popup-window-system`. Reuses PopupWindow/PopupManager base.
> Vietnamese UI. Source of truth: PC `faction_bonus.txt` (Reference/PcFaction)
> read by `FactionBonusService`. Also fixes a HUD wiring bug: `BtnFaction` was
> wrongly toggling the `StallCurrencySelector` instead of opening a faction panel.

## Research / source evidence
- [x] Loaded required port/resource skills: `jx-pc-port-rule`, `jx-hud-port`, `jx-pc-resource-resolver`.
- [x] External research requirement satisfied: Exa fetched Unity UI Toolkit list/table best practices (small stat tables use a flex column inside ScrollView; ListView virtualization not needed for ≤10 rows).
- [x] Current Unity source mapped: `OnFactionClick` toggled `_stallCurrencySelector` (wrong feature); `FactionBonusService` (sandbox) exposes `GetByFaction(factionId)` + `ComputeHp/Mp/Atk/DefBonus(factionId, level)`; `FactionBonusPanelService` is a stub returning empty and was bypassed.

## Implementation
- [x] Add `FactionContent` implementing `IPopupContent` + `IPopupLayoutHint`, title `Môn Phái`.
- [x] Render header (faction name + level), totals row (Tăng Máu / Tăng Nội Lực / Tăng Công / Tăng Thủ via `FactionBonusService.Compute*`), and a bonus table from `FactionBonusService.GetByFaction(factionId)`.
- [x] Null/empty-safe: with no service or empty data, shows a status row + zero totals.
- [x] Add `Faction.uss` and link it from `GameHud.uxml`.
- [x] Wire `BtnFaction` through `PopupManager.Instance.Show(new FactionContent(...))`; resolve `FactionBonusService`, `PlayerProgression.faction` (CombatFaction), and `level` from `SandboxManager`. Resolve Vietnamese name via the authoritative `GetFactionNameVi(CombatFaction)`.
- [x] Leave `_stallCurrencySelector` intact (still used by `SelectStallCurrency`); only un-wire `BtnFaction` from it.

## Faction id scheme note (follow-up)
- `CombatFaction` enum ints (Shaolin=1 … KunLun=10) and `PartyService.FactionNameVi` ints (1=Thiếu Lâm, 2=Võ Đang, 3=Nga My, … 7=Cái Bang) use DIFFERENT orderings.
- `FactionContent` takes the factionId (for bonus-table queries) and a caller-resolved name, so it does not guess. When the PC `faction_bonus.txt` data lands, reconcile which scheme its `FactionId` column uses.

## Verification
- [x] Unity refresh/compile completed; no new C# compile errors observed.
- [x] `run_tests(mode="EditMode", category_names=["Popup"])` → 46/46 passed (+6 Faction).
- [x] `run_tests(group_names=[GameHudControllerTests, HudDataBridgeTests])` → 22/22 passed (no regression from the OnFactionClick rewire).
- [x] Updated `GameHudControllerTests.OnFactionClick_*` for the migration (no longer toggles stall selector; degrades gracefully without PopupManager).

## Follow-up (NOT this slice)
- Populate `Reference/PcFaction/faction_bonus.txt` with real PC data and reconcile the factionId scheme.
- Real faction bonus grant on level-up / join-faction gameplay.
- Provide a dedicated HUD opener for the StallCurrencySelector if it needs UI access (currently only reachable via `SelectStallCurrency`).
