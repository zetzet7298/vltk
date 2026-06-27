# Proposal — Mobile-Native HUD Layout (HUD-004)

> Change: `mobile-native-hud-layout`
> Explore input: folded inline (exa + deepwiki UX research + current-state audit below).
> Hard constraint: **NO fabricated art.** Every visual element MUST reuse an existing PC sprite
> already ported into the repo. Only the ARRANGEMENT changes to mobile-native ergonomics; the PC
> visual identity (sprites, icons, frame art) is preserved 1:1.
> `default_locale: vi`.

## Why

The current HUD is a faithful **PC-replica toolbar** (`快捷栏.spr` filigree frame + 9 numbered
hotbar slots + T/P skill slots + 6-button toggle row + 8-button menu row, all crammed across
the bottom-center). That layout is ergonomic for a desktop mouse/keyboard, not for two thumbs on
a 16:9 phone. Research confirms (Titan Quest mobile, ACM thumb-zone study, Smashing "Thumb Zone",
RuneScape mobile port):

- Bottom-center cramped bars fight the thumbs and hide the action.
- Movement belongs bottom-left; the skill/action cluster belongs bottom-right within the right
  thumb's natural arc (primary comfort radius ~27–41 mm, low-frequency buttons toward the 41 mm
  arc near the screen edge).
- Min tap target ≥ 44 pt; hit area should exceed the visual footprint; HUD must be thin and
  contextually minimal.
- The virtual joystick already exists and is polished but is **currently force-hidden** to match
  the PC baseline — that hide must be removed for real mobile play.

The user requirement: keep the PC art identity, but redesign the LAYOUT for a real mobile action
MMORPG — left joystick, right-hand combat cluster (1 main + 5 sub assignable slots), walk/run +
mount/dismount + meditate buttons near the combat cluster, 3 usable-item quick slots between the
combat cluster and the minimap, and relocate overflow PC UI into the empty minimap↔topbar region.
Bottom-center is reserved for a future chat canvas.

## What Changes (high level)

1. **Re-enable + reposition the joystick** — stop force-hiding `MobileJoystick`; anchor it
   bottom-left within the left-thumb zone (existing `MobileJoystick.cs` + `TouchInputService`
   are reused unchanged; only its enable/anchor changes).
2. **Right-hand combat cluster** — replace the single T/P pair with a **6-slot assignable combat
   bar**: 1 designated "main" slot (larger, sits under the thumb's natural rest point) + 5 "sub"
   slots arranged in a right-thumb fan/arc. All 6 are player-assignable (any active skill, or a
   Khinh Công / light-conduct action). Frames reuse `btn_skill_empty_pc.png` / the PC skill-slot
   sprites; skill icons reuse the `Generated/cai_bang_skill_*.png` catalog. "Main" is only the
   player's priority slot (visually bigger), not a fixed gameplay role.
3. **Right-hand action buttons** — `btn_run` (walk/run), `btn_horse` (mount/dismount),
   `btn_sit` (meditate/打坐) grouped beside the combat cluster, within right-thumb reach. These
   reuse the exact PC `PcButtons/btn_*.png` sprites.
4. **Usable-item quick slots** — 3 slots placed on the right side between the combat cluster and
   the minimap, for assignable consumables (e.g. **Ngũ Hoa Ngọc Lộ** HP/MP recovery). Frame art
   reuses the PC numbered-slot chrome from the `快捷栏` slot family (the same slot well sprites
   the PC toolbar uses for slots 1/2/3). Assignment + consume logic is the real binding; data
   layer (item parsers) already exists from archived changes.
5. **Relocate overflow PC UI into the minimap↔topbar gap** — the 8 menu buttons
   (`btn_char_f1`…`btn_chatroom`) and the buff/debuff panel move out of the bottom strip into the
   currently-empty top region (between the top status bar and the minimap), arranged as compact
   PC-icon buttons. This keeps every PC UI affordance reachable without bottom clutter.
6. **Reserve bottom-center for chat** — remove the PC `快捷栏` replica bottom strip from the
   center; the bottom-center lane is kept clear for the future mobile chat canvas (per user note).
   Chat already renders above the strip today; this formalizes the lane.
7. **Top bar + minimap unchanged** — HP/MP/EXP/Stamina/Level/WorldSort status bar and the
   minimap + its 4 map buttons stay exactly as today.

### Non-goals (explicit follow-up)
- Real combat execution wiring for the 6 combat slots (this change = LAYOUT + assignment UI;
  combat-skill firing is a separate gameplay change — currently the skill system already fires
  via `PcSkillPanelService`; the combat bar binds/assigns, a later change wires tap→fire if not
  already).
- Actual consumable effect of Ngũ Hoa Ngọc Lộ beyond assignment (consume → backend effect is a
  gameplay change).
- The future chat canvas itself (bottom-center is only RESERVED here).
- Redesigning top bar or minimap visuals.

## Sprite-Reuse Inventory (proves "no fabrication")

All needed art already exists in `Assets/UI/HUD/Art/`:

| Element | Reused sprite(s) |
|---|---|
| Combat slot frame (×6) | `btn_skill_empty_pc.png`, `btn_pc_left_skill_slot.png`, `btn_pc_right_skill_slot.png` |
| Combat skill icons | `Generated/cai_bang_skill_*.png` (164 icons) |
| Walk/Run | `PcButtons/btn_run.png` (+ `btn_run_over.png`) |
| Mount/Dismount | `PcButtons/btn_horse.png` (+ `btn_horse_over.png`) |
| Meditate/Sit | `PcButtons/btn_sit.png` (+ `btn_sit_over.png`) |
| Other toggles (trade/camera/pk) | `PcButtons/btn_trade.png`, `btn_camera.png`, `btn_pk.png` |
| 8 menu buttons | `PcButtons/btn_char_f1`…`btn_chatroom`, `btn_itemex` |
| Usable-item quick-slot chrome | PC `快捷栏` numbered-slot well sprites (slots 1/2/3 family) |
| Joystick art | existing joystick jade-medallion art already used by `MobileJoystick` |
| Top bar / minimap | unchanged existing art |

> Design phase must confirm the exact SPR hash for the numbered-slot chrome via
> `jx-pc-resource-resolver` and cross-check `_labels.json` (`name_vi`). If a specific quick-slot
> chrome SPR is unavailable, fall back to reusing the existing `快捷栏` slot well already cropped
> — still PC art, still no fabrication.

## UX Design Principles (from research)

- **Left thumb = movement** (joystick bottom-left). **Right thumb = actions** (combat cluster +
  action buttons bottom-right, fan arc within 27–41 mm comfort radius).
- **High-frequency actions near the thumb rest point** (the "main" combat slot + run/mount/
  meditate); low-frequency buttons toward the 41 mm arc / relocated to the top gap.
- **Tap target ≥ 44 pt**; hit areas may exceed visual footprints (invisible padding).
- **Contextual minimalism**: keep the bottom-center clear; show only what combat needs.
- **Anchor-based** USS (bottom-left, bottom-right, top-between) so it reflows across 16:9 devices;
  no raw pixel multiply (per the project's 4:3→16:9 rule).

## Scope / Impact

- **Edit**: `Assets/UI/HUD/GameHud.uxml` (restructure bottom area; add combat cluster, quick
  slots, relocated menu buttons; remove the `快捷栏` replica bottom-center strip).
- **Edit**: `Assets/UI/HUD/GameHud.uss` (new anchor-based classes for the clusters; wire the
  currently-`none` toggle/menu button icon backgrounds to the `PcButtons/` sprites).
- **Edit**: `Assets/Scripts/UI/GameHudController.cs` (remove `HideMobileJoystick` force-hide;
  bind the 6 combat slots + 3 quick slots + action buttons to assignment/consume handlers;
  relocate menu-button click wiring to the new positions).
- **Reused unchanged**: `MobileJoystick.cs`, `TouchInputService.cs`, `PcSkillPanelService`,
  item/equipment data services, top bar + minimap logic.
- **Tests**: EditMode category `HUD`/`MobileHud` — joystick enabled, 6 combat slots present,
  3 quick slots present, action buttons present & icon-art-wired, menu buttons relocated &
  still firing, bottom-center lane reserved (no combat element there), top bar/minimap untouched.
- **Risk: review workload** — this is a whole-HUD layout restructure (UXML + USS + C#), likely
  **> 400 changed lines** → forecast as a **chained/multi-slice** delivery (per auto-forecast
  preflight). Candidate slices: (S1) joystick re-enable + bottom strip removal + bottom-center
  lane reserved; (S2) right combat cluster (1+5) + action buttons; (S3) quick slots +
  minimap-gap menu relocation + icon wiring. Exact slicing finalized at `tasks` phase.

## Key Design Decisions

- **D1 — Layout is mobile-native, art is PC-native.** We abandon the PC bottom-toolbar
  *arrangement* but preserve every PC *sprite* 1:1. Fidelity to PC visual identity is mandatory;
  only ergonomics change.
- **D2 — "Main" combat slot is player-chosen priority, not a fixed role.** All 6 slots are
  assignable (skill or Khinh Công). "Main" is merely the bigger slot at the thumb rest point.
- **D3 — Bottom-center reserved for future chat.** No combat/quick-slot element lands in the
  bottom-center lane.
- **D4 — Overflow PC UI → minimap↔topbar gap.** The 8 menu buttons + buff panel relocate to the
  empty top region rather than being dropped, preserving every PC affordance.
- **D5 — Anchor-based, not pixel-multiply.** Clusters anchor to screen corners per the project's
  4:3→16:9 rule; no raw coordinate scaling of art.

## Open Items for Spec/Design (not product-blocking)

- Exact combat-cluster geometry (fan-arc angles, slot sizes in design px) — design finalizes.
- Whether action buttons (run/mount/meditate) sit beside the combat cluster (same fan) or form a
  small separate row just above it.
- Exact SPR hash for the usable-item quick-slot chrome (resolver in design phase).
- Slice boundaries for the chained delivery (tasks phase).

## Acceptance (draft)

- Vision check: mobile screenshot shows joystick bottom-left, 1+5 combat cluster bottom-right,
  run/mount/meditate beside it, 3 quick slots up the right side, 8 menu buttons + buff in the top
  gap, bottom-center clear, top bar + minimap identical to today.
- Every visible element traces to an existing PC sprite (no fabricated asset).
- HUD EditMode tests green; no regression to top bar/minimap/popups.
