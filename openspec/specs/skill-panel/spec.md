# Skill Panel Specification

> Domain: **skill-panel** (canonical spec, established by change `migrate-skill-panel-popup`).
> Source of truth: PC `Reference/PcSkills.txt` + `bin/client/script/skill/*.lua`, ported 100%
> via `PcSkillPanelService` (reused unchanged). `default_locale: vi`.

## Purpose

The skill panel is opened through the shared `PopupManager` — like every other toolbar
feature button — as a `SkillContent` popup body. The body owns the 30-cell skill grid, the
skill-point summary, the tap-to-select detail toggle, and the "+" add-point upgrade that spends
a real fight-skill point, binding and mutating exclusively through `PcSkillPanelService`. No
skill data, catalog, or progression rules are introduced here.

> ⚠️ **GAMEPLAY-TOUCHING DOMAIN.** Opening the skill panel mutates the live
> `PlayerProgressionState` (spending fight-skill points and granting faction skill-panel
> progression). Progression-grant ordering and upgrade semantics MUST be preserved exactly.

## Requirements

### Requirement: SkillContent popup-body contract

The system SHALL provide a `SkillContent` class that implements both `IPopupContent` and
`IPopupLayoutHint`, mirroring `FactionContent`. `TitleVi` SHALL be the Vietnamese string
`"Kỹ năng võ công"`. The body SHALL render the skill grid, skill-point summary, selection
detail, and upgrade affordance purely in UIToolkit inside `Build(VisualElement body)`.
`SkillContent` SHALL obtain the active `SkillCatalog`, the live `PlayerProgressionState`, and
the resolved `CombatFaction` through its constructor. The body SHALL NOT branch on any single
faction.

#### Scenario: Content implements the popup contracts

- GIVEN a `SkillContent` constructed with a Cái Bang catalog and progression
- WHEN its interfaces are inspected
- THEN it SHALL be an `IPopupContent` AND an `IPopupLayoutHint`
- AND `TitleVi` SHALL equal `"Kỹ năng võ công"`

#### Scenario: Vietnamese title and labels

- GIVEN the skill popup is opened
- WHEN the title bar and body render
- THEN the title and every visible label SHALL be Vietnamese
- AND no `CaiBang`-prefixed element name or hardcoded faction title SHALL remain in the content

### Requirement: Skill grid layout — 30 cells, single scrollable page

The grid SHALL render exactly `PcSkillPanelService.PcFightSkillSlotsPerPage` (`30`) cells in a
single scrollable page. The number of populated cells SHALL equal
`PcSkillPanelSnapshot.rows.Count` for the active faction; the remaining cells SHALL be empty
placeholders. The page count SHALL be governed by `PcSkillPanelService.PcFightSkillPageCount`
(`1`). The system SHALL NOT introduce any multi-page tab UI.

#### Scenario: Grid has 30 cells for any faction

- GIVEN a `SkillContent` built for Cái Bang (26 resolvable skills)
- WHEN `Build` populates the grid
- THEN the grid SHALL contain exactly `30` cells
- AND exactly `26` cells SHALL be populated with skill rows
- AND exactly `4` cells SHALL be empty placeholders

#### Scenario: Slot count sourced from the service constant

- GIVEN any faction catalog
- WHEN the grid is built
- THEN the populated+empty cell count SHALL equal `PcSkillPanelService.PcFightSkillSlotsPerPage`
- AND SHALL NOT be a hard-coded literal duplicated in the content class

### Requirement: Skill-point summary display

The body SHALL display the available fight-skill points read from
`PcSkillPanelSnapshot.skillPoints`. After a progression grant or an upgrade, the summary SHALL
reflect the current `skillPoints` value on re-render.

#### Scenario: Summary shows granted skill points

- GIVEN a Cái Bang progression freshly granted faction skill-panel progression
- WHEN the popup body renders
- THEN the skill-point summary SHALL display the snapshot's `skillPoints` value
- AND for the Cái Bang fixture that value SHALL be `"200"` (PC parity)

### Requirement: Skill selection detail toggle (interactive parity)

Tapping a populated skill cell SHALL toggle that skill's selected state using the same
semantics: selecting an already-selected skill SHALL deselect it (`selectedSkillId → 0`). While
a skill is selected, the body SHALL show its detail derived entirely from the matching
`PcSkillPanelRow`: `displayName`, current level (`learnedLevel` / `maxLevel`),
`requiredLevel`, `summary`, `nextLevelSummary`, and `upgradeStatus`. When no skill is
selected, the detail region SHALL be cleared.

#### Scenario: Tap toggles selection and shows detail

- GIVEN the grid is populated and no skill is selected
- WHEN a skill cell is tapped
- THEN that skill SHALL become selected
- AND the detail region SHALL show that skill's `displayName`, level, and `upgradeStatus`

#### Scenario: Tap a selected skill deselects it

- GIVEN a skill is currently selected
- WHEN the same skill cell is tapped again
- THEN `selectedSkillId` SHALL become `0`
- AND the detail region SHALL be cleared

### Requirement: Upgrade mutates live progression (interactive parity)

Each skill row whose `PcSkillPanelRow.canUpgrade` is true SHALL show an upgrade affordance
("+"). Activating it SHALL spend one fight-skill point by calling
`PcSkillPanelService.TryUpgrade(progression, catalog, skillId)`, which MUTATES the live
`PlayerProgressionState`. On a successful upgrade the body SHALL re-render (grid, summary, and
detail) so the new `learnedLevel` and the reduced `skillPoints` are visible. A row that cannot
upgrade (`canUpgrade == false`) SHALL NOT present a spendable affordance.
`PcSkillPanelService.TryUpgrade` semantics are reused unchanged.

#### Scenario: Upgrade spends a point and re-renders

- GIVEN an upgradable skill is selected and `skillPoints > 0`
- WHEN the "+" affordance is activated
- THEN `PcSkillPanelService.TryUpgrade` SHALL be invoked on the live progression
- AND the skill's `learnedLevel` SHALL increase by `1`
- AND the displayed `skillPoints` SHALL decrease by `1`
- AND the grid/summary/detail SHALL re-render to reflect the new state

#### Scenario: Non-upgradable skill has no spendable affordance

- GIVEN a skill whose `canUpgrade` is false
- WHEN the row renders
- THEN it SHALL NOT present an actionable "+" spend affordance
- AND activating the row SHALL NOT mutate `PlayerProgressionState`

### Requirement: Data-reuse invariant — no skill-logic duplication

`SkillContent` SHALL bind and mutate skill data EXCLUSIVELY through
`PcSkillPanelService.BuildPage` / `PcSkillPanelService.TryUpgrade` and the
`PcSkillPanelSnapshot` / `PcSkillPanelRow` model. The content class and `GameHudController`
SHALL NOT duplicate skill ordering, description text, level-cap computation, `canUpgrade`
logic, or `upgradeStatus`/`summary`/`nextLevelSummary` generation.
`PcSkillPanelService`'s public API and behavior SHALL remain unchanged.

#### Scenario: Snapshot is the single source of grid/detail text

- GIVEN the popup body is built
- WHEN any grid cell or detail field is rendered
- THEN every displayed skill name, level, summary, next-level, and status string SHALL come
  from a `PcSkillPanelRow` produced by `PcSkillPanelService.BuildPage`
- AND the content SHALL contain no locally-computed skill description or level-cap logic

### Requirement: Progression-grant preservation on open (gameplay-critical)

Opening the skill popup SHALL grant faction skill-panel progression, and this grant SHALL occur
BEFORE `PcSkillPanelService.BuildPage` is called. The grant SHALL resolve the active faction
(`progression.faction`, defaulting to `CombatFaction.CaiBang` when `None`) and SHALL invoke
`SandboxManager.GrantFactionSkillPanelProgression(faction)` when the sandbox is available, or
fall back to `PlayerProgressionState.GrantFactionSkillPanelProgression(catalog, faction)` when
it is not. The grant SHALL be idempotent: reopening the popup SHALL NOT change spent points or
skill levels beyond the first-open post-grant state.

#### Scenario: First open grants progression before building the page

- GIVEN a Cái Bang progression that has not yet received the skill-panel grant
- WHEN `BtnSkills` opens the popup
- THEN faction skill-panel progression SHALL be granted
- AND `BuildPage` SHALL run AFTER the grant so the snapshot reflects the granted skills/points
- AND the body SHALL render the granted state

#### Scenario: Reopen is idempotent

- GIVEN the popup was opened once (the grant already applied)
- WHEN the popup is closed and reopened
- THEN the grant SHALL be applied again with no additional effect
- AND skill points and learned levels SHALL match the first-open post-grant state

#### Scenario: Fallback grant path works without the sandbox

- GIVEN `SandboxManager.Instance` is null (EditMode)
- WHEN the popup opens
- THEN the open path SHALL use `PlayerProgressionState.GrantFactionSkillPanelProgression(catalog, faction)`
- AND the snapshot SHALL still reflect the granted skills/points

### Requirement: Popup layout hint — PC-footprint parity

`SkillContent` SHALL implement `IPopupLayoutHint` with non-zero `Width` and `Height` sized to
match the prior inline skill-panel footprint, so the popup occupies the same screen region as
the original inline panel. The parity target is the prior inline clamp
`Rect(338, 110, 205, 376)` (≈ width `205`, height `376`).

#### Scenario: Layout hint positions the window like the inline panel

- GIVEN a `SkillContent`
- WHEN its `IPopupLayoutHint` values are read
- THEN `Width` and `Height` SHALL be greater than zero
- AND `Width`/`Height` SHALL approximate the prior `205 × 376` inline footprint
- AND `Left`/`Top` SHALL position the window in the prior `338, 110` screen region

### Requirement: BtnSkills wiring via PopupManager

`OnSkillsClick` SHALL open the skill popup via
`PopupManager.Show(new SkillContent(...))`, using the same one-line `OnXxxClick` shape as the
sibling handlers. When `PopupManager.Instance` is null, `OnSkillsClick` SHALL no-op
gracefully (log and return, matching the sibling guards) rather than throw.

#### Scenario: BtnSkills opens a single popup window

- GIVEN the HUD is initialised with a bound `PopupManager`
- WHEN `BtnSkills` is clicked
- THEN exactly one `SkillContent` window SHALL be shown via `PopupManager`
- AND it SHALL share the same close/backdrop/single-focus behavior as the other popups

#### Scenario: Missing PopupManager does not throw

- GIVEN `PopupManager.Instance` is null
- WHEN `OnSkillsClick` runs
- THEN it SHALL return without throwing
- AND no inline skill panel SHALL be toggled

### Requirement: GameHudController de-inlining

`GameHudController` SHALL have its inline skill-panel implementation removed. The inline skill
fields, methods (`OpenSkillPanel`, `SetSkillPanel`, `CloseSkillPanel`, `SelectSkill`,
`TryUpgradeSelectedSkill`, `TryUpgradeSkill`, `PopulateSkillPanel`), and the inline-only
public surface (`IsSkillPanelVisible`, `PcSkillPanelRowCount`, `CurrentSkillSnapshot`,
`CurrentSelectedSkillId`, `CurrentSkillPageIndex`) SHALL be absent. `BindElements` SHALL no
longer query `CaiBangSkillPanel*` elements; the `RegisterClick` wiring for those elements and
the `SizeRootToScreen` skill-panel clamp SHALL be removed.

#### Scenario: Removed fields and methods are absent

- GIVEN the post-change `GameHudController`
- WHEN its declared members are enumerated
- THEN none of the listed skill fields, methods, or public properties SHALL be present
- AND no reference to a `CaiBang*` skill element name SHALL remain in the controller

### Requirement: IMGUI skill-panel render retirement

`PcHudVietnameseTextOverlay` SHALL no longer render the skill panel. `DrawSkillPanelText()`
and its call site in `OnGUI` SHALL be removed. The IMGUI assets/styles that served only this
path SHALL be removed. The `SkillContent` popup body SHALL be the SINGLE source of truth for
skill-panel visuals. The overlay SHALL contain no remaining reference to the removed HUD
skill-panel surface.

#### Scenario: Overlay no longer draws the skill panel

- GIVEN the post-change overlay with a skill popup open
- WHEN `OnGUI` runs
- THEN it SHALL NOT call any skill-panel draw routine
- AND no skill grid, skill-point summary, or skill detail SHALL be drawn by the overlay

### Requirement: Test migration (RED-first)

The system SHALL maintain `SkillContentTests` marked `[Category("Skill")]`, asserting the
content behavior: `TitleVi`, `IPopupLayoutHint` values, the 30-cell grid, the skill-point
summary, the selection toggle, the upgrade mutation of `PlayerProgressionState`, and the
progression-grant idempotency. The faction skill-panel fixtures SHALL drive `SkillContent`
(production path) while PRESERVING their existing PC-parity assertions (exact skill IDs,
Vietnamese display names, summary `"200"`, `30` slots). The `PcSkillPanelService` data-service
tests SHALL remain unchanged and green.

#### Scenario: Faction fixtures keep PC-parity assertions

- GIVEN a migrated faction fixture (e.g. Cái Bang)
- WHEN it drives the new `SkillContent`
- THEN it SHALL still assert the faction skill IDs, the Vietnamese names, the `"200"` summary,
  `30` grid slots, and the populated row count

#### Scenario: Data-service tests are untouched

- GIVEN the existing `PcSkillPanelService`-level tests
- WHEN the domain is extended
- THEN those tests SHALL remain unmodified and SHALL still pass
