# Popup Window System Specification

> Domain: **popups** (canonical spec, established by change `add-popup-window-system` / HUD-003).
> Covers the reusable popup shell (`PopupWindow` / `PopupManager` / `IPopupContent`) plus the
> feature windows delivered alongside it (Character Info, Inventory, Treasure, Team, Faction).
> The Skill window content has its own canonical domain `skill-panel`. All shipped art is
> Vietnamese SPR variants; no Chinese-text art. UI Toolkit design space is **1280×720**.
> `default_locale: vi`.

## Purpose

Provide a reusable, content-agnostic popup window infrastructure so that every HUD toolbar
feature button (`BtnStatus`, `BtnItems`, `BtnTreasure`, `BtnTeam`, `BtnFaction`, `BtnSkills`)
opens its feature through the shared `PopupManager` rather than ad-hoc inline panels. The shell
owns the ornate chrome (frame, title, close, backdrop, focus/z-order); each feature supplies its
body via `IPopupContent`.

## Requirements

### Requirement: Popup shell is reusable and content-agnostic

The system SHALL provide a `PopupWindow` shell (frame chrome + title bar + close button +
focus/z-order) that renders any feature body supplied via an `IPopupContent` contract, with no
feature-specific code inside the shell.

#### Scenario: Content renders inside the shell

- GIVEN a `PopupWindow` instantiated with any `IPopupContent`
- WHEN the window renders
- THEN the content's body renders inside the shell
- AND the content class owns its own tabs/slots/buttons while the shell owns the frame, title,
  and close affordance

### Requirement: PopupManager hosts windows with single-focus

A `PopupManager` SHALL act as the single overlay host on the HUD root. It SHALL render a dim
backdrop behind the active window and SHALL enforce single-focus: opening a window brings it to
front; by default only one window is interactive at a time.

#### Scenario: Show adds backdrop + focuses the window

- GIVEN the HUD has a bound `PopupManager`
- WHEN `PopupManager.Show(content)` is called
- THEN the backdrop + window are added to the HUD overlay
- AND the window is brought to front and marked focused

#### Scenario: Single-focus closes the prior window

- GIVEN a window is already open
- WHEN another window is shown
- THEN the previously focused window is closed (or unfocused)
- AND only one interactive window remains

#### Scenario: Close restores HUD interactivity

- GIVEN a window is open
- WHEN `PopupManager.Close()` (or the window's close button, or a backdrop tap) is invoked
- THEN the window + backdrop are removed and HUD interactivity is restored

### Requirement: Close button is Vietnamese "Đóng" SPR

The close affordance SHALL use the decoded `关闭_vn.spr` art (renders the text **"Đóng"**), with
normal/hover/press states. No Chinese-text close art shall ship.

#### Scenario: Close button shows Vietnamese "Đóng"

- GIVEN a popup window is open
- WHEN the close button renders
- THEN its background is the `关闭_vn.spr` frames (btn_close_vn)
- AND the rendered text is "Đóng"

### Requirement: Feature windows open through PopupManager

Each delivered feature window (Character Info, Inventory, Treasure, Team, Faction) SHALL open
through `PopupManager.Show(new XxxContent(...))` from its corresponding HUD button handler, using
the same one-line `OnXxxClick` shape. When `PopupManager.Instance` is null, the handler SHALL
no-op gracefully (log and return) rather than throw.

#### Scenario: Feature button opens one window

- GIVEN the HUD is initialised with a bound `PopupManager`
- WHEN a feature button (e.g. BtnStatus) is tapped
- THEN exactly one corresponding content window SHALL be shown via PopupManager
- AND it shares the same close/backdrop/single-focus behavior as the other popups

#### Scenario: Missing PopupManager does not throw

- GIVEN `PopupManager.Instance` is null
- WHEN a feature button handler runs
- THEN it SHALL return without throwing

### Requirement: Character Info window matches the reference layout

The Character Info window SHALL reproduce the reference layout: 3 tabs (**Thuộc tính / Trang bị /
Đánh giá**), a header (character name + PK + Trùng sinh + character watermark), an equipment
paperdoll, and action buttons (**Khóa / Đính / Tháo**), all inside the ornate frame with the
**Đóng** close button.

#### Scenario: Tabs switch the visible body

- GIVEN the Character Info window is open
- WHEN a tab is tapped
- THEN the visible body switches to that tab's content
- AND Trang bị is the initial/default tab

### Requirement: Trang bị paperdoll binds real equipment data where it exists

The paperdoll SHALL display equipment slots laid out per the reference and SHALL bind real data
for the slots that have a backing data source:

- Bound (real data): Weapon, Armor (Body), Helmet (Head), Mount — via
  `PlayerEquipmentService.GetVariant(slot)` + `SandboxManager.Instance.ItemDb.Resolve(itemId)`
  for the icon.
- Framework slots (mapping-known): Ring, Necklace, Belt, Boots — read from
  `EquipmentSlotMappingService`; rendered as labeled slots.
- Display-only framework slots: Mask, Amulet (×2), Charm, Trinket (×2) — visible, labeled, empty.

#### Scenario: Bound slot resolves item icon

- GIVEN `PlayerEquipmentService` reports a Weapon variant
- WHEN the Weapon slot renders
- THEN the item icon is resolved from `ItemDb` and shown

#### Scenario: Unequipped bound slot shows empty frame

- GIVEN a bound slot has no equipped variant
- WHEN the slot renders
- THEN it shows its empty labeled frame

### Requirement: Thuộc tính tab binds player stats

The Thuộc tính tab SHALL display player attributes bound to `PlayerStateResponse` (level, exp,
transLife/Trùng sinh, freePoint, magicPoint, strength, dexterity, vitality, spirit, series,
money, repute). No fabricated/hardcoded stat values shall be shown as live data.

#### Scenario: Stat sourced from the response

- GIVEN a `PlayerStateResponse` with strength=35
- WHEN the Thuộc tính list renders
- THEN Sức Mạnh shows 35 sourced from that response (or a clearly-labeled placeholder when no
  response is available)

### Requirement: Đánh giá tab present, content deferred

The Đánh giá tab SHALL be present and selectable. Its body is a clearly-marked "sắp ra mắt"
(coming soon) placeholder — no appraisal logic.

#### Scenario: Placeholder body, restores on tab switch

- GIVEN the Character Info window is open
- WHEN Đánh giá is selected
- THEN a "sắp ra mắt" placeholder body shows
- AND selecting another tab restores that tab's real content

### Requirement: Action buttons present, non-destructive

Khóa / Đính / Tháo buttons SHALL be present and clickable. They SHALL be non-destructive: each
logs its action via `SubsystemLog` and performs no gameplay mutation. (Real equip/unequip/socket
gameplay is a separate gameplay-touching domain — see Follow-up.)

#### Scenario: Tháo logs without mutating state

- GIVEN the Character Info window is open
- WHEN "Tháo" is tapped
- THEN the unequip intent is logged
- AND no equipment state changes

### Requirement: EditMode test coverage

EditMode tests SHALL cover: PopupManager open/single-focus/close lifecycle; CharacterInfo binds
real equipment (Weapon/Armor/Helmet/Mount) from a seeded `PlayerEquipmentService`; tab switching
between the 3 tabs; Thuộc tính stat bind from a seeded `PlayerStateResponse`.

#### Scenario: Popup test category stays green

- GIVEN the change's EditMode tests
- WHEN they run under category `Popup`
- THEN they pass with zero failures

## Out of Scope (explicit follow-up — separate changes/domains)

- Real equip/unequip/socket (Khóa/Đính/Tháo) gameplay logic (gameplay-touching; item
  parser + accessory equip-binding data layers already delivered in archived changes).
- Mask/Amulet/Charm/Trinket data binding (display-only framework slots in this domain).
- Drag-to-move window, resize, window persistence.
- Migrating remaining inline panels (Trade / Stall / Face picker) onto the new base.
- Đánh giá appraisal logic.
