# Spec: Cai Bang Skill PC Parity

## Requirement: PC evidence traceability
The implementation SHALL maintain an evidence matrix for each Cai Bang skill touched by this change.

### Scenario: Skill audit records source evidence
- **Given** a Cai Bang skill is implemented or modified
- **When** the implementation reads PC data
- **Then** the skill evidence SHALL record skill id, Vietnamese name, PC Lua symbol, relevant `skills.txt` row/columns, missile ids/columns from `missles.txt`, visual SPR/WAV paths, and resolved hashed resource filenames where applicable.

### Scenario: No guessed PC resource filenames
- **Given** a skill references a PC `.spr`, `.wav`, `.ini`, `.lua`, or packed resource path
- **When** the resource is imported or used in mobile
- **Then** the path SHALL be resolved with the JX pack hash workflow from `jx-pc-resource-resolver`, including encoding considerations for GBK/CP1258/latin1 as needed.

## Requirement: Phi Long Tại Thiên homing parity
`Phi Long Tại Thiên` (`SkillId=357`) SHALL match PC missile count and target tracking behavior.

### Scenario: Level 20 fires four dragons
- **Given** a player casts `Phi Long Tại Thiên` at level 20
- **When** the skill effect is created
- **Then** exactly four dragon missiles SHALL be spawned according to PC level data.

### Scenario: Dragons track a moving target
- **Given** four `Phi Long Tại Thiên` dragon missiles are in flight
- **And** the selected target moves after cast
- **When** the missile update runs
- **Then** the missile target position SHALL be re-read from the live target, not only from the cast-time position.

### Scenario: Parallel lanes remain visually stable
- **Given** level 20 `Phi Long Tại Thiên` creates multiple dragons
- **When** the dragons face and move toward a live target
- **Then** each dragon SHALL preserve its own lane/offset and SHALL NOT visually collapse all heads into the same target center.

## Requirement: Kháng Long spread parity
`Kháng Long Hữu Hối` (`SkillId=358`, and any PC alias such as `128` if present) SHALL use PC missile form data.

### Scenario: Missile form selects spread type
- **Given** a Cai Bang skill has `skill_misslesform_v` in `gaibang.lua`
- **When** mobile configures the visual/missile effect
- **Then** `missileForm == 2` SHALL route to fan/radial spread using `skill_param1_v`, while non-fan forms SHALL use the correct PC-parallel behavior where applicable.

## Requirement: Buff skills apply PC state values
Cai Bang buff skills SHALL apply values and durations from PC Lua rather than hard-coded approximations.

### Scenario: Hoạt Bất Lưu Thủ speed buff
- **Given** `Hoạt Bất Lưu Thủ` (`SkillId=127`) is cast at a supported level
- **When** the player moves while the buff is active
- **Then** movement speed SHALL include `fastwalkrun_p` from `gaibang.lua` for the current skill level and duration.

### Scenario: Túy Điệp Cuồng Vũ buff attributes
- **Given** `Túy Điệp Cuồng Vũ` (`SkillId=130`) is cast
- **When** active states are applied
- **Then** all relevant PC attributes SHALL be represented, including resistance, fire magic/damage, deadly strike enhancement, life max, skill cost, and durations as encoded in `gaibang.lua`.

### Scenario: Buffs expire
- **Given** a Cai Bang buff has a finite PC duration
- **When** enough combat time has elapsed
- **Then** the active state SHALL expire and no longer modify movement, damage, defense, or visuals.

## Requirement: Active damage skills use PC combat data
Active Cai Bang attack skills SHALL use PC-derived skill level data for damage, range, missile count, missile speed, cast animation, and impact behavior.

### Scenario: Damage calculation includes defender state
- **Given** a defender has active resist/defense states
- **When** a Cai Bang attack skill applies damage
- **Then** damage SHALL be computed with populated defender stats rather than an empty default defender state.

## Requirement: Visual and SFX parity
Cai Bang skill visuals and sounds SHALL resolve from PC resource paths and be rendered with PC semantics.

### Scenario: PC missile sprite is used
- **Given** a skill missile references an `AnimFile` in `missles.txt`
- **When** rendering the skill in mobile
- **Then** the corresponding PC SPR SHALL be resolved/imported and used, preserving frame direction semantics.

### Scenario: Aura/support visuals are visible
- **Given** a support/aura/buff skill has a PC pre-cast/cast/status visual
- **When** the buff is active
- **Then** mobile SHALL show the correct aura/ring/status visual and state icon where supported by the current HUD/runtime.

## Requirement: Test discipline
All implementation slices SHALL include focused tests.

### Scenario: Development test run is filtered
- **Given** a Cai Bang implementation slice is modified
- **When** tests are run during development
- **Then** use Unity EditMode category filtering such as `category_names=["CaiBang"]` and/or `category_names=["!Slow"]`, not the full EditMode suite.

### Scenario: Shared combat changes trigger broader validation
- **Given** shared combat/runtime services are modified
- **When** preparing to push
- **Then** run the relevant Cai Bang tests and any required broader/shared combat tests; full EditMode is reserved for final gate only.
