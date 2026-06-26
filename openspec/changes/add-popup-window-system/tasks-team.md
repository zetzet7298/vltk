# Tasks — Team Window (HUD-003 slice 4) — BtnTeam

> Slice 4 of `add-popup-window-system`. Reuses PopupWindow/PopupManager base.
> Vietnamese UI. Source of truth: existing PC manifest service `TeamPanelService`
> (`a05d7a2c.dat` 组队 window). Migrates `OnTeamClick` from an inline HUD
> `_teamPreview` toggle to a focused popup window.

## Research / source evidence
- [x] Loaded required port/resource skills: `jx-pc-port-rule`, `jx-hud-port`, `jx-pc-resource-resolver`.
- [x] External research requirement satisfied: Exa fetched Unity UI Toolkit ScrollView/ListView best practices (pooling, small lists use ScrollView). Small team roster (max 6 members) does not need ListView virtualization.
- [x] Current Unity source mapped: `GameHudController.OnTeamClick` toggled inline `_teamPreview` (hardcoded placeholder members); `TeamPanelService` (PC `a05d7a2c` controls + `BuildRows(party)`) and `PartyService` (live roster) already exist.

## Implementation
- [x] Add `TeamContent` implementing `IPopupContent` + `IPopupLayoutHint`, title `Đội`.
- [x] Render roster panel from `TeamPanelService.BuildRows(PartyService, nearbyListClosed)` (live members + party status).
- [x] Render PC-derived control manifest rows (8 controls: Invite/Kick/Appoint/Refresh/Leave/Dismiss/CloseTeam/Cancel) with Vietnamese labels + `a05d7a2c` source IDs.
- [x] Add `Team.uss` and link it from `GameHud.uxml`.
- [x] Wire `BtnTeam` through `PopupManager.Instance.Show(new TeamContent(party))`; resolve `PartyService` from `SandboxManager.Instance`.
- [x] Remove dead inline code (`_teamPreview` field, `PopulateTeamPreview`, its binding) superseded by the popup. The `TeamPreview` UXML element remains `hidden` + unbound (harmless).

## Verification
- [x] Unity refresh/compile completed; no new C# compile errors observed.
- [x] `run_tests(mode="EditMode", category_names=["Popup"])` → 40/40 passed (+6 Team tests).
- [x] `run_tests(mode="EditMode", category_names=["HUD"])` → 13/13 passed (no regression in GameHudController).

## Follow-up (NOT this slice)
- Real invite/kick/appoint/leave/dismiss gameplay wired to `PartyService` mutations.
- Nearby-player list (PC `NearbyScroll`) population.
- Remove orphaned `TeamPreview` UXML element + unused `.hud-team-*` USS classes (pure cleanup).
