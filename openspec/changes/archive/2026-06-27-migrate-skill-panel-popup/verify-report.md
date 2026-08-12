# Verify Report — `migrate-skill-panel-popup`

> Phase: **verify** · Independent read-only review of a **GAMEPLAY-TOUCHING** change (mutates live
> `PlayerProgressionState` via fight-skill-point spend + grants faction skill-panel progression).
> Backend: `both` (openspec + Engram). Verification is static (read code + artifacts + parent-cited
> test evidence); Unity was NOT rerun by this executor (parent collected the run evidence — accepted
> and cited). Port rule respected: `PcSkillPanelService` reused unchanged.

**Verdict: PASS** — all 11 requirements satisfied against code; 47/47 tasks checked; zero stray
references; gameplay-critical grant-before-BuildPage ordering + live-ref + idempotency confirmed in
code AND asserted by tests.

---

## 1. Structured status & action context findings

- Native SDD status `isNonAuthoritative: true`, `nextRecommended: "resolve-via-engram"`, change
  unresolvable in the native JSON (artifact store `both` without a resolved `changeName`).
  Readiness resolved directly from the openspec artifacts (proposal/spec/design/tasks/apply-progress
  read from `openspec/changes/migrate-skill-panel-popup/`). No blocker.
- `actionContext.mode: repo-local`; `allowedEditRoots: ["/var/www/vltk-mobile/harness"]`. All
  implementation edits live under `/var/www/vltk-mobile/Assets/` (the workspace root / Unity project),
  proven inside the authoritative workspace. Verify writes only to
  `openspec/changes/migrate-skill-panel-popup/verify-report.md` (read-only contract honored — no
  implementation code edited).
- Shipped commits verified present in `git log`: `ef4d556bf` (PR-1), `fe5a77cd7` (PR-2),
  `99be0e88c` (docs follow-up). Working tree has **no staged files** and only one untracked
  `vltk-mobile.slnx` (IDE solution file, unrelated).

---

## 2. Requirement coverage map (11 REQ — all PASS)

| # | Requirement | Status | Evidence |
|---|-------------|--------|----------|
| 1 | SkillContent popup-body contract | **PASS** | `SkillContent.cs`: `public sealed class SkillContent : IPopupContent, IPopupLayoutHint` (l.34); `TitleVi => "Kỹ năng võ công"` (l.41); constructor injects catalog/progression/faction/factionNameVi/artFolder/grantProgression (DI mirrors `FactionContent`); `Build(body)` renders summary+grid+detail in UIToolkit; body is faction-generic (no per-faction branch). Tests T2/T3. |
| 2 | 30-cell grid, single scrollable page | **PASS** | `Refresh()` loops `slotIndex in [0, PcSkillPanelService.PcFightSkillSlotsPerPage)`; populated=`snap.rows.Count`, rest empty; `PageIndex=0` const (`PcFightSkillPageCount==1`); no 2-tab UI. Slot count sourced from the service constant, not a literal. Tests T4/T5 (30 cells, 26 populated Cái Bang, 4 empty). |
| 3 | Skill-point summary display | **PASS** | `_summary.text = snap.skillPoints.ToString(...)` in `Refresh`; re-renders after grant/upgrade. Test T7 (summary `"200"` after `OnShow`). |
| 4 | Selection detail toggle | **PASS** | `SelectSkill` toggle `_selectedSkillId == skillId ? 0 : skillId`; detail derived entirely from `selectedRow` (displayName, `learnedLevel/maxLevel`, summary, nextLevelSummary, upgradeStatus); cleared when none selected. Tests T8 (select+deselect). |
| 5 | Upgrade mutates live progression | **PASS** | `+` only rendered when `row.canUpgrade`; `TryUpgrade(int)` → `PcSkillPanelService.TryUpgrade(_progression, _catalog, skillId)` on the LIVE ref, then `Refresh()`; `+` callback calls `StopPropagation()`. Non-upgradable rows render no `+`. Tests T9 (200→199, level+1), T10 (max-level cap), T11 (low-level gate). |
| 6 | Data-reuse invariant (no duplication) | **PASS** | All grid/detail text comes from `PcSkillPanelRow` produced by `BuildPage`; no locally-computed ordering/description/level-cap/canUpgrade. **`PcSkillPanelService.cs` diff between `ef4d556bf^..fe5a77cd7` is empty** (untouched). |
| 7 | Progression-grant preservation on open **(gameplay-critical)** | **PASS** | `OnShow()` runs the grant BEFORE `Refresh`/`BuildPage`. Runtime: `_grantProgression(_faction)` = `SandboxManager.GrantFactionSkillPanelProgression` (verified `SandboxManager.cs:1697`: mutates `PlayerProgression` IN PLACE). EditMode fallback: `_progression?.GrantFactionSkillPanelProgression(_catalog, _faction)` (exists `PlayerProgressionService.cs:61`). Faction resolved in `OnSkillsClick` (CaiBang when `None`). **Live-ref proof:** `OnSkillsClick` captures `sandbox.PlayerProgression`; the grant mutates that SAME instance, so the body reads granted points without a re-fetch (see §4). Idempotent: tests T12 (fallback path) + retargeted `HudButtonSkills_*` (callback path) both assert `skillPoints==200` across two `OnShow()`. |
| 8 | Popup layout hint PC-footprint | **PASS** | `Width=205 / Height=376 / Left=338 / Top=110` (strict parity with prior inline `Rect(338,110,205,376)`). Test T3. |
| 9 | BtnSkills wiring via PopupManager | **PASS** | `OnSkillsClick` → `manager.Show(new SkillContent(...))`, same one-line shape as `OnFactionClick`/`OnStatusClick`; null-`PopupManager` guard (log+return); `CloseMapPreview()`. Test `OnSkillsClick_WithoutPopupManager_DoesNotThrow`. |
| 10 | GameHudController de-inlining | **PASS** | All 7 skill fields, 7 methods, 5 public props absent (read full controller, incl. tail l.1177–1300); `BindElements` has no `CaiBangSkill*` query and no `RegisterClick` for those; `SizeRootToScreen` has no skill clamp. No `CaiBang` skill reference anywhere in the controller. `GameHud.uxml` `CaiBangSkillPanel` block fully removed (0 matches); `.hud-cb-*` USS fully removed (0 matches in `GameHud.uss`). |
| 11 | IMGUI skill-panel render retirement + test migration | **PASS** | `DrawSkillPanelText` + `OnGUI` call site + `EnsureSkillTextures` + skill GUIStyles/textures + `_caiBangIconTextures` + `DrawPcTooltip` all absent; only an explanatory comment remains. `grep` over `Assets/Scripts` + `Assets/Tests`: **zero** stray references to `IsSkillPanelVisible`/`CurrentSkillSnapshot`/`TryUpgradeSkill`/`DrawSkillPanelText`/`_skill*`/`_caiBangIconTextures`. `SkillContentTests.cs` `[Category("Skill")]` RED-first (13 tests); `CaiBangSkillPanelTests::HudButtonSkills_*` retargeted to drive `SkillContent`, PC-parity assertions preserved (30 cells, 26 rows, ids, `"Bổng Đả Ác Cẩu"`, `"200"`); `GameHudControllerTests` SetUp untouched + additive no-throw test; `PcSkillPanelService` tests untouched. |

**Aggregate: 11/11 PASS.**

---

## 3. Task completion status

- `tasks.md` unchecked `- [ ]` implementation-task scan: **ZERO unchecked** (`grep -nE '^\s*- \[ \]'`
  → no matches). 47/47 tasks checked (T1–T46; T-note folded into implementation).
- Review-workload forecast honored: tasks.md forecast `Chained PRs recommended: Yes`,
  `Chain strategy: feature-branch-chain`, split at the **wiring boundary**. Confirmed delivered as
  PR-1 (`ef4d556bf`, additive) → PR-2 (`fe5a77cd7`, behavior switch + cleanup). The returned PR
  boundary matches the forecast exactly. `size:exception` was NOT used; each PR stays within budget
  (PR-1 additive ~708 LOC flagged as DEV-3; PR-2 net −538/+127).
- No scope creep beyond assigned tasks. The 10 faction skill-panel fixtures vs. the 4 named in the
  spec is **pre-existing organic growth** (extra factions ported earlier as pure data-service tests),
  NOT new scope: only `CaiBangSkillPanelTests` (the sole HUD-coupled fixture) was retargeted;
  `CuiYan`/`KunLun`/`TianRen` + `Shaolin`/`EMei`/`TangMen`/`WuDang`/`TianWang`/`WuDu` are untouched
  pure data-service tests (`grep` confirms none reference the removed HUD surface).

---

## 4. Gameplay-critical deep-check (live progression ref + grant ordering)

The parent flagged this as gameplay-touching; verified explicitly in code:

1. **Grant-before-BuildPage ordering** — `SkillContent.OnShow()` (l.103–105):
   `if (_grantProgression != null) _grantProgression(_faction); else _progression?.Grant…; Refresh();`
   `Refresh()` is the only caller of `BuildPage`. Order is guaranteed (no async, no early return
   between grant and `Refresh`).
2. **Live-ref identity** — `OnSkillsClick` captures `progression = sandbox.PlayerProgression` (the
   live field) and passes it to `SkillContent`. The runtime grant `_grantProgression` is
   `sandbox.GrantFactionSkillPanelProgression`, whose body (`SandboxManager.cs:1697–1702`) does
   `PlayerProgression ??= new PlayerProgressionState(); PlayerProgression.Grant…(catalog, faction);`
   — mutating the SAME instance the content holds. So the popup reads granted points without a
   post-grant re-fetch (the prior inline code re-fetched defensively; the in-place mutation makes
   that unnecessary — documented inline in `OnSkillsClick`).
3. **Idempotency** — `OnShow` is re-invoked on every open; `GrantFactionSkillPanelProgression` is
   idempotent (asserted by `ReopeningPanelProgression_*` + `OnShow_GrantIsIdempotent_OnReopen`).
4. **Test proof of the live ref** — retargeted `HudButtonSkills_*` passes a `grantProgression`
   callback that mutates the shared `progression` ref and records the resolved faction; after
   `OnShow()` it asserts `progression.fightSkillPoints == 200` AND the callback received
   `CombatFaction.CaiBang` — proving grant ran on the live ref passed at construction.

**No gameplay regression found.**

---

## 5. Test / validation commands (parent-collected; accepted and cited)

Unity EditMode runs (mcp-for-unity bridge, Unity 6000.4) — recorded in `apply-progress.md`:

| Command | Result | Scope |
|---------|--------|-------|
| `run_tests category_names=["Skill"]` | **12/12 passed** | `SkillContentTests` |
| `run_tests group_names=[CaiBangSkillPanelTests]` | **12/12 passed** | retargeted `HudButtonSkills_*` green |
| `run_tests group_names=[GameHudControllerTests]` | **10/10 passed** | new no-throw test + SetUp integrity |
| `run_tests category_names=["Popup"]` | **46/46 passed** | no sibling regression |
| `run_tests category_names=["CaiBang"]` | 82 total, 81 passed, 1 skipped, **0 failed** | data-service + PC parity |
| `run_tests mode=EditMode` (FULL) | 4076 ran, 25 failed — **all pre-existing, out of scope** | see baseline disclaimer |
| `refresh_unity scope=all compile=request` | `resulting_state=idle`, 0 `error CS####` | clean compile |

This executor did NOT rerun Unity (parent-collected evidence accepted per task instructions).

### Baseline disclaimer (pre-existing failures — NOT regressions)

The 25 full-suite failures are all outside Skill/Popup/HUD/CaiBangSkillPanel scope and were
pre-existing before this change (per `apply-progress.md`): Backend (×10), BaLang enemy damage number
(×1), CaiBang *combat-parity* damage tests (×2 — order-dependent, pass in focused isolation),
CombatSkillSlot buff (×1), InventoryService weapon SPR (×1), Mount/MalePlayerVisual sprite decode
(×8, `Slow`), PcWeaponThief (×2). None touch symbols changed by this change.

---

## 6. Strict TDD compliance

- Strict TDD active (Unity EditMode runner). `apply-progress.md` contains a `TDD Cycle Evidence`
  table with RED-by-construction (test authored before `SkillContent` existed), observed RED
  compile failure (`StringAssert.IsNotEmpty` → corrected to `Assert.IsNotEmpty`), GREEN
  (impl), and VERIFY states.
- Test files cross-referenced against the actual codebase: `SkillContentTests.cs`,
  `CaiBangSkillPanelTests.cs`, `GameHudControllerTests.cs` all exist and match the recorded behavior.
- **Assertion quality audit (changed/created tests):** no tautologies, no ghost loops, no type-only
  assertions, no smoke-only tests, no implementation-detail CSS-only assertions. Assertions are
  semantic PC-parity (exact skill IDs `115..1074`, `"Bổng Đả Ác Cẩu"`, summary `"200"`, 30 cells /
  26 populated, `fightSkillPoints 200→199`, `skillLevels[117]==1`, max-level + low-level gate
  rejections, grant idempotency). The two class-level `Assert.IsNotNull(typeof(MalePlayerVisual))`
  lines in the retargeted fixture are intentional in-assembly references that *prove the feature
  does not touch player visual* (they would fail to compile, not just assert-false, if the types
  were removed) — acceptable, not a tautology.

---

## 7. Review workload / PR-boundary findings

- Chained-PR forecast delivered at the wiring boundary (PR-1 additive → PR-2 switch), as
  recommended. `Chain strategy: feature-branch-chain` honored.
- Each PR stays within or near the 400-line budget; combined exceeds it (forecast High) but the
  split mitigates per-PR review burden. PR-1 ~708 LOC flagged as DEV-3 (mostly test + comments;
  production logic ~180 LOC) — additive/safe-revert; accepted.
- `PcSkillPanelService` (PR-1 protected surface) verified untouched.
- Discovered blast-radius handled correctly: `GMPlayerTab.cs` reflected onto the removed HUD
  surface (`OpenSkillPanel`/`IsSkillPanelVisible`); faithfully re-pointed to `OnSkillsClick`
  (NonPublic) + a minimal read-only `PopupManager.CurrentContent` getter (DEV-5). No behavior
  regression; no new direct type coupling (`VLTK.Sandbox` stays reflection-based).
- `PopupManager` gained `CurrentContent` (3 lines, doc'd) — not a PR-1 protected file; acceptable.

---

## 8. Residual risks

1. **No automated visual baseline** (manual screenshot not captured — EditMode-only executor).
   Visual parity is structurally verified (exact `IPopupLayoutHint` `205×376 @(338,110)` + ported
   38×51/36×36/5-col geometry + reused `cai_bang_skill_<id>` art, all asserted in tests), but the
   IMGUI→UIToolkit grid *render* look-shift has no automated parity gate. Reviewer visual
   confirmation recommended at sync; not a parity-blocking gate.
2. **Domain naming ambiguity** — the spec self-names domain `migrate-skill-panel-popup`, which is
   the **change name**, not a canonical reusable domain. This does not block verification
   (requirements are internally consistent). Candidate canonical domain names for the parent to
   decide at **sync**: `skill-panel` (new standalone domain), or fold into an existing `hud` /
   `popups` domain. Flagged as a sync-phase decision, not a verify blocker.
3. **DEV-1 path/namespace deviation** — `SkillContent` lives at `Assets/Scripts/UI/Skill/` +
   `namespace VLTK.UI.Skill`, not design §3.1's `Assets/Scripts/UI/Popup/Skill/` +
   `VLTK.UI.Popup`. Functionally identical (same assembly `VLTK.UI`); justified by mirroring the
   *actual* sibling pattern (`Faction`/`CharacterInfo`/`Inventory`/`Treasure`/`Team` all use
   `UI/{Feature}/` + `VLTK.UI.{Feature}`). Note for consistency.
4. **Detail-region fit in a 205-wide body** — the stacked layout (summary→grid→detail) is the
   agreed pattern; if cramped at runtime, design D3 tolerates only `Height→~430, Top↓` (width/left
   fixed at 205/338).

---

## 9. Next recommended phase

**sync** — the change is implementation-complete and verified PASS. Sync should:
- pick a canonical domain (see residual risk #2);
- propagate the `SkillContent` symbol + the `migrate-skill-panel-popup` follow-up closure into the
  project spec/deltas (the `add-popup-window-system` follow-up is already marked done in
  `99be0e88c`);
- then **archive**.

No blockers for archive from the verify side.

---

## 10. Skill resolution

`skill_resolution: none` — no executor/phase skill paths were injected by the parent for this
verify run; the embedded verify executor contract + project `AGENTS.md` rules were used directly.
Parent may pass indexed verify-skill paths next time.
