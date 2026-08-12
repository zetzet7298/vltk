# Change Proposal: Port Khinh Công and Action Buttons

## Intent

Improve the mobile combat HUD by making the right-thumb controls useful and PC-source backed:

- Swap the currently empty combat sub-slot with assigned sub-slot 1 so the first visible sub-slot keeps an assigned combat skill.
- Port PC Khinh Công (lightness skill) from `jx-pc`, including its PC icon, and assign it to the slot made empty by the swap.
- Replace the current action-button no-op/log behavior with real walk/run, mount/dismount, and meditate/sit behavior where existing Unity runtime services already support it.

## Problem

The current mobile HUD exposes five combat sub-slots and action buttons (`ActionBtnRun`, `ActionBtnHorse`, `ActionBtnSit`), but one combat sub-slot is empty and the three action buttons are currently safe stubs/no-op logs. This makes core PC movement actions appear present but non-functional, and leaves Khinh Công unavailable from the right-thumb combat cluster despite being a canonical PC special skill.

## Scope

### In scope

1. **Combat sub-slot assignment**
   - Preserve `MobileSkillSlotCount = 5`.
   - Swap the currently empty combat sub-slot with assigned sub-slot 1.
   - Assign PC Khinh Công skill `SkillId=210` into the newly empty sub-slot.
   - Keep slot rendering and assignment keyed by `skillId`, not by display order alone.

2. **PC Khinh Công data and icon port**
   - Add Khinh Công to the Unity combat/skill catalog if it is missing.
   - Decode/import its PC icon into the existing generated HUD skill-icon asset flow.
   - Bind the imported icon so the assigned slot displays the real PC icon.

3. **Action button behaviors**
   - `ActionBtnRun`: toggle walk/run state and ensure player movement speed/visual state reflects the selected mode.
   - `ActionBtnHorse`: call the existing mount service / `SandboxPlayerController.ToggleMount` path to mount or dismount.
   - `ActionBtnSit`: toggle meditation/sit state, using PC-derived behavior where available and a minimal Unity runtime state only where PC source is missing.
   - Keep action buttons distinct from combat slots, as required by the existing HUD spec.

4. **Tests / verification target for implementation phase**
   - Add or update targeted EditMode tests around slot defaults, Khinh Công assignment/icon mapping, and action-button wiring.
   - Use targeted test categories/namespaces during development; do not run the full EditMode suite except as a final gate if shared systems are changed.

### Out of scope

- Reworking the mobile HUD layout beyond the requested slot swap and action-button behavior.
- Changing the number of combat sub-slots or moving the combat cluster.
- Porting full Khinh Công VFX, jump animation, stamina formulas, or PvP/terrain edge rules unless directly required for a minimal usable button.
- Implementing consumable quick-slot backend effects.
- Replacing top bar, minimap, chat, menu, or quick-slot behavior.

## Affected areas

- `Assets/UI/HUD/GameHud.uxml` and `Assets/UI/HUD/GameHud.uss` only if visual slot ordering or labels need adjustment.
- `Assets/Scripts/UI/CombatSkillSlotController.cs` for default deck/slot assignment and Khinh Công slot behavior.
- `Assets/Scripts/UI/GameHudController.cs` for action-button click handlers currently wired as stubs.
- `Assets/Scripts/Sandbox/SandboxPlayerController.cs` and existing mount/movement services for walk/run, mount/dismount, and meditate/sit runtime state.
- Skill catalog/runtime data under `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs` or the existing catalog source if Khinh Công is not already represented.
- HUD generated skill-icon assets and provenance docs for the Khinh Công icon.
- Targeted EditMode tests under `Assets/Tests/EditMode/...`.

## PC evidence

| Item | Evidence |
| --- | --- |
| Khinh Công skill | `Assets/StreamingAssets/Reference/PcSkills.txt` line ~211: Vietnamese row `Khinh công`, `SkillId=210`. |
| PC skill icon path | Canonical GBK path: `\spr\Ui\技能图标\轻功.spr`. The Reference file displays the same path mojibaked as `\spr\Ui\技能图标\ầỏạƯ.spr` due encoding. |
| PC icon hash | JX Pack hash `gbk=bf787a8a`, found at `/var/www/jx-pc/pak_unpacked/update01/unknown/bf787a8a.spr` and `/var/www/jx-pc/pak_unpacked/spr/unknown/bf787a8a.spr`. |
| PC script path | Canonical path: `\script\skill\special\轻功.lua`; Reference file displays mojibaked `\script\skill\special\ầỏạƯ.lua`. |
| Current combat HUD | `Assets/UI/HUD/GameHud.uxml` defines `SkillSlot0..4`, `PrimaryAttackBtn`, and action buttons `ActionBtnRun`, `ActionBtnHorse`, `ActionBtnSit`. |
| Current default Cái Bang deck | `CombatSkillSlotController.DefaultDeckByFaction` currently assigns `{357,358,1073,130,127}` with `MobileSkillSlotCount=5`. |
| Current action handlers | `GameHudController` loads action icons and wires `OnRunClick`, `OnHorseClick`, `OnSitClick`; comments indicate these are currently no-op/log stubs. |
| Existing runtime support | `SandboxPlayerController` already has movement speed logic, `PlayerMountService`, and `ToggleMount` support. |

### Source caveat

The mandatory PC source path from the porting rule, `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem`, is absent on disk in this environment. Implementation must therefore treat `/var/www/vltk-mobile/Assets/StreamingAssets/Reference/PcSkills.txt` plus `/var/www/jx-pc/pak_unpacked/...` as the available PC evidence for this proposal, and should record that caveat in code/provenance where PC runtime behavior cannot be fully verified.

## Risks and mitigations

- **Slot-index drift**: swapping visible slots can accidentally change skill identity if code relies on array index. Mitigate by keeping display/icon/cast behavior keyed by `skillId` and covering with tests.
- **Khinh Công behavior ambiguity**: full PC behavior may live in missing source/Lua. Mitigate by implementing only the minimal source-backed assignment/icon first, and clearly marking any provisional runtime behavior.
- **Action button state conflicts**: walk/run, sit, mount, joystick movement, click-to-move, and dash can conflict. Mitigate by defining state precedence: dash/combat overrides movement, sit cancels movement and dismount/mount transitions cancel sit as needed.
- **Mobile touch regression**: action buttons must remain pickable without blocking joystick or combat slots. Mitigate with HUD interaction tests and existing joystick safety rules.
- **Asset orientation/provenance**: decoded SPR icons may be vertically flipped or mistraced. Mitigate by using the resolver hash, decoding from the found PC SPR, visual inspection, and provenance documentation.

## Rollback

Rollback is straightforward and localized:

1. Restore the previous default deck/slot assignment values.
2. Remove Khinh Công from the default combat slot assignment while leaving imported PC art harmlessly unused.
3. Revert `OnRunClick`, `OnHorseClick`, and `OnSitClick` to their previous no-op/log behavior if runtime regressions appear.
4. Keep the existing HUD layout unchanged unless implementation required a minimal slot-label/order adjustment.

## Success criteria / acceptance

- The combat cluster still has exactly one main slot and five sub-slots.
- The previously empty combat sub-slot and assigned sub-slot 1 are swapped as requested.
- The newly empty slot is assigned PC Khinh Công `SkillId=210`.
- Khinh Công displays the real PC icon decoded from `bf787a8a.spr`, with provenance recorded.
- Tapping run toggles walk/run behavior rather than only logging.
- Tapping horse mounts/dismounts through the existing mount runtime path.
- Tapping sit toggles meditate/sit state and cancels incompatible movement as designed.
- No unrelated HUD regions, menu buttons, minimap, top bar, quick slots, or chat layout are changed.
- Targeted tests for slot assignment/icon mapping/action wiring pass.

## Proposal question round

The implementation scope appears narrow, but these product questions should be reviewed before apply to avoid overbuilding or choosing the wrong behavior:

1. Should Khinh Công in this first slice be **only a combat-slot skill with icon/assignment**, or should tapping it immediately perform a movement/jump/lightness action if enough PC behavior can be recovered?
2. For walk/run, should the default state on mobile remain the current fast movement, or should it match PC by starting in one explicit state and toggling speed/visual label each tap?
3. For meditate/sit, what is the first-slice outcome: visual sit pose only, passive recovery behavior, or both if PC formulas are available?
4. When mounted, should pressing meditate automatically dismount, be blocked, or do nothing with a user-facing message?
5. Should Khinh Công be assigned only for the Cái Bang sandbox default deck for now, or become a universal default special skill across factions when the combat cluster initializes?

Assumption for this proposal: implement the smallest source-backed slice now: universal Khinh Công skill/icon assignment in the requested empty slot, real walk/run and mount toggles using existing Unity services, and minimal sit/meditate state behavior without broad HUD/layout changes.