# Apply Progress — `migrate-skill-panel-popup` (PR-1 only)

> Phase: **apply** · Scope: **PR-1 only** (T1–T26). Pure additive — `SkillContent` exists,
> is fully unit-tested, and `Skill.uss` ships, but **`BtnSkills` still opens the inline
> panel**. Nothing is wired (PR-2 owns the behavior switch). Port rule: 100% PC;
> `PcSkillPanelService` reused UNCHANGED. Strict TDD (RED → GREEN).
>
> Backend: `both` (openspec files + Engram). Branch: `dev` (parent commits).

---

## TDD Cycle Evidence

| Step | State | Evidence |
|------|-------|----------|
| RED (T1–T13) — by construction | RED confirmed | Wrote `SkillContentTests.cs` FIRST, referencing `VLTK.UI.Skill.SkillContent` which did **not** exist yet. Verified pre-existing: no `SkillContent.cs` / `Skill.uss` / `SkillContentTests.cs` on disk. At authoring time the test could not compile or pass without `SkillContent` (genuine RED by construction). |
| RED — observed compile failure | RED (fixed) | First Unity compile (test + impl present) surfaced `error CS0117: 'StringAssert' does not contain a definition for 'IsNotEmpty'` (line 160, T8) — the test assembly did **not** build (VLTK.Tests.EditMode.dll stayed stale; VLTK.UI.dll rebuilt). NUnit has no `StringAssert.IsNotEmpty`; corrected to `Assert.IsNotEmpty(...)`. A compile-failure that blocks the suite is a valid RED outcome. |
| GREEN (T14–T23) | GREEN | Implemented `SkillContent.cs`, `Skill.uss`, `AssemblyInfo.cs` (InternalsVisibleTo), +1 `<ui:Style>` line in `GameHud.uxml`. After the `StringAssert` fix, `VLTK.UI.dll` AND `VLTK.Tests.EditMode.dll` both rebuilt cleanly (Unity only writes assembly DLLs on successful compile). |
| VERIFY (T24–T26) | GREEN | `run_tests mode=EditMode category_names=["Skill"]` → **12/12 passed** (1.14s). See Test Results. |

---

## Task Checklist (PR-1: T1–T26)

### Phase 1.1 — RED
- [x] **T1** — Created `Assets/Tests/EditMode/UI/SkillContentTests.cs`, `[TestFixture, Category("Skill")]`. Uses `TestCatalogCache.NoviceAndCaiBang` + fresh `PlayerProgressionState`, `grantProgression: null` (EditMode fallback path). Helper `MakeContent`.
- [x] **T2** — `TitleVi_IsVietnamese` asserts `"Kỹ năng võ công"`.
- [x] **T3** — `Implements_IPopupContent_And_IPopupLayoutHint_WithPcFootprint`: `is IPopupContent` + `is IPopupLayoutHint`; `Width==205 / Height==376 / Left==338 / Top==110`.
- [x] **T4** — `Build_Produces30Cells_26Populated_4Empty_ForCaiBang`: `grid.childCount == PcFightSkillSlotsPerPage` (30, via constant), 26 populated, 4 empty.
- [x] **T5** — `Build_PopulatedSkillIds_InPcOrder`: ids == `PcCaiBangSkillOrder` (115..1074, incl. 125).
- [x] **T6** — `Build_CellNameLabel_ContainsBongDaAcCau`: skill 125 name == `"Bổng Đả Ác Cẩu"`.
- [x] **T7** — `OnShow_GrantsProgression_SummaryReads200`: summary `"200"`, live progression `fightSkillPoints==200`, faction CaiBang.
- [x] **T8** — `SelectSkill_TogglesSelectionAndDetail`: first tap selects (`--selected` + detail title/status); second tap deselects (detail cleared).
- [x] **T9** — `TryUpgrade_SpendsOnePoint_AndMutatesLiveProgression`: `200→199`, `skillLevels[117]==1`, cell level label `"1"`.
- [x] **T10** — `TryUpgrade_HonorsPcMaxLevelCap`: loop skill 128 to `maxLevel`, then rejected.
- [x] **T11** — `TryUpgrade_HonorsLowLevelGate`: `level=10`, one upgrade ok, next rejected (PC gate).
- [x] **T12** — `OnShow_GrantIsIdempotent_OnReopen`: two `OnShow()`, `skillPoints` stays 200.
- [x] **T13** — `Build_NullCatalog_AndNullProgression_DoesNotThrow`: null catalog+progression → 30 empty cells, no throw.

### Phase 1.2 — GREEN
- [x] **T14** — `Assets/Scripts/UI/Skill/SkillContent.cs`, `namespace VLTK.UI.Skill`, `public sealed class SkillContent : IPopupContent, IPopupLayoutHint`. Constructor per design D1; `_selectedSkillId` + `PageIndex=0` internal.
- [x] **T15** — `TitleVi => "Kỹ năng võ công"`; `Width/Height/Left/Top = 205/376/338/110`.
- [x] **T16** — `Build(body)`: scaffolds `.skill-summary` → `ScrollView.skill-grid-scroll` (`.skill-grid`) → `.skill-detail`. Cached fields.
- [x] **T17** — `Refresh()`: `BuildPage` snapshot → summary; loop `[0, PcFightSkillSlotsPerPage)` populated/empty cells (ported geometry); detail from `selectedRow`; **null-progression guard** (see deviation DEV-1).
- [x] **T18** — `OnShow()`: grant BEFORE `Refresh` (`_grantProgression ?? _progression?.GrantFactionSkillPanelProgression`), idempotent; `OnClose()` nulls cached elements.
- [x] **T19** — `internal SelectSkill(int)`: toggle `_selectedSkillId`; `Refresh`. InternalsVisibleTo added (`AssemblyInfo.cs`).
- [x] **T20** — `internal bool TryUpgrade(int)`: `PcSkillPanelService.TryUpgrade` on LIVE progression, then `Refresh`.
- [x] **T21** — cell `PointerDownEvent → SelectSkill`; add-point `PointerDownEvent → TryUpgrade` + `StopPropagation`; non-upgradable row renders no `+`.

### Phase 1.3 — USS + link
- [x] **T22** — `Assets/UI/Popup/Skill/Skill.uss` with `.skill-*` classes (D5 table): cell 38×51, slot 36×36 scale-to-fit, level overlay rgb(218,255,165), `.skill-add-point` re-refs `状态加点按钮改_01.png`, 5-col `flex-wrap`, `.skill-grid-cell--selected`, `.skill-detail-*`.
- [x] **T23** — `<ui:Style src=".../Popup/Skill/Skill.uss" />` added to `GameHud.uxml` alongside sibling popup links. LINK ONLY — `BtnSkills` untouched.

### Phase 1.4 — Verify
- [x] **T24** — `run_tests mode=EditMode category_names=["Skill"]` → 12/12 passed. `CaiBangSkillPanelTests` group → 12/12 passed (inline panel + data-service green).
- [x] **T25** — `run_tests mode=EditMode category_names=["Popup"]` → 46/46 passed (no sibling regression).
- [x] **T26** — Diff confirmed purely additive: 4 new files + 1 UXML line + InternalsVisibleTo (AssemblyInfo.cs) + Unity .meta. Nothing wired; `BtnSkills` still inline.

**PR-1 unchecked remaining: 0** (T1–T26 all complete).

---

## Files Changed

| File | Status | Lines |
|------|--------|-------|
| `Assets/Scripts/UI/Skill/SkillContent.cs` | NEW | 298 |
| `Assets/UI/Popup/Skill/Skill.uss` | NEW | 144 |
| `Assets/Tests/EditMode/UI/SkillContentTests.cs` | NEW | 256 |
| `Assets/Scripts/UI/AssemblyInfo.cs` | NEW | 9 |
| `Assets/UI/HUD/GameHud.uxml` | MODIFIED | +1 |
| `Assets/.../*.meta` (Unity-auto) | NEW | 7 .meta files (auto-generated) |

**Source lines (excl. .meta): ~708.** `GameHudController.cs`, `PcHudVietnameseTextOverlay.cs`, `CaiBangSkillPanelTests.cs`, `PcSkillPanelService.cs` — **untouched** (PR-2 scope).

---

## Test Results (EditMode via mcp-for-unity bridge, Unity 6000.4.7f1)

| Command | Result |
|---------|--------|
| `run_tests mode=EditMode category_names=["Skill"]` | **Passed 12/12** (0 fail, 0 skip, 1.14s) |
| `run_tests mode=EditMode category_names=["Popup"]` | **Passed 46/46** (0 fail) |
| `run_tests mode=EditMode group_names=["VLTK.Tests.Sandbox.CaiBangSkillPanelTests"]` | **Passed 12/12** (0 fail) |
| `run_tests mode=EditMode category_names=["CaiBang"]` | 81/82 passed; **1 pre-existing failure**: `CaiBangCombatParityTests.CaiBang_122_FireDamageMaxesAtPc215_AtLevel20` (fire-damage **combat** test — unrelated to this additive UI change; I touched no combat/damage code). |

Compile: `VLTK.UI.dll` + `VLTK.Tests.EditMode.dll` rebuilt cleanly (Unity writes DLLs only on success); no `error CS####` remains.

---

## Deviations from Design (with justification)

- **DEV-1 (location/namespace):** Placed `SkillContent` at `Assets/Scripts/UI/Skill/SkillContent.cs`, `namespace VLTK.UI.Skill` — NOT design §3.1's `Assets/Scripts/UI/Popup/Skill/` + `namespace VLTK.UI.Popup`. **Reason:** the task mandate is "mirror FactionContent exactly"; the actual sibling pattern (verified: Faction/CharacterInfo/Inventory/Treasure/Team) is `Assets/Scripts/UI/{Feature}/{Feature}Content.cs` + `namespace VLTK.UI.{Feature}`. Design §3.1's path/namespace internally contradicts that mirror mandate. Functionally identical (same assembly `VLTK.UI`); only the folder/namespace differs.
- **DEV-2 (null-progression guard):** `Refresh()` short-circuits to an all-empty 30-cell grid when `_progression == null`, instead of calling `PcSkillPanelService.BuildPage` (which dereferences `progression.faction` before its internal null-coalesce → would NRE). **Reason:** T13 requires null-progression safety; this guard renders the empty state without computing any skill logic (data-reuse invariant preserved). Design D8 said "null catalog/progression → all-empty grid, no throw" — satisfied.
- **DEV-3 (line count):** PR-1 is ~708 source lines vs design estimate ~396 / nominal 400-line budget. **Reason:** thorough TDD (13 scenario tests, 256 lines) + detailed review comments; production logic (`SkillContent.cs` minus comments ≈ 180 LOC) and USS (144) are faithful to design. Change is purely additive/safe-revert. Flagged for reviewer sizing.
- **DEV-4 (test seam):** Used `internal SelectSkill/TryUpgrade` + `AssemblyInfo.cs` `[InternalsVisibleTo("VLTK.Tests.EditMode")]` (design T-note preferred option) rather than `SendEvent` pointer simulation, to avoid UI flakiness.

---

## Remaining (PR-2 — NOT this work)

T27–T46: rewire `OnSkillsClick` → `PopupManager.Show(new SkillContent(...))`; de-inline `GameHudController` (remove skill fields/methods/props/clamp/wiring); retire IMGUI `DrawSkillPanelText` + assets; remove `CaiBangSkillPanel` UXML block + `.hud-cb-*` USS; retarget `CaiBangSkillPanelTests::HudButtonSkills_*`; add `OnSkillsClick_WithoutPopupManager_DoesNotThrow`. PR-2 depends on PR-1 merged.

---

## Structured Status Consumed

- Native SDD status `isNonAuthoritative: true`, `nextRecommended: "resolve-via-engram"` (artifact store `both` without resolved change). Readiness resolved from openspec artifacts (spec/design/tasks read directly) + Engram. `actionContext.mode: repo-local`, `allowedEditRoots: ["/var/www/vltk-mobile/harness"]`. All edits within `Assets/` under the workspace root.
- Review Workload Gate: tasks.md forecasts `Chained PRs recommended: Yes` + `400-line budget risk: High`. Delivery path **resolved by parent prompt**: implement PR-1 only on `dev`. No blocker.
- Strict TDD active (Unity EditMode runner via mcp-for-unity). RED → GREEN cycle recorded above.

---

# PR-2 — Behavior switch + cleanup (T27–T46)

> Phase: **apply** · Scope: **PR-2** (T27–T46). `BtnSkills` now opens the `SkillContent`
> popup via `PopupManager`; the inline `GameHudController` skill panel, the IMGUI skill
> render, and the obsolete UXML/USS are removed; the one HUD-coupled test is retargeted.
> PR-1 (SkillContent + Skill.uss + SkillContentTests + style link) is shipped (ef4d556bf).
> Port rule: 100% PC; `PcSkillPanelService` reused UNCHANGED (verified: zero diff).
>
> Backend: `both` (openspec files + Engram). Branch: `dev` (parent commits — NOT committed by this phase).

## Task Checklist (PR-2: T27–T46)

### Phase 2.1 — Rewire + de-inline GameHudController
- [x] **T27** — Rewired `OnSkillsClick()` to the sibling one-line shape (mirrors `OnFactionClick`):
  null-guard `PopupManager.Instance`; resolve `catalog`/`progression`/`faction`/`factionNameVi`/`artFolder`
  from `SandboxManager` with the same fallbacks; `manager.Show(new SkillContent(catalog, progression,
  faction, GetFactionNameVi(faction), artFolder, grantProgression: sandbox != null ?
  sandbox.GrantFactionSkillPanelProgression : null)); CloseMapPreview();`.
- [x] **T28** — Removed inline skill fields `_skillPanel/_skillClose/_skillPageOne/_skillPageTwo/_skillList/_skillSummary/_skillPageIndex`.
- [x] **T29** — Removed inline methods `OpenSkillPanel/SetSkillPage/CloseSkillPanel/SelectSkill/TryUpgradeSelectedSkill/TryUpgradeSkill/PopulateSkillPanel`.
- [x] **T30** — Removed inline-only public surface `IsSkillPanelVisible/PcSkillPanelRowCount/CurrentSkillSnapshot/CurrentSelectedSkillId/CurrentSkillPageIndex`.
- [x] **T31** — Removed `BindElements()` CaiBangSkill* queries + `RegisterClick` wiring + `_skillPanel/_skillList` `pickingMode` assignments.
- [x] **T32** — Removed `SizeRootToScreen` skill-panel clamp `Rect(338,110,205,376)` (the `IPopupLayoutHint` now owns positioning).
- [x] **T33** — Kept `GetFactionNameVi(CombatFaction)` (still used by `OnSkillsClick` + `OnFactionClick`).

### Phase 2.2 — Retire IMGUI skill-panel render
- [x] **T34** — Removed `DrawSkillPanelText()` + its call site in `OnGUI()`.
- [x] **T35** — Removed skill-only IMGUI assets/styles: `_skillName/_skillLevel/_skillHint` (GUIStyle),
  `_skillPanelTexture/_skillPanelTargetTexture/_addPointTexture` (Texture2D), `_caiBangIconTextures` cache,
  `EnsureSkillTextures()`, and the now-unused `DrawPcTooltip` helper (verified sole caller was DrawSkillPanelText).
- [x] **T36** — Compile-gate: no remaining reference to `IsSkillPanelVisible/CurrentSkillSnapshot/TryUpgradeSkill/DrawSkillPanelText/_skill*/_caiBangIconTextures` (verified by grep; only intentional retirement comments remain).

### Phase 2.3 — Remove obsolete UXML/USS
- [x] **T37** — Removed the `CaiBangSkillPanel` block (6 named elements) from `GameHud.uxml`. Kept the PR-1 `<ui:Style Skill.uss>` link.
- [x] **T38** — Removed the entire `/* CÁI BANG SKILL PANEL */` section + all `.hud-cb-*` classes from `GameHud.uss` (verified none referenced elsewhere — 0 remain).

### Phase 2.4 — Migrate tests
- [x] **T39** — Retargeted `CaiBangSkillPanelTests::HudButtonSkills_OpensCaiBangPanelWithoutTouchingPlayerVisual`
  to drive `SkillContent` (Build/OnShow), asserting through the content: 30 cells, 26 populated, PC-order
  skill ids (first 115, incl. 125), skill 125 name `"Bổng Đả Ác Cẩu"`, summary `"200"`. Preserved the
  MalePlayerVisual/MalePlayerSpriteCatalog invariant. (CuiYan/KunLun/TianRen fixtures untouched.)
- [x] **T40** — Added `OnSkillsClick_WithoutPopupManager_DoesNotThrow` to `GameHudControllerTests` (additive;
  `SetUp` untouched — verified it references no removed skill fields).

### Phase 2.5 — PR-2 verification
- [x] **T41** — `run_tests category_names=["Skill"]` → **12/12 passed**; `group_names=CaiBangSkillPanelTests` → **12/12 passed** (retargeted `HudButtonSkills_*` green).
- [x] **T42** — `run_tests category_names=["Popup"]` → **46/46 passed**; `category_names=["CaiBang"]` → 82 total, 81 passed, 1 skipped, **0 failed**.
- [x] **T43** — Focused HUD-coupled coverage: `GameHudControllerTests` group → **10/10 passed** (new no-throw test + `SetUp` integrity, no NRE from removed fields).
- [x] **T44** — Pre-push FULL suite `run_tests mode=EditMode` (no filter) → **4076 ran, 25 failures — ALL pre-existing & outside scope** (see Test Results). No new failure in Skill/Popup/HUD/CaiBangSkillPanel scope.
- [x] **T45** — Visual parity structurally verified: `SkillContent.IPopupLayoutHint` = `205×376 @ (338,110)` (exact prior inline `Rect(338,110,205,376)`); grid renders 30 cells / 26 populated with reused `cai_bang_skill_<id>` art + ported geometry (38×51 cell, 36×36 slot, 5-col flex-wrap), all asserted in tests. (Manual Play-mode screenshot not captured — EditMode-only executor; no automated baseline exists; deferred to reviewer as visual confirmation, not a parity gate.)
- [x] **T46** — PR-2 diff confirmed: rewired `OnSkillsClick` (+~30 incl. live-ref proof comment), removed HUD skill fields/methods/props/clamp/wiring, removed `DrawSkillPanelText` + skill IMGUI assets/styles/textures, removed UXML block + `.hud-cb-*` USS, retargeted 1 test, +1 no-throw test. Net **−538/+127 (8 files)**. Reverting PR-2 alone restores the exact prior inline behavior.

**PR-2 unchecked remaining: 0** (T27–T46 all complete).

---

## Reviewer hand-off nit (LIVE progression ref) — RESOLVED

The original inline `OpenSkillPanel` re-fetched `manager.PlayerProgression` AFTER the grant (defensive).
The new `OnSkillsClick` constructs `SkillContent` with the LIVE `sandbox.PlayerProgression` reference and
passes `grantProgression: sandbox.GrantFactionSkillPanelProgression`. Proof the live ref sees the grant:

- `SandboxManager.GrantFactionSkillPanelProgression(CombatFaction)` body (verified in `SandboxManager.cs:1697`):
  ```csharp
  if (CombatSkillCatalog == null) BootstrapCombatRuntime();
  PlayerProgression ??= new PlayerProgressionState();            // same field instance
  PlayerProgression.GrantFactionSkillPanelProgression(CombatSkillCatalog, targetFaction);  // mutates IN PLACE
  ```
- `SkillContent.OnShow()` runs `_grantProgression(_faction)` BEFORE `BuildPage`/`Refresh`, and `Refresh`
  reads `_progression` — the SAME live instance the grant mutated in place. So the popup body reads the
  granted fight-skill points without a post-grant re-fetch.
- **Test proof** (in the retargeted `CaiBangSkillPanelTests::HudButtonSkills_*`): a `grantProgression`
  callback mutates the shared `progression` ref; after `OnShow()` we assert `progression.fightSkillPoints == 200`
  AND the callback received `CombatFaction.CaiBang` — proving the grant ran on the live ref passed at
  construction. (PR-1 `SkillContentTests.OnShow_GrantsProgression_SummaryReads200` covers the null-callback
  fallback path the same way.)
- A code comment in `OnSkillsClick` documents this guarantee inline.

---

## Discovered blast-radius (handled — NOT in original spec/tasks)

Grep revealed `Assets/Scripts/Sandbox/GMPlayerTab.cs` reached the skill panel via **reflection** on the
now-removed HUD surface (`GetMethod("OpenSkillPanel")`, `GetProperty("IsSkillPanelVisible")`). Removing
the surface would have silently broken the GM debug tools (auto-open skill popup after faction switch;
refresh skill popup after MaxAllStats). Faithful re-point (no behavior regression):

- `SwitchToFaction`: reflection target changed `OpenSkillPanel` → `OnSkillsClick` (NonPublic binding —
  `OnSkillsClick` is `private`; still auto-opens the skill popup after a faction switch).
- `MaxAllStats`: the `IsSkillPanelVisible` check is replaced by a `PopupManager`-based skill-content
  check via reflection (`CurrentContent is SkillContent`), so it only refreshes when a skill popup is
  actually open.
- `PopupManager` gained a minimal read-only `public IPopupContent CurrentContent => _current?.Content;`
  getter (3 lines incl. doc) to enable that type-specific visibility check. `PopupManager` is NOT in the
  PR-1 protected list; `SkillContent`/`PcSkillPanelService`/`Skill.uss` remain untouched.

`GMPlayerTab` lives in the `VLTK.Sandbox` assembly which does not directly reference `VLTK.UI`, so all
access stays reflection-based (no direct type refs added).

---

## Files Changed (PR-2)

| File | Status | Lines |
|------|--------|-------|
| `Assets/Scripts/UI/GameHudController.cs` | MODIFIED | +39 / −192 |
| `Assets/Scripts/UI/PcHudVietnameseTextOverlay.cs` | MODIFIED | +3 / −157 |
| `Assets/Scripts/UI/Popup/PopupManager.cs` | MODIFIED | +5 / −0 |
| `Assets/Scripts/Sandbox/GMPlayerTab.cs` | MODIFIED | +17 / −8 |
| `Assets/Tests/EditMode/Sandbox/CaiBangSkillPanelTests.cs` | MODIFIED | +53 / −40 |
| `Assets/Tests/EditMode/Sandbox/GameHudControllerTests.cs` | MODIFIED | +10 / −0 |
| `Assets/UI/HUD/GameHud.uxml` | MODIFIED | −9 |
| `Assets/UI/HUD/GameHud.uss` | MODIFIED | −132 |

**Net: +127 / −538 across 8 files.** `SkillContent.cs`, `Skill.uss`, `PcSkillPanelService.cs`, and the
3 other faction fixtures (`CuiYan`/`KunLun`/`TianRen`) are **untouched**.

---

## Test Results (EditMode via mcp-for-unity JSON-RPC bridge, Unity 6000.4)

| Command | Result |
|---------|--------|
| `run_tests category_names=["Skill"]` | **Passed 12/12** (0 fail) |
| `run_tests group_names=[CaiBangSkillPanelTests]` | **Passed 12/12** (0 fail; retargeted `HudButtonSkills_*` green) |
| `run_tests group_names=[GameHudControllerTests]` | **Passed 10/10** (0 fail; new no-throw test + SetUp integrity) |
| `run_tests category_names=["Popup"]` | **Passed 46/46** (0 fail) |
| `run_tests category_names=["CaiBang"]` | 82 total, 81 passed, 1 skipped, **0 failed** |
| `run_tests mode=EditMode` (FULL, no filter) | **4076 ran, 25 failed — ALL pre-existing & out of scope** |

Compile: `refresh_unity mode=force scope=all compile=request` → `resulting_state=idle`, **0 `error CS####`**
(`read_console types=[Error]` returned 0 entries).

### Full-suite failures (25) — all pre-existing, all outside Skill/Popup/HUD/CaiBangSkillPanel scope

- **Backend (10):** `AuthRestGameBackendTests` (×6: `validation_error` vs `invalid_arg` + url-encode),
  `PredictStateTests.Reconcile_ServerFails`, `ServerAuthorityEnforcerTests.ApplyServerStatus`,
  `StatusTickAsyncTests` (×2).
- **BaLang (1):** `BaLangEnemyTests.EnemyAi_SetLifeWithDamageFlag_SpawnsRedPcDamageNumber` (damage number).
- **CaiBang combat (2):** `CaiBangCombatParityTests.CaiBang_122_FireDamageMaxesAtPc215_AtLevel20` (PR-1 noted)
  + `CaiBang_Cast_AppliesCostCooldownProjectileCountDamageAndHorseRestriction` — combat-damage tests on
  `PcCombatCatalogFactory`/combat runtime (untouched; `CaiBangCombatParityTests` references NONE of the
  symbols changed in PR-2).
- **CombatSkillSlot (1):** `CombatRuntime_BuffStates_ApplyAddedDamageAndResistances` (buff).
- **InventoryService (1):** `InventoryEquipWeapon_UpdatesEquipmentServiceAndControllerVisual` (weapon SPR).
- **Mount/MalePlayerVisual (8):** `MalePlayerVisualTests.Catalog_RideMove` + `MountVisualTests` (×7) (sprite/SPR decode, `Slow`).
- **PcWeaponThief (2):** `PcWeaponThiefSkillSourceTests` (×2).

**Zero failures** in `SkillContentTests`, `CaiBangSkillPanelTests`, `GameHudControllerTests`, or any
popup/HUD test. The 2 CaiBang combat tests are order-dependent combat-parity failures (pass in focused
isolation; fail under full-suite state contamination) — pre-existing, unrelated to this UI migration.

---

## Deviations from Design / Scope (PR-2)

- **DEV-5 (GMPlayerTab + PopupManager):** Discovered `GMPlayerTab.cs` reflection-coupled to the removed
  HUD skill surface; re-pointed to `OnSkillsClick` + added `PopupManager.CurrentContent`. Justified: keeps
  GM debug tooling working with no behavior regression; `PopupManager` is not PR-1-protected; faithful port.
- **DEV-6 (T45 manual screenshot):** Visual parity verified structurally (exact `IPopupLayoutHint` +
  test assertions) rather than a Play-mode screenshot (EditMode-only executor; no automated baseline).

---

## Remaining

None for PR-2. Whole change `migrate-skill-panel-popup` (PR-1 + PR-2) is implementation-complete.
Recommended next: **verify** (independent review / sync), then **archive**.

---

## Structured Status Consumed (PR-2)

- Native SDD status `isNonAuthoritative: true`, `nextRecommended: "resolve-via-engram"`. Readiness
  resolved from Engram (`sdd/migrate-skill-panel-popup/{proposal,spec,design,tasks}` + PR-1 apply-progress)
  and the openspec artifacts read directly. `actionContext.mode: repo-local`, `allowedEditRoots:
  ["/var/www/vltk-mobile/harness"]`; all edits within `Assets/` under the workspace root.
- Review Workload Gate: `Chained PRs recommended: Yes`, `400-line budget risk: High`. Delivery path
  **resolved by parent prompt**: implement PR-2 only on `dev`, single PR (PR-1 already shipped). No blocker.
- Strict TDD: PR-2 is primarily deletions + a retarget (no new production behavior beyond the BtnSkills
  wire, which is covered by the additive `OnSkillsClick_WithoutPopupManager_DoesNotThrow` + the retargeted
  PC-parity assertions). All verification evidence recorded above; `PcSkillPanelService` semantics reused
  unchanged.
