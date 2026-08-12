# HUD Specification

> Domain: **hud**.
> Current baseline established by `mobile-native-hud-layout` / HUD-004.
> Historical note: HUD-002 (`add-bottom-bar-pc-frame`) recovered the real PC `快捷栏.spr`
> (hash `ebb69f9b`) and used it as a full bottom-strip frame. HUD-004 supersedes that desktop
> toolbar ARRANGEMENT for mobile play: the PC art identity is preserved, but the full bottom
> strip is no longer the canonical runtime layout. `default_locale: vi`.

## Purpose

The runtime HUD is a **mobile-native arrangement using PC sprites 1:1**. Movement belongs to the
bottom-left joystick; combat and high-frequency actions belong to the bottom-right thumb zone;
quick usable-item slots climb the right side; overflow PC menu buttons and buffs live in the
minimap↔topbar gap; bottom-center is reserved for the future chat canvas. The top status bar and
minimap remain PC-parity and unchanged.

Hard invariant: **no fabricated art**. Every visible HUD control introduced by the mobile layout
MUST use an existing ported PC sprite or a direct crop/extraction from a genuine decoded PC SPR.

## Requirements

### Requirement: Top bar and minimap unchanged

The top status bar (Level/Stamina/HP/MP/EXP/WorldSort + Vietnamese captions) and the minimap
(frame, content, player dot, scene name/position, 4 map buttons) SHALL remain unchanged from the
PC-parity baseline.

#### Scenario: Top bar and minimap render intact

- GIVEN the Sandbox scene is playing
- WHEN the HUD loads
- THEN the top status bar appears with Level, HP, MP, Stamina, EXP, WorldSort, and Vietnamese captions
- AND the minimap appears at top-right with its frame, dots, coordinates, and map buttons

### Requirement: Mobile joystick enabled bottom-left

The `MobileJoystick` SHALL be visible and active in mobile play, anchored bottom-left and layered
above the UIToolkit HUD. The HUD SHALL NOT force-hide it for PC-parity screenshots.

#### Scenario: Joystick controls movement

- GIVEN the Sandbox scene is playing
- WHEN the player drags the bottom-left joystick
- THEN the joystick emits movement through `TouchInputService`
- AND no HUD element covers the joystick input area

### Requirement: Combat cluster 1+5 in the right-thumb zone

The HUD SHALL provide exactly 6 assignable combat slots anchored bottom-right: 1 larger player
priority slot and 5 sub slots arranged in a right-thumb fan. All 6 slots SHALL be assignable to
skills or Khinh Công/light-conduct actions. Slot frames SHALL use ported PC skill-slot art.

#### Scenario: Combat cluster shape

- GIVEN the HUD loads
- THEN the combat cluster contains exactly one larger main slot and five sub slots
- AND the cluster sits bottom-right without overlapping joystick, chat, quick slots, top bar, or minimap

### Requirement: Right-hand action buttons

Walk/run, mount/dismount, and meditate/sit buttons SHALL sit beside the combat cluster in
right-thumb reach. They SHALL use existing PC sprites (`btn_run`, `btn_horse`, `btn_sit`) and
SHALL be distinct from the 6 combat slots.

#### Scenario: Action buttons use PC icons

- GIVEN the right-thumb combat region renders
- THEN run, horse, and sit buttons are present and show their PC icons

### Requirement: Three usable-item quick slots on the right side

The HUD SHALL provide exactly 3 usable-item quick slots on the right side between the combat
cluster and minimap. They SHALL use PC `快捷栏` numbered-well chrome (`btn_quick_item_1/2/3_pc`
or equivalent genuine PC extraction) and SHALL be assignable to usable consumables such as
Ngũ Hoa Ngọc Lộ. Backend consume effects are outside this HUD layout requirement; activation may
log a consume intent until gameplay logic lands.

#### Scenario: Quick slots do not overlap combat or minimap

- GIVEN the HUD loads
- THEN 3 quick slots are stacked on the right side between the minimap and combat cluster
- AND they do not overlap the combat cluster or minimap

### Requirement: Overflow PC menu buttons in the minimap↔topbar gap

The PC menu buttons (`BtnStatus`, `BtnItems`, `BtnItemEx`, `BtnSkills`, `BtnQuest`, `BtnTeam`,
`BtnFaction`, `BtnChatRoom`) and `BtnTreasure` SHALL live in the gap between the top status bar
and minimap as compact PC-icon buttons. The buff/debuff panel SHALL also render in this top-gap
region when buffs exist.

#### Scenario: Relocated menu buttons remain functional

- GIVEN the top-gap menu row is visible
- WHEN a relocated menu button is tapped
- THEN its existing handler runs (opening its popup when implemented, or safe no-op/log when deferred)
- AND no button overlaps the top status bar or minimap

### Requirement: Bottom-center reserved for chat

The bottom-center lane SHALL remain clear of combat slots, quick slots, action buttons, menu
buttons, and the old PC `快捷栏` full strip. The lane hosts the PC-parity chat bar (the 聊天条
surface defined in the **chat** domain), which is now implemented rather than reserved for the
future. The chat bar is the only content permitted in the bottom-center lane; it SHALL NOT be
displaced or overlapped by combat, quick-slot, action-button, menu, or toolbar clusters.
(Previously: the bottom-center lane was described as reserved for a "future mobile chat
canvas"; the chat bar is now implemented by change `port-pc-chat-bar-parity`.)

#### Scenario: No PC bottom strip

- GIVEN the HUD loads
- THEN the old full-width `快捷栏` bottom toolbar is absent
- AND bottom-center contains only the PC-parity chat bar

#### Scenario: Bottom-center hosts the chat bar without overlap

- GIVEN the HUD loads with the chat bar present in the bottom-center lane
- THEN the combat cluster, quick slots, action buttons, top-gap menu, top bar, and minimap do
  not overlap the chat bar
- AND the bottom-left joystick input lane remains touchable

### Requirement: Sprite-reuse invariant

Every introduced or repositioned visible HUD element SHALL use existing ported PC art:
combat frames (`btn_skill_empty_pc` / PC skill-slot frames), action/menu PC buttons, generated
skill icons, and quick-slot PC numbered wells. The change SHALL NOT introduce fabricated art.

#### Scenario: Asset audit passes

- GIVEN the HUD asset list
- WHEN visible control art is inspected
- THEN each element traces to an existing ported PC sprite or a genuine PC crop/extraction

### Requirement: Anchor-based layout

HUD clusters SHALL use anchor-based layout for 16:9 mobile screens: joystick bottom-left,
combat cluster bottom-right, quick slots right side, menu/buffs top-gap. Sprites SHALL preserve
aspect ratio and SHALL NOT be raw-stretched.

#### Scenario: 16:9 layout has no overlap

- GIVEN the HUD renders at the 1280×720 design resolution
- THEN the joystick, combat cluster, quick slots, top-gap menu, top bar, minimap, and chat lane
  occupy their own regions without overlap

### Requirement: HUD regression tests stay green

The HUD EditMode category SHALL cover the mobile layout structure and remain green: joystick
visibility contract, combat cluster, quick slots, top-gap menu, top bar/minimap regression, and
bottom-center chat lane reservation.

#### Scenario: HUD tests pass

- GIVEN the HUD EditMode tests
- WHEN category `HUD` runs
- THEN all HUD tests pass with zero failures

## Legacy / Follow-up

- HUD-002 PC `快捷栏` full bottom-frame layout is archived as a historical PC-parity baseline and
  is no longer the canonical runtime mobile arrangement.
- Future chat canvas implementation owns the reserved bottom-center lane.
- Real combat tap→fire refinements and consumable backend effects remain gameplay follow-ups.
- Further PC menu popups (Quest/ChatRoom/ItemEx) may replace current safe no-op/log handlers.
