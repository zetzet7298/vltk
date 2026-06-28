# Tasks: Port Khinh Công and Action Buttons

## Implementation

- [ ] Verify current runtime slot state from code/tests/screenshot and identify which `SkillSlotN` is empty versus assigned sub-slot 1.
- [ ] Add/adjust tests for default combat deck order so the requested empty-slot/sub-slot-1 swap and Khinh Công assignment are locked by `skillId`.
- [ ] Resolve/decode PC Khinh Công icon from `bf787a8a.spr` into the existing generated HUD skill icon folder.
- [ ] Update generated art provenance with PC path `\spr\Ui\技能图标\轻功.spr`, hash `bf787a8a`, and resolved file path(s).
- [ ] Ensure the combat skill catalog resolves `SkillId=210` as Vietnamese `Khinh công` with source-backed metadata; add it if missing.
- [ ] Update `CombatSkillSlotController` default deck/slot mapping to include Khinh Công in the requested slot and preserve five sub-slots.
- [ ] Update slot icon resolution if needed so universal/special `SkillId=210` loads the generated PC icon without changing existing Cái Bang icon mappings.
- [ ] Add `SandboxPlayerController` walk/run state and toggle API; apply speed multiplier in movement tick in a testable way.
- [ ] Wire `ActionBtnRun` handler in `GameHudController` to the player walk/run toggle.
- [ ] Wire `ActionBtnHorse` handler in `GameHudController` to existing `SandboxPlayerController.ToggleMount()` / `PlayerMountService`.
- [ ] Add `SandboxPlayerController` meditate/sit state and toggle API; entering meditation clears movement and prevents movement while active.
- [ ] Wire `ActionBtnSit` handler in `GameHudController` to meditate/sit toggle.
- [ ] Add or update targeted EditMode tests for action button behavior/state transitions.
- [ ] Refresh Unity, resolve compile/console errors, and run targeted tests (HUD and affected sandbox tests; do not run full EditMode during dev loop).
- [ ] Enter play mode and capture a HUD screenshot verifying Khinh Công icon appears in the requested slot and action buttons still show PC sprites.

## Verification

- [ ] `git diff --check` passes.
- [ ] Targeted Unity EditMode tests pass.
- [ ] Unity console has no compile errors.
- [ ] Screenshot evidence confirms no unrelated HUD layout regression.
- [ ] Fresh review is run before commit/push.

## Review workload forecast

Expected diff is moderate and cross-cutting: HUD controller, combat slot controller, player controller, generated icon asset/provenance, and tests. Forecast likely under 400 changed lines if implementation stays minimal. If catalog parsing requires broader changes, pause before apply and split into a smaller first slice.
