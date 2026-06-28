# Apply Progress: Port Khinh Công and Action Buttons

## Status: implementation complete, verification in progress

## Implementation evidence

### Slot swap + Khinh Công assignment
- `CombatSkillSlotController.cs`:
  - Default Cái Bang deck changed from `{357,358,1073,130,127}` to `{210,357,358,1073,130}`. Slot 0 (sub-slot 1, lower-left) now holds PC Khinh Công (`SkillId=210`). The previous sub-slot-1 skill (357) is preserved in the next slot.
  - Added `MigrateCaiBangDeckToKhinhCongDefaultIfNeeded()` so already-serialized scenes with the legacy deck or zero slots migrate to the new deck at runtime (the empty slot the user reported was a stale serialized deck).
  - Added helpers `ContainsSkill`, `ContainsEmptySlot`, `MatchesDeck` and `LegacyCaiBangDefaultDeck` constant.
- Catalog:
  - `PcCombatCatalogFactory.cs` adds `UniversalLightnessSkill = 210` and registers it via `UtilitySkill(210, "轻功", "Khinh công", ...)` from PC `Skills.txt` evidence (icon `轻功.spr`, script `轻功.lua`).
  - Icon switch maps `210 => \spr\Ui\技能图标\轻功.spr` and `358 => icon_sk_gb_41.spr` (Long-family alias).

### PC icon provenance
- Decoded frame 0 of PC SPR hash `bf787a8a.spr` (`\spr\Ui\技能图标\轻功.spr`) via `~/Projects/vltktool/extract_item_spr.py`.
- Saved `cai_bang_skill_210.png` (30x30 RGBA) into BOTH generated asset roots the HUD loader uses:
  - `Assets/UI/HUD/Art/Generated/cai_bang_skill_210.png`
  - `Assets/StreamingAssets/UI/HUD/Art/Generated/cai_bang_skill_210.png` (this is the path the runtime `LoadIcon` actually reads — initial miss was placing it only under `Assets/UI`).
- Added `cai_bang_skill_358.png` alias (copy of 128 / icon_sk_gb_41, same Long family) so no sub-slot is empty.
- Updated `PC_SOURCE.txt` in both roots with PC path, hash `bf787a8a`, resolved SPR file paths, and the 358 alias note.

### Action buttons (walk/run, mount, meditate)
- `SandboxPlayerController.cs`:
  - Added `IsRunning`, `IsMeditating`, `walkSpeedMultiplier`, `ToggleWalkRun()`, `ToggleMeditation()`.
  - Movement tick now applies `IsRunning ? 1 : walkSpeedMultiplier`; meditation zeroes input/target and blocks movement; mount toggle exits meditation first; keyboard/joystick input is ignored while meditating.
- `GameHudController.cs`:
  - `OnRunClick`, `OnSitClick`, `OnHorseClick` now bridge to `SandboxManager.Instance.PlayerController` (`ToggleWalkRun`, `ToggleMeditation`, `ToggleMount`) instead of no-op logs.

### Tests
- `CombatSkillSlotTests.cs`:
  - Updated default-deck assertion to `{210,357,358,1073,130}` and added "Khinh công appears exactly once" check.
  - Added `Catalog_ResolvesPcKhinhCongSpecialSkill` (asserts id 210, Vietnamese name, faction None, icon path, radius, style).
- `PlayerMovementTests.cs`:
  - Added `SandboxController_WalkRunToggle_ChangesMovementDistance` (run distance > walk distance).
  - Added `SandboxController_Meditation_CancelsAndBlocksMovementUntilToggledOff` (meditation clears target, blocks movement, releases on second tap).

## Verification results

- Unity compile: 0 errors.
- EditMode HUD category: 16/16 PASS.
- EditMode `CombatSkillSlotTests`: 25/25 PASS (deck/catalog tests green).
- EditMode `PlayerMovementTests`: 13/13 PASS (new walk/run + meditation tests green).
- Runtime HUD screenshot (vision): PASS — all 5 sub slots filled, Khinh Công footprint icon visible in sub-slot 1, run/horse/sit action buttons show PC sprites, no layout regression.
- Pre-existing unrelated failure: `CombatRuntime_BuffStates_ApplyAddedDamageAndResistances` fails with `damage1=0` on BOTH clean dev HEAD (changes stashed) and with this change. Confirmed pre-existing test-isolation/runtime issue, not introduced by this change. File a follow-up; do not block this change.

## Follow-up runtime fix: true PC Walk/Sit/Jump actions

After runtime QA, mount/dismount worked but Khinh Công, walk/run, and meditate were still visually no-op or functionally gated. Additional PC-source research found the missing player action suffixes in `vltksource_new` `Client 6.0/settings/npcres/男主角未骑马关联表.txt` and `男主角躯体.txt`:

- `Walk` / `走路` → `WK01..WK04` per weapon type.
- `SitDown` / `打坐` → `ZZ01` shared suffix.
- `JumpFly` / `跳跃` (Khinh Công visual) → `JP01` shared suffix.
- Verified real PC SPR hashes exist for male armor variant 019 in `pak_unpacked` and staged missing runtime SPRs into `Assets/StreamingAssets/Sprites/`.

Implementation follow-up:
- Added `PlayerVisualAction.Walk`, `Sit`, `Jump` and suffix resolution in male/female catalogs.
- Added `walkMode` + `isMeditating` to `IPlayerVisual`; male/female visuals now play `WK*` while walking and force `ZZ01` while meditating.
- `Sit` and `Jump` actions are one-shot / hold-final-frame, matching PC behavior (sit-down should hold seated frame instead of looping back to standing).
- Added `SkillDefinition.isLeapSkill`, marks skill 210 Khinh Công as leap, and changed the cast pipeline to call `SandboxPlayerController.BeginLeap()` (JP01 + forward dash) instead of skipping when `dashDurationSeconds=0`.
- Fixed `SkillNotKnown` for skill 210 by registering universal Khinh Công in `PlayerProgressionState.knownSkills` and `skillLevels`.

Follow-up verification:
- Unity compile: 0 errors.
- LSP diagnostics: 0 errors on changed C# files.
- EditMode HUD category: 16/16 PASS.
- EditMode `CombatSkillSlotTests|PlayerMovementTests`: 38/38 PASS.
- Runtime Walk: `pc.IsRunning=false`, action `Walk`, active SPRs `MA_*_WK01`, missing=0.
- Runtime Sit: action `Sit`, active final seated sprite `MA_BD_019_ZZ01_008` size 33x41, missing=0; vision confirmed cross-legged seated meditation pose.
- Runtime Khinh Công: skill 210 known=true level=20; `TriggerSkillSlot(...,210)` immediately sets action `Jump`, missing=0; after 1s position moved from y=-52041 to y=-52281 (240-unit PC-style leap), then returns Idle.

## Open items before close
- Fresh-context reviewer attempted twice but subagent timed out before producing findings; parent self-reviewed diff + evidence.
- Commit (Conventional Commit) + push to dev.
- Update spec delta `hud` requirements if apply surfaces any deviation (none so far).
