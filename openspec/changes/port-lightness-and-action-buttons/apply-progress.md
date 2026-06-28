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

## Open items before close
- Fresh-context reviewer over the diff before commit/push.
- Commit (Conventional Commit) + push to dev.
- Update spec delta `hud` requirements if apply surfaces any deviation (none so far).
