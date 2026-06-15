# Phi Long Missile Visual — Bug Report (2026-06-15)

## User complaint
"sao khong thay cac con rong sau xe muc tieu khi trung muc tieu nhu game pc"
"4 con rong xuyen muc tieu va no thu ve lai 1 doan ngan, cam giac nhu sau xe muc tieu"

## PC source EVIDENCE (đã verify)

### Skills.txt skill 357 (Phi Long Tại Thiên)
```
IsMelee = 0
ByMissle = 1
MslsGenerate = 5
ChildSkillId = 166
CharAnimId = 11
AttackRadius = 400
```

### gaibang.lua::feilong_zaitian (config chi tiết)
```lua
missle_speed_v       = {1,20→20,24}
skill_misslenum_v    = {1,1→12,2→16,3→20,4}     -- 4 missile ở level 20
skill_attackradius   = {1,448→20,512}
skill_collideevent   = [3]={{1,389},{20,389}}   -- va chạm → trigger skill 389 (explosion)
skill_misslesform_v  = {1,1→11,1→11,0}          -- form Single/parallel
```

### missles.txt id 166 (Phi Long missile)
```
MoveKind     = 5         ← HOMING missile (bám target, recalc direction mỗi 8 frames)
Speed        = 30
LifeTime     = 24
ColVanish    = 1         ← biến mất khi va chạm
AnimFile2    = \spr\skill\丐帮\mag_gb_05_亢龙有悔.spr
AnimFileInfo2 = 80,16,1  ← 80 frames, 16 directions, 1 tick interval
AnimFile4    = \spr\skill\丐帮\mag_gb_bz5_爆炸效果.spr  (explosion)
AnimFileInfo4 = 6,1,2    ← 6 frames, 1 direction, 2 tick interval
LightColor   = (255, 174, 60)  ← vàng cam
LightRadius  = 90
```

### KMissle::StepFly case 5 (binary 0x0808d04a) — HOMING logic
```c
m_118++;                       // frame counter++
if (m_118 <= 7) goto fly;      // 8 frames đầu: bay theo direction cũ
m_118 = 0;                     // reset
// check sub-skill refire...
if (m_11c > 0) {               // countdown pursue
    m_11c--;
    if (m_11c != 0) goto fly;
}
// Recalc direction = target - current position (HOMING)
GetMpsPos(&curX, &curY);
deltaX = (targetX - curX) << 10;  // fixed-point scale
deltaY = (targetY - curY) << 10;
// → velocity mới hướng về target
```

→ **Kết luận**: MoveKind=5 = HOMING MISSILE. "Xuyên + thu về ngắn" user thấy = SPR animation `mag_gb_05_亢龙有悔.spr` có frames thrust+retract + homing bám target.

## SPR UID hash (PC path → UID)

```
\spr\skill\丐帮\mag_gb_05_亢龙有悔.spr    → a31b9f04 (gbk encoding)
\spr\skill\丐帮\mag_gb_bz5_爆炸效果.spr   → c33e96c2 (gbk encoding)
```

Verify: `ComputePathUidHex(path)` C# = `a31b9f04` ✓

## Mobile port state

### ✅ ĐỦ resources
- `/var/www/vltk-mobile/Assets/StreamingAssets/Sprites/a31b9f04.spr` — missile 166 SPR TỒN TẠI
- `/var/www/vltk-mobile/Assets/StreamingAssets/Sprites/c33e96c2.spr` — explosion SPR TỒN TẠI
- pak_unpacked có source SPR: `pak_unpacked/skills/unknown/a31b9f04.spr`

### ✅ Runtime spawn đúng
`CombatRuntimeService.SpawnProjectiles` + `PcCaiBangModTuning.PhiLongAtLevel`:
- Level 20 → missileCount = 4 ✓
- Play mode verify: `[Projectile] Spawned projectile for skill 166 (effect=<missing>)` × 4

### ✅ SkillEffectVisualService case 357 setup ĐÚNG
```csharp
case 357:
    SetupPcMissile(fx, "a31b9f04", 80, 16, 1, 30, 24,
                   "c33e96c2", 6, 1, 2,
                   new Color(1f, 174f/255f, 60f/255f));  // vàng cam đúng PC
    int count = level >= 20 ? 4 : (level >= 16 ? 3 : ...);
    SetupPcPhiLongSpread(fx, count, 32);
```
→ `fx.pcMissileSpriteKey = "a31b9f04"` đúng UID, frames/dirs đúng PC.

## ❌ BUG — Render layer không vẽ SPR

### Bug 1: SkillEffectRenderer.DrawMissiles (IMGUI OnGUI path)
File: `Assets/Scripts/UI/SkillEffectRenderer.cs` line 75-115

```csharp
private void DrawMissiles(ActiveSkillEffect fx) {
    if (fx.missileCount <= 1) {
        DrawProjectile(screenPos, fx.color, fx.trailEnabled, fx.casterPos);
    } else { /* loop multi-missile */ }
}

private void DrawProjectile(...) {
    DrawCircle(screenPos, 6f, color, 3f);   // ← VÒNG TRÒN 6px, KHÔNG load SPR!
    DrawCircle(screenPos, 3f, coreColor, 1f);
}
```

**KHÔNG load `fx.pcMissileSpriteKey` (`a31b9f04`)** để render SPR thật. Chỉ vẽ chấm tròn. User thấy "chấm vàng" thay vì "con rồng".

### Bug 2: SkillEffectWorldOverlay có logic load SPR nhưng không trigger
File: `Assets/Scripts/UI/SkillEffectWorldOverlay.cs`

Có code đúng:
```csharp
sr.sprite = fx.HasPcMissileSprite ? FirstValidPcSprite(fx.pcMissileSpriteKey) : _dotSprite;
```

Nhưng test play mode: `SkillVFX_* GameObjects = 0` → visual không spawn qua path này.

Cần investigate: có thể WorldOverlay chưa được enable trong scene, hoặc LateUpdate không poll đúng service.

### Bug 3: ResolveMissileSprite key sai (minor, không ảnh hưởng case 357)
File: `Assets/Scripts/Sandbox/SkillEffectVisualService.cs` line 492-499

```csharp
private Sprite ResolveMissileSprite(SkillDefinition skill) {
    string missileKey = $"missile_{skill.childSkillId}";  // "missile_166" — WRONG KEY
    return _sprService?.ResolveSprite(missileKey, 32, 32);
}
```

Key `missile_166` không match UID hash. SPR service miss cache. (case 357 bị override bởi ConfigureCaiBangVisuals, nhưng log warning gây noise).

## Fix cần làm (KHÔNG đoán, theo PC source)

### Priority 1: SkillEffectRenderer.DrawMissiles phải load SPR
```csharp
private void DrawMissiles(ActiveSkillEffect fx) {
    // PC source: KMissleRes::Draw — render SPR frame theo direction
    Sprite missileSpr = LoadMissileSprite(fx.pcMissileSpriteKey, fx.pcMissileDirection);
    if (missileSpr != null) {
        // PC: chọn frame = dir * framePerDir + localFrame
        // DrawTexture(missileSpr)
    } else {
        DrawCircle(...);  // fallback cũ
    }
}
```

### Priority 2: Verify SkillEffectWorldOverlay spawn trong play mode
- Đảm bảo GameObject "SkillVFX_357_*" tạo khi cast 357 qua CombatSkillSlotController
- Nếu không spawn: debug LateUpdate poll + EnsureResources

### Priority 3: Fix ResolveMissileSprite key
```csharp
private Sprite ResolveMissileSprite(SkillDefinition skill) {
    // Parse missles.txt để lấy SPR path cho skill.childSkillId
    // Hash path → UID → resolve
    var visual = _missileRegistry?.Get(skill.childSkillId);
    if (visual?.Anim2?.sprPath != null) {
        string uid = SprRuntimeService.ComputePathUidHex(visual.Anim2.sprPath);
        return _sprService?.ResolveSprite(uid, 64, 64);
    }
    return null;
}
```

## Cần từ user

1. **Xác nhận**: mày cast Phi Long thấy gì hiện tại? "Chấm vàng nhỏ bay tới target" hay "không thấy gì"?
2. Nếu thấy chấm: bug ở SkillEffectRenderer (chỉ vẽ circle). Fix load SPR.
3. Nếu không thấy gì: bug ở WorldOverlay không spawn. Debug LateUpdate.

Sau khi xác nhận, tôi fix Priority 1/2 theo PC source (KMissleRes::Draw flow).
