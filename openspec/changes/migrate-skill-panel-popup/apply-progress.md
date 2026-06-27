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
