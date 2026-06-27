# Tasks — Migrate Skill Panel onto PopupWindow (`migrate-skill-panel-popup`)

> Phase: **tasks** · Splits the design's two-PR forecast into concrete, file-scoped,
> dependency-ordered work units. TDD: every new behavior ships RED (test first, failing)
> → GREEN (impl) → verify. Port rule: 100% PC; `PcSkillPanelService` reused untouched.
> Mirror: `FactionContent.cs` / `Faction.uss` / `FactionContentTests.cs`.
>
> **Gameplay-touching change:** the popup grants faction skill-panel progression and
> spends live `PlayerProgressionState` fight-skill points. Grant-before-BuildPage ordering
> and upgrade semantics MUST be preserved exactly (asserted in tests).

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | PR-1 ≈ +396 (pure additive); PR-2 ≈ 435 changed (net −325 / +110). Combined ≈ 830. |
| 400-line budget risk | High (combined) — Low per-PR (each ≤ ~400) |
| Chained PRs recommended | Yes |
| Suggested split | PR-1 (additive SkillContent + test + USS, nothing wired) → PR-2 (rewire BtnSkills, de-inline HUD, retire IMGUI, migrate fixtures) |
| Delivery strategy | auto-chain |
| Chain strategy | feature-branch-chain |

```text
Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High
```

**Split rationale:** the work crosses the 400-line budget, so it is split at the **wiring
boundary**. PR-1 adds `SkillContent` + `Skill.uss` + `SkillContentTests` + one
`<ui:Style>` link — zero behavior change, safe single-commit revert. PR-2 flips `BtnSkills`
to `PopupManager.Show(new SkillContent(...))`, de-inlines `GameHudController`, retires the
IMGUI overlay render, removes the obsolete UXML/USS, and migrates the one HUD-coupled test.
Reverting PR-2 alone restores the exact prior inline behavior (no data migration).

**Cross-PR dependency:** PR-2 depends on PR-1's files. PR-1 is merged to `main` first;
PR-2 is branched off the updated `main` and merged second.

---

## Input verification (done before authoring — no tasks, facts for the reviewer)

- `GameHudControllerTests.cs` actually lives at `Assets/Tests/EditMode/Sandbox/GameHudControllerTests.cs`
  (input list said `/UI/` — corrected). Read its `SetUp`: it reflects only
  `_buffPanel/_tradeInfoPanel/_tradeInfoClose/_tradePartner*/_stallCurrency*/_stall*Btn/
  _facePicker*/_faceBtn` — **no skill fields**. Removing the HUD skill fields does NOT break
  its `SetUp` (matches design D7). Only an additive `OnSkillsClick_*` no-throw test is added there.
- `CaiBangSkillPanelTests.cs` lives at `Assets/Tests/EditMode/Sandbox/`. Only
  `HudButtonSkills_OpensCaiBangPanelWithoutTouchingPlayerVisual` couples to the removed HUD
  surface (reflection on `_skillPanel/_skillSummary/_skillList` + `OpenSkillPanel()` +
  `IsSkillPanelVisible/PcSkillPanelRowCount/CurrentSkillSnapshot`). All other tests in the
  file are pure `PcSkillPanelService`/`PlayerProgressionState` data-service tests → untouched.
- `CuiYanSkillPanelTests` / `KunLunSkillPanelTests` / `TianRenSkillPanelTests` are pure
  data-service tests → untouched (design D7).
- `PcSkillPanelService` has `BuildPage(catalog, progression, selectedSkillId, pageIndex)`,
  `TryUpgrade(progression, catalog, skillId)`, `PcFightSkillSlotsPerPage==30`,
  `PcFightSkillPageCount==1`, and `PcCaiBangSkillOrder` (26 ids, first `115`, includes `125`
  = "Bổng Đả Ác Cẩu"). `GrantFactionSkillPanelProgression` mutates in place, idempotent.

---

# PR-1 — Additive `SkillContent` (no behavior change, nothing wired yet)

> Goal: `SkillContent` exists, is fully unit-tested, and `Skill.uss` ships — but `BtnSkills`
> still opens the old inline panel. Pure additive; safe revert.

## Phase 1.1 — RED: `SkillContentTests.cs` (tests fail, `SkillContent` does not exist)

- [x] **T1 — Create `Assets/Tests/EditMode/UI/SkillContentTests.cs`** with
  `[TestFixture, Category("Skill")]` (class-level category, mirrors `FactionContentTests`).
  Use `TestCatalogCache.NoviceAndCaiBang` + a fresh `PlayerProgressionState` (no
  `SandboxManager`) so the `grantProgression: null` fallback path is exercised. Add a small
  helper `NewContent()` returning `new SkillContent(catalog, progression, CombatFaction.CaiBang,
  "Cái Bang", artFolder, grantProgression: null)`. Confirm it does **not** compile/fails because
  `VLTK.UI.Popup.Skill`/`SkillContent` does not exist yet (expected RED).
- [x] **T2 — Add `TitleVi_IsVietnamese`**: assert `content.TitleVi == "Kỹ năng võ công"`.
- [x] **T3 — Add `Implements_IPopupContent_And_IPopupLayoutHint_WithPcFootprint`**: assert
  `content is IPopupContent` AND `content is IPopupLayoutHint`; cast to hint and assert
  `Width==205f`, `Height==376f`, `Left==338f`, `Top==110f` (strict parity with the prior inline
  `Rect(338,110,205,376)`).
- [x] **T4 — Add `Build_Produces30Cells_26Populated_4Empty_ForCaiBang`**: `Build(body)` then
  `OnShow()` (so the grant runs); assert the grid container has exactly
  `PcSkillPanelService.PcFightSkillSlotsPerPage` (`30`) children, exactly `26` populated
  (carry `--upgradable`/skill id) and `4` empty (`--empty`). Do not hard-code `30` — reference
  the service constant.
- [x] **T5 — Add `Build_PopulatedSkillIds_InPcOrder`**: assert populated skill ids equal the
  `PcSkillPanelService.PcCaiBangSkillOrder` sequence (first `115`, last `1074`, includes `125`).
- [x] **T6 — Add `Build_CellNameLabel_ContainsBongDaAcCau`**: assert the cell for skill `125`
  carries `"Bổng Đả Ác Cẩu"` (exact PC VI parity).
- [x] **T7 — Add `OnShow_GrantsProgression_SummaryReads200`**: after `OnShow()` assert the
  skill-point summary element text equals `"200"` (PC parity for the Cái Bang grant).
- [x] **T8 — Add `SelectSkill_TogglesSelectionAndDetail`**: first tap selects (detail region
  shows `displayName`/level/`upgradeStatus`); second tap on the same skill deselects
  (`selectedSkillId→0`, detail region cleared). Drive selection via the content's testable seam
  (see T-note below), not via a removed HUD method.
- [x] **T9 — Add `TryUpgrade_SpendsOnePoint_AndMutatesLiveProgression`**: after `OnShow()`,
  upgrade an upgradable skill (e.g. `117`); assert `progression.fightSkillPoints` drops `200→199`,
  `progression.skillLevels[117]==1`, and a re-`Refresh` shows `learnedLevel+1`.
- [x] **T10 — Add `TryUpgrade_HonorsPcMaxLevelCap`**: loop-upgrade a skill (e.g. `128`) to its
  `maxLevel`, then assert a further `TryUpgrade` is rejected (returns false / `canUpgrade` false).
- [x] **T11 — Add `TryUpgrade_HonorsLowLevelGate`**: set `progression.level=10`, upgrade `117`
  once (succeeds), assert the next `TryUpgrade` of `117` is rejected (PC gate
  `desiredLevel <= playerLevel - reqLevel + 1`).
- [x] **T12 — Add `OnShow_GrantIsIdempotent_OnReopen`**: call `OnShow()` twice with no spend in
  between; assert `skillPoints` stays `200` (grant re-runs with no extra effect).
- [x] **T13 — Add `Build_NullCatalog_AndNullProgression_DoesNotThrow`**: construct
  `new SkillContent(null, null, CombatFaction.CaiBang, null, null, null)`; assert
  `Build(body)` + `OnShow()` do not throw (null-safe; mirrors `FactionContent` null-service guard).
- [x] **T-note (applies to T8–T11):** selection + upgrade are content-owned callbacks fired by
  pointer events on the cell / `+` affordance (design D6). To keep these assertions robust in
  EditMode, `SkillContent` MUST expose a testable seam — either `internal void SelectSkill(int)`
  / `internal bool TryUpgrade(int)` with `[InternalsVisibleTo("VLTK.Tests")]` (preferred, no UI
  flakiness), OR the test invokes the registered `PointerDownEvent` via `SendEvent`. The impl
  task (Phase 1.2) picks the seam; the RED tests are written against the `internal` method shape.

> RED gate: after T1–T13 the file either does not compile (missing `SkillContent`) or every
> test fails. Commit the RED file.

## Phase 1.2 — GREEN: implement `SkillContent.cs` (make tests pass)

- [x] **T14 — Create `Assets/Scripts/UI/Popup/Skill/SkillContent.cs`** in namespace
  `VLTK.UI.Popup`, `public sealed class SkillContent : IPopupContent, IPopupLayoutHint`.
  Constructor per design D1:
  `SkillContent(SkillCatalog catalog, PlayerProgressionState progression, CombatFaction faction,
  string factionNameVi, string artFolder, System.Action<CombatFaction> grantProgression = null)`.
  Store all as readonly fields. Add `private int _selectedSkillId;` and `private const int PageIndex = 0;`
  (owned internally, design D1).
- [x] **T15 — Implement the `IPopupLayoutHint` + title surface**: `TitleVi => "Kỹ năng võ công"`,
  `Width => 205f`, `Height => 376f`, `Left => 338f`, `Top => 110f` (satisfies T2, T3).
- [x] **T16 — Implement `Build(VisualElement body)`**: `body.Clear()`; `body.AddToClassList("skill-body")`;
  scaffold `[.skill-summary] → [ScrollView .skill-grid-scroll containing .skill-grid] → [.skill-detail]`.
  Cache the summary label, the grid container, and the detail region as fields. End by calling
  `Refresh()` is deferred to `OnShow` (see T18); `Build` itself must be safe to call before grant.
- [x] **T17 — Implement `Refresh()`**: rebuild the snapshot via
  `PcSkillPanelService.BuildPage(_catalog, _progression, _selectedSkillId, PageIndex)`; write
  `snap.skillPoints` into the summary; loop `for slotIndex in [0, PcFightSkillSlotsPerPage)`:
  populated cell (`< rows.Count`) → slot icon + level label + add-point (only when
  `row.canUpgrade`) + name label + `--upgradable` when `canUpgrade` + select callback; empty cell
  → `--empty`. Port the cell geometry inline `PopulateSkillPanel` used (38×51 cell, 36×36 slot,
  5-col `flex-wrap`). When `snap.selectedRow.HasValue`, render the detail region
  (`displayName`, `learnedLevel/maxLevel`, `requiredLevel`, `summary`, `nextLevelSummary`,
  `upgradeStatus`); else clear it. NULL-safe: null catalog/progression → all-empty grid, no throw.
- [x] **T18 — Implement `OnShow()` (gameplay-critical, design D2):** grant progression BEFORE
  `BuildPage`/`Refresh`:
  `if (_grantProgression != null) _grantProgression(_faction); else _progression?.GrantFactionSkillPanelProgression(_catalog, _faction);`
  then `Refresh();`. Satisfies T7 (summary `200`), T12 (idempotent reopen). `OnClose()` nulls
  the cached elements.
- [x] **T19 — Implement `internal SelectSkill(int skillId)`**: toggle
  `_selectedSkillId = _selectedSkillId == skillId ? 0 : skillId; Refresh();` (design D6).
  Add `[InternalsVisibleTo("VLTK.Tests")]` to the assembly if not already present (verify the
  asmdef/`AssemblyInfo` — siblings may already declare it; reuse, do not duplicate).
- [x] **T20 — Implement `internal bool TryUpgrade(int skillId)`**:
  `if (PcSkillPanelService.TryUpgrade(_progression, _catalog, skillId)) { Refresh(); return true; } return false;`
  on the LIVE `_progression` ref (design D6). The `+` element `PointerDownEvent` calls
  `TryUpgrade(row.skillId)` + `evt.StopPropagation()`; a non-upgradable row renders NO `+`.
- [x] **T21 — Wire cell selection `PointerDownEvent` → `SelectSkill(row.skillId)`** with
  `evt.StopPropagation()`, mirroring inline `PopulateSkillPanel`.

> GREEN gate: T1–T13 now pass. No behavior change yet — `BtnSkills` still opens the inline panel.

## Phase 1.3 — USS + stylesheet link

- [x] **T22 — Create `Assets/UI/Popup/Skill/Skill.uss`** with the `.skill-*` classes from design
  D5, porting the geometry the inline `.hud-cb-*` used: `.skill-body`, `.skill-summary`,
  `.skill-grid-scroll`, `.skill-grid`, `.skill-grid-cell`, `.skill-grid-cell--upgradable`,
  `.skill-grid-cell--empty`, `.skill-grid-slot`, `.skill-grid-slot--empty`, `.skill-grid-level`,
  `.skill-add-point` (re-reference `状态加点按钮改_01.png`), `.skill-grid-name`, the new
  `.skill-grid-cell--selected`, and `.skill-detail` / `.skill-detail-title` /
  `.skill-detail-level` / `.skill-detail-summary` / `.skill-detail-next` / `.skill-detail-status`
  (formerly the external IMGUI tooltip). 5-column `flex-wrap`, 38×51 cells, 36×36 slots.
- [x] **T23 — Add `<ui:Style src="project://database/Assets/UI/Popup/Skill/Skill.uss" />` to
  `Assets/UI/HUD/GameHud.uxml`** alongside the existing popup `<ui:Style>` declarations.
  LINK ONLY — do not wire `BtnSkills`, do not touch any element. (The `.skill-*` classes resolve
  at runtime once PR-2 shows the popup.)

## Phase 1.4 — PR-1 verification

- [x] **T24 — Run `run_tests mode=EditMode category_names=["Skill"]`** → all `SkillContentTests`
  green. (Reuse-of-data invariant: `category_names=["CaiBang"]` also still green — the inline
  `HudButtonSkills_*` test is unchanged in PR-1.)
- [x] **T25 — Sanity: `category_names=["Popup"]`** still green (no sibling popup regression from
  the shared `<ui:Style>` addition).
- [x] **T26 — Open PR-1.** Confirm diff is purely additive (`SkillContent.cs`, `Skill.uss`,
  `SkillContentTests.cs`, +1 `<ui:Style>` line, possible `InternalsVisibleTo`/`AssemblyInfo`
  one-liner). Nothing wired; `BtnSkills` still inline.

**PR-1 task count: 26 — ALL COMPLETE (T1–T26 done; see apply-progress.md).**

---

# PR-2 — Behavior switch + cleanup (mostly deletions)

> Depends on PR-1 merged to `main`. Goal: `BtnSkills` opens `SkillContent` via
> `PopupManager`; the inline panel, IMGUI skill render, and obsolete UXML/USS are removed; the
> HUD-coupled test is retargeted.

## Phase 2.1 — Rewire `BtnSkills` + de-inline `GameHudController`

- [ ] **T27 — Rewire `OnSkillsClick()` in `Assets/Scripts/UI/GameHudController.cs`** to the
  sibling one-line shape (mirror `OnFactionClick`/`OnStatusClick`, design §3.2): null-guard
  `PopupManager.Instance` (log + return); resolve `catalog`/`progression`/`faction`/`factionNameVi`/`artFolder`
  from `SandboxManager` with the same fallbacks (`PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog()`,
  `new PlayerProgressionState()`, `CombatFaction.CaiBang` when `None`); pass
  `grantProgression: sandbox != null ? sandbox.GrantFactionSkillPanelProgression : null`; then
  `manager.Show(new SkillContent(...))`; `CloseMapPreview();`. Replace the existing
  `OnSkillsClick() => OpenSkillPanel();` body.
- [ ] **T28 — Remove the inline skill fields** from `GameHudController`: `_skillPanel`,
  `_skillClose`, `_skillPageOne`, `_skillPageTwo`, `_skillList` (ScrollView), `_skillSummary`
  (Label), and `_skillPageIndex` (int).
- [ ] **T29 — Remove the inline skill methods**: `OpenSkillPanel`, `SetSkillPage`,
  `CloseSkillPanel`, `SelectSkill`, `TryUpgradeSelectedSkill`, `TryUpgradeSkill`,
  `PopulateSkillPanel`.
- [ ] **T30 — Remove the inline-only public surface**: `IsSkillPanelVisible`,
  `PcSkillPanelRowCount`, `CurrentSkillSnapshot`, `CurrentSelectedSkillId`,
  `CurrentSkillPageIndex`.
- [ ] **T31 — Remove the `BindElements()` skill queries**: `_skillPanel = root.Q("CaiBangSkillPanel")`,
  `_skillClose = root.Q("CaiBangSkillClose")`, `_skillList = root.Q<ScrollView>("CaiBangSkillList")`,
  `_skillPageOne = root.Q("CaiBangSkillPageOne")`, `_skillPageTwo = root.Q("CaiBangSkillPageTwo")`,
  `_skillSummary = root.Q<Label>("CaiBangSkillSummary")`, and the `RegisterClick` lines for
  `CaiBangSkillClose`/`CaiBangSkillPageOne`/`CaiBangSkillPageTwo`, plus the `_skillPanel`/`_skillList`
  `pickingMode = Position` assignments.
- [ ] **T32 — Remove the `SizeRootToScreen` skill-panel clamp**: delete the
  `if (_skillPanel != null) { style.left = Clamp(338f,...); style.top = Clamp(110f,...); }` block
  (the `IPopupLayoutHint` now owns positioning).
- [ ] **T33 — Keep `GetFactionNameVi(CombatFaction)`** (still used by `OnSkillsClick` and log
  context; reused, do not remove).

## Phase 2.2 — Retire the IMGUI skill-panel render

- [ ] **T34 — Remove `DrawSkillPanelText()` from `Assets/Scripts/UI/PcHudVietnameseTextOverlay.cs`**
  and its call site in `OnGUI()` (the `DrawSkillPanelText();` line).
- [ ] **T35 — Remove the skill-only IMGUI assets/styles from the overlay**: fields
  `_skillName`/`_skillLevel`/`_skillHint` (GUIStyle), `_skillPanelTexture`/`_skillPanelTargetTexture`/
  `_addPointTexture` (Texture2D), the `_caiBangIconTextures` cache; the `EnsureSkillTextures()`
  method and its call in `EnsureStyles()`; and the `DrawPcTooltip` helper IF it is now unused
  (verify no other caller — keep it if shared). The `SkillContent` popup body is the single
  source of truth for skill visuals.
- [ ] **T36 — Compile-gate the overlay**: confirm no remaining reference to
  `IsSkillPanelVisible` / `CurrentSkillSnapshot` / `TryUpgradeSkill` / `DrawSkillPanelText` /
  `_skill*` / `_caiBangIconTextures` (a stray reference fails to compile).

## Phase 2.3 — Remove obsolete UXML/USS

- [ ] **T37 — Remove the `CaiBangSkillPanel` block from `Assets/UI/HUD/GameHud.uxml`**: the
  whole comment + container and its 6 named elements (`CaiBangSkillPanel`, `CaiBangSkillClose`,
  `CaiBangSkillSummary`, `CaiBangSkillList`, `CaiBangSkillPageOne`, `CaiBangSkillPageTwo`).
  Keep the PR-1 `<ui:Style Skill.uss>` link.
- [ ] **T38 — Remove all `.hud-cb-*` classes from `Assets/UI/HUD/GameHud.uss`** (the
  `/* CÁI BANG SKILL PANEL */` section): `.hud-cb-skill-panel .hud-cb-header .hud-cb-title
  .hud-cb-close .hud-cb-close-text .hud-cb-summary .hud-cb-list .hud-cb-grid-cell
  .hud-cb-grid-cell-upgradable .hud-cb-grid-cell-empty .hud-cb-grid-slot .hud-cb-grid-slot-empty
  .hud-cb-grid-level .hud-cb-add-point .hud-cb-grid-name .hud-cb-page-tab .hud-cb-page-one
  .hud-cb-page-two .hud-cb-page-tab-label .hud-cb-page-tab-active`. (Verify none are referenced
  elsewhere before deleting.)

## Phase 2.4 — Migrate tests

- [ ] **T39 — Retarget `CaiBangSkillPanelTests::HudButtonSkills_OpensCaiBangPanelWithoutTouchingPlayerVisual`**
  in `Assets/Tests/EditMode/Sandbox/CaiBangSkillPanelTests.cs`: stop using reflection on removed
  fields / `hud.OpenSkillPanel()`. Construct `new SkillContent(TestCatalogCache.NoviceAndCaiBang,
  progression, CombatFaction.CaiBang, "Cái Bang", artFolder, grantProgression: null)`, `Build(body)`,
  `OnShow()`, and assert THROUGH the content: grid `30` cells, `26` populated, PC-order skill ids
  (first `115`), skill `125` name `"Bổng Đả Ác Cẩu"`, summary `"200"`. Preserve the
  MalePlayerVisual/MalePlayerSpriteCatalog "does not touch player visual" invariant. Every other
  test in the file stays unchanged (data-service level).
- [ ] **T40 — Add `OnSkillsClick_WithoutPopupManager_DoesNotThrow` to
  `Assets/Tests/EditMode/Sandbox/GameHudControllerTests.cs`** (additive, mirrors
  `OnTeamClick_WithoutPopupManager_DoesNotThrow` / `OnFactionClick_WithoutPopupManager_DoesNotThrow`):
  assert `PopupManager.Instance` is null, invoke `OnSkillsClick`, assert no throw and no inline
  panel toggled. DO NOT touch `SetUp` (verified: it references no skill fields).

## Phase 2.5 — PR-2 verification

- [ ] **T41 — Run `run_tests mode=EditMode category_names=["Skill"]`** → `SkillContentTests` +
  the retargeted `HudButtonSkills_*` green.
- [ ] **T42 — Run `run_tests mode=EditMode category_names=["Popup","CaiBang"]`** → sibling
  popups + Cái Bang data-service/PC-parity tests green.
- [ ] **T43 — Run `run_tests mode=EditMode category_names=["!Slow"]`** (HUD + broad). Only
  pre-existing failures allowed: `Backend` / `BaLang` / `Mount` / `PcWeaponThief` /
  `InventoryService`. No NEW failures, no fixture NRE from removed skill fields.
- [ ] **T44 — Pre-push full suite gate: `run_tests mode=EditMode`** (no filter). Same
  allowed-pre-existing-failures rule. This is the final gate before push (project test-run rule).
- [ ] **T45 — Visual parity check (manual, no automated baseline):** open the skill popup in
  Play mode and screenshot before/after for the Cái Bang grid — reused art + ported geometry, 30
  cells, 26 populated, summary `200`. Confirm the 205×376 window sits in the prior 338,110 region.
- [ ] **T46 — Open PR-2.** Confirm diff = rewired `OnSkillsClick` (~+15), removed HUD fields/
  methods/props/clamp/wiring, removed `DrawSkillPanelText` + skill IMGUI assets, removed UXML
  block + `.hud-cb-*` USS, retargeted one test, +1 no-throw test. Reverting PR-2 alone restores
  the exact prior inline behavior.

**PR-2 unchecked task count: 20** (T27–T46).

---

## Data-reuse & scope guardrails (apply-time checklist)

- `SkillContent` binds/mutates ONLY via `PcSkillPanelService.BuildPage` / `TryUpgrade` /
  `PcFightSkillSlotsPerPage` / `PcFightSkillPageCount` and the `PcSkillPanelSnapshot` /
  `PcSkillPanelRow` model. No locally-computed skill description, ordering, level-cap, or
  `canUpgrade` logic.
- `PcSkillPanelService` public API + behavior unchanged. `CuiYanSkillPanelTests` /
  `KunLunSkillPanelTests` / `TianRenSkillPanelTests` untouched.
- No `CaiBang*` element name or hardcoded faction title remains in `SkillContent` (generic;
  faction-resolved by caller).
- All UI labels Vietnamese (`"Kỹ năng võ công"`, `"Đóng"` via shared chrome, etc.).
- Page count stays 1 (no re-introduced 2-tab UI); 30-slot single scrollable page.

---

## Follow-up (NOT this change)

- **Visual regression baseline:** add an automated screenshot/diff harness for popup bodies so
  the IMGUI→UIToolkit grid move has a parity gate (currently manual only).
- **Multi-page tabs:** `PcFightSkillPageCount == 1` today; if a faction ever exceeds 30 skills,
  re-introduce paged navigation inside `SkillContent` (out of scope here).
- **Skill hotbar / drag-to-slot assignment:** separate feature; `SkillContent` exposes selection
  but does not assign slots.
- **Remaining inline panels:** Trade / Stall / Face picker still inline — independent migrations,
  same `IPopupContent` pattern.
- **Detail-region fit:** if the 205-wide body proves cramped for the detail at apply, the only
  tolerated adjustment per design D3 is bumping `Height→~430` and lowering `Top`; width/left stay
  205/338.
- **Reconcile faction-id scheme:** `FactionContent` note about `CombatFaction` vs `PartyService`
  faction-id ordering — unrelated to this change but worth a follow-up audit.
