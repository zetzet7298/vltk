# Spec Delta: Port Khinh Công and Action Buttons

## Domain: hud

### Requirement: Combat sub-slot order includes PC Khinh Công

The HUD combat cluster SHALL keep exactly one main slot and five sub slots. The default visible sub-slot assignment SHALL swap the currently empty sub-slot with assigned sub-slot 1, then assign PC Khinh Công (`SkillId=210`) into the newly empty sub-slot. The slot identity, icon, label, cast behavior, and picker assignment SHALL be driven by `skillId`, not by visible slot label or array index alone.

#### Scenario: Khinh Công occupies the requested empty slot

- GIVEN the Sandbox HUD initializes the default combat deck
- WHEN the combat cluster renders
- THEN the slot that was empty before the swap displays PC Khinh Công (`SkillId=210`)
- AND the skill previously in sub-slot 1 is displayed in the former empty sub-slot position
- AND the cluster still contains five sub slots plus one larger primary slot

#### Scenario: Slot casts by assigned skill id

- GIVEN a combat sub-slot has `SkillId=210`
- WHEN the player taps that slot
- THEN the combat controller resolves the assigned skill id as `210`
- AND it does not infer Khinh Công from the slot index or label

### Requirement: Khinh Công PC icon provenance

The Khinh Công icon SHALL be decoded/imported from the PC SPR path `\spr\Ui\技能图标\轻功.spr`, resolved by JX Pack hash `bf787a8a.spr`, and exposed through the existing HUD skill icon loading flow. The implementation SHALL record the PC source path, hash, and physical resolved file path in the generated art provenance.

#### Scenario: Icon source is genuine PC art

- GIVEN the imported Khinh Công icon asset
- WHEN provenance is inspected
- THEN it references PC path `\spr\Ui\技能图标\轻功.spr`
- AND hash `bf787a8a`
- AND `/var/www/jx-pc/pak_unpacked/.../bf787a8a.spr`

#### Scenario: Khinh Công icon renders in HUD

- GIVEN the HUD loads with Khinh Công assigned
- WHEN `CombatSkillSlotController.RefreshSlotVisuals()` resolves slot icons
- THEN the Khinh Công slot displays the imported PC icon without fallback, fabricated, or screenshot-baked art

### Requirement: Walk/run action button toggles movement mode

The right-thumb run action button SHALL toggle the player between walk and run movement modes using existing runtime movement flow. The selected state SHALL affect movement speed deterministically and SHALL be observable by tests without relying only on logs.

#### Scenario: Run button toggles state

- GIVEN the Sandbox player is initialized
- WHEN `ActionBtnRun` is tapped
- THEN the player walk/run state toggles
- AND a second tap restores the previous state

#### Scenario: Walk/run state changes movement speed

- GIVEN the player has identical movement input in walk and run states
- WHEN movement is simulated for the same delta time
- THEN run movement distance is greater than walk movement distance

### Requirement: Horse action button uses existing mount runtime

The horse action button SHALL mount or dismount through the existing `SandboxPlayerController.ToggleMount()` / `PlayerMountService` path. It SHALL NOT duplicate mount state outside the mount service.

#### Scenario: Horse button toggles mounted state

- GIVEN the player has a valid default horse id
- WHEN `ActionBtnHorse` is tapped
- THEN the existing mount service enters mounting/mounted state
- WHEN it is tapped again after mounting completes
- THEN the existing mount service enters dismounting/unmounted state

### Requirement: Sit action button toggles meditate state

The sit action button SHALL toggle a player meditate/sit state. When entering meditate, current movement target/input SHALL be canceled and movement SHALL pause while meditating. Leaving meditate SHALL allow normal movement again. If full PC recovery formulas are unavailable, the first slice MAY implement visual/runtime state only but SHALL mark recovery behavior as a follow-up.

#### Scenario: Sit starts meditation and cancels movement

- GIVEN the player is moving or has a click-to-move target
- WHEN `ActionBtnSit` is tapped
- THEN meditate state becomes active
- AND current movement target/input is cleared
- AND subsequent movement tick does not move the player while meditating

#### Scenario: Sit toggles meditation off

- GIVEN meditate state is active
- WHEN `ActionBtnSit` is tapped again
- THEN meditate state becomes inactive
- AND normal movement can resume

### Requirement: HUD action buttons remain touch-safe and PC-sprite based

The run, horse, and sit buttons SHALL remain visually distinct from combat slots, use existing PC sprites, and be pickable without blocking joystick or unrelated HUD elements.

#### Scenario: Action button hit targets remain isolated

- GIVEN the HUD loads
- WHEN action buttons are queried
- THEN `ActionBtnRun`, `ActionBtnHorse`, and `ActionBtnSit` are present and pickable
- AND combat slots, quick slots, joystick lane, top bar, and minimap remain structurally unchanged
