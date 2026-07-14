# Design: Port Khinh Công and Action Buttons

## Evidence and constraints

- User-facing behavior must be Vietnamese, but code/artifact identifiers remain English unless extending existing Vietnamese labels.
- PC source rule was applied. The configured source path `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem` is absent in this environment, so this change uses the available PC-derived reference and unpacked client evidence:
  - `Assets/StreamingAssets/Reference/PcSkills.txt`: row `Khinh công`, `SkillId=210`.
  - Canonical PC icon path: `\spr\Ui\技能图标\轻功.spr`.
  - JX Pack hash: `bf787a8a`.
  - Resolved SPR: `/var/www/jx-source/pak_unpacked/update01/unknown/bf787a8a.spr` and `/var/www/jx-source/pak_unpacked/spr/unknown/bf787a8a.spr`.
  - Script path evidence: `\script\skill\special\轻功.lua`.
- Existing HUD structure:
  - `Assets/UI/HUD/GameHud.uxml`: `SkillSlot0..4`, `PrimaryAttackBtn`, `ActionBtnRun`, `ActionBtnHorse`, `ActionBtnSit`.
  - `Assets/UI/HUD/GameHud.uss`: sub-slot visual positions `.hud-combat-pos-sub1..sub5` and action button positions.
  - `Assets/Scripts/UI/CombatSkillSlotController.cs`: `MobileSkillSlotCount=5`, default Cái Bang deck currently `{357,358,1073,130,127}`, assignment keyed by skill id, picker, visual refresh.
  - `Assets/Scripts/UI/GameHudController.cs`: loads button art, registers action click handlers; current handlers are stubs/no-op logs.
  - `Assets/Scripts/Sandbox/SandboxPlayerController.cs`: movement tick, mount service, `ToggleMount()`.

## Approach

### 1. Slot swap and Khinh Công assignment

Prefer a data-level default deck change rather than moving UXML elements. The user asks to swap the currently empty visual slot with assigned sub-slot 1 and then put Khinh Công in the freed slot. In current code, default deck may be fully assigned, while the screenshot shows an empty visual slot due runtime/default mismatch. The implementation should:

1. Add a named constant for Khinh Công skill id (`210`) near combat slot defaults.
2. Make the default deck include `210` exactly once.
3. If the current empty slot is caused by a zero in deck data, swap the zero and slot-0 skill id before assigning `210` to that zero position.
4. If there is no zero in the default deck, use the visual position that corresponds to the user's empty slot as the Khinh Công slot while preserving the formerly slot-1 skill in the former empty position.
5. Cover the resolved order in tests so the intended slot mapping does not drift.

This keeps the UXML/USS fan layout stable and avoids unnecessary visual churn. Only edit UXML/USS if tests or runtime prove the empty slot is purely visual-position order rather than deck data.

### 2. Import Khinh Công icon

Use the existing skill icon generation/loader flow instead of adding one-off HUD code.

Expected output:

- `Assets/UI/HUD/Art/Generated/cai_bang_skill_210.png` or a more neutral generated name if the existing loader is generalized.
- Update `Assets/UI/HUD/Art/Generated/PC_SOURCE.txt` with PC path, hash, and resolved file.

The existing controller currently calls icons as `cai_bang_skill_{skillId}` in both the combat HUD and picker. For minimal impact, add `cai_bang_skill_210.png` even though Khinh Công is universal/special, then optionally follow up with a neutral naming refactor later.

### 3. Catalog support for Khinh Công

Check whether `PcCombatCatalogFactory` / existing catalog already includes skill id `210`. If missing, add it source-backed from `PcSkills.txt` with:

- display name: `Khinh công`
- raw PC name/path fields retained where existing model supports them
- icon asset name/path mapping to generated icon
- skill style based on `PcSkills.txt` row (`SkillStyle` / active style as parsed by current factory)

If full execution behavior is unavailable, its slot tap may initially log/route through existing skill cast flow and fail gracefully, but icon/assignment must be correct.

### 4. Walk/run runtime state

Add explicit walk/run state to `SandboxPlayerController` instead of storing it in `GameHudController`:

- `public bool IsRunning { get; private set; } = true;`
- `public float walkSpeedMultiplier = 0.55f` or PC-derived value if found during apply.
- `public void ToggleWalkRun()` toggles state and cancels meditation if needed.
- Movement tick multiplies base speed by `IsRunning ? 1f : walkSpeedMultiplier`, before mount multiplier and existing `FastWalkRunP` state multiplier.

This makes tests possible and prevents HUD-only state divergence.

### 5. Mount/dismount button

Wire `GameHudController.OnHorseClick()` to locate `SandboxManager.Instance?.PlayerController` and call `ToggleMount()`. Do not duplicate state in HUD. Existing `PlayerMountService` remains authoritative.

### 6. Sit/meditate runtime state

Add minimal first-slice meditation state to `SandboxPlayerController`:

- `public bool IsMeditating { get; private set; }`
- `public void ToggleMeditation()` toggles it.
- Entering meditation clears movement target/input and prevents movement tick from applying movement.
- Exiting meditation allows movement again.
- If mounted, first-slice policy: block meditation while mounted with a log, or dismount before meditating. The safer minimal policy is block while mounted to avoid animation conflicts unless PC source proves auto-dismount.

Recovery formulas, buff effects, and exact PC sit animation can be follow-up if missing PC source prevents source-backed parity.

### 7. HUD controller wiring

`GameHudController` should remain a thin UI bridge:

- `OnRunClick()` → `SandboxManager.Instance?.PlayerController?.ToggleWalkRun()`
- `OnHorseClick()` → `SandboxManager.Instance?.PlayerController?.ToggleMount()`
- `OnSitClick()` → `SandboxManager.Instance?.PlayerController?.ToggleMeditation()`

Keep `RegisterClick` / `pickingMode` behavior and existing PC icon loading.

## Tests

Targeted EditMode, category `HUD` and/or existing sandbox categories:

1. Combat default deck test: includes `210` once and preserves five slots.
2. Slot swap test: verifies the requested slot mapping after defaults initialize.
3. Icon provenance/asset test: verifies generated `cai_bang_skill_210.png` exists and `PC_SOURCE.txt` mentions `\spr\Ui\技能图标\轻功.spr` and `bf787a8a`.
4. Action wiring test: invoking HUD handlers or registered callbacks changes `SandboxPlayerController` state for walk/run, mount, and meditation.
5. Movement test: walk/run changes distance for same input/delta; meditation prevents movement.

Run targeted tests first (`HUD` / affected sandbox namespace). Run broader tests only if shared catalog or player controller changes require it.

## Risks

- `cai_bang_skill_210` naming is semantically imperfect because Khinh Công is universal. It is acceptable as a compatibility bridge for the existing loader; record as follow-up.
- Full Khinh Công movement/VFX cannot be faithfully implemented without PC runtime script/source. Keep this slice to icon/assignment unless source is recovered.
- Meditation visual animation may not exist as a clean PC-backed action in current player visual catalog. Do not fake complex animation; state behavior and log are acceptable first slice if documented.
