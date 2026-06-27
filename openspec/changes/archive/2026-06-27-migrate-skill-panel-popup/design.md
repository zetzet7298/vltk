# Design — Migrate Skill Panel onto PopupWindow (`migrate-skill-panel-popup`)

> Phase: **design** · Change closes the last unchecked follow-up of `add-popup-window-system`.
> Port rule: 100% ported from PC — no invention. `PcSkillPanelService` is reused
> **untouched** (read/upgrade semantics identical to today). Vietnamese UI (`vi`).
> Mirror pattern: `FactionContent.cs` + `Faction.uss` + `FactionContentTests.cs`.

This document resolves every Open Item the spec left for design and fixes the
review-workload forecast / chained-PR split. DESIGN ONLY — no apply.

---

## 1. Architecture summary

`BtnSkills` stops toggling an inline `CaiBangSkillPanel` UXML element and instead opens a
`SkillContent` popup body through the existing `PopupManager`, exactly like `BtnFaction` /
`BtnStatus` / `BtnItems` / `BtnTeam` / `BtnTreasure`. One content class owns the skill grid,
the skill-point summary, the tap-to-select detail toggle, and the "+" upgrade that spends a
real fight-skill point. All data binding/mutation flows through the reused
`PcSkillPanelService`; no skill logic is duplicated.

```
BtnSkills ─▶ OnSkillsClick (GameHudController)
            │   resolve catalog / live progression / faction from SandboxManager (fallbacks)
            │   resolve factionNameVi via GetFactionNameVi
            └─▶ PopupManager.Show(new SkillContent(catalog, progression, faction, factionNameVi,
                                                   artFolder, grantProgression: manager.Grant...))
                        │
                        ├─ PopupWindow shell: chrome (title "Kỹ năng võ công" + "Đóng") + body
                        │   ApplyLayoutHint(width=205,height=376,left=338,top=110)
                        └─ SkillContent.Build(body) → SkillContent.OnShow()
                                │  OnShow: grant faction skill-panel progression (idempotent)
                                │          BEFORE BuildPage  (gameplay-critical ordering preserved)
                                └─ Refresh() → PcSkillPanelService.BuildPage(catalog, progression,
                                                       selectedSkillId, pageIndex=0)
                                       renders: summary + 30-cell grid + detail region
   user tap cell   ─▶ SelectSkill(skillId) toggle → Refresh()
   user tap "+"    ─▶ PcSkillPanelService.TryUpgrade(progression, catalog, skillId) → Refresh()
```

Data flow is **identical** to the inline path today; only the rendering surface moves from
an inline `ScrollView` + an IMGUI overlay to a single UIToolkit popup body (matching every
sibling popup).

---

## 2. Resolved design decisions

### D1 — `SkillContent` constructor signature (mirrors `FactionContent`)

`FactionContent` constructor-injects its service + resolved primitives and owns its own UI
state. `SkillContent` follows the same shape:

```csharp
public sealed class SkillContent : IPopupContent, IPopupLayoutHint
{
    public string TitleVi => "Kỹ năng võ công";
    public float Width  => 205f;
    public float Height => 376f;
    public float Left   => 338f;
    public float Top    => 110f;

    private readonly SkillCatalog _catalog;
    private readonly PlayerProgressionState _progression; // LIVE ref — mutated by TryUpgrade
    private readonly CombatFaction _faction;              // resolved by caller (CaiBang when None)
    private readonly string _factionNameVi;               // for body header; optional
    private readonly string _artFolder;                   // HUD art root for skill icons
    private readonly System.Action<CombatFaction> _grantProgression; // null → EditMode fallback

    // Selection + page state owned INTERNALLY (not received per-call):
    private int _selectedSkillId;
    private const int PageIndex = 0;   // PcFightSkillPageCount == 1; no tabs

    public SkillContent(
        SkillCatalog catalog,
        PlayerProgressionState progression,
        CombatFaction faction,
        string factionNameVi,
        string artFolder,
        System.Action<CombatFaction> grantProgression = null) { ... }
}
```

**Selection/page owned internally — YES.** `_selectedSkillId` and `PageIndex` are private
fields of the content, not passed per-call. This matches the spec ("SkillContent owns these
callbacks") and the sibling ownership model; it also avoids any shared mutable HUD state.

**Why an optional `grantProgression` callback** instead of always using
`PlayerProgressionState.GrantFactionSkillPanelProgression`: the spec mandates the runtime
use `SandboxManager.GrantFactionSkillPanelProgression(faction)` and fall back to the
progression method only when the sandbox is absent. Passing the grant as a callback lets the
runtime take the manager path while tests pass `null` and exercise the fallback — both
documented branches verifiable. `grantProgression` is `null`-safe.

**`PlayerProgressionState.GrantFactionSkillPanelProgression(catalog, faction)` mutates in
place** (verified: `CaiBangSkillPanelTests` asserts on the same object after the grant), so
holding the live reference keeps `TryUpgrade` mutations visible to the content without
re-reading from the manager.

### D2 — Grant-call location = `SkillContent.OnShow` (re-run safe)

**Decision: the progression grant lives inside `SkillContent.OnShow`, before `BuildPage`.**

```csharp
public void OnShow()
{
    if (_grantProgression != null)
        _grantProgression(_faction);                 // runtime: SandboxManager.Grant...
    else
        _progression.GrantFactionSkillPanelProgression(_catalog, _faction); // EditMode fallback
    Refresh();                                        // BuildPage AFTER grant
}
```

Rationale:
- The spec requires "grant BEFORE BuildPage, idempotent on reopen, sandbox-fallback preserved."
  `OnShow` is called by `PopupManager.Show` immediately after mounting (verified in
  `PopupManager.Show`), and is re-invoked on every open of a cached popup → reopen is
  inherently re-run-safe, matching the inline `OpenSkillPanel` semantics.
- `GrantFactionSkillPanelProgression` is idempotent (re-granting after spending a point leaves
  spent points/levels unchanged — asserted by `ReopeningPanelProgression_*` tests).
- Faction resolution stays in `OnSkillsClick` (caller), exactly like `OnFactionClick`
  resolves faction/level before constructing `FactionContent`. The content receives the
  already-resolved faction.

This satisfies the gameplay-critical "Progression-grant preservation on open" requirement.

### D3 — `IPopupLayoutHint` = the prior inline `Rect(338,110,205,376)`

**Decision: `Width=205, Height=376, Left=338, Top=110`** (strict parity), NOT the sibling
`FactionContent` hint (`460×480 @ 410,80`).

Justification:
- The spec's parity target is explicit: *"Width/Height SHALL approximate the prior `205 × 376`
  inline footprint; Left/Top SHALL position the window in the prior `338, 110` screen region."*
- `PopupWindow.ApplyLayoutHint` (verified) sets `style.width/height/left/top` directly from the
  hint, so these four values ARE the full window geometry (chrome + body). The skill sheet art
  (`技能` / UiSkillsSheet.ini) is authored at **205×376**; the sibling FactionContent hint
  would stretch the 5-column×36px skill-icon grid and break it.
- Fixed exact values make the layout-hint scenario deterministic and assertable in
  `SkillContentTests`.

**Detail region placement (formerly drawn OUTSIDE the panel by the IMGUI overlay):** the
selected-skill detail (a ~220-wide tooltip in the old overlay) now lives **inside** the body.
In a 205-wide window the detail cannot sit beside a ~190-wide grid, so the body stacks
`[skill-point summary] → [scrollable 30-cell grid] → [compact wrapping detail region]`, the
same vertical-stack pattern `FactionContent` uses (header→totals→table→footer). The grid
scrolls within the available height (it already scrolled at 191×278 inline). If, at apply, the
detail proves cramped, the only tolerated adjustment is bumping `Height` to ~430 and lowering
`Top` to keep the window fully on-screen — width/left stay 205/338. This is the documented
apply-time tolerance.

### D4 — `CaiBangSkillPanel` UXML/USS: FULL removal

**Decision: fully remove the `CaiBangSkillPanel` block and its USS classes** (do not leave
unreferenced markup). This matches the Team/Faction popup-cleanup precedent and avoids
shipping dead, faction-misnamed elements that nothing queries.

Exact UXML element block to remove from `Assets/UI/HUD/GameHud.uxml` (the whole comment +
container, 6 named elements):

```xml
<!-- Cái Bang skill sheet: ... -->
<ui:VisualElement name="CaiBangSkillPanel" class="hud-cb-skill-panel hidden">
    <ui:VisualElement name="CaiBangSkillClose"   class="hud-cb-close"/>
    <ui:Label       name="CaiBangSkillSummary"   text="200" class="hud-cb-summary"/>
    <ui:ScrollView  name="CaiBangSkillList"      class="hud-cb-list"/>
    <ui:VisualElement name="CaiBangSkillPageOne" class="hud-cb-page-tab hud-cb-page-one">...</ui:VisualElement>
    <ui:VisualElement name="CaiBangSkillPageTwo" class="hud-cb-page-tab hud-cb-page-two">...</ui:VisualElement>
</ui:VisualElement>
```

USS classes to remove from `Assets/UI/HUD/GameHud.uss` (only ever used by the skill panel —
verified present only under the `/* CÁI BANG SKILL PANEL */` section):

```
.hud-cb-skill-panel   .hud-cb-header   .hud-cb-title   .hud-cb-close   .hud-cb-close-text
.hud-cb-summary       .hud-cb-list     .hud-cb-grid-cell   .hud-cb-grid-cell-upgradable
.hud-cb-grid-cell-empty   .hud-cb-grid-slot   .hud-cb-grid-slot-empty   .hud-cb-grid-level
.hud-cb-add-point     .hud-cb-grid-name   .hud-cb-page-tab   .hud-cb-page-one
.hud-cb-page-two      .hud-cb-page-tab-label   .hud-cb-page-tab-active
```

**Add** to `GameHud.uxml` (alongside the existing popup stylesheets) so the new popup's
`.skill-*` classes resolve at runtime:
```xml
<ui:Style src="project://database/Assets/UI/Popup/Skill/Skill.uss" />
```

**Art note:** `.hud-cb-close` (`btn_close_skill_02_vi.png`) becomes obsolete — the popup uses
the shared `PopupWindow` close chrome ("Đóng"). `.hud-cb-add-point` art
(`状态加点按钮改_01.png`) is re-referenced inside `Skill.uss` for the "+" affordance. Skill
icons (`cai_bang_skill_<id>.png`) are reused as-is.

### D5 — Grid cell construction = skill-scoped USS under `Assets/UI/Popup/Skill/Skill.uss`

**Decision: rename to skill-scoped `.skill-*` classes owned by the popup**, porting the same
geometry the inline `PopulateSkillPanel` used (38×51 cells, 36×36 slot, 5-col `flex-wrap`,
level/add-point/name overlays, `-upgradable`/`-empty` variants). This gives the popup a single
owner for its visuals (consistent with `Faction.uss` owning `.faction-*`), rather than
re-using HUD-namespace `.hud-cb-*` classes that the cleanup in D4 deletes.

| inline class (removed)            | new `Skill.uss` class                |
|-----------------------------------|--------------------------------------|
| `.hud-cb-skill-panel` (body)      | `.skill-body`                        |
| `.hud-cb-summary`                 | `.skill-summary`                     |
| `.hud-cb-list`                    | `.skill-grid-scroll`                 |
| `.hud-cb-grid-cell`               | `.skill-grid-cell`                   |
| `.hud-cb-grid-cell-upgradable`    | `.skill-grid-cell--upgradable`       |
| `.hud-cb-grid-cell-empty`         | `.skill-grid-cell--empty`            |
| `.hud-cb-grid-slot`               | `.skill-grid-slot`                   |
| `.hud-cb-grid-slot-empty`         | `.skill-grid-slot--empty`            |
| `.hud-cb-grid-level`              | `.skill-grid-level`                  |
| `.hud-cb-add-point`               | `.skill-add-point`                   |
| `.hud-cb-grid-name`               | `.skill-grid-name`                   |
| (new) selected highlight          | `.skill-grid-cell--selected`         |
| (new, was external IMGUI tooltip) | `.skill-detail`, `.skill-detail-title`, `.skill-detail-level`, `.skill-detail-summary`, `.skill-detail-next`, `.skill-detail-status` |

Cell build loop (in `SkillContent.Refresh`) is a direct port of `PopulateSkillPanel`:
`for slotIndex in [0, PcFightSkillSlotsPerPage)`: if `slotIndex < snap.rows.Count` build a
populated cell (slot icon, level label, add-point, name, select callback; `--upgradable` when
`row.canUpgrade`), else build an empty cell (`--empty`/`--empty`). The slot count is sourced
from `PcSkillPanelService.PcFightSkillSlotsPerPage` — never a duplicated literal.

### D6 — Selection + upgrade callbacks (interactive parity)

Both are content-owned and both re-render through `PcSkillPanelService.BuildPage` (via
`Refresh()`):

```csharp
private void SelectSkill(int skillId)
{
    _selectedSkillId = _selectedSkillId == skillId ? 0 : skillId;  // toggle
    Refresh();
}

private void TryUpgrade(int skillId)
{
    if (PcSkillPanelService.TryUpgrade(_progression, _catalog, skillId))  // mutates live progression
        Refresh();
}
```

Wiring (port of inline `PopulateSkillPanel` callback registration):
- populated cell `PointerDownEvent` → `SelectSkill(row.skillId)`.
- the `+` add-point element on upgradable rows `PointerDownEvent` → `TryUpgrade(row.skillId)`
  (and `evt.StopPropagation()` so it does not also toggle selection).
- a non-upgradable row (`canUpgrade == false`) renders NO actionable `+`, so activating the row
  only toggles selection and never mutates `PlayerProgressionState`.

`Refresh()` rebuilds the snapshot (`BuildPage(catalog, progression, _selectedSkillId, 0)`)
and re-renders grid + summary + detail, so the post-upgrade `learnedLevel` (+1) and
`skillPoints` (−1) are immediately visible.

### D7 — Test strategy (RED-first, retargeted fixtures, verified blast radius)

**New file: `Assets/Tests/EditMode/UI/SkillContentTests.cs`**, `[TestFixture, Category("Skill")]`,
built with `TestCatalogCache.NoviceAndCaiBang` + a fresh `PlayerProgressionState`, **no
SandboxManager** (so the `grantProgression=null` fallback path is exercised). RED-first:

| # | Assertion |
|---|-----------|
| 1 | `TitleVi == "Kỹ năng võ công"` |
| 2 | implements `IPopupContent` AND `IPopupLayoutHint` (`Width==205, Height==376, Left==338, Top==110`) |
| 3 | `Build` produces exactly `30` grid cells, `26` populated + `4` empty (Cái Bang) |
| 4 | populated skill IDs in PC order `115..1074` (first `==115`) |
| 5 | a cell's name label contains `"Bổng Đả Ác Cẩu"` (skill 125) |
| 6 | skill-point summary reads `"200"` after the `OnShow` grant |
| 7 | tap toggles selection: first tap selects + detail region shows; second tap deselects + detail clears |
| 8 | `TryUpgrade` spends one point (`skillPoints 200→199`, `learnedLevel` +1) |
| 9 | upgrade honors PC max-level cap (loop to `maxLevel`, then `TryUpgrade` is rejected) |
| 10 | upgrade honors the low-level gate (`progression.level=10` blocks the 2nd upgrade of skill 117) |
| 11 | grant idempotency: a second `OnShow()` (no spend in between) leaves `skillPoints==200` |

**Faction fixtures — retarget scope is SMALLER than the proposal assumed:**
- `CuiYanSkillPanelTests` / `KunLunSkillPanelTests` / `TianRenSkillPanelTests` are **pure
  data-service tests** (`Grant*SkillPanelProgression` / `TryUpgrade` / `PcSkillPanelService.Build`
  only; no HUD reflection, no `OpenSkillPanel`). **They need NO change** — they already assert
  through the reused `PcSkillPanelService`, which is untouched.
- Only `CaiBangSkillPanelTests::HudButtonSkills_OpensCaiBangPanelWithoutTouchingPlayerVisual`
  couples to the removed HUD surface (reflection on `_skillPanel`/`_skillSummary`/`_skillList` +
  `hud.OpenSkillPanel()` + `IsSkillPanelVisible`/`PcSkillPanelRowCount`/`CurrentSkillSnapshot`).
  **Retarget this single test** to construct `new SkillContent(...)` and assert through the
  content: grid cell count `30`, populated `26`, PC-order skill IDs, `"Bổng Đả Ác Cẩu"`,
  summary `"200"`, preserving the exact PC-parity assertions. It still keeps the
  `MalePlayerVisual`/`MalePlayerSpriteCatalog` "does not touch player visual" invariant.
- All other `CaiBangSkillPanelTests` (Grant/TryUpgrade/Snapshot/PC-parity/Icon PNG assertions)
  are data-service level and **stay unchanged**.

**`GameHudControllerTests.cs` SetUp — NO surgery needed (verified):** its `SetUp` reflects only
`_buffPanel`/`_tradeInfoPanel`/`_tradeInfoClose`/`_tradePartner*`/`_stallCurrency*`/`_stall*Btn`/
`_facePicker*`/`_faceBtn`. It references **no skill fields**, and no test calls a removed skill
method. So removing the skill fields does not break it. The only change to this file is an
**additive** sibling-parity test `OnSkillsClick_WithoutPopupManager_DoesNotThrow` (mirrors
`OnTeamClick_WithoutPopupManager_DoesNotThrow` / `OnFactionClick_WithoutPopupManager_DoesNotThrow`),
asserting the rewired handler degrades gracefully when `PopupManager.Instance` is null.

`PcSkillPanelService`-level tests remain untouched and green (data-reuse invariant).

### D8 — Drop `Skill.uxml`; code-build like `FactionContent` (scope narrowing)

The proposal listed a `Skill.uxml`, but the **actual mirror pattern** (`FactionContent`) builds
its body entirely in C# inside `Build(body)` and ships only a `.uss`. There is no
`Faction.uxml`. To mirror the sibling faithfully (and shrink the diff), **`SkillContent.Build`
constructs its VisualElements in code; only `Skill.uss` ships.** No `Skill.uxml`. This is a
deliberate narrowing, consistent with the "mirror FactionContent" mandate.

---

## 3. Contracts

### 3.1 `SkillContent` (new — `Assets/Scripts/UI/Popup/Skill/SkillContent.cs`)

```csharp
namespace VLTK.UI.Popup
{
    public sealed class SkillContent : IPopupContent, IPopupLayoutHint
    {
        string TitleVi { get; }   // "Kỹ năng võ công"
        float Width  => 205f;     // IPopupLayoutHint
        float Height => 376f;
        float Left   => 338f;
        float Top    => 110f;

        SkillContent(SkillCatalog catalog, PlayerProgressionState progression,
                     CombatFaction faction, string factionNameVi, string artFolder,
                     System.Action<CombatFaction> grantProgression = null);

        void Build(VisualElement body);   // IPopupContent — heavy: grid + detail scaffold
        void OnShow();                     // grant (idempotent) → Refresh();  re-run safe
        void OnClose();                    // null cached elements
    }
}
```

Namespace chosen as `VLTK.UI.Popup` (co-located with sibling popups under
`Assets/Scripts/UI/Popup/Skill/`), mirroring the `Assets/UI/Popup/Faction/` folder layout for
the `.uss`. File path: `Assets/Scripts/UI/Popup/Skill/SkillContent.cs`.

### 3.2 `OnSkillsClick` (rewired — `GameHudController.cs`)

```csharp
private void OnSkillsClick()
{
    var manager = PopupManager.Instance;
    if (manager == null) { SubsystemLog.Info("HUD", "PopupManager not initialised"); return; }

    var sandbox = SandboxManager.Instance;
    SkillCatalog catalog = sandbox != null ? sandbox.CombatSkillCatalog
                                           : PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
    PlayerProgressionState progression = sandbox != null ? sandbox.PlayerProgression
                                                         : new PlayerProgressionState();
    CombatFaction faction = progression.faction != CombatFaction.None ? progression.faction
                                                                       : CombatFaction.CaiBang;
    manager.Show(new SkillContent(catalog, progression, faction, GetFactionNameVi(faction),
                                  artFolder,
                                  grantProgression: sandbox != null
                                      ? sandbox.GrantFactionSkillPanelProgression : null));
    CloseMapPreview();
    SubsystemLog.Info("HUD", "Open Skills popup");
}
```

Same one-line-dependency-resolution shape as `OnFactionClick`. `GetFactionNameVi` is reused
(it stays; it is also used for log context). Null-`PopupManager` guard matches the siblings.

---

## 4. File changes (per PR)

### PR-1 — additive (no behavior change, nothing wired yet)

| File | Change | ~Lines |
|------|--------|--------|
| `Assets/Scripts/UI/Popup/Skill/SkillContent.cs` | **NEW** — content class (grid/summary/detail/select/upgrade/OnShow grant) | +200 |
| `Assets/UI/Popup/Skill/Skill.uss` | **NEW** — `.skill-*` classes, ported geometry | +90 |
| `Assets/Tests/EditMode/UI/SkillContentTests.cs` | **NEW** — `[Category("Skill")]`, RED→GREEN (D7 table) | +105 |
| `Assets/UI/HUD/GameHud.uxml` | ADD `<ui:Style src=".../Popup/Skill/Skill.uss" />` (classes unused until PR-2 wires it) | +1 |
| **PR-1 total** | **pure additive; zero behavior change; safe single-commit revert** | **≈ +396** |

PR-1 ships behind nothing: `SkillContent` exists and is unit-tested, but `BtnSkills` still
opens the inline panel. Reviewable as one additive unit within the 400-line budget.

### PR-2 — behavior switch + cleanup (mostly deletions)

| File | Change | ~Lines |
|------|--------|--------|
| `Assets/Scripts/UI/GameHudController.cs` | rewire `OnSkillsClick` (+15); REMOVE skill fields (`_skillPanel/_skillClose/_skillPageOne/_skillPageTwo/_skillList/_skillSummary/_skillPageIndex`), methods (`OpenSkillPanel/SetSkillPage/CloseSkillPanel/SelectSkill/TryUpgradeSelectedSkill/TryUpgradeSkill/PopulateSkillPanel`), public props (`IsSkillPanelVisible/PcSkillPanelRowCount/CurrentSkillSnapshot/CurrentSelectedSkillId/CurrentSkillPageIndex`), `BindElements` skill queries, `RegisterClick` skill wiring, and the `SizeRootToScreen` `Rect(338,110,205,376)` clamp | −250 / +15 |
| `Assets/Scripts/UI/PcHudVietnameseTextOverlay.cs` | REMOVE `DrawSkillPanelText`, its `OnGUI` call site, `EnsureSkillTextures`, the skill GUIStyles (`_skillName/_skillLevel/_skillHint`), textures (`_skillPanelTexture/_skillPanelTargetTexture/_addPointTexture`) and the `_caiBangIconTextures` cache | −110 |
| `Assets/UI/HUD/GameHud.uxml` | REMOVE the `CaiBangSkillPanel` block (6 named elements + comment) | −8 |
| `Assets/UI/HUD/GameHud.uss` | REMOVE all `.hud-cb-*` classes (D4 list) | −60 |
| `Assets/Tests/EditMode/Sandbox/CaiBangSkillPanelTests.cs` | RETARGET the single `HudButtonSkills_*` test to drive `SkillContent` (keep every PC-parity assertion; keep the player-visual invariant) | ±30 |
| `Assets/Tests/EditMode/Sandbox/GameHudControllerTests.cs` | ADD `OnSkillsClick_WithoutPopupManager_DoesNotThrow` (sibling parity; SetUp untouched) | +10 |
| **PR-2 total** | **net deletions (~−380) + ~55 additions; rewired handler is ~15 lines** | **≤ 400 changed** |

PR-2 is the behavior switch. Reverting PR-2 alone restores the exact prior inline behavior
(no data migration, no persistent-state cleanup). `CuiYan`/`KunLun`/`TianRen` fixtures are
**not touched** (verified: pure data-service tests).

---

## 5. Data-reuse invariant (no duplication)

`SkillContent` binds and mutates skill data **only** through:
`PcSkillPanelService.BuildPage` / `PcSkillPanelService.TryUpgrade` /
`PcSkillPanelService.PcFightSkillSlotsPerPage` / `PcFightSkillPageCount` and the
`PcSkillPanelSnapshot` / `PcSkillPanelRow` model. Every displayed name, level, summary,
next-level, and status string comes from a `PcSkillPanelRow` produced by `BuildPage`. The
content contains **no** locally-computed skill description, ordering, level-cap, or
`canUpgrade` logic. `PcSkillPanelService`'s public API and behavior are unchanged by this
change.

---

## 6. Test run plan (EditMode via MCP)

```
# PR-1 verification — new SkillContent tests + reused data-service parity
unityMCP run_tests mode=EditMode category_names=["Skill","CaiBang"]

# PR-2 verification — HUD wiring + sibling no-throw parity; skip slow sprite decode
unityMCP run_tests mode=EditMode category_names=["!Slow"]
```

Full suite (`!` removed) only as the pre-push final gate, per project test-run rule.

---

## 7. Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Progression grant no longer fires (gameplay regression) | Grant moved into `OnShow` BEFORE `BuildPage`; idempotent; both runtime (`SandboxManager.Grant…`) and EditMode (`PlayerProgressionState.Grant…`) branches covered + asserted (D7 #6, #11). |
| `TryUpgrade` stops mutating live progression | Content holds the live `PlayerProgressionState` ref captured at construction (same ref `SandboxManager.PlayerProgression`); `Grant…Progression` mutates in place (verified). Asserted (D7 #8–#10). |
| Detail region cramped in a 205-wide body | Stacked layout below the grid (D3); apply-time tolerance allows `Height→~430, Top↓` only; width/left fixed. |
| Look shift (IMGUI grid → UIToolkit grid) | Reused art + ported geometry; screenshot before/after parity check recommended at apply (no automated visual baseline exists). |
| `GameHudControllerTests` SetUp breaks | Verified: SetUp references no skill fields; only an additive `OnSkillsClick_*` test is added. |
| Stray overlay reference to removed HUD surface | PR-2 removes `DrawSkillPanelText` + `EnsureSkillTextures` + all consumers; compile gate catches any leftover reference. |

---

## 8. Open Items resolved by this design

- ✅ SkillContent constructor signature & internal state ownership — D1.
- ✅ Grant-call location — D2 (`OnShow`).
- ✅ `IPopupLayoutHint` pixel values — D3 (`205×376 @ 338,110`).
- ✅ `CaiBangSkillPanel` UXML removal vs. leave-unreferenced — D4 (full removal).
- ✅ Grid cell construction / class names — D5 (skill-scoped `.skill-*`).
- ✅ Selection + upgrade callbacks — D6.
- ✅ Test strategy + retarget scope — D7.
- ✅ Review-workload forecast / chained-PR split — **2 PRs** (§4; D8 drops `Skill.uxml`).

## 9. Review-workload forecast & PR split (explicit)

Forecast = **2 PRs** (auto-forecast confirms; the change crosses the 400-line budget):

- **PR-1** (additive, ≤400): `SkillContent.cs` + `Skill.uss` + `SkillContentTests.cs` + 1
  `GameHud.uxml` `<ui:Style>` line. Nothing wired; `BtnSkills` still inline. ~+396 lines, all
  additive → light review, safe revert.
- **PR-2** (behavior switch, ≤400 changed): rewire `OnSkillsClick`, de-inline
  `GameHudController` (remove fields/methods/props/clamp/wiring), retire IMGUI
  (`DrawSkillPanelText` + assets), remove UXML block + `.hud-cb-*` USS, retarget the single
  Cái Bang HUD test, add `OnSkillsClick_*` no-throw test. Net ~−380 + ~55 additions → mostly
  deletions, low review burden.

Split boundary = the **wiring boundary** (PR-1 ships the content+test; PR-2 flips `BtnSkills`
to use it and tears down the inline path). Reverting PR-2 alone restores the prior inline
behavior exactly.
