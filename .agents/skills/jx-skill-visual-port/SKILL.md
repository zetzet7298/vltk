---
name: jx-skill-visual-port
description: >-
  Port, fix, or visually match JX Online 1 / Võ Lâm Truyền Kỳ combat skills,
  visual effects (VFX), projectiles/missiles, precast/impact sprites, sound
  effects (SFX), and flight/movement mechanics in the VLTK-mobile Unity client
  using PC Skills.txt, Missles.txt, and Lua scripts. Use this skill whenever the
  user asks for combat skill effects, dragon visual spread (parallel vs fan-spread),
  straight flight vs homing missiles (MoveKind), individual explosion visuals (đạn nổ/hiệu
  ứng va chạm), sound priority (sfx/wav), or any C# skill visual renderer
  (SkillEffectWorldOverlay, SkillEffectRenderer, SkillEffectVisualService) changes.
---

# JX Combat Skill Visual & Projectile Porting

Use this skill to port, debug, or visually match JX/VLTK combat skill visual effects (VFX), projectiles, explosions, and sounds in the Unity mobile client. Goal: 100% PC-accurate visual presentation and flight mechanics.

## Key Files & Structure

| File | Role |
| --- | --- |
| `Assets/Scripts/Sandbox/SkillEffectVisualService.cs` | Calculates positions, handles flight duration, checks collision gates, manages skill phases. |
| `Assets/Scripts/UI/SkillEffectWorldOverlay.cs` | Renders PC SPR projectiles, precast, and individual explosion/impact animations. |
| `Assets/Scripts/UI/SkillEffectRenderer.cs` | Handles legacy IMGUI fallback visuals (must not double-render PC SPRs). |
| `Assets/Scripts/Sandbox/PcSkillVisualConfig.cs` / `PcSkillVisualAutoMapper.cs` | Parses `Missles.txt` and caches PC metadata (speeds, durations, MoveKind, sounds). |
| `Assets/StreamingAssets/Reference/Missles.txt` | Authoritative PC missile config database. |
| `Assets/StreamingAssets/Reference/Skills.txt` | Authoritative PC skill definition database. |

## 1. Double-Rendering Ghost Prevention

PC SPR visual effects are rendered entirely in **World Space** by `SkillEffectWorldOverlay` (which uses `SpriteRenderer` layers at a scale of `PPU = 1f` where 1 sprite pixel equals 1 world unit).

- **Pitfall**: Drawing the same effect inside `SkillEffectRenderer.cs` (which uses IMGUI overlays) produces duplicate "ghost" images on screen due to layout scaling differences.
- **Rule**: In `SkillEffectRenderer.DrawMissiles()` and `DrawImpact()`, always check `fx.HasPcMissileSprite` or `fx.HasPcImpactSprite` and **early-return** immediately to bypass IMGUI rendering.

## 2. Speed and Lifetime Scaling (PPU = 1f)

All positions (casterPos, targetPos, missilePositions) in the visual service are mapped to raw PC pixel coordinates:

- **Unit Equivalence**: 1 world unit = 1 PC pixel (PPU = 1f).
- **Speed Formula**: PC missile speed from `Missles.txt` is in pixels per tick. Since PC runs at 18 FPS, scale speed to seconds:
  ```csharp
  fx.missileSpeed = speedPerTick * 18f; // pixels per second
  ```
- **Duration Formula**: Missile life is in ticks. Scale to seconds:
  ```csharp
  fx.missileDuration = lifeTicks / 18f; // seconds
  ```

## 3. Trajectory and Homing Logic (MoveKind)

Projectiles must follow the exact flight mechanics defined by the PC `MoveKind` column from `Missles.txt`:

- **Homing (MoveKind = 5)**: Homing missiles track target positions dynamically. In C#, check if `fx.pcMissileMoveKind == 5 && fx.getCurrentTargetPos != null`. These track the live target position and trigger a collision when distance is `<= fx.rendRadius`.
- **Linear/Straight (MoveKind != 5)**: Straight missiles fly in a linear path to their initial target coordinates regardless of whether the target moves. They trigger a collision when the traveled distance equals or exceeds the target's distance:
  ```csharp
  float targetDist = Vector2.Distance(fx.targetPos, origin);
  float traveled = Vector2.Distance(mp, origin);
  if (traveled >= targetDist - fx.rendRadius) collided = true;
  ```

## 4. Spreads and Fan Angles (gaibang.lua rules)

Multi-missile skills (e.g. Cái Bang level 20 skills) calculate spacing based on level configuration:

- **Parallel Spread (Phi Long type)**: Indicated by `skill_misslesform_v != 2`. Missiles fly parallel to each other. Calculate starting offset using a step spacing (in world units) from the Lua script's `skill_param1_v` (default 32):
  ```csharp
  float offset = (count - 1) / 2f - i; // symmetric spacing
  ```
- **Fan Spread (Kháng Long type)**: Indicated by `skill_misslesform_v == 2`. Missiles radiate outwards. Spacing angles are computed in 64-direction units using `angleStep64` from `skill_param1_v`:
  ```csharp
  float angleDeg = (angleStep64 * offset) * 360f / 64f;
  ```

## 5. SPR Pivot Mathematics

To avoid visual gaps, blurring, or offsets on rotated missile sprites, calculate the texture pivot using the PC frame offsets relative to the header center:

```csharp
float pivotX = 0.5f;
float pivotY = 0.5f;
if (frame.width > 0)
    pivotX = (decoded.header.centerX - frame.offsetX) / (float)frame.width;
if (frame.height > 0)
    pivotY = (frame.height - (decoded.header.centerY - frame.offsetY)) / (float)frame.height;
```

## 6. Individual Explosions & Phase Delays

Missiles do not all explode at the same time. Each missile must vanish and play its impact animation individually at its point of collision:

- **Individual Tracking**: Keep track of collision times in an array (e.g. `missileExplodeStartTime[si] = elapsed`).
- **Hide Projectile**: Once a missile has hit, hide its projectile sprite immediately.
- **Render Explosion**: Use `SelectPcImpactFrame(fx, explodeTime)` to render the impact frame at the collision coordinate.
- **Phase Delay**: Do not transition the `ActiveSkillEffect` to the `Impact` phase until **all** active individual missile explosions are completed:
  ```csharp
  bool allExploded = true;
  for (int i = 0; i < fx.missileCount; i++)
  {
      float explodeTime = fx.elapsed - fx.missileExplodeStartTime[i];
      if (!fx.missileArrived[i] || explodeTime < fx.impactDuration)
          allExploded = false;
  }
  if (allExploded) TransitionToPhase(SkillEffectPhase.Impact);
  ```

## 7. Null Safety Guardrails

In Unit Tests, some skills are simulated as single-missile effects where `missilePositions`, `missileOrigins`, or `missileExplodeStartTime` arrays might be `null`.
Always guard array access to prevent `NullReferenceException` at runtime or in tests:

```csharp
Vector2 mp = (fx.missilePositions != null && si < fx.missilePositions.Length) 
    ? fx.missilePositions[si] 
    : fx.currentMissilePos;
```

## 8. State Aura Loop & Entry Frame Splitting

Self-buff status effects (e.g. Túy Điệp Cuồng Vũ, Hoạt Bất Lưu Thủ) often contain introductory visual animations (like gold text representing the skill name at frames `0` to `lo - 1`) and subsequent looping particle effects (like golden butterflies at frames `lo` to `hi`).
- **Intro/Entry Playback**: Introductory frames must play exactly once at the beginning of the cast.
- **Loop Playback**: Once the elapsed ticks exceed the intro frame duration (`lifeTick >= lo * interval`), loop strictly within the loop frame range (`lo` to `hi`).
- **Math Formula**:
  ```csharp
  int lo = fx.pcAuraFrameStart;
  int hi = fx.pcAuraFrameEnd > 0 ? fx.pcAuraFrameEnd : totalFrames - 1;
  int interval = Mathf.Max(1, fx.pcPreCastIntervalTicks);
  int lifeTick = Mathf.Max(0, Mathf.FloorToInt(fx.elapsed * 18f));
  int entryTicks = lo * interval;

  if (lifeTick < entryTicks)
  {
      frameIndex = Mathf.Clamp(lifeTick / interval, 0, totalFrames - 1);
  }
  else
  {
      int loopSpan = Mathf.Max(1, hi - lo + 1);
      int loopTick = lifeTick - entryTicks;
      int local = (loopTick / interval) % loopSpan;
      frameIndex = Mathf.Clamp(lo + local, 0, totalFrames - 1);
  }
  ```

## 9. State Aura Position & Horse Mount Heights

Auras must adjust their vertical height dynamically based on their target position (Head/Body/Feet) and whether the caster is riding a horse.
- **Pivot Offset**: Rely entirely on the native sprite's custom pivot `(pivotX, pivotY)` computed from the PC headers (`(centerX - offsetX) / width`, `(height - (centerY - offsetY)) / height`). Do not add manual coordinate offsets, as this shifts the sprite twice.
- **Position Height Offsets**: On PC, Head/Body/Feet status effects are drawn relative to:
  - **Head (`stateAuraPos == 1`)**: Base offset of `10` pixels. If riding a horse, shift up by `38` pixels (`yOffset = 48f`).
  - **Feet (`stateAuraPos == 2`)**: Base offset of `0` pixels. No shift even when riding (`yOffset = 0f`).
  - **Body (`stateAuraPos == 3` / default)**: Base offset of `0` pixels. If riding a horse, shift up by `38` pixels (`yOffset = 38f`).
- **Implementation (PPU = 1f)**:
  ```csharp
  float yOffset = 0f;
  bool isMounted = player.visual != null && player.visual.IsMounted;

  if (fx.stateAuraPos == 1) // Head
  {
      yOffset = 10f;
      if (isMounted) yOffset += 38f;
  }
  else if (fx.stateAuraPos == 2) // Feet
  {
      yOffset = 0f;
  }
  else // Body / Default
  {
      yOffset = 0f;
      if (isMounted) yOffset += 38f;
  }
  ```

## Verification & Tests

After modifying skill visuals:

1. **Verify EditMode Tests**: Run `run_tests` targeting `VLTK.Tests.Sandbox.CombatSkillSlotTests` or related visual tests to verify no compilation/logic regressions.
2. **Visually Check in Play Mode**: Check projectile angles, fan spreads, and individual hit explosions.
3. **Sound Check**: Verify cast and hit sounds play correctly as configured in `Missles.txt`.
