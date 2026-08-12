# Spec — Mobile-Native HUD Layout (HUD-004)

> Change: `mobile-native-hud-layout`
> Spec format: requirement/scenario deltas vs the current PC-replica HUD baseline.
> Hard constraint (carried from proposal): **NO fabricated art.** Every visible element MUST trace
> to an existing ported PC sprite. Only the ARRANGEMENT becomes mobile-native; the PC visual
> identity is preserved. UI Toolkit design space is **1280×720** (16:9). `default_locale: vi`.

## Requirements

### Requirement: Movement joystick is enabled and anchored bottom-left

The force-hide of `MobileJoystick` SHALL be removed. The joystick SHALL be active in mobile play
and SHALL be anchored in the left-thumb zone (bottom-left), reusing the existing `MobileJoystick`
component and `TouchInputService` unchanged. The joystick SHALL remain above the UIToolkit HUD
layer so it never gets covered.

#### Scenario: Joystick is visible and active in mobile play

- GIVEN the Sandbox scene is playing
- WHEN the HUD loads
- THEN the virtual joystick SHALL be visible and active at the bottom-left
- AND `MobileJoystick.gameObject.activeSelf` SHALL be true
- AND the player character SHALL move when the joystick is dragged

#### Scenario: Joystick layering is preserved

- GIVEN the joystick and the HUD are both rendered
- THEN the joystick sortingOrder SHALL keep it above the UIToolkit HUD panel
- AND no HUD element SHALL cover the joystick interaction area

### Requirement: Right-hand combat cluster — 1 main + 5 sub assignable slots

The HUD SHALL provide a combat cluster of exactly **6 slots** anchored in the right-thumb zone
(bottom-right): 1 designated "main" slot and 5 "sub" slots arranged within the right thumb's
natural fan arc. All 6 slots SHALL be player-assignable: each may hold any active fight skill OR
a Khinh Công (light-conduct) action. The "main" slot SHALL be visually larger than the sub slots
(ergonomic emphasis) but SHALL NOT carry a fixed gameplay role — it is the player's chosen
priority slot. Slot frames SHALL reuse the PC skill-slot sprites
(`btn_skill_empty_pc.png` / `btn_pc_left_skill_slot.png` / `btn_pc_right_skill_slot.png`); skill
icons SHALL reuse the `Generated/cai_bang_skill_*.png` catalog.

#### Scenario: Combat cluster has exactly 6 slots

- GIVEN the HUD loads
- WHEN the combat cluster renders
- THEN it SHALL contain exactly 6 slots
- AND exactly 1 slot SHALL be the larger "main" slot
- AND exactly 5 slots SHALL be "sub" slots

#### Scenario: All six slots are assignable

- GIVEN the combat cluster and a learned skill catalog
- WHEN the player assigns skills into the 6 slots
- THEN each slot SHALL accept any active fight skill or a Khinh Công action
- AND the "main" slot SHALL accept the same kinds of assignments as a sub slot (no fixed role)

#### Scenario: Combat slot frames reuse PC sprites

- GIVEN the combat cluster renders
- WHEN any slot's frame art is inspected
- THEN it SHALL use a PC skill-slot sprite (no fabricated frame)

### Requirement: Action buttons beside the combat cluster

The walk/run toggle, mount/dismount, and meditate (sit) buttons SHALL be present in the
right-thumb zone, grouped beside the combat cluster. Each SHALL reuse the exact PC sprite:
`PcButtons/btn_run.png`, `PcButtons/btn_horse.png`, `PcButtons/btn_sit.png` (with their `_over`
hover states). These buttons SHALL be distinct from the 6 combat slots.

#### Scenario: Three action buttons present with PC icons

- GIVEN the HUD loads
- WHEN the right-thumb zone renders
- THEN walk/run, mount/dismount, and meditate buttons SHALL be present beside the combat cluster
- AND each button's icon SHALL be the matching PC sprite (not `background-image: none`)

#### Scenario: Action buttons are separate from combat slots

- GIVEN the combat cluster and the action buttons
- THEN the 3 action buttons SHALL be distinct elements from the 6 combat slots

### Requirement: Usable-item quick slots (3) up the right side

Three usable-item quick slots SHALL be placed on the right side, between the combat cluster and
the minimap, for assignable consumables (e.g. **Ngũ Hoa Ngọc Lộ** — HP/MP recovery). Their frame
chrome SHALL reuse the PC numbered-slot well sprites (the `快捷栏` slots 1/2/3 family) resolved via
`jx-pc-resource-resolver`; if that specific SPR is unavailable, they SHALL fall back to reusing
the existing `快捷栏` slot-well art already cropped (still PC art, no fabrication). Each slot SHALL
be assignable to a usable item from the player inventory.

#### Scenario: Three quick slots placed up the right side

- GIVEN the HUD loads
- WHEN the right side renders
- THEN exactly 3 usable-item quick slots SHALL appear, ascending from the combat cluster toward
  the minimap
- AND each quick-slot frame SHALL be PC slot-chrome (no fabricated art)

#### Scenario: Quick slots accept a usable item

- GIVEN the inventory contains a usable item (e.g. Ngũ Hoa Ngọc Lộ)
- WHEN the player assigns it to a quick slot
- THEN the slot SHALL display the item icon and SHALL be activatable
- (Actual consume effect is a separate gameplay change — this requirement covers assignment UI only.)

### Requirement: Overflow PC UI relocated to the minimap↔topbar gap

The 8 menu buttons (`btn_char_f1`…`btn_chatroom` / `btn_itemex`) and the buff/debuff panel SHALL
be relocated out of the bottom strip into the empty region between the top status bar and the
minimap, as compact PC-icon buttons. Every menu button SHALL remain reachable and its click
handler (`OnXxxClick` → `PopupManager.Show`) SHALL still fire.

#### Scenario: Menu buttons relocated and still firing

- GIVEN the HUD loads
- WHEN the 8 menu buttons render
- THEN they SHALL appear in the minimap↔topbar region (NOT in the bottom strip)
- AND tapping each menu button SHALL open its popup through `PopupManager` (no regression)

#### Scenario: Buff panel relocates to the gap

- GIVEN a buff/debuff exists
- WHEN the buff panel renders
- THEN it SHALL appear in the minimap↔topbar region (NOT the bottom strip)

### Requirement: Bottom-center lane reserved for future chat

No combat slot, quick slot, action button, or menu button SHALL occupy the bottom-center lane.
The bottom-center region SHALL be kept clear for the future mobile chat canvas.

#### Scenario: Bottom-center is clear of control elements

- GIVEN the HUD loads
- WHEN the bottom-center lane is inspected
- THEN it SHALL contain no combat slot, quick slot, action button, or menu button
- AND the chat panel (current) SHALL remain the only bottom-area content, ready for the future
  chat canvas

### Requirement: Top bar and minimap unchanged (regression guard)

The top status bar (Level/Stamina/HP/MP/EXP/WorldSort + Vietnamese captions) and the minimap
(frame, content, player dot, scene name/pos, 4 map buttons) SHALL remain byte-for-byte identical
to the current HUD in layout, art, and behavior.

#### Scenario: Top bar unchanged

- GIVEN the HUD loads
- WHEN the top status bar renders
- THEN every bar (HP/MP/EXP/Stamina), Level, WorldSort, and caption SHALL match the pre-change HUD

#### Scenario: Minimap unchanged

- GIVEN the HUD loads
- WHEN the minimap renders
- THEN the frame, content, player dot, scene name/pos, and 4 map buttons SHALL match the pre-change HUD
- AND tapping a map button SHALL still open the map preview (no regression)

### Requirement: Sprite-reuse invariant — no fabricated art

Every visible HUD element introduced or repositioned by this change SHALL use an existing ported
PC sprite. The change SHALL NOT introduce any newly-authored/fabricated graphic asset. The
usable-item quick-slot chrome is the only element whose exact SPR is resolved in design; if its
specific SPR is unavailable it falls back to an existing `快捷栏` crop (still PC art).

#### Scenario: No new art asset is authored

- GIVEN the change's diff
- WHEN the added/modified asset files are inspected
- THEN no fabricated graphic asset SHALL be introduced
- AND every visible element SHALL trace to a pre-existing PC sprite listed in the proposal inventory

### Requirement: Anchor-based layout, no raw pixel-multiply of art

The combat cluster, quick slots, action buttons, and relocated menu buttons SHALL use
anchor-based USS layout (anchored to bottom-left, bottom-right, and top-between regions) so the
layout reflows across 16:9 devices. Art SHALL NOT be raw-multiplied by a scale factor; aspect
ratios of sprites SHALL be preserved.

#### Scenario: Clusters anchor to screen regions

- GIVEN the HUD loads at the 1280×720 design space
- WHEN the layout is computed
- THEN the joystick SHALL anchor bottom-left, the combat cluster + action buttons + quick slots
  SHALL anchor bottom-right / right side, the menu buttons + buff SHALL anchor top-between
- AND no sprite SHALL be stretched out of its original aspect ratio

### Requirement: EditMode test coverage

EditMode tests (category `HUD`/`MobileHud`) SHALL cover: joystick enabled; exactly 6 combat
slots (1 main + 5 sub); 3 quick slots present; run/mount/meditate buttons present and
icon-art-wired; 8 menu buttons relocated to the gap and still firing; bottom-center lane free of
control elements; top bar and minimap structurally unchanged.

#### Scenario: Mobile HUD tests stay green

- GIVEN the change's EditMode tests
- WHEN they run under category `HUD` (or `MobileHud`)
- THEN they pass with zero failures
- AND the pre-existing HUD EditMode tests remain green (no regression)

## Non-Goals

- Real combat tap→fire wiring for the 6 combat slots (assignment UI only; firing via existing
  `PcSkillPanelService` if already wired, else a separate gameplay change).
- Actual consumable backend effect of Ngũ Hoa Ngọc Lộ beyond assignment.
- The future chat canvas itself (bottom-center only RESERVED here).
- Redesigning the top bar or minimap visuals.
- Migrating remaining inline panels (Trade/Stall/Face picker) — already out of scope.

## Open Items for Design

- Combat-cluster fan geometry (angles, slot sizes in design px).
- Whether action buttons share the combat fan or form a separate row.
- Exact SPR hash for usable-item quick-slot chrome (resolver in design).
- Slice boundaries for the chained delivery (tasks phase).
