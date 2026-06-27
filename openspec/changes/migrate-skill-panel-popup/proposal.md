# Proposal — Migrate Skill Panel onto PopupWindow (HUD-003 follow-up)

> Change ID: `migrate-skill-panel-popup` · Closes the last unchecked follow-up of
> change `add-popup-window-system` (its `Non-Goals`: *"Not redesigning the existing
> SkillPicker/Team/Faction panels to the new base"*).
> Port rule: **100% ported from PC — do not invent**. PC source of truth for skill data
> is `Reference/PcSkills.txt` + `bin/client/script/skill/*.lua` (already consumed by the
> reused `PcSkillPanelService`). Vietnamese UI is mandatory (`default_locale: vi`).
> Mirror pattern: `FactionContent` / `CharacterInfoContent` / `InventoryContent` /
> `TreasureContent` shipped by `add-popup-window-system` (+ its slice-2 follow-ups).

## Why

`BtnSkills` currently opens the skill panel **inline**, not through the popup system that
every other bottom-toolbar feature button already uses:

- `OnSkillsClick()` → `GameHudController.OpenSkillPanel()` toggles a hardcoded
  `CaiBangSkillPanel` UXML element's `hidden` class and populates a `ScrollView` grid
  directly from inside `GameHudController`.
- This is the **only** remaining toolbar feature that bypasses `PopupManager` /
  `PopupWindow`. The result is inconsistent UX (no shared chrome/close/backdrop,
  hand-positioned panel that `SizeRootToScreen` clamps with a magic `Rect(338,110,205,376)`),
  and a large slab of feature code (~250 lines) living inside the HUD controller that
- duplicates the per-popup `IPopupContent` pattern every sibling already follows,
- is faction-misnamed (`CaiBang*`) even though the grid is generic (it renders the
  *active* faction catalog), and
- is rendered twice in practice: once by the UIToolkit `ScrollView` grid and again by the
  IMGUI overlay `PcHudVietnameseTextOverlay.DrawSkillPanelText()`.

Migrating it onto `PopupManager.Show(new SkillContent(...))` finishes the popup unification,
gives the skill window the same Vietnamese chrome/close/backdrop as Character Info / Inventory
/ Treasure / Faction, and lets the feature own its own grid + selection + upgrade in one
content class — full interactive parity, including upgrade that **mutates real
`PlayerProgressionState`**.

## What Changes (high level)

1. **New `SkillContent` popup body** (`IPopupContent` + `IPopupLayoutHint`, mirroring
   `FactionContent`) — owns the skill grid (30-slot single scrollable page), the skill-point
   summary, skill selection (tap to toggle detail), and the upgrade affordance ("+" add-point).
   Title = `"Kỹ năng võ công"` (VI) or faction-scoped title resolved via `GetFactionNameVi`.
   New UXML/USS under `Assets/UI/Popup/Skill/` following the sibling folder layout.
2. **Reuse, do not rewrite, the data service** — `PcSkillPanelService.BuildPage(...)` /
   `.TryUpgrade(...)` / `.PcFightSkillSlotsPerPage` / `.PcFightSkillPageCount` and the
   `PcSkillPanelSnapshot` / `PcSkillPanelRow` model are the single source of truth. The content
   binds + mutates through them exactly as the inline code does today.
3. **Preserve the faction-skill-panel progression grant** — `OnSkillsClick` currently grants
  faction skill-panel progression (via `SandboxManager.GrantFactionSkillPanelProgression` /
  `PlayerProgressionState.GrantFactionSkillPanelProgression`) *before* building the page. This
  behavior (and its idempotency on reopen) MUST be preserved verbatim when the popup opens.
4. **Wire `BtnSkills`** → `PopupManager.Show(new SkillContent(catalog, progression, faction,
   ...))` — same one-line `OnXxxClick` shape as `OnStatusClick`/`OnItemsClick`/`OnFactionClick`.
5. **Remove the inline skill-panel implementation from `GameHudController`** — the fields
  (`_skillPanel`, `_skillClose`, `_skillPageOne`, `_skillPageTwo`, `_skillList`, `_skillSummary`,
  `_skillPageIndex`), their `BindElements()` queries, the `RegisterClick` wiring for
  `CaiBangSkillClose/PageOne/PageTwo`, the `SizeRootToScreen` clamp, and the methods
  `OpenSkillPanel()` / `SetSkillPage()` / `CloseSkillPanel()` / `SelectSkill()` /
  `TryUpgradeSelectedSkill()` / `TryUpgradeSkill()` / `PopulateSkillPanel()` plus the public
  surface they exposed (`IsSkillPanelVisible`, `PcSkillPanelRowCount`, `CurrentSkillSnapshot`,
  `CurrentSelectedSkillId`, `CurrentSkillPageIndex`). The interaction logic moves into
  `SkillContent`.
6. **Retire the IMGUI skill-panel rendering** in `PcHudVietnameseTextOverlay.DrawSkillPanelText()`
  (grid icons, add-point click → `TryUpgradeSkill`, selected-skill detail tooltip). Once the
  popup body renders the grid + detail natively in UIToolkit (matching every sibling popup),
  the IMGUI overlay no longer owns skill-panel visuals. The shared IMGUI skill textures /
  styles that *only* served this path are removed; nothing else draws the skill panel.

## Scope

**In (this change):**
- `SkillContent.cs` (grid + summary + selection + upgrade), `Assets/UI/Popup/Skill/Skill.uxml`
  + `Skill.uss`, sized via `IPopupLayoutHint` to match the current PC-like panel footprint.
- Grid = single scrollable page, 30 slots (`PcFightSkillSlotsPerPage`), unused cells empty —
  identical to today's mobile behavior (`PcFightSkillPageCount == 1`).
- Selection + upgrade parity: tap a skill toggles its detail (summary / current level /
  next-level / `upgradeStatus` VI text); the "+" add-point spends one fight-skill point via
  `PcSkillPanelService.TryUpgrade` and mutates the live `PlayerProgressionState`.
- `BtnSkills` wired to `PopupManager.Show(new SkillContent(...))`.
- `GameHudController` de-inlined (fields + methods removed; `OnSkillsClick` rewired).
- `PcHudVietnameseTextOverlay.DrawSkillPanelText()` retired.
- **Migrate the 4 faction skill-panel fixtures** + the HUD controller test SetUp so they assert
  against the new popup content (see Impact/Risks).

**Out (follow-up / non-goals):**
- Any change to skill *data*, catalog, or progression rules — `PcSkillPanelService` is reused
  untouched (no new gameplay).
- Skill *drag-to-slot* / hotbar assignment (separate feature).
- Re-introducing multi-page tabs (`PcFightSkillPageCount == 1`; the two `CaiBangSkillPage*`
  tabs were already vestigial — a single scrollable grid stays).
- Porting other remaining inline panels (Trade/Stall/Face picker) — those are independent.
- Refreshing HUD art; skill icons already exist as `cai_bang_skill_<id>.png` under
  `UI/HUD/Art/Generated` and are reused as-is.

## Key Design Decisions

### D1 — Pure UIToolkit popup body, retire the IMGUI render (recommended)
Every sibling popup (`FactionContent`, `CharacterInfoContent`, `InventoryContent`,
`TreasureContent`) renders its body purely in UIToolkit via `Build(body)`; none rely on the
IMGUI overlay. To be consistent, `SkillContent` owns the grid + detail in the popup body, and
`DrawSkillPanelText()` is removed. **Trade-off to confirm (Open Question Q1):** the IMGUI
overlay was originally chosen so HUD text draws *above nameplates*; the popup is a modal window
that sits above gameplay anyway, so retiring the overlay render is safe — but this is the one
real product call in this change.

### D2 — Interaction moves into the content class
`SelectSkill` / `TryUpgradeSkill` live on `GameHudController` today because the panel was inline.
In the popup, the content class owns these callbacks (it holds the catalog + progression refs
via constructor, exactly like `FactionContent` takes `FactionBonusService`). The public HUD API
that only served the inline panel is removed rather than left as a thin facade, so there is one
owner (the content) — matching siblings. (Spec/design phase finalizes whether a tiny
`SkillPanelController` is extracted for testability.)

### D3 — Faction name, not "CaiBang", in code and visuals
The panel is generic (renders the active faction catalog). Drop the `CaiBang*` element names;
title/scope use the resolved VI faction name via `GetFactionNameVi(faction)` (already on the
controller, reused). No faction-specific branching inside the content.

### D4 — Progression grant preserved on open
`SkillContent` (or the open path) calls `GrantFactionSkillPanelProgression` exactly as
`OpenSkillPanel()` does today, *before* `BuildPage`, so reopen idempotency and the existing
"grant then build" ordering are unchanged. The `CaiBangSkillPanelTests` that assert this
idempotency must keep passing.

### D5 — Test fixtures migrate, not get deleted
The faction skill-panel fixtures encode real PC parity assertions (exact skill IDs, VI display
names like "Bổng Đả Ác Cẩu", `summary=="200"`, 30 slots / 26 rows). These assertions are
**valuable** and stay — they are retargeted from the inline panel to `SkillContent` (e.g. build
the content, assert `CurrentSkillSnapshot` / row count / VI names). Data-service tests
(`PcSkillPanelService`-level) are untouched.

## Impact / Risks

- **New files:** `SkillContent.cs`, `Assets/UI/Popup/Skill/Skill.uxml`, `Skill.uss`,
  `Assets/Tests/EditMode/.../SkillContentTests.cs`.
- **Edited:** `GameHudController.cs` (de-inline + rewire `OnSkillsClick`),
  `PcHudVietnameseTextOverlay.cs` (retire `DrawSkillPanelText`),
  `Assets/UI/GameHud.uxml` (the `CaiBangSkillPanel` block becomes obsolete — leave removal vs.
  hide to design; the elements must no longer be queried).
- **Migration risk — test-fixture blast radius (this is a gameplay-touching migration):**
  - `Assets/Tests/EditMode/Sandbox/CaiBangSkillPanelTests.cs::HudButtonSkills_OpensCaiBangPanelWithoutTouchingPlayerVisual`
    injects `_skillPanel`/`_skillSummary`/`_skillList` via reflection and calls
    `hud.OpenSkillPanel()`, asserting `IsSkillPanelVisible`, `PcSkillPanelRowCount==30`,
    `rows.Count==26`, exact skill IDs, VI name `"Bổng Đả Ác Cẩu"`, and `summary=="200"`.
    **These fields/methods are removed by this change → the test must be rewritten** to drive
    `SkillContent` (and/or `OnSkillsClick` → `PopupManager.Show`). The pure
    `PcSkillPanelService` assertions in the same file stay green.
  - Same-pattern fixtures (one per faction) must be updated in lockstep:
    `CuiYanSkillPanelTests.cs`, `KunLunSkillPanelTests.cs`, `TianRenSkillPanelTests.cs`.
  - `Assets/Tests/EditMode/UI/GameHudControllerTests.cs` uses reflection on HUD fields in
    `SetUp`; **removing fields breaks the whole fixture** → its `SetUp` must be updated
    together (or the reflection helpers made null-safe). This is the highest-risk edit because
    it can take down an unrelated fixture if missed.
  - `PcHudVietnameseTextOverlay.cs::DrawSkillPanelText` depends on `IsSkillPanelVisible` /
    `CurrentSkillSnapshot` / `TryUpgradeSkill`; removing it removes those consumers (good), but
    any stray reference left in the overlay will fail to compile.
- **Risk: line budget / chained PR.** Removing ~250 lines of inline logic, adding
  `SkillContent` (~200) + USS (~80), retiring the IMGUI path (~80 removed), and migrating ~4
  test fixtures + the HUD SetUp (~150) forecasts **well over the 400-line review budget**.
  **Chained PR is likely** (e.g. PR-1: `SkillContent` + USS + new test, panel still inline
  behind the new content but not yet wired; PR-2: wire `BtnSkills`, de-inline HUD, retire IMGUI,
  migrate fixtures). Auto-forecast = **2 PRs**; the apply phase should split at the wiring
  boundary if it crosses the budget.
- **Risk: no visual regression baseline.** Skill icons + layout are unchanged (reused art +
  same grid geometry), but the move from an IMGUI-drawn grid to a UIToolkit grid means the
  *rendered* look shifts; design/verify should screenshot before/after for parity.

## Non-Goals

- Not changing skill data, catalog, or progression (read/upgrade semantics identical to today).
- Not adding skill hotbar/drag-to-slot.
- Not restoring the vestigial 2-page tabs (stays a single scrollable page).
- Not migrating the other inline panels (Trade/Stall/Face picker).
- Not matching a specific PC INI window shell beyond the existing `PopupWindow` chrome
  (the skill window on PC is engine-hardcoded; the project's `PopupWindow` is the agreed
  reconstructed chrome, identical to siblings).

## Rollback

Each PR is reversible: PR-1 adds only new files (no behavior change → safe revert). PR-2 is the
behavior switch (rewire `OnSkillsClick`, de-inline, retire IMGUI, migrate tests) — revert PR-2
alone restores the exact prior inline behavior; no data migration exists, so there is no
persistent-state cleanup needed.

## Success Criteria

- Tapping `BtnSkills` opens exactly one `SkillContent` window through `PopupManager`, with the
  same close/backdrop behavior as the other popups.
- Grid renders 30 cells, all active-faction skills (26 for Cái Bang) with correct VI names and
  icons; unused cells empty — identical to today.
- Skill-point summary, selection (toggle detail), and upgrade ("+" → spend 1 point, mutate
  `PlayerProgressionState`) all work inside the popup; reopen keeps spent points/levels
  (progression-grant idempotency preserved).
- No `CaiBang*` inline panel logic remains in `GameHudController`; `DrawSkillPanelText` removed.
- Migrated fixtures pass under category `CaiBang` (+ faction categories); new `SkillContent`
  test passes under a `Popup`-style category; `GameHudControllerTests` SetUp still compiles/runs.
- All UI labels Vietnamese.

## Proposal Question Round (auto mode — answers assumed, please correct)

This change is well-scoped by the parent, but a few genuine product calls shape the spec.
**Assumed answers (inline) — flag any you want changed, or request a second round:**

1. **IMGUI render (Q1 — the one real decision):** Retire `DrawSkillPanelText()` entirely so the
   popup body is the single source of truth (consistent with Faction/CharacterInfo/Inventory)?
   *Assumed: YES — retire it.* (If you want the IMGUI overlay kept as a parallel/fallback
   render, say so; it changes the de-inline scope.)
2. **Public HUD API:** Remove the inline-only surface (`OpenSkillPanel`, `IsSkillPanelVisible`,
   `PcSkillPanelRowCount`, `CurrentSkillSnapshot`, `CurrentSelectedSkillId`,
   `CurrentSkillPageIndex`) rather than keep facades? *Assumed: YES — remove; content owns it.*
3. **Page tabs:** Keep a single scrollable 30-slot page (no 2-tab UI), since
   `PcFightSkillPageCount == 1`? *Assumed: YES.*
4. **Test fixture strategy:** Retarget the 4 faction fixtures + HUD SetUp to the new content
   (keeping the PC-parity assertions), rather than deleting them? *Assumed: YES.*
