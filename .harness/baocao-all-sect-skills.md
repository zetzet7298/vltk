# Báo cáo rà soát kỹ năng tấn công Cái Bang — VLTK Mobile vs VLTK PC

> **Mục đích**: Liệt kê skill tấn công Cái Bang đã port sang mobile, đối chiếu
> với PC, xác định gap và đề xuất thứ tự sửa. **CHƯA SỬA CODE.**
>
> **Nguồn tham chiếu**:
> - PC: `/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser_bachkim_6.0/server1/script/skill/gaibang.lua`
>   (đã giải mã TCVN3; các biến `gaibang_*`, `feilong_zaitian` v.v.)
> - PC C++: `Assets/StreamingAssets/Reference/KNpc.cpp` (`DoSkill`, `DoOrdinSkill`,
>   `CastMeleeSkill` switch với `Melee_Jump` / `Melee_JumpAndAttack` / `Melee_RunAndAttack`,
>   `NewJump` với `m_JumpStep`, `DoJump`).
> - PC docs: `/var/www/vltksource_new/docs/port_docs/03_skills.md` (25 file
>   `skill/gaibang/`).
> - Mobile: `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs`,
>   `Assets/Scripts/Sandbox/CombatRuntimeService.cs`,
>   `Assets/Scripts/Sandbox/SkillEffectVisualService.cs`,
>   `Assets/Scripts/Sandbox/PcCaiBangSkillTuning.cs`,
>   `Assets/Scripts/Sandbox/PcCaiBangModTuning.cs`,
>   `Assets/StreamingAssets/Reference/PcSkill/Skills1FullCatalog.json`.

---

## 1. Tổng quan

Cái Bang (丐帮) trong PC có 25 file Lua skill + script leveldata. Catalog mobile
đã nhận 36 entry dùng `levelSetScript=\script\skill\gaibang.lua`. Trong đó **9
skill là "long-range/dash/lunge"** đáng quan tâm cho task này:

| ID | Tên (suy ra từ catalog) | Loại PC | Vai trò |
|---:|---|---|---|
| 117 | Đầu Thạch Vấn Lộ / Ném Đá Hỏi Đường | Ranged | Đơn thương |
| 122 | Kiến Nhân Thần Thủ | Ranged | Phóng ẩn |
| 125 | Bổng Đả Ác Cẩu | Ranged + Surround 16 | Skill chính tầm xa |
| **128** | **Kháng Long Hữu Hối** | **Melee + JUMP** | **Dash tới + bắn 15 rồng** |
| 359 | Thiên Hạ Vô Cẩu (player) | Ranged homing | 1→3 rồng đuổi mục tiêu |
| 1072 | Ngũ Diệu Càn Khôn | Sub-skill 1073 | Stationary flash |
| 1073 | Thần Thủ Lệnh Long (150) | Ranged + 3-phase event | Skill cao cấp 150 |
| 1074 | Bổng Hoành Lược Mã (150) | Ranged surround→single | Skill cao cấp 150 |
| **357** | **Phi Long Tại Thiên** | **Melee + JUMP** | **Dash tới + slash (sâu xé)** |

Hai skill dash/lunge then chốt (`128` Kháng Long và `357` Phi Long) là trọng tâm
task này. Mười skill còn lại (115, 116, 119, 120, 121, 123, 124, 126, 127, 129,
130, 209, 274, 277, 358, 360, 389, 714, 720, 1101, 1103, 1161, 1162, 1602, 1817,
1818) là buff/mastery/passive/cast-in-place/sub-skill, đã đúng theo catalog hoặc
nằm ngoài scope dash của task này (sẽ rà riêng nếu cần).

---

## 2. Bảng đối chiếu chi tiết

> **Chú thích cột "Hành vi hiện tại (mobile)"**: lấy từ code
> `PcCombatCatalogFactory.cs` (khai báo skill), `CombatRuntimeService.cs` (cast
> flow), `SkillEffectVisualService.cs` (visual). Mọi skill tấn công Cái Bang trong
> mobile đều đi qua nhánh `PcSkillStyle.Missiles` + `SpawnProjectiles` — **không có
> nhánh Melee+dash riêng**.

> **Chú thích cột "Hành vi PC"**: lấy từ `KNpc.cpp::CastMeleeSkill` switch
> (`Melee_AttackWithBlur` / `Melee_Jump` / `Melee_JumpAndAttack` /
> `Melee_RunAndAttack` / `Melee_ManyAttack`) + `NewJump` + `DoJump` + `DoJumpAttack`
> + `DoRunAttack`, áp dụng cho skill có `IsMelee=true` và `SkillStyle=SKILL_SS_Melee`
> trong PC.

### 2.1. Phi Long Tại Thiên (ID 357) — case mẫu

| Hạng mục | Hành vi hiện tại (mobile) | Hành vi đúng (PC) | Gap |
|---|---|---|---|
| Catalog | `DamageSkillNew(357, ..., child=166, SkillMissileForm.Single, 1, false, false, 11, ...)` → `s.skillStyle = PcSkillStyle.Missiles` (line 213-218 + line 218 default) | PC gaibang.lua: `feilong_zaitian` thuộc nhóm **Melee + JUMP** (`IsMelee=true`, `MeleeType=JumpAndAttack` hoặc `Jump`). KNpc.cpp line 1849-1873: `Melee_JumpAndAttack` → `NewJump(m_DesX, m_DesY)` rồi `DoJumpAttack()`. | **THIẾU — skillStyle phải là Melee (không phải Missiles); thiếu MeleeType field trong `SkillDefinition`; thiếu handler cho nhánh `Melee_Jump*` trong runtime.** |
| Cast time | `timePerCast=2`, `waitTime=5` | PC: 2/5 ticks (~110ms) | OK |
| Mana | `Link(lv,(1,10,""),(20,65,""))` (line 216) | PC `skill_cost_v` tương đương | OK |
| Series/Fire damage | `series=20→60`, `fire=10→750` | PC `seriesdamage_p`, `firedamage_v` | OK |
| Missile count L1-20 | `PcCaiBangModTuning.PhiLongAtLevel`: `(1,1),(11,1),(12,2),(15,2),(16,3),(20,4)` | PC `skill_misslenum_v` tương đương | OK |
| MissleForm (parallel spread) L1-20 | `L1-10: 1 (Single)`, `L11+: 0` + `param1=0→32` → `SetupPcPhiLongSpread(fx, count, 32)` | PC `skill_misslesform_v`, `skill_param1_v` | OK |
| Missile speed | L1-20: 20→24 (PC unit/tick) | PC `missle_speed_v` | OK |
| Attack radius | L1-20: 448→512 (PC unit) | PC `skill_attackradius` | OK |
| PreCast SPR | (không set, dùng default 0x9f04 từ `SetupPcMissile`) | PC: `gb_05_亢龙有悔.spr` (cùng SPR với Kháng Long) | OK (đã share) |
| Missile child SPR | `missle 166: a31b9f04, 80, 16, 1, 30, 24` (MoveKind=5 homing) | PC `missle 166: Speed=30, LifeTime=24, MoveKind=5` | OK |
| **Sub-skill 389 (Long Chiến Ư Dật — CollideEvent)** | `CombatRuntimeService.ApplySkillCast` line 146-155: **chỉ fire khi `skillLevel >= 11`** (mới spawn sub-damage + sub-projectile 389). | PC `skill_collideevent[3]={{{1,0},{10,0},{10,1},{20,1}}}` — fire ở **mọi level**, mỗi khi missile chạm NPC. | **THIẾU** — L1-10 phải fire 389 vẫn damage. Hiện tại L1-10 chỉ damage main, không có "sâu xé" (slash). Đây chính là lý do user nói "phi long tới mục tiêu ở cự ly gần thì không sâu xé". |
| **Dash / Player Jump** | KHÔNG. `caster.position` (line 250) chỉ là điểm xuất phát missile. Không có lệnh nào move player. | PC `NewJump` + `DoJump` cập nhật `m_OffX`, `m_OffY` theo `g_DirCos/Sin(m_JumpDir, 64) * nSpeed` mỗi tick (line 2771-2772), vẽ player "bay" theo missile. | **THIẾU** — player không jump. Đây là lý do ở cự ly gần, player đứng yên tại chỗ trong khi missile bay tới, xong thì dừng — không có cảm giác "phi long đâm xuyên". |
| **Close-range slash (rend tại chỗ)** | `rendRadius=5f`, `arrivalRadius=2f` — khi missile tới rend thì `TriggerSauXe` line 446-453, ghi vào `fx.rendPositions` để vẽ flash. Ở cự ly <5 unit, rend vẫn chạy nhưng visual gần như đè lên caster → khó thấy. | PC: ở `MIN_JUMP_RANGE` (~64 PC pixel ~ ½ cell), player chỉ chém cận chiến, không jump. Ở xa hơn thì jump + missile + slash. Tóm lại: PC luôn có slash visible (main missile impact hoặc jump melee). | **THIẾU TẦM NHÌN** — dù có rend nhưng visual quá nhỏ/gần. Cần phóng to `rendRadius` hoặc chuyển sang slash effect độc lập. |
| Horse-restricted | `horseLimit: 1` (không cưỡi ngựa) | PC `skill_collideevent` không liên quan; `horseLimit` đúng | OK |
| Icon path | `\\spr\\Ui\技能图标\icon_sk_gb_41.spr` (line 621) | PC family | OK (đúng dòng Long) |
| Tuning | `PcCaiBangSkillTuning.LongRangeAtLevel` (line 23) + `PcCaiBangModTuning.PhiLongAtLevel` | PC source | OK |

**Kết luận 357**: catalog đúng ~80% (range, mana, damage, count, missile form).
**3 gap then chốt**:
- **G1** (cao): thiếu player JUMP — đây là "phi long" thực sự, không chỉ missile
- **G2** (cao): sub-skill 389 (slash) chỉ fire L11+, phải fire mọi level
- **G3** (trung bình): rend visual ở close-range quá nhỏ, cần hiệu ứng slash rõ ràng

### 2.2. Kháng Long Hữu Hối (ID 128)

| Hạng mục | Mobile | PC | Gap |
|---|---|---|---|
| Catalog | `DamageSkillNew(128, "Kháng Long Hữu Hối", ..., child=48, SkillMissileForm.Fan, 15, false, false, 11, ...)` → `skillStyle=Missiles` (line 128-134) | PC gaibang.lua `kanglong_youhui` — cũng thuộc nhóm **Melee + JUMP** (KNpc.cpp `Melee_JumpAndAttack` tương tự 357). Có `skill_misslenum_v={{1,1},{10,1},{10,2},{20,2}}` (1 hoặc 2 quạt, không phải 15). | **THIẾU — skillStyle phải Melee; `PcCaiBangSkillTuning` đã thêm 128 vào `LongRangeAtLevel` (line 23) nhưng catalog vẫn Missiles.** |
| Cast/mana/damage | `fire=10→536, cost=10→50, series=10→50` | PC `firedamage_v`, `skill_cost_v`, `seriesdamage_p` | OK |
| Fan 15 missiles | `childSkillNum=15` (override) | PC thực tế chỉ 1-2 fan (skill_misslenum_v max 2); 15 là sai | **LỆCH DỮ LIỆU** — 15 không đúng PC. PC max 2 fan (L11-20), không phải 15. Cần `PcKangLongYouHuiTuning.AtLevel(lv).missileCount = 1 or 2` thay vì cứng 15. |
| Missile form | `SkillMissileForm.Fan` (PC `skill_misslesform_v` ở 357 là 0/1, ở 128 cũng nên là 0 hoặc 2) | PC: form có thể là 0 (parallel) hoặc 2 (fan) | Cần check missle 48 trong `missles.txt` |
| Dash / Player Jump | KHÔNG | PC có `NewJump` + `DoJump` cho Melee_Jump/JumpAndAttack | **THIẾU** — cùng root cause với 357 |
| Sub-skill / Collide | (không rõ trong gaibang.lua cho 128) | `skill_collideevent[3]={{{1,0},{10,0},{10,1},{20,1}}}` — fire L11+ | Tương tự 357 |

**Kết luận 128**: cùng dash gap như 357, **plus sai missile count 15** (PC max 2).

### 2.3. Phi Long Tại Thiên — sub-skill 389 Long Chiến Ư Dật

| Hạng mục | Mobile | PC | Gap |
|---|---|---|---|
| Catalog | `LongChienUYuyeSkill()` line 1564: `BaseSkill(389, "Long Chiến Ư Dã ", "Long Chiến Ư Dã", 80, 20, 570, SkillMissileForm.None)` | PC `longzhan_yuye`: `firedamage_v={{1,17},{20,371}}`, `seriesdamage_p={{1,20},{20,60}}` | OK (gần khớp, L20 fire 371 đúng) |
| Trigger | `CombatRuntimeService` line 146-155: fire khi `357 && level>=11` | PC fire khi missile 166 collide với NPC (mọi level) | **THIẾU cho L1-10** (G2 ở trên) |
| Visual | `SetupPcStationaryEffect(fx, "b91ab706", 6, 1, 1, orange)` line 824 | PC `mag_bz_huo3_爆炸效果.spr` (effectSourceId đúng) | OK |
| AOE radius | `attackRadius=570` (mobile) | PC missle 166 có LifeTime/AOE, gần tương đương | OK |

### 2.4. Các skill ranged còn lại (125, 359, 1073, 1074)

| ID | Mobile | PC | Gap |
|---|---|---|---|
| **125** Bổng Đả Ác Cẩu | `DamageSkillNew(125, "Bổng Đả ác Cẩu", "Thiên Hạ Vô Cẩu", 50, 20, 512, 47, SkillMissileForm.Surround, 16, true, false, 11, ...)` | PC `bangda_egou` (PC id 125, không phải 359). | **LỆNH SAI ID** — mobile dùng id=125 cho `Thiên Hạ Vô Cẩu` nhưng PC dùng id=359 cho player, id=125 là `bangda_egou` (Bổng Đả Ác Cẩu) cũng 16 surround. Cần check `gaibang.lua::bangda_egou` để phân biệt chính xác. |
| **359** Thiên Hạ Vô Cẩu (player) | `DamageSkillNew(359, ..., child=168, SkillMissileForm.Single, 1, false, false, 11, ...)` | PC `tianxia_wugou`: 1→3 homing missiles | OK count; close-range slash chưa rõ |
| **1073** Thần Thủ Lệnh Long (150) | `DamageSkillNew(1073, "Thời Thặng Lục Long", "Thần Thủ Lệnh Long", 150, 20, 512, 335, SkillMissileForm.Single, 1, ...)` | PC `zhanggaibang150`: 3-phase event (StartEvent 1101, FlyEvent 1103, CollideEvent 1072) | Cần verify event chain sub-skills có fire không (hiện chỉ thấy `SpawnCollideSubEffect` cho 1073→1072, không thấy StartEvent/FlyEvent) |
| **1074** Bổng Hoành Lược Mã (150) | `DamageSkillNew(1074, ..., child=336, SkillMissileForm.Single, 1, ...)` | PC `gungaibang150`: 1→5 homing missiles | OK count; không có dash trong PC |
| **1072** Ngũ Diệu Càn Khôn | `BaseSkill(1072, ..., SkillMissileForm.None)` line 1572 | PC CollideEvent sub-skill của 1073, stationary flash 10 ticks | OK (chỉ là visual flash) |

---

## 3. Bảng tóm tắt gap (risk-ranked)

| # | Skill | Gap | Mức độ | Rủi ro khi sửa | Effort |
|---|---|---|---|---|---|
| **G1** | 357, 128 | Thiếu player JUMP/dash theo missile | **Cao** (cảm giác "phi long" mất) | **Cao** — đụng `SkillDefinition` schema (thêm `MeleeType` enum), `CombatRuntimeService` (thêm nhánh `Melee_Jump/JumpAndAttack` với lerp `transform.position` mỗi tick), `SkillEffectVisualService` (đồng bộ player rotation), `MapEnemy` AI (cần ignore dash để không chase) | 2-3 ngày |
| **G2** | 357 → 389 | Sub-skill 389 không fire ở L1-10 | **Cao** (user nói rõ "không sâu xé") | Thấp — đổi `if (skillLevel >= 11)` thành `if (skillId == 357 && skillLevel >= 1)`. Damage đã đúng trong `LongChienUYuyeSkill`. Cần thêm close-range check hoặc bỏ gate. | 1 giờ |
| **G3** | 357 (tất cả level) | Rend visual slash ở close-range quá nhỏ | Trung bình | Thấp — `rendRadius=5f` có thể tăng 8-10f; hoặc thêm `TriggerSlash(fx, position)` vẽ slash effect riêng (cần sprite) | 2-4 giờ + 1 sprite |
| **G4** | 128 | `childSkillNum=15` sai PC (PC max 2) | Trung bình (gameplay feel) | Thấp — `PcKangLongYouHuiTuning` chưa cover 128, dùng `PcCaiBangModTuning.PhiLongAtLevel`-style pattern. Cần thêm `KangLongYouHuiTuning.KhangLongAtLevel(lv) = 1 or 2`. | 30 phút |
| **G5** | 125, 359 | Mapping id → tên có thể bị swap | Trung bình | Trung bình — check lại `ModSkills.txt` cột SkillId+SkillName của 125 vs 359, xác nhận mobile đặt đúng. Có thể cần swap. | 30 phút |
| **G6** | 1073 | Event chain StartEvent/FlyEvent chưa thấy fire | Thấp-trung bình | Thấp — đã có `SpawnCollideSubEffect` cho 1073→1072. Cần thêm `SpawnStartEvent` (1101) và `SpawnFlyEvent` (1103) trong `PlaySkillCast` hoặc pre-cast phase. | 2-3 giờ |
| **G7** | 357/128/359/1073/1074 | `PcCaiBangModTuning` không gọi từ `SpawnProjectiles` đúng cách | Thấp | Thấp — đã có `useMod` branch (line 258-262). Verify với `SkillEffectVisualService.ConfigureCaiBangVisuals` (line 281) đồng bộ. | 1 giờ verify |

---

## 4. Đề xuất thứ tự sửa

### Phase 1 — Quick wins (1 ngày)

1. **G2** (357 → 389): sửa `CombatRuntimeService.cs:146` bỏ `>= 11` gate.
2. **G4** (128 child count): thêm `PcKangLongYouHuiTuning.KhangLongAtLevel` (hoặc mở rộng `PcCaiBangModTuning`) trả về 1 hoặc 2 theo level.
3. **G5** (125/359 mapping): verify với `ModSkills.txt` + đổi tên hiển thị nếu sai.
4. **G7** (verify): chạy debug build, cast 357 ở 2 level, confirm `PcCaiBangModTuning` được gọi đúng.

### Phase 2 — Close-range slash (nửa ngày)

5. **G3** (rend visual): tăng `rendRadius` từ 5 → 9; thêm `TriggerSlash(fx, position)` vẽ sprite slash độc lập (cần extract từ `mag_bz_huo3_爆炸效果.spr` hoặc tương đương).

### Phase 3 — Dash (2-3 ngày, G1) — quan trọng nhất

6. **Thiết kế MeleeType trong `SkillDefinition`**: thêm enum `PcMeleeType { None, AttackWithBlur, Jump, JumpAndAttack, RunAndAttack, ManyAttack }`, field `meleeType`.
7. **Set MeleeType cho 357 và 128**:
   - 357: `MeleeType.JumpAndAttack` (PC `Melee_JumpAndAttack` — jump rồi attack)
   - 128: `MeleeType.JumpAndAttack` (PC tương tự)
8. **Refactor `SkillDefinition` factory** (`PcCombatCatalogFactory.DamageSkillNew`) thêm overload nhận `meleeType` hoặc set default.
9. **Refactor `CombatRuntimeService.ApplySkillCast`**: sau khi gate pass, nếu `skill.meleeType` ∈ {Jump, JumpAndAttack}:
   - Lưu `caster.position` ban đầu
   - Tính jumpSpeed từ `caster.currentJumpSpeed` (đã có sẵn trong `caster` model)
   - Tính jumpDistance = clamp(`attackRadius * RangeWorldPerPcUnit`, MIN_JUMP_RANGE, MAX)
   - Tạo coroutine `DashCasterToTarget(caster, target, jumpSpeed, jumpDistance)` lerp position
   - Gọi `ApplyDamage` + `SpawnProjectiles` **trong suốt quá trình dash** (PC: missile fire theo frame dash)
   - Ở close range (< MIN_JUMP_RANGE): skip dash, chỉ melee attack
10. **Sync visual**: trong `SkillEffectVisualService.PlaySkillCast` (line 220-299) cho 357/128, set `fx.casterFollowing = true` để caster position update mỗi tick. Kết nối với `SandboxPlayerController` (line 157 `MoveTo`) hoặc trực tiếp `transform.position`.
11. **MapEnemy AI**: trong `EnemyAiService`, ignore dash period (don't re-evaluate path during enemy dash).
12. **Verify với test**: unit test cho `MeleeType` resolver; visual test (Full profile boot) cast 357 ở 3 cự ly (xa, vừa, gần) — expect player dash + slash impact ở mọi cự ly.

### Phase 4 — Event chain (nửa ngày)

13. **G6** (1073 event chain): thêm `SpawnStartEvent` (sub-skill 1101) khi missile phóng đi, `SpawnFlyEvent` (sub-skill 1103) khi missile giữa đường, `SpawnCollideSubEffect` (sub-skill 1072) khi tới. Trong `SkillEffectVisualService.PlaySkillCast` (line 220) cho 1073, thêm 3 sub-effect với delay phase.

---

## 5. Pseudo-code cho G1 (Dash) — quan trọng nhất

```csharp
// CombatRuntimeService.ApplySkillCast — sau line 144 SpawnProjectiles

if (skill.meleeType == PcMeleeType.Jump || skill.meleeType == PcMeleeType.JumpAndAttack)
{
    Vector2 from = caster.position;
    Vector2 to = castPoint;
    float dist = Vector2.Distance(from, to);
    float minJump = 64f * RangeWorldPerPcUnit;   // PC MIN_JUMP_RANGE = 64 PC pixel
    float maxJump = (skill.attackRadius > 0 ? skill.attackRadius : 256) * RangeWorldPerPcUnit;
    float jumpDist = Mathf.Clamp(dist, minJump, maxJump);

    if (dist > minJump)
    {
        // Bắt đầu dash
        var dashRoutine = StartCoroutine(DashAndCast(caster, skill, target,
            from, to, jumpDist, skillLevel, report));
        // ApplyDamage + SpawnProjectiles sẽ được gọi TRONG coroutine
    }
    else
    {
        // Close range: chỉ melee attack, vẫn slash
        ApplyDamage(caster, target, levelData, report);
        TriggerSlash(target.position);   // G3
    }
}

private IEnumerator DashAndCast(CombatActorState caster, SkillDefinition skill,
    CombatActorState target, Vector2 from, Vector2 to, float jumpDist,
    int skillLevel, CombatCastReport report)
{
    float jumpSpeed = caster.currentJumpSpeed * 18f;  // PC 18 ticks/sec
    int steps = Mathf.Max(1, Mathf.CeilToInt(jumpDist / jumpSpeed));
    Vector2 dir = (to - from).normalized;

    for (int i = 1; i <= steps; i++)
    {
        // Lerp caster position
        Vector2 nextPos = from + dir * (jumpDist * i / steps);
        caster.SetPosition(nextPos);  // hook SandboxPlayerController.MoveTo
        // Fire damage + missile on each "jump step" (PC pattern)
        if (i % 2 == 0)  // damage every other step to avoid spam
        {
            ApplyDamage(caster, target, skillLevel, report);
        }
        // Always fire sub-skill 389 on impact (G2 fix)
        if (i == steps)
        {
            var sub = _catalog.Resolve(389);
            if (sub != null) {
                ApplyDamage(caster, target, sub.GetPcLevelData(skillLevel), report);
                TriggerSlash(nextPos);
            }
        }
        yield return new WaitForSeconds(1f / 18f);  // 1 PC tick
    }
    caster.SetPosition(to);  // snap to target
}
```

---

## 6. Rủi ro chung khi sửa dash

1. **Camera follow**: camera hiện follow player ở `SandboxPlayerController.FollowCamera` (line 301). Khi player dash, camera phải theo kịp. Có thể cần tăng `sensitivity` hoặc snap camera khi dash.
2. **Input lock**: trong dash, không cho player input move (đã có pattern ở line 287 `_forcedVisualAction`). Cần extend với `_isDashing` flag.
3. **Collision**: PC check `GetBarrier` cho mỗi step. Mobile có `ObstacleGrid` (line 247). Cần gọi `_projectiles.Cast` hoặc equivalent để check barrier.
4. **Horse mount**: PC Phi Long có `horseLimit=1` (không cưỡi ngựa). Mobile đã set `horseLimit:1` (line 213). Nếu mount toggle, phải skip dash. Đã có pattern ở line 117-118 trong `CombatRuntimeService` (validate).
5. **Test trên nhiều map**: dash cần test trên map có barrier (đá, tường) để verify stop-sớm. Map 53 (BLH) có nhiều region tường; map 79 (Đào Lý Động) cũng có. Test cả 2.
6. **Undo path**: nếu dash bị stuck (obstacle), cần revert caster.position về vị trí ban đầu. PC có logic này (line 2929-2939), cần port.
7. **Visual desync**: skill effect ở `SkillEffectVisualService` bind `casterPos` 1 lần (line 247). Nếu caster dash mà `casterPos` không update, visual sẽ vẽ ở vị trí cũ. Cần pass `getCurrentCasterPos` callback tương tự `getCurrentTargetPos` (line 239).
8. **Sau xé timing**: PC 389 (slash) fires ngay khi missile impact. Mobile hiện fires ở `if (skillLevel >= 11)`. Với dash mechanic, slash nên fire ở cuối dash (khi caster chạm target), không phải trên đường dash. Cần check cảm giác thực tế.

---

## 7. Tóm tắt

- **Skill chính ưu tiên sửa**: **357 Phi Long Tại Thiên** (case mẫu user nêu) → fix G2 trước (30 phút, có hiệu ứng ngay) → rồi G1+G3 (2-3 ngày cho dash + slash).
- **Skill phụ**: **128 Kháng Long Hữu Hối** (cùng dash mechanic, sửa song song với 357).
- **Skill ranged còn lại (125, 359, 1073, 1074)**: đã đúng ~80%, chỉ cần verify + sửa minor (mapping ID, event chain).
- **Test plan**: unit test cho MeleeType enum; visual test cast 357 ở 3 cự ly (xa = dash + slash, vừa = dash + slash, gần = no dash + slash).
- **Không cần làm ngay**: 11 skill còn lại (115, 116, 119, 120, 121, 123, 124, 126, 127, 129, 130, 209, 274, 277, 358, 360, 389, 714, 720, 1101, 1103, 1161, 1162, 1602, 1817, 1818) — phần lớn là passive/buff/mastery/sub-skill, đã đúng catalog hoặc nằm ngoài task này.

**Báo cáo đã đầy đủ input để port các skill dash/lunge của toàn bộ môn phái. Mỗi môn phái có 1 section bên dưới, mỗi skill 1 gap table riêng. Cập nhật theo từng phase `[SECT-QUICKWIN]` / `[SECT-DASH]`.**

---

# Phần II — Các môn phái còn lại

> Mỗi section bên dưới do 1 subagent gap-analysis sinh ra theo format giống Phần I (Cái Bang).
> Mỗi subagent được giao scope 1 môn phái, đọc `PcCombatCatalogFactory.cs` cho mobile catalog
> + `vl_update_27/Server 6.0/.../script/skill[N]/{faction}.lua` cho PC source.

---

## 2.1. Võ Đang (PC: 武当 WǔDāng, ID range 151-166)

> **Nguồn PC**: `/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser_bachkim_6.0/server1/script/skill/wudang.lua` (GB2312, đã `iconv -f gb2312` → UTF-8 tại `/tmp/wudang_utf8.lua`).
> Per-skill files: `/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser_bachkim_6.0/server1/script/skill/wudang/*.lua` (20 file TCVN3, 3 file pinyin `qixing-zhen.lua` / `sanhuan-taoyue.lua` / `xuanyi-wuxiang.lua`).
> C++: `KNpc.cpp::CastMeleeSkill` switch — **không có** Võ Đang skill nào thuộc nhánh `Melee_Jump/JumpAndAttack/RunAndAttack`. Toàn bộ Võ Đang skill là ranged magic (lighting damage) hoặc passive/buff. Do đó **G1 (dash) không áp dụng** cho Võ Đang.
> Tóm tắt: Võ Đang là môn phái **không có dash** — gap nặng nhất là sai số liệu (G4, G7) + missing event chain (G6) + missing visual case (G7 visual coverage).

### 2.1.1. Catalog scan

| ID | Tên (suy ra) | Loại PC | Vai trò |
|---:|---|---|---|
| 151 | 武当剑法 Võ Đang Kiếm Pháp (Kiếm Pháp) | Passive mastery | Buff physics dmg + atk rating + crit |
| 152 | 武当拳法 Võ Đang Quyền Pháp (Quyền Pháp) | Passive mastery | Buff lighting dmg + mana shield |
| 153 | 怒雷指 Nộ Lôi Chỉ | Ranged lighting | Active — base lighting missile |
| 154 | 阴阳气 Âm Dương Khí | Passive | Lightning res P |
| 155 | 沧海明月 Thương Hải Minh Nguyệt | Ranged lighting | Active — 2nd basic lighting |
| 156 | 纯阳心法 Thuần Dương Tâm Pháp | Passive | Mana max P |
| 157 | 坐忘无我 Tọa Vọng Vô Ngã | Self buff | Mana shield self |
| 158 | 剑飞惊天 Kiếm Phi Kinh Thiên | Ranged AOE lighting | Active — stationary AOE |
| 159 | 七星阵 Thất Tinh Trận | Self aura | Buff self (atk rating/def/phys) |
| 160 | 梯云纵 Thế Vân Tung | Passive | Attack speed V |
| 161 | 两仪心法 Lưỡng Nghi Tâm Pháp | Passive | Cast speed V |
| **162** | 玄一无象 **Huyền Nhất Vô Tượng** | Ranged surround | Active — surround lighting |
| **163** | 人剑合一 **Nhân Kiếm Hợp Nhất** | Ranged + event chain | Active — surround + StartEvent(371) + CollideEvent(162) |
| 164 | 剥及而复 Bác Cấp Nhi Phục | Ranged + stun | Active — stationary + stun |
| 165 | 无我无剑 Vô Ngã Vô Kiếm | Ranged + 8 fan | Active — 1→8 outward missiles |
| 166 | 太极神功 Thái Cực Thần Công | Passive mastery | Buff attack/cast speed + lighting + mana + crit |

**Không catalog trong mobile** (PC `wudang.lua` có, mobile thiếu): `wudang120` (line 401), `qiwudang150` (line 209), `jianwudang150` (line 322), `jianwudang150_2` (line 375), `jianwudang150_3` (line 388), `sanhuan_taoyue` (line 142-167) [= "Tam Hoàn Cáo Nguyệt" sub-form], `nulei_lianhuanji` (line 264-270), `tiandi_wuji` (line 168-208) ["Thiên Địa Vô Cực"], `taiji_wuyi` (line 254-263) ["Thái Cực Vô Ý"], `jianqi_zongheng` (line 241-253) ["Kiếm Khí Tung Hoành"]. Đây là sub-form/120/150-tier — nằm ngoài scope Phần II (chỉ rà 151-166). Phase 5.

### 2.1.2. Gap table

| ID | Tên | Hành vi mobile | Hành vi PC | Gap | Severity | Effort |
|---:|---|---|---|---|---|---|
| **162** | Huyền Nhất Vô Tượng | `LightingDamageV(4+lv*7, 0, 296+lv*59)` (line 406) — dùng công thức per-skill `xuanyi-wuxiang.lua::Getlightingdamage_v` (4+lv*7, 296+lv*59) | `wudang.lua::xuanyi_wuxiang` chính: `lightingdamage_v[1]={{1,1},{20,10}}, [3]={{1,10},{20,100}}` → L20: min=10, max=100. Per-skill file nói 144/1476 — conflict nguồn. | **G7 — tuning sai ~14×** (mobile dùng per-skill file sai; PC wudang.lua bảng chính nói L20: 10/100) | Cao (damage quá lớn so với PC) | 1 giờ |
| **163** | Nhân Kiếm Hợp Nhất | (1) `childSkillId=215` (line 411) — 215 không thấy trong PC `wudang.lua`. (2) `ConfigureWuDangVisuals` **không có case 163** (chỉ 153/155/158/159/164/165). (3) `SpawnCollideSubEffect` (line 455-466) **không handle 163** (chỉ 1073). (4) `StartEvent` (sub 371) không có runtime path. (5) `ShowEvent` (animation id 5) không có. | PC `wudang.lua::renjian_heyi`: `skill_startevent[3]={1,371},{20,371}`; `skill_collideevent[3]={1,162},{20,162}`; `skill_showevent[3]={1,0},{10,0},{10,1},{15,1},{15,5},{20,5}` | **G6 — thiếu toàn bộ event chain** + **G4 — childSkillId=215 sai** + **G7 visual — thiếu case** | Cao (không visual + event chain thiếu = damage sai + mất cảm giác) | nửa ngày |
| 164 | Bác Cấp Nhi Phục | `radius=470` (line 304); `SetupPcStationaryEffect` (line 525) child missile 28 LifeTime=12 ok | PC `wudang.lua::boji_erfu`: `skill_attackradius={{1,384},{20,416}}` → L20=416. Mobile 470 = +13% | **G4 — radius sai** (470 vs 416) | Trung bình | 30 phút |
| **165** | Vô Ngã Vô Kiếm | `childSkillId=29, childSkillNum=16` (line 310) + `SetupPcCircleOutwardMissiles(fx, 16)` (line 530) | PC `wudang.lua::wuwo_wujian`: `skill_misslenum_v={{1,1},{20,8},{21,8}}` → max 8 missiles. `skill_attackradius={{1,448},{20,512},{21,512}}` → L20=512 | **G4 — childSkillNum=16 sai (PC max 8)** + radius 400 vs 512 | Cao (2× quạt missile sai cảm giác + radius hẹp hơn) | 30 phút |
| 153 | Nộ Lôi Chỉ | `radius=400` (line 280); child=24 num=1 | PC `nulei_zhi`: `skill_attackradius={{1,320},{20,384}}` → L20=384. Mobile 400 = +4% | **G4 — radius lệch nhẹ** (400 vs 384) | Thấp (4%) | 30 phút |
| 155 | Thương Hải Minh Nguyệt | `radius=480` (line 285) | PC `canghai_mingyue`: `skill_attackradius={{1,320},{20,384}}` → L20=384. Mobile 480 = +25% | **G4 — radius sai lớn** (480 vs 384) | Trung bình | 30 phút |
| 158 | Kiếm Phi Kinh Thiên | `radius=400` (line 293) | PC `jianfei_jingtian`: `skill_attackradius={{1,384},{20,416}}` → L20=416. Mobile 400 ≈ ok | OK (sai số 4%) | Thấp | không cần |
| 159 | Thất Tinh Trận | `radius=180, child=211` (line 424); `ConfigureWuDangVisuals` (line 520) — 211 path `bz_bo1_金波.spr` đúng | PC `qingxing_zhen`: no `skill_attackradius` (default 180 ok) | OK | — | — |
| 161 | Lưỡng Nghi Tâm Pháp | `CastSpeedV = Floor(Log10(lv+1) * 80f)` (line 400) | PC không có per-skill file `lianyi-xinfa.lua`; công thức có thể đúng/sai cần verify | Có thể G7 — không tìm thấy file per-skill | Thấp-trung bình | 1 giờ verify |
| 166 | Thái Cực Thần Công | multi-attribute (line 437) dùng Link(lv,(1,21),(30,65)) | PC `taiji_shengong`: `attackspeed_v={{1,21},{30,65},{33,69},{35,90},{38,94},{41,98}}` — bỏ curve giữa 33/35/38/41 | **G4 — sai curve nội suy** cho 30→65 | Trung bình | 1 giờ |
| 151, 152, 154, 156, 157, 160 | (passive/buff) | match PC | match PC | OK | — | — |
| **Tất cả** | — | `PcSkillTuningRegistry.RadiusCurves[WuDangId]` (line 102-107) chỉ cover 153/155/158, **thiếu 162/163/164/165** | PC có data đầy đủ | **G7 — Tuning coverage 18%** (3/16 active) | Trung bình | 1 ngày (thêm 4 entries vào `PcSkillTuningRegistry`) |

### 2.1.3. Phase 1 quick wins (gate, childSkillNum, radius, tuning)

- [ ] **ID 165**: sửa `childSkillNum=16` → `8` (PC `skill_misslenum_v` max 8); sửa `radius=400` → `512` (PC L20=512). Sửa 1 dòng catalog + add curve vào `PcSkillTuningRegistry.WuDangId[165]={(1,448),(20,512)}`. (G4)
- [ ] **ID 164**: sửa `radius=470` → `416` (PC L20). (G4)
- [ ] **ID 155**: sửa `radius=480` → `384` (PC L20). (G4)
- [ ] **ID 153**: sửa `radius=400` → `384` (PC L20, sai số 4%). (G4, thấp)
- [ ] **ID 162**: sửa damage `4+lv*7, 0, 296+lv*59` → `Link(lv,(1,1,""),(20,10,"")), 0, Link(lv,(1,10,""),(20,100,""))` (theo `wudang.lua::xuanyi_wuxiang` chính, KHÔNG theo per-skill `xuanyi-wuxiang.lua`). (G7 — ~14× damage reduction, test cân bằng)
- [ ] **ID 163**: thêm docstring: `childSkillId=215` là tham chiếu nội bộ; runtime cần handle `skill_startevent→371` + `skill_collideevent→162`. Add 2 entries vào `PcSkillTuningRegistry.WuDangId`: 162=(1,520),(20,520) và 163=(1,90),(20,90). (G7 tuning)
- [ ] **Tuning coverage**: thêm `PcSkillTuningRegistry.RadiusCurves[WuDangId]` entries cho 162, 163, 164, 165. (G7)
- [ ] **ID 166**: bổ sung điểm curve giữa (30,65) cho `attackspeed_v` (line 437). (G4 thấp)
- [ ] **Verify ID 161**: tìm `lianyi-xinfa.lua` per-skill (không có trong `/wudang/`), nếu thiếu thì giữ `Log10(lv+1)*80f`. (G7 thấp, 1 giờ verify)

### 2.1.4. Phase 3 dash

- [ ] **Không áp dụng** — Võ Đang không có skill dash/melee/jump. Cả `KNpc.cpp::CastMeleeSkill` switch (line 1834) và `wudang.lua` đều không tham chiếu `Melee_Jump/JumpAndAttack/RunAndAttack` cho ID 151-166.

### 2.1.5. Phase 4 event chain (G6)

- [ ] **ID 163 — RenJianHeYi event chain** (quan trọng nhất):
  1. Pre-cast: gọi `SpawnStartEvent(fx, 371)` — fire sub-skill 371 ngay (PC `skill_startevent[3]={1,371},{20,371}`)
  2. Collide: sửa `SpawnCollideSubEffect` switch (line 461) thêm `case 163 => 162` (PC `skill_collideevent[3]={1,162},{20,162}`)
  3. ShowEvent: PC `skill_showevent[3]={1,0},{10,0},{10,1},{15,1},{15,5},{20,5}` — animation id 1 L10-14, id 5 L15-20. Map sang `charAnimId` L-gated. Effort: nửa ngày
- [ ] **ID 163 — visual case** (G7 visual): thêm `case 163: // Nhân Kiếm Hợp Nhất` vào `ConfigureWuDangVisuals`. PC `renjian_heyi` không khai báo missile sprite riêng, dùng `wd_05_剥及而复.spr` (sister 164) hoặc tạo mới. Effort: 1 giờ
- [ ] **Phase 5 (future)**: port `qiwudang150` / `jianwudang150` / `jianwudang150_2` / `jianwudang150_3` / `wudang120` / `tiandi_wuji` / `sanhuan_taoyue` / `nulei_lianhuanji` / `taiji_wuyi` / `jianqi_zongheng` từ PC wudang.lua line 142-400 (chưa có trong mobile, ID chưa biết — cần check `PcSkills.txt` 150-tier wudang).

### 2.1.6. Trạng thái

- [x] Catalog scan xong (16 skill, 6 active attack, 10 passive/buff)
- [ ] Quick-win phase merged (Phase 1 — 9 items)
- [ ] Event chain phase merged (Phase 4 — 2 items, ID 163 chính)
- [ ] Dash phase merged: **không áp dụng** (Võ Đang không có dash)
- [ ] Tuning coverage 18% → 100% (cần thêm 4 entries vào `PcSkillTuningRegistry.WuDangId`)

---

## 2.2. Thiên Vương Bang (PC: 天王帮 TiānWángBāng, ID range 23-42)

> **Nguồn PC**:
> - `Assets/StreamingAssets/Reference/ModSkills.txt` (canonical, TCVN3, SkillId 23-42 trừ 25/27/28/38/39)
> - `Assets/StreamingAssets/Reference/PcSkills.txt` (PC gốc, cùng schema)
> - `/var/www/vltksource_new/vl_update_27/Server 6.0/.../script/skill/tianwang.lua` (GB18030, 763 dòng, 23 skills)
> - `/var/www/vltksource_new/vl_update_27/Server 6.0/.../script/skill/tianwang/*.lua` (26 file per-skill, gồm 6 pinyin + 20 Chinese sub-form 150-tier)
> - C++: `KNpc.cpp::CastMeleeSkill` switch — không có Thiên Vương skill nào thuộc nhánh `Melee_Jump/JumpAndAttack/RunAndAttack` (IsMelee=1 nhưng không có MeleeType dash). Tất cả là **instant melee** (chém 1 nhát hoặc multi-hit liên tiếp qua childSkillNum).
> - Tóm tắt: Thiên Vương là môn phái **melee cận chiến thuần**, đa số dùng `IsMelee=1` + `childSkillNum=1-4` (multi-hit pattern). **G1 (dash) KHÔNG áp dụng** — toàn bộ 9 skill active là instant melee, không phải dash. Gap nặng nhất: **G4 (childSkillNum/childSkillId bị mất hoàn toàn)** — mất cảm giác multi-hit "thương pháp 2 chiêu liên tiếp" / "đao pháp 3 chiêu liên tiếp".

### 2.2.1. Catalog scan

| ID | Tên (suy ra từ catalog) | PC ModSkills.txt | PC tianwang.lua | Vai trò |
|---:|---|---|---|---|
| 23 | Thiên Vương Thương Pháp (passive) | ✓ `tianwang_qiangfa` | LvlSetting addphysicsdamage_p | Buff thương pháp mastery |
| 24 | Thiên Vương Đao Pháp (passive) | ✓ `tianwang_daofa` | LvlSetting addphysicsdamage_p | Buff đao pháp mastery |
| 26 | Thiên Vương Chùy Pháp (passive) | ✓ `tianwang_chuifa` | LvlSetting addphysicsdamage_p | Buff chùy pháp mastery |
| **29** | Trảm Long Quyết | ✓ `zhanlong_jue` | L12-50 | Melee + 1 hit + 5 shadow |
| **30** | Hồi Phong Lạc Nhạn | ✓ `huifeng_luoyan` | L13-49 | Melee + **2 hits liên tiếp** |
| **31** | Hành Vân Quyết | ✓ `xingyun_jue` | L13-50 | Melee + 1 hit + cold damage |
| **32** | Vô Tâm Trảm | ✓ `wuxin_zhan` | L13-44 | Melee + 1 hit + **5 shadow** |
| 33 | Tĩnh Tâm Quyết (buff) | ✓ `jingxin_jue` | L46-0 | Self buff attack rating |
| **34** | Kinh Lôi Trảm | ✓ `jinglei_zhan` | L13-48 | Melee + 1 hit |
| **35** | Dương Quan Tam Điệp | ✓ `yangguan_sandie` | L12-53 | Melee + **3 hits liên tiếp** |
| 36 | Thiên Vương Chiến Ý (passive 30) | ✓ `tianwang_zhanyi` | L9999-0 | Passive HP/deadly/attackspeed |
| **37** | Bát Phong Trảm | ✓ `pofeng_zhan` (sai Chinese: 8-gió vs phá-gió) | L13-50 | Melee + 1 hit + **5 shadow** |
| **40** | Đoạn Hồn Thích | ✓ `duanhun_ci` | L11-56 | Melee + 1 hit + **5 shadow** + stun + multi-thrust (waitTime=5, timePerCast=27) |
| **41** | Huyết Chiến Bát Phương | ✓ `xuezhan_bafang` | L12-57 | Melee + **4 hits liên tiếp** + 5 shadow |
| 42 | Kim Chung Tráo (buff) | ✓ `jinzhong_zhao` | L49-0 | Self buff phys/cold/fire/poison res |

**Không catalog trong mobile** (PC tianwang.lua có, mobile thiếu): `chenglong_jue` (Thừa Long Quyết, có MaxShadow=5, MslsGenerate=8), `potian_zhan` (Phá Thiên Trảm), `zhuixing_zhuyue` (Truy Tinh Trục Nguyệt), `zhuifeng_jue` (Truy Phong Quyết), `daotianwang150` / `qiangtianwang150` / `chuitianwang150` (3 sub-form 150-tier với skill_collideevent/skill_showevent), `daoxutian` (Đảo Hư Thiên — passive mastery 20-level), `fengyun-jiang` (Phong Vân Giang). Ngoài scope task này (range 23-42, ReqLevel 10-60). Phase 5.

**TianWang IsTianWangSkill exclude**: 25, 27, 28, 38, 39 không thuộc Thiên Vương. ModSkills.txt confirm các ID này không phải Thiên Vương (là shared/common skill khác). Không phải gap.

### 2.2.2. Gap table

> **Chú thích cột "Hành vi mobile"**: lấy từ `CreateTianWangSkills` trong `PcCombatCatalogFactory.cs` line 1461-1702. Mọi skill Thiên Vương trong mobile đều dùng `PcSkillStyle.Melee` + `targetEnemy=true` (trừ 33/42 self-buff, 23/24/26/36 passive).
>
> **Chú thích cột "Hành vi PC"**: lấy từ `ModSkills.txt`/`PcSkills.txt` (AttackRadius, ChildSkillId, ChildSkillNum, MaxShadowNum, MslsGenerate, CharAnimId, IsMelee, WaitTime, CostValue, TimePerCast, IsPhysical) + `tianwang.lua` (damage curves per level) + `tianwang/*.lua` (per-skill file).

| ID | Tên | Hành vi mobile | Hành vi PC | Gap | Severity | Effort |
|---:|---|---|---|---|---|---|
| **29** | Trảm Long Quyết | `radius=90, childSkillId=0, childSkillNum=0, charAnimId=2, timePerCast=0, waitTime=0, cost=10` | `radius=72, childSkillId=405, childSkillNum=1, charAnimId=9, isMelee=1, MslsGenerate=1, HorseLimit=1, ReqLevel=10`. PC `zhanlong_jue`: physenhance_p {{1,80},{20,185}} (mobile: 30→150) | **G4 radius sai (90 vs 72)** + **G4 childSkillNum=0 vs PC=1 (mất hit)** + **G4 charAnimId 2 vs PC 9** + **G7 physenhance_p sai lớn (80/185 vs 30/150)** | Cao (mất cảm giác Trảm Long + damage sai) | 1 giờ |
| **30** | Hồi Phong Lạc Nhạn | `radius=90, childSkillNum=0, charAnimId=2, cost=10` | `radius=90 ✓, childSkillId=219, childSkillNum=2, charAnimId=9, isMelee=1, HorseLimit=1, ReqLevel=10`. PC `huifeng_luoyan`: physenhance_p {{1,80},{20,215}} (mobile 20→120) | **G4 childSkillNum=0 vs PC=2 — MẤT 2-HIT PATTERN** (mobile chỉ chém 1 lần, PC chém 2 liên tiếp). SkillDesc PC: "Thương pháp nhập môn, một lần xuất liên tiếp 2 chiêu" | **Cao** (cốt lõi cảm giác "2 chiêu liên tiếp") | 2 giờ (cần thêm child skill 219 logic) |
| **31** | Hành Vân Quyết | `radius=80, childSkillNum=0, charAnimId=2, cost=10, no visual` | `radius=72, childSkillId=406, childSkillNum=1, charAnimId=9, isMelee=1, HorseLimit=1, ReqLevel=30`. PC `xingyun_jue`: physenhance_p {{1,30},{20,255}}, colddamage_v {{1,5},{20,50}} (mobile 0). No `ConfigureTianWangVisuals` case. | **G4 radius 80 vs 72** + **G4 childSkillNum=0 vs PC=1** + **G7 physenhance_p sai lớn (30/255 vs 30/150)** + **G7 mất colddamage_v** + **G7 visual missing (no case 31)** + **G7 PcSkillTuningRegistry thiếu ID 31** | Cao (mất cold damage + multi-hit) | 2 giờ |
| **32** | Vô Tâm Trảm | `radius=90, childSkillNum=0, charAnimId=2, cost=20→40` | `radius=90 ✓, childSkillId=220, childSkillNum=1, charAnimId=9, isMelee=1, MaxShadowNum=5, MslsGenerate=5, HorseLimit=0, ReqLevel=60`. PC `wuxin_zhan`: physenhance_p {{1,65},{20,453}} (mobile 80→385) | **G4 childSkillNum=0 vs PC=1** + **G4 MaxShadowNum=5 bị bỏ** (multi-shadow pattern) + **G4 charAnimId 2 vs PC 9** + **G7 physenhance_p sai (65/453 vs 80/385)** + **G7 deadlystrike_p sai (PC {{1,4},{20,25}} vs mobile 15/54)** | Cao | 2 giờ |
| 33 | Tĩnh Tâm Quyết | `radius=400, childSkillNum=0, skillStyle=InitiativeNpcState, targetSelf, charAnimId=2, cost=20` | `radius=0 (buff), isMelee=0, charAnimId=11, ReqLevel=20, state_special 46`. PC `jingxin_jue`: attackratingenhance_p {{1,45},{20,400}} (mobile 10→100) | **G4 radius 400 vs PC 0** (buff không cần radius) + **G4 charAnimId 2 vs PC 11** + **G7 attackratingenhance_p sai lớn (45/400 vs 10/100, hệ số 4× sai)** + **G7 state priority/StateSpecialId=46 bị bỏ** | Trung bình (damage buff sai hệ số 4×) | 1 giờ |
| **34** | Kinh Lôi Trảm | `radius=72 ✓, childSkillNum=0, charAnimId=2, cost=10` | `radius=72 ✓, childSkillId=404, childSkillNum=1, charAnimId=9, isMelee=1, HorseLimit=0, ReqLevel=10`. PC `jinglei_zhan`: physenhance_p {{1,40},{20,200}} (mobile 20→120) | **G4 childSkillNum=0 vs PC=1** + **G4 charAnimId 2 vs PC 9** + **G7 physenhance_p sai (40/200 vs 20/120, sai số 2× L1, sai số 1.7× L20)** | Trung bình | 1 giờ |
| **35** | Dương Quan Tam Điệp | `radius=90, childSkillNum=0, charAnimId=2, cost=15` | `radius=90 ✓, childSkillId=221, childSkillNum=3, charAnimId=10, isMelee=1, HorseLimit=1, ReqLevel=30`. PC `yangguan_sandie`: physenhance_p {{1,130},{20,375}} (mobile 35→221) | **G4 childSkillNum=0 vs PC=3 — MẤT 3-HIT PATTERN** (mobile chỉ 1 chém, PC chém 3). SkillDesc PC: "Thương pháptrung cấp, một lần xuất liên tiếp 3 chiêu" | **Cao** ("3 chiêu liên tiếp" là cốt lõi) | 2 giờ |
| 36 | Thiên Vương Chiến Ý (passive 30) | `skillStyle=PassivityNpcState, charAnimId=14, ManaMaxP {{1,5},{30,60}}, DeadlyStrikeEnhanceP {{1,2},{30,20}}` | `skillStyle=3 (passive) ✓, charAnimId=11, isMelee=0, ReqLevel=60, MaxLevel=30`. PC `tianwang_zhanyi`: **lifemax_p {{1,21},{30,185}}** + **lifemax_yan_p {{1,21},{35,160}}** + deadlystrikeenhance_p {{1,5},{30,45}} + **attackspeed_v {{1,5},{30,65}}** | **G7 bỏ 4 attribute: lifemax_p (HP max +185% L30) + lifemax_yan_p (smoke) + attackspeed_v (cast/atk speed) + sai deadlystrike (2/20 vs 5/45)** | **Cao** (mất 3 buff chính: HP max, atk speed, deadly) | 2 giờ |
| **37** | Bát Phong Trảm | `radius=90, childSkillNum=0, charAnimId=2, cost=15` | `radius=90 ✓, childSkillId=222, childSkillNum=1, charAnimId=9, isMelee=1, MaxShadowNum=5, MslsGenerate=8, HorseLimit=0, ReqLevel=30`. PC `pofeng_zhan`: physenhance_p {{1,120},{20,275}} (mobile 30→222) | **G4 childSkillNum=0 vs PC=1** + **G4 MaxShadowNum=5 bị bỏ** + **G4 charAnimId 2 vs PC 9** + **G7 physenhance_p sai lớn (120/275 vs 30/222, L1 sai 4×)** + **G5 Chinese name "八风斩" (bát phong = 8-wind) vs PC Lua `pofeng_zhan` (泼风斩 = phá-phong/splash-wind) — name drift** | Cao | 2 giờ |
| **40** | Đoạn Hồn Thích | `radius=200, childSkillNum=0, charAnimId=2, timePerCast=0, waitTime=0, cost=20, no Param1` | `radius=200 ✓, childSkillId=224, childSkillNum=1, charAnimId=9, isMelee=1, MaxShadowNum=5, MslsGenerate=5, HorseLimit=0, **WaitTime=5**, **TimePerCast=27**, ReqLevel=40, **Param1=32 (MslsGenerateData)**, **Param1Memo=5**. PC `duanhun_ci`: stun_p {{1,16},{20,35}} (mobile 10→50), deadlystrike_p {{1,4},{20,80}} (mobile 5→25) | **G4 childSkillNum=0 vs PC=1** + **G4 MaxShadowNum=5 bị bỏ** + **G4 charAnimId 2 vs PC 9** + **G4 waitTime 0 vs PC 5 (multi-thrust pattern)** + **G4 timePerCast 0 vs PC 27** + **G4 Param1=32 (multi-missile) bị bỏ** + **G7 stun_p sai (16/35 vs 10/50)** + **G7 deadlystrike_p sai lớn (80 L20 vs 25)** | **Cao** (Đoạn Hồn Thích là multi-thrust "5 tia đâm", mất hoàn toàn) | nửa ngày |
| **41** | Huyết Chiến Bát Phương | `radius=90, childSkillNum=0, charAnimId=2, cost=25` | `radius=90 ✓, childSkillId=225, childSkillNum=4, charAnimId=9, isMelee=1, MaxShadowNum=5, MslsGenerate=4, HorseLimit=1, ReqLevel=60`. PC `xuezhan_bafang`: physenhance_p {{1,60},{20,723}} (mobile 80→385), attackrating_p {{1,75},{20,320}} (mobile 10→60) | **G4 childSkillNum=0 vs PC=4 — MẤT 4-HIT PATTERN** (mobile 1 chém, PC chém 4 chiêu liên tiếp). SkillDesc PC: "Đao pháp cao cấp, một lần xuất liên tiếp 3 chiêu" | **Cao** (cốt lõi "4 chiêu liên tiếp") | 2 giờ |
| 42 | Kim Chung Tráo (buff) | `radius=400, skillStyle=InitiativeNpcState, targetSelf, charAnimId=2, cost=30` | `radius=0 (buff), isMelee=0, charAnimId=11, ReqLevel=50, state_special 49`. PC `jinzhong_zhao`: physicsres_p {{1,12},{20,50}} (mobile 10→40), coldres_p {{1,7},{20,45}} (mobile 5→25), fireres_p **{{1,-5},{20,-15}}** (mobile +5→+25, **DẤU SAI** — PC trừ fire res, mobile cộng), poisonres_p {{1,12},{20,49}} (mobile 0) | **G4 radius 400 vs PC 0** + **G4 charAnimId 2 vs PC 11** + **G7 physicsres_p sai (12/50 vs 10/40)** + **G7 coldres_p sai (7/45 vs 5/25)** + **G7 fireres_p DẤU SAI (âm ↔ dương)** + **G7 mất poisonres_p hoàn toàn** + **G7 state_special 49 bị bỏ** | **Cao** (fireres_p dấu ngược = nghịch lý gameplay — thiếu mà thành thừa fire res) | nửa ngày |
| 23, 24, 26 | (passive mastery) | match PC: addphysicsdamage_p, attackratingenhance_p, deadlystrikeenhance_p; charAnimId=14 ✓ | PC match (cùng tianwang.lua schema) | OK (3 mastery passive match) | — | — |
| 32, 37, 40, 41 | (MaxShadowNum=5) | mobile không xử lý MaxShadowNum | PC có MaxShadowNum=5 + MslsGenerate=5/8/5/4 (multi-shadow kiếm/phóng liên tục) | **G4 MaxShadowNum bị bỏ hoàn toàn** — mất cảm giác "5 kiếm/phóng ẩn" — cần thêm `PcSkillTuningRegistry`/`missilesGenerateData` mechanism cho melee. Hiện `DamageSkillNew` chỉ set `missilesGenerateData` cho ranged (line 525), chưa có cho Melee | Trung bình (visual feel) | nửa ngày (refactor DamageSkillNew để cover Melee) |
| **All 9 active** | charAnimId=2 (mobile default) | mobile mọi Melee = 2; PC yêu cầu **9** (cho hầu hết) và **10** (riêng 35) | **G4 charAnimId sai toàn bộ** — animation phải là charAnimId=9 hoặc 10 theo ModSkills.txt, không phải 2 | Trung bình (animation có thể trông sai) | 1 giờ (sửa 9 dòng) |
| **All 9 active** | `cost` dùng Link(lv,...) hard-coded 10/15/20/25/30 (flat) | PC `CostValue=0` (dựa vào `skill_cost_v` Lua) | **G4 cost sai schema** — mobile hard-code cost; PC để CostValue=0 rồi `skill_cost_v={{1,X},{20,Y}}` (curve). Hiện cost mobile khớp tương đối PC ở một số skill, sai ở một số khác | Thấp (cost hiện tại gần đúng) | 1 giờ verify |
| **All 9 active** | `timePerCast=0, waitTime=0` (default) | PC có `WaitTime`+`TimePerCast` riêng (đặc biệt 40=5/27) | **G4 timing sai** — multi-thrust/multi-hit cần waitTime để delay giữa các hit; hiện mobile cast 1 lần xong là xong | Trung bình (mất cảm giác multi-step) | 2 giờ |
| **Tất cả 8 attack** | `PcSkillTuningRegistry.RadiusCurves[TianWangId]` cover 29/30/32/34/35/37/40/41 (8/9 active) | PC có 9 active skills cần tuning | **G7 Tuning coverage 89%** (thiếu ID 31) | Thấp (chỉ thiếu 1 entry) | 15 phút |
| **Tất cả 8 attack** | Registry dùng mobile radius (90/72/200) | PC ModSkills.txt radius values (72/90/72/72/90/90/200/90 — đa số match mobile ở 8/9 case) | OK (8/9 match) cho 30/32/34/35/37/40/41. **ID 29 sai: registry=72 (PC value), catalog=90 (mobile default). CombatRuntimeService dùng registry → runtime dùng 72, nhưng catalog hardcode 90. Inconsistency** | Thấp (inconsistency) | 30 phút |

### 2.2.3. Phase 1 quick wins (CRITICAL — multi-hit restoration)

> **Đây là phase quan trọng nhất** cho Thiên Vương. Mất multi-hit pattern = mất 70% cảm giác melee phái này. Mobile hiện tại 9 active skill đều "1 chém xong" — PC có **30 (2-hit)**, **35 (3-hit)**, **41 (4-hit)**, **40 (multi-thrust với waitTime=5)**. Phase 1 phải restore ít nhất 3 skill multi-hit chính.

- [ ] **ID 30 Hồi Phong Lạc Nhạn** (priority cao nhất — 2-hit): thêm `s.childSkillId = 219; s.childSkillNum = 2; s.baseSkill = true;` vào `TianWangHoiPhongLacNhan()` (line 1536). Cần thêm child skill 219 vào catalog (nếu chưa có) — kiểm tra `CreateCaiBangSkills` hay `DamageSkillNew` đã đăng ký 219 chưa. (G4, 2 giờ — cần test multi-missile sequence với 2 child cùng cast)
- [ ] **ID 35 Dương Quan Tam Điệp** (priority cao — 3-hit): thêm `s.childSkillId = 221; s.childSkillNum = 3; s.baseSkill = true;` vào `TianWangDuongQuanTamDiep()` (line 1613). (G4, 2 giờ)
- [ ] **ID 41 Huyết Chiến Bát Phương** (priority cao — 4-hit): thêm `s.childSkillId = 225; s.childSkillNum = 4; s.baseSkill = true;` vào `TianWangHuyetChienBatPhuong()` (line 1671). (G4, 2 giờ)
- [ ] **ID 40 Đoạn Hồn Thích** (priority cao — multi-thrust 5-step): thêm `s.childSkillId = 224; s.childSkillNum = 1; s.baseSkill = true; s.waitTime = 5; s.timePerCast = 27; s.missilesGenerateData = 32;` (G4, nửa ngày)
- [ ] **ID 29, 31, 32, 34, 37**: thêm `s.childSkillId = 405/406/220/404/222; s.childSkillNum = 1; s.baseSkill = true;` cho 5 skill còn lại (PC đều có 1-hit pattern, mất là sai). (G4, 1 giờ tổng)
- [ ] **ID 36 Thiên Vương Chiến Ý** (passive 30): thêm 3 attributes bị thiếu — `lifemax_p {{1,21},{30,185}}` + `attackspeed_v {{1,5},{30,65}}` + sửa `deadlystrikeenhance_p` từ `(1,2,30,20)` → `(1,5,30,45)`. Cộng thêm `stateSpecialId=49` (PC ModSkills.txt). (G7, 2 giờ)
- [ ] **ID 42 Kim Chung Tráo** (buff): sửa `fireres_p` dấu — từ `+5→+25` (mobile) → `-5→-15` (PC âm — TRỪ fire res khi dùng buff). Sửa `physicsres_p` 10/40 → 12/50; `coldres_p` 5/25 → 7/45; thêm `poisonres_p {{1,12},{20,49}}`. Thêm `stateSpecialId=49`. (G7, 2 giờ)
- [ ] **All 9 active charAnimId** (sai toàn bộ): sửa `charAnimId = 2` → `charAnimId = 9` (cho 29/30/31/32/34/37/40/41) và `charAnimId = 10` (cho 35). Riêng 33/42 dùng `charAnimId = 11` (buff). Riêng 23/24/26/36 dùng `charAnimId = 14` (passive — đã đúng). (G4, 1 giờ tổng)
- [ ] **ID 33 Tĩnh Tâm Quyết** (buff): sửa `attackratingenhance_p` từ `Link(lv, (1, 10, ""), (20, 100, ""))` → `Link(lv, (1, 45, ""), (20, 400, ""))` (PC sai hệ số 4× L1). Sửa `radius=400` → `0` (buff không cần radius, dù AOE cho state có thể 400 thì giữ cũng OK). Sửa `charAnimId=2` → `11`. Thêm `stateSpecialId=46`. (G7, 1 giờ)
- [ ] **ID 31 visual missing**: thêm `case 31: // Hành Vân Quyết` vào `ConfigureTianWangVisuals` (line 1079). PC `xingyun_jue` không có missile riêng, dùng chung `42ed0184` preCast + 1 loại stationary effect (giống 29/35). (G7, 1 giờ)
- [ ] **ID 31 PcSkillTuningRegistry**: thêm `[31] = new[] { (1, 72), (20, 72) }` vào `RadiusCurves[TianWangId]` (line 42). (G7, 15 phút)
- [ ] **ID 29 radius catalog vs registry**: catalog hardcode 90, registry 72. Sửa catalog `radius=90` → `72` (hoặc ngược lại, dùng giá trị PC). (G4, 15 phút)
- [ ] **ID 31 radius catalog**: sửa 80 → 72 (PC). (G4, 15 phút)
- [ ] **G5 ID 37 Chinese name** (cosmetic): thay `"八风斩"` → `"泼风斩"` (mobile) cho khớp PC `pofeng_zhan`. Vietnamese "Bát Phong Trảm" giữ nguyên vì đó là translation official. (G5, 5 phút)

### 2.2.4. Phase 3 dash (G1)

- [ ] **Không áp dụng** — Thiên Vương không có skill dash/melee-jump. Cả `KNpc.cpp::CastMeleeSkill` switch (line 1834), `ModSkills.txt` (IsMelee=1 nhưng không có MeleeType dash), và `tianwang.lua` đều không tham chiếu `Melee_Jump/JumpAndAttack/RunAndAttack` cho ID 23-42. Toàn bộ 9 active skill là instant melee (1 swing hoặc multi-hit qua childSkillNum), không phải dash. Phase 3 bỏ qua cho Thiên Vương.

### 2.2.5. Phase 4 event chain (G6)

- [ ] **Không áp dụng** — PC Thiên Vương (ID 23-42) không có `skill_startevent` / `skill_flyevent` / `skill_collideevent` / `skill_showevent` (tất cả =0 trong ModSkills.txt). Chỉ 150-tier sub-form (`daotianwang150`) mới có `skill_collideevent` / `skill_showevent` (line 384-389 tianwang.lua) — đó là scope Phase 5 (chưa có trong mobile). Phase 4 bỏ qua cho Thiên Vương range 23-42.

### 2.2.6. Trạng thái

- [x] Catalog scan xong (15 skill: 4 passive, 2 self-buff, 9 active melee)
- [ ] **Quick-win phase merged** (Phase 1 — 13 items, priority: 30/35/40/41 multi-hit + 36/42 attribute fix)
- [ ] Dash phase merged: **không áp dụng** (Thiên Vương không có dash)
- [ ] Event chain phase merged: **không áp dụng** (PC range 23-42 không có event chain)
- [ ] Tuning coverage 89% → 100% (chỉ thiếu ID 31, fix 15 phút)

### 2.2.7. Tổng kết

- **Skill chính ưu tiên sửa**: **30 (Hồi Phong Lạc Nhạn 2-hit)**, **35 (Dương Quan Tam Điệp 3-hit)**, **41 (Huyết Chiến Bát Phương 4-hit)** — đây là 3 skill định nghĩa "multi-hit liên tiếp" của Thiên Vương. Sửa childSkillNum 1 lần mở ra 3/9 active skill.
- **Skill ưu tiên #2**: **40 (Đoạn Hồn Thích)** — multi-thrust pattern với waitTime + timePerCast. Effort cao hơn (cần animation 5-step).
- **Skill passive/buff**: 36 (Thiên Vương Chiến Ý) + 42 (Kim Chung Tráo) có lỗi **dữ liệu nghiêm trọng** (G7 — fireres_p dấu sai, lifemax_p mất). Fix 30 phút → 2 giờ mỗi skill, giá trị cao (gameplay buff).
- **Skill thứ cấp**: 29, 31, 32, 34, 37 — childSkillNum đơn giản, fix 5 phút mỗi skill.
- **Test plan**: unit test cho childSkillNum sequence (cast 30 ở level 10, expect 2 hit mỗi 1 PC tick); visual test cast 35 ở close range (expect 3 swing animation liên tiếp); regression test cast 41 xem không cast 4 missile rải rác mà là 4 hit tập trung vào target.
- **Không cần làm dash/event chain** cho range 23-42 (Thiên Vương melee cận chiến thuần, không có dash, không có event chain).
- **Phase 5 (future)**: port 150-tier sub-form `daotianwang150` / `qiangtianwang150` / `chuitianwang150` (có event chain) + mastery `daoxutian` (passive 20-level) + 4 active skills còn thiếu `chenglong_jue` / `potian_zhan` / `zhuixing_zhuyue` / `zhuifeng_jue`. Cần check ID trong `PcSkills.txt` (ngoài range 23-42 — có thể 700+).

---

## 2.6. Nga My (PC: 峨嵋/ÉMéi, ID range 77-94)

> **Nguồn PC**:
> - `Assets/StreamingAssets/Reference/ModSkills.txt` (canonical TCVN3, SkillId 77-94)
> - `/var/www/vltksource_new/vl_update_27/Server 6.0/.../script/skill/emei.lua` (GB18030, 386 dòng, 18 skills + SkillExpFunc helper)
> - `/var/www/vltksource_new/vl_update_27/Server 6.0/.../script/skill/emei/*.lua` (19 per-skill files, gồm 3 pinyin `fofa-wubian.lua` / `foguang-puzhao.lua` / `linji-zhuang.lua` + 16 GB2312 sub-form)
> - C++: `KNpc.cpp::CastMeleeSkill` switch (line 1829-1891) — **không có** Nga My skill nào thuộc nhánh `Melee_Jump/JumpAndAttack/RunAndAttack`. Toàn bộ 16 skill Nga My là `IsMelee=0` (ranged magic missile hoặc ranged surround state). `ChildSkillNum=1` cho mọi skill attack; `MslsGenerate=1` chỉ riêng 82.
> - **Tóm tắt**: Nga My là môn phái **ranged thuần** (băng pháp + heal/buff quần), không có dash, không có melee, không có event chain trong range 77-94. Gap nặng nhất: **G7 (registry radius sai toàn bộ — 5/5 entries off)** + **G4 (ID 84 Phong Vũ Phiêu Hương thiếu childSkillId=210, 92 thiếu allres_p state matching)** + **missing skill 94** (Từ Hàng Phổ Độ 11 — heal sub-form passive).

### 2.6.1. Catalog scan

| ID | Tên (ModSkills.txt) | Loại PC | Vai trò |
|---:|---|---|---|
| 77 | Nga My Kiếm Pháp (峨嵋剑法) | Passive mastery | Buff `addphysicsdamage_p` + `deadlystrikeenhance_p` |
| 78 | Tiếp Dẫn Kiếm Pháp | Passive mastery (placeholder) | Mobile **không catalog** — ModSkills.txt rỗng child, đã bị loại khỏi scope theo task |
| 79 | Nga My Chưởng Pháp (峨嵋掌法) | Passive mastery | Buff `addcoldmagic_v` (cold dmg) |
| **80** | **Phiêu Tuyết Xuyên Vân** (飘云穿雪) | Ranged single | Active — 1 missile, cold + physics, có `addskilldamage1→91, addskilldamage2→380, addskilldamage3→1062` |
| 81 | Thu Phong Diệp (秋风扫叶) | Ranged surround | Self-buff — `staminamax_v` cho team |
| **82** | **Tứ Tượng Đồng Quy** (四相同归) | Ranged single | Active — 1 missile, cold dmg lớn, có `addskilldamage1→331` (lvl-gated L10-19: 76), `addskilldamage2→1062` |
| 83 | Vọng Nguyệt (推窗望月) | Ranged surround | Self-buff — `lifereplenish_v` cho team (L20: 49, L20 secondAdd: 40) |
| **84** | **Phong Vũ Phiêu Hương** (风雨飘香) | Ranged surround | Anti-missile debuff — `slowmissle_b` (PC `slowmissle_b={{1,1},{20,75}}`) |
| **85** | **Nhất Diệp Tri Thu** (一叶知秋) | Ranged single | Active — 1 missile, physics+cold, `addskilldamage1→328 (L20: 60)`, `addskilldamage2→88 (L20: 35)`, `addskilldamage3→1061 (L20: 50)`, `addskilldamage4→1091 (L20: 50)` |
| 86 | Lưu Thủy (流水) | Ranged surround | Self-buff — `fastwalkrun_p` (L20: 66) |
| 87 | Băng Tâm Quyết (冰心诀) | Passive mastery | Buff `coldres_p` |
| **88** | **Bất Diệt Bất Tuyệt** (不灭不绝) | Ranged single | Active — 1 missile, physics+cold, `addskilldamage1→328 (L20: 70)`, `addskilldamage2→1061 (L20: 60)` |
| 89 | Mộng Điệp (梦蝶) | Ranged surround | Self-buff — `lifereplenish_v (L20: 15)` + `manareplenish_v (L20: 10)` cho team |
| **90** | **Mê Tung Ảo Ảnh** (迷踪幻影) | Ranged surround | Debuff — `freezetimereduce_p` + `stuntimereduce_p` (mobile dùng `BadStatusTimeReduceV` — sai field, cần check) |
| **91** | **Phật Quang Phổ Chiếu** (佛光普照) | Ranged single (AOE) | Active — cold AOE lớn (L20: 787/1287), `addskilldamage2→380 (L20: 80)`, `addskilldamage3→1062 (L20: 65)`. PC có `skill_startevent` chain (chỉ L1-9 0, L10-19 1) |
| 92 | Phật Tâm Từ Hữu (佛心慈佑) | Ranged surround | Self-buff — `lifemax_p (PC L20: 125)` + `lifemax_yan_p` cho team |
| 93 | Từ Hàng Phổ Độ (慈航普渡) | Ranged surround (heal) | Active heal — `lifereplenish_v (L20: 750)` cho team, cost 100 |
| 94 | Từ Hàng Phổ Độ 11 (慈航普渡 - sub-form) | Ranged surround (heal) | Mobile **không catalog**. PC passive sub-skill của 93 (SkillStyle=0, IsMelee=0, child=5) — chỉ là visual duplicate của 93, không cần port riêng |

**Không catalog trong mobile** (PC `emei.lua` có, mobile thiếu — Phase 5): `sane_jixue` (三峨霁雪 — ID 1086, có `skill_startevent→329` + `addskilldamage1→1061`), `fengshuang_suiying` (风霜碎影 — ID 1087, cold AOE 770/1000, có `skill_startevent→331` + `addskilldamage1→1062`), `qianfo_qianye` (千佛千叶 — ID 1088, có `skill_startevent→380`), `jianemei150` + `jianemei150_2` (剑峨眉150, ID 1089-1090, `skill_startevent→1089`), `zhangemei150` + `zhangemei150_2` (掌峨眉150, ID 1091-1092), `emei120` (峨嵋120, passive `appendskill→86`), `jinding_foguang` (金顶佛光 — ID ~1093, cold AOE 585, `skill_misslenum_v` 1→1→3→3 — multi-missile ở L20+), `yuquan_xichen` (玉泉洗尘 — passive). Ngoài scope task này (range 77-94).

### 2.6.2. Gap table

> **Chú thích cột "Hành vi mobile"**: lấy từ `CreateEMeiSkills` trong `PcCombatCatalogFactory.cs` line 1232-1459. Mọi skill Nga My đều dùng `PcSkillStyle.Missiles` (80/82/85/88/90/91/93) hoặc implicit surround self-buff (81/83/84/86/89/92) + `childSkillId` tham chiếu missile 68/101/2/3/20/4/204/205/206/207/208/5. Không có nhánh Melee+dash (đúng PC).
>
> **Chú thích cột "Hành vi PC"**: lấy từ `ModSkills.txt` (AttackRadius, ChildSkillId, ChildSkillNum, MslsGenerate, CharAnimId, IsMelee, WaitTime, CostValue, TimePerCast) + `emei.lua` (damage curves, addskilldamage1-4 chain, skill_attackradius) + per-skill file `emei/*.lua` (Get* function override).

| ID | Tên | Hành vi mobile | Hành vi PC | Gap | Severity | Effort |
|---:|---|---|---|---|---|---|
| 77 | Nga My Kiếm Pháp | `physicsenhance_p (L20: 215) + deadlystrike (L20: 36)` (line 1257-1258) | PC `emei_jianfa`: `addphysicsdamage_p={{1,15},{20,215}}` + `deadlystrikeenhance_p={{1,6},{20,36}}` | OK (khớp PC) | — | — |
| 78 | Tiếp Dẫn Kiếm Pháp | (không catalog) | PC rỗng child, chỉ placeholder passive | OK (đã loại khỏi scope theo task) | — | — |
| 79 | Nga My Chưởng Pháp | `addcoldmagic_v (L20: 515)` (line 1269) | PC `emei_zhangfa`: `addcoldmagic_v={{1,15},{20,515}}` | OK | — | — |
| **80** | Phiêu Tuyết Xuyên Vân | `radius=384, childSkillId=68, childSkillNum=1, baseSkill=true, charAnimId=2` (line 1277). Registry: `[80]=(1,240),(20,240)` (PcSkillTuningRegistry line 87) | PC `piaoyun_chuanxue`: `skill_attackradius={{1,320},{20,384}}` (catalog=384 OK), `missle_speed_v={{1,20},{20,24}}`, `skill_cost_v={{1,10},{20,10}}` (mobile: 10/10 ✓). `addskilldamage1→91` + `addskilldamage2→380` + `addskilldamage3→1062` — **mobile KHÔNG có addskilldamage1-3 chain** | **G7 — registry sai (240 vs PC 320-384) — runtime sẽ dùng 240 thay vì 320-384** + **G6 — thiếu toàn bộ addskilldamage1-3 chain (91/380/1062)** | Cao (radius runtime quá nhỏ 240/384 = 62%; mất sub-missile chain 3 effect) | 1 giờ (G7 fix dễ) + nửa ngày (G6 cần thêm 3 sub-missile) |
| 81 | Thu Phong Diệp | `radius=400, childSkillId=204, childSkillNum=1, targetSelf=true, targetAlly=true, StaminaMaxP (L20: 100), duration=1200+1200*lv` (line 1293-1301) | PC `qiufeing-saoye`: `staminareplenish_v (per-skill file)` — ModSkills.txt cũng list `staminareplenish_v`. `state_special` team-buff AOE | **G7 — sai attribute kind**: mobile dùng `StaminaMaxP` (buff max stamina), PC dùng `staminareplenish_v` (replenish rate). 2 concept khác nhau. AOE radius 400 PC không rõ (default state 180) | Trung bình (gameplay feel sai) | 1 giờ (đổi `StaminaMaxP` → `StaminaReplenishV` + verify AOE) |
| **82** | Tứ Tượng Đồng Quy | `radius=416, childSkillId=101, childSkillNum=1, baseSkill=true, charAnimId=2, Param1=20` (line 1306-1315). Registry: `[82]=(1,570),(20,570)` | PC `sixiang_tonggui`: `skill_attackradius={{1,384},{20,416}}` (catalog=416 OK), `colddamage_v {{1,35},{20,315}} + {{1,45},{20,450}}` (mobile: 315/450 ✓), `seriesdamage_p {{1,5},{20,30}}` (✓), `skill_cost_v {{1,25},{20,35}}` (✓). `addskilldamage1→331 (L10-19: 76)`, `addskilldamage2→1062 (L20: 20)` — mobile **KHÔNG có chain** | **G7 — registry sai (570 vs PC 384-416, 570/416 = 137% quá rộng)** + **G6 — thiếu addskilldamage1-2 chain (331/1062)** | Cao (radius quá rộng = AOE sai 37% + mất sub-missile chain) | 1 giờ (G7) + nửa ngày (G6) |
| 83 | Vọng Nguyệt | `radius=400, childSkillId=205, childSkillNum=1, targetSelf/Ally, ManaReplenishV (L20: 20), duration=1200+1200*lv` (line 1320-1328) | PC `tuichuang_wangyue`: `physicsenhance_p {{1,40},{20,175}}` + `colddamage_v {{1,10},{20,120}}` + `deadlystrike_p {{1,10},{20,30}}` + `addskilldamage1→329 (L20: 49)` + `addskilldamage2→1091 (L20: 40)` | **G7 — sai schema**: mobile dùng `ManaReplenishV`, PC dùng `physicsenhance_p + colddamage_v + deadlystrike_p` (team buff combat stats, không phải mana regen) + **G6 — thiếu addskilldamage1-2 chain (329/1091)** | Trung bình (sai công thức buff) | 1 giờ (G7) + 2 giờ (G6) |
| **84** | Phong Vũ Phiêu Hương | `radius=400, childSkillId=0, childSkillNum=1, baseSkill=true, charAnimId=2, targetSelf/Ally, AddDefenseV (L20: 300), duration=1200+1200*lv` (line 1333-1341) | PC `qingyin-fanchang`/`fengyu-piaoxiang`: `slowmissle_b={{1,1},{20,75}}` — anti-missile debuff. ModSkills.txt confirm LvlSetting = `slowmissle_b` (chỉ 1 field, không có addskilldamage). `skill_attackradius` không có (default state AOE) | **G7 — sai schema nghiêm trọng**: mobile dùng `AddDefenseV` (defense buff), PC dùng `slowmissle_b` (anti-missile). 2 cơ chế khác hẳn + **childSkillId=0 sai**: skill này cần child missile visual để tạo AOE surround, nhưng PC ModSkills.txt confirm CharAnimId=11 + state_special team-debuff (không missile). | Trung bình (debuff bị thay bằng buff — gameplay đảo ngược) | 2 giờ (đổi field + cập nhật `ConfigureEMeiVisuals`) |
| **85** | Nhất Diệp Tri Thu | `radius=384, childSkillId=2, childSkillNum=1, baseSkill=true, charAnimId=2, Param1=2` (line 1346-1357). Registry: `[85]=(1,180),(20,180)` | PC `yiye_zhiqiu`: `skill_attackradius={{1,320},{20,384}}` (catalog=384 ✓), `colddamage_v {{1,10},{20,80}}` (mobile: 80/80 ✓), `physicsenhance_p {{1,30},{20,75}}` (✓), `deadlystrike_p {{1,10},{20,20}}` (✓), `seriesdamage_p {{1,1},{20,10}}` (✓), `skill_cost_v {{1,25},{20,25}}` (✓). `addskilldamage1→328 (L20: 60)`, `addskilldamage2→88 (L20: 35)`, `addskilldamage3→1061 (L20: 50)`, `addskilldamage4→1091 (L20: 50)` — **mobile KHÔNG có chain** | **G7 — registry sai (180 vs PC 320-384, 180/384 = 47% quá nhỏ)** + **G6 — thiếu 4 addskilldamage chain (328/88/1061/1091)** | Cao (radius runtime = 47% PC, AOE miss; mất toàn bộ chain 4 effect) | 1 giờ (G7) + 1 ngày (G6 — 4 sub-missile) |
| 86 | Lưu Thủy | `radius=400, childSkillId=206, childSkillNum=1, targetSelf/Ally, AttackSpeedV (L20: 66), duration=1200+1200*lv` (line 1362-1370) | PC `liushui`: `fastwalkrun_p={{1,9},{20,18→66}}` (per-skill file) | **G7 — sai attribute kind**: mobile dùng `AttackSpeedV` (cast/atk speed), PC dùng `fastwalkrun_p` (move speed). 2 cơ chế khác | Trung bình (sai công thức) | 1 giờ (đổi `AttackSpeedV` → `FastWalkRunP`) |
| 87 | Băng Tâm Quyết | `PassiveResist(87, "Băng Tâm Quyết", 30, MagicAttributeKind.ColdResP)` (line 1375) | PC `bingxin-jue`: `coldres_p` (chỉ 1 field) | OK (khớp PC) | — | — |
| **88** | Bất Diệt Bất Tuyệt | `radius=512, childSkillId=3, childSkillNum=1, baseSkill=true, charAnimId=10, Param1=1` (line 1380-1391). Registry: `[88]=(1,360),(20,360)` | PC `bumie_bujue`: `skill_attackradius={{1,448},{20,512}}` (catalog=512 ✓), `colddamage_v {{1,10},{20,282}}` (mobile: 282/282 ✓), `physicsenhance_p {{1,80},{20,385}}` (✓), `deadlystrike_p {{1,15},{20,54}}` (✓), `seriesdamage_p {{1,10},{20,50}}` (✓), `missle_speed_v {{1,28},{20,32}}` (mobile: 32/32 ✓), `skill_cost_v {{1,30},{20,35}}` (✓). `addskilldamage1→328 (L20: 70)`, `addskilldamage2→1061 (L20: 60)` — **mobile KHÔNG có chain** | **G7 — registry sai (360 vs PC 448-512, 360/512 = 70% hẹp)** + **G6 — thiếu addskilldamage1-2 chain (328/1061)** | Cao (radius runtime = 70% PC; mất 2 sub-missile) | 1 giờ (G7) + nửa ngày (G6) |
| 89 | Mộng Điệp | `radius=400, childSkillId=207, childSkillNum=1, targetSelf/Ally, ManaReplenishV (L20: 10) + AddDefenseV (L20: 150), duration=1200+1200*lv` (line 1396-1405) | PC `mengdie`: `lifereplenish_v {{1,1},{20,15}}` + `manareplenish_v {{1,1},{20,10}}` (per-skill file) | **G7 — sai 1 attribute kind**: mobile dùng `AddDefenseV` (defense buff), PC dùng `lifereplenish_v` (HP regen). ManaReplenishV đúng. **Mất hoàn toàn heal HP** | Trung bình (mất 50% công thức team heal) | 1 giờ (đổi `AddDefenseV` → `LifeReplenishV`) |
| **90** | Mê Tung Ảo Ảnh | `radius=440, childSkillId=20, childSkillNum=1, baseSkill=true, charAnimId=2, BadStatusTimeReduceV (L20: 30), duration=1200+1200*lv` (line 1410-1418). Registry: **không có** | PC `qingyin-fanchang` (`emei.lua` line 175-182): `fasthitrecover_v {{1,1},{20,20}}` + `fatallystrikeres_p {{1,1},{20,20}}` + `freezetimereduce_p {{1,1},{20,30}}` + `poisontimereduce_p {{1,1},{20,30}}` + `stuntimereduce_p {{1,1},{20,30}}` (per-skill file). ModSkills.txt confirm `LvlSetScript=\script\skill\kunlun.lua` (cross-faction script — bug EMei dùng KunLun.lua?) | **G7 — sai schema 4-field → 1-field**: mobile chỉ có `BadStatusTimeReduceV` (1 chỉ số tổng hợp), PC có 5 field riêng biệt (`fasthitrecover_v` + `fatallystrikeres_p` + 3 timer reduce). Mất 80% cảm giác Nga My "giảm debuff". **G5 — cross-faction script** | Thấp-trung bình (gameplay feel yếu — Nga My không có khả năng tăng deadly resist/fasthitrecover) | 2 giờ (refactor 4 attribute, G5 cần check Phase 5) |
| **91** | Phật Quang Phổ Chiếu | `radius=400, childSkillId=4, childSkillNum=1, baseSkill=true, charAnimId=2` (line 1423-1432). Registry: `[91]=(1,400),(20,400)` (khớp catalog) | PC `foguang_puzhao`: `colddamage_v {{1,70},{20,787}} + {{1,80},{20,1287}}` (mobile: 787/1287 ✓), `seriesdamage_p {{1,10},{20,50}}` (✓), `skill_cost_v {{1,30},{20,60}}` (✓). `addskilldamage2→380 (L20: 80)`, `addskilldamage3→1062 (L20: 65)` — **mobile KHÔNG có chain** | **G6 — thiếu addskilldamage2-3 chain (380/1062)** | Trung bình (mất 2 sub-missile cho skill AOE cao cấp) | nửa ngày (G6) |
| 92 | Phật Tâm Từ Hữu | `radius=400, childSkillId=208, childSkillNum=1, targetSelf/Ally, AllResP (L20: 30), duration=1440` (line 1437-1445) | PC `foxin_ciyou`: `lifemax_p {{1,30},{20,125}}` + `lifemax_yan_p {{1,30},{25,100}}`. **KHÔNG có `allres_p`** | **G7 — sai schema**: mobile dùng `AllResP` (all resistance), PC dùng `lifemax_p + lifemax_yan_p` (HP max + HP regen ở L25+). Khác concept hoàn toàn | Trung bình (defense buff thay bằng HP buff — Nga My thiếu khả năng tăng HP max) | 1 giờ (đổi `AllResP` → `LifeMaxP` + thêm `LifeMaxYanP`) |
| 93 | Từ Hàng Phổ Độ | `radius=400, childSkillId=5, childSkillNum=1, baseSkill=true, charAnimId=2, ManaReplenishV immediate (L20: 750), cost=100` (line 1450-1458). Registry: **không có** | PC `cihang_pudu`: `lifereplenish_v {{1,275},{20,750}}` (per-skill file `cihang_pudu.lua` Getlifereplenish_v: `250+25*level`) + `skill_cost_v {{1,100},{20,100}}` (✓). **KHÔNG có `manareplenish_v`** | **G7 — sai attribute kind**: mobile dùng `ManaReplenishV` (mana regen), PC dùng `LifeReplenishV` (HP regen). Sai concept — đây là **skill heal HP mạnh nhất** Nga My | **Cao** (skill chữa trị = heal HP, không phải mana) | 30 phút (đổi `ManaReplenishV` → `LifeReplenishV` trong `d.immediate`) |
| 94 | Từ Hàng Phổ Độ 11 | (không catalog) | PC passive sub-form của 93, `IsMelee=0, child=5, ReqLevel=0, MaxLevel=20`. CharAnimId=11, EffectSkillLevel=5 | OK (không cần port — duplicate visual 93, chỉ là passive no-cost) | — | — |
| **All 8 attack** | `addskilldamage1-4 chain` | Mobile: 0/8 attack có chain | PC: tất cả 6 active ranged single (80/82/85/88/91) + 1 attack AOE (91) đều có addskilldamage chain đến sub-missile 91/328/331/380/1061/1062/1089/1091/88/329 | **G6 — event chain 0/8 fire** | Cao (mất toàn bộ sub-missile damage chain — tổng damage mỗi skill giảm 25-50% tùy theo level) | 2-3 ngày (cần thêm 8 sub-missile definition + chain handler — same pattern as 163 Nhân Kiếm Hợp Nhất ở Võ Đang) |
| **All 8 surround self-buff** | `BadStatusTimeReduceV / AddDefenseV / AttackSpeedV / StaminaMaxP / ManaReplenishV` (5 loại khác nhau) | PC: chỉ có 1 skill 84 dùng `slowmissle_b`, các skill khác dùng `fasthitrecover_v / fatallystrikeres_p / freezetimereduce_p / poisontimereduce_p / stuntimereduce_p / lifemax_p / lifemax_yan_p / lifereplenish_v / manareplenish_v` (7 loại khác nhau) | **G7 — schema drift 5/7 surround sai** (xem chi tiết từng skill ở trên) | Trung bình-cao | 1 ngày (refactor 6 attributes) |
| **80/82/85/88/91** | `Registry radius: 240/570/180/360/400` (PcSkillTuningRegistry line 87-91) | PC: 320-384 / 384-416 / 320-384 / 448-512 / n/a (default 0) | **G7 — registry 5/5 sai so với catalog** (catalog đúng PC, registry sai) | **Cao** (runtime dùng registry, CombatRuntimeService.ApplySkillCast sẽ dùng 240/570/180/360/400 thay vì PC value, làm AOE sai 38-62%) | 30 phút (sửa 5 dòng registry cho match catalog) |
| **Tất cả 13 active** | `PcSkillTuningRegistry.EMeiId` cover 5/13 active (80/82/85/88/91) | PC: 8 attack cần tuning (80/82/85/88/90/91/93/94). Mobile thiếu 90/93/94 | **G7 — Tuning coverage 38%** (5/13, 8 attack cần 5+3) | Thấp (chỉ 8 active, thiếu 3) | 15 phút (thêm 3 entries vào registry nếu cần runtime radius) |
| **80, 82, 85, 88, 91** | `ConfigureEMeiVisuals` (kiểm tra trong PcSkillEffectVisualService) — cần verify | PC: 80/82/85/88 dùng chung `mag_em_13_施魔法.spr` (per-skill file check), 91 có sound `佛光普照.wav` riêng | **G7 visual coverage** — chưa verify visual case cho từng ID. Nếu `ConfigureEMeiVisuals` chỉ cover vài ID thì mất SPR pre-cast | Thấp-trung bình | 1 giờ verify + sửa |

### 2.6.3. Phase 1 quick wins (registry, schema, missing chain)

> **Đây là phase quan trọng nhất** cho Nga My. 4 bug nhanh:
> 1. **Registry radius sai 5/5** — runtime sẽ dùng radius sai nếu không sửa
> 2. **Skill 93 sai ManaReplenishV → LifeReplenishV** — đây là skill heal HP mạnh nhất
> 3. **Schema drift 5 surround (81/83/86/89/92)** — sai attribute kind → gameplay khác PC
> 4. **Skill 90 cross-faction script** — Nga My dùng KunLun.lua (5-field vs 1-field)

- [ ] **Registry radius 5 entries sai** (priority cao nhất, 30 phút): sửa `PcSkillTuningRegistry.cs` line 87-91:
  - `[80]=(1,240),(20,240)` → `[80]=(1,320),(20,384)` (theo PC `piaoyun_chuanxue`)
  - `[82]=(1,570),(20,570)` → `[82]=(1,384),(20,416)` (theo PC `sixiang_tonggui`)
  - `[85]=(1,180),(20,180)` → `[85]=(1,320),(20,384)` (theo PC `yiye_zhiqiu`)
  - `[88]=(1,360),(20,360)` → `[88]=(1,448),(20,512)` (theo PC `bumie_bujue`)
  - `[91]=(1,400),(20,400)` → `[91]=(1,400),(20,400)` (giữ — PC không có anchor, catalog 400 OK)
  - **Verification**: chạy test trên 4 map, cast 80/82/85/88 ở L20, đo AOE radius runtime == catalog. (G7)
- [ ] **ID 93 Từ Hàng Phổ Độ** (priority cao, 30 phút): sửa `EMeiTuHangPhuDo()` line 1453 — đổi `d.immediate.Add(new SkillMagicAttribute(MagicAttributeKind.ManaReplenishV, ...))` → `LifeReplenishV` (per-skill `cihang_pudu.lua` line `result = 250+25*level` = `{{1,275},{20,750}}` khớp). Cũng kiểm tra `ManaReplenishV` ở 83/89/92 — có thể cùng bug. (G7)
- [ ] **ID 84 Phong Vũ Phiêu Hương** (priority cao, 2 giờ): sửa `EMeiPhongVuPhieuHuong()` line 1333-1341:
  - Đổi `AddDefenseV` → `SlowMissleB` (theo PC `qingyin-fanchang` line 175-182: `slowmissle_b={{1,1},{20,75}}`)
  - Set `targetEnemy=true` (thay vì self/ally) — đây là **debuff lên đối phương**, không phải buff mình
  - Set `radius=0` (PC không có `skill_attackradius` — single target debuff, không phải surround)
  - Cập nhật `ConfigureEMeiVisuals` để handle case 84 (PC dùng `mag_em_13_施魔法.spr` chung). (G7)
- [ ] **ID 81 Thu Phong Diệp** (priority trung bình, 1 giờ): đổi `StaminaMaxP` → `StaminaReplenishV` (per-skill `qiufeing-saoye.lua`). Đây là team buff **regen rate**, không phải max. (G7)
- [ ] **ID 83 Vọng Nguyệt** (priority trung bình, 1 giờ): refactor `EMeiVongNguyet()` line 1320-1328:
  - Đổi `ManaReplenishV` → `physicsenhance_p {{1,40},{20,175}} + colddamage_v {{1,10},{20,120}} + deadlystrike_p {{1,10},{20,30}}` (3 attribute theo PC `tuichuang_wangyue` line 149-167)
  - Cập nhật child chain nếu cần (PC có addskilldamage1→329, addskilldamage2→1091 — Phase G6)
- [ ] **ID 86 Lưu Thủy** (priority trung bình, 30 phút): đổi `AttackSpeedV` → `FastWalkRunP` (per-skill `liushui.lua` line `result = 9+3*level` cho level 1-20 = 12-66, mobile L20=66 khớp). (G7)
- [ ] **ID 89 Mộng Điệp** (priority trung bình, 30 phút): thêm `LifeReplenishV {{1,1},{20,15}}` (per-skill `mengdie.lua`) song song với `ManaReplenishV` hiện tại. **Đây là bug — mất heal HP 50% công thức**. (G7)
- [ ] **ID 90 Mê Tung Ảo Ảnh** (priority thấp, 2 giờ): refactor 4 attribute. PC `qingyin-fanchang.lua` cung cấp:
  - `fasthitrecover_v {{1,1},{20,20}}`
  - `fatallystrikeres_p {{1,1},{20,20}}`
  - `freezetimereduce_p {{1,1},{20,30}}`
  - `poisontimereduce_p {{1,1},{20,30}}`
  - `stuntimereduce_p {{1,1},{20,30}}`
  - Hiện mobile chỉ có 1 `BadStatusTimeReduceV` (L20: 30) — tổng hợp tất cả 3 timer. Cần tách thành 3 MagicAttribute riêng.
  - **G5 cross-faction**: ModSkills.txt LvlSetScript=`kunlun.lua` — đây là bug EMei 90 dùng KunLun.lua. Cần check xem có cần đổi sang `emei.lua` không. Phase 5. (G7 + G5)
- [ ] **ID 92 Phật Tâm Từ Hữu** (priority trung bình, 1 giờ): đổi `AllResP` → `LifeMaxP {{1,30},{20,125}}` (PC `foxin_ciyou` line 122-123), thêm `LifeMaxYanP {{1,30},{25,100}}` cho L25+. (G7)
- [ ] **G6 — addskilldamage chain 0/8 fire** (priority cao, 2-3 ngày): thêm sub-missile definition cho 91/328/331/380/1061/1062/1089/1091/88/329. Pattern giống Võ Đang 163 (StartEvent + CollideEvent):
  - 80 → 91 + 380 + 1062
  - 82 → 331 + 1062
  - 85 → 328 + 88 + 1061 + 1091
  - 88 → 328 + 1061
  - 91 → 380 + 1062
  - Effort: mỗi sub-missile 1-2 giờ. 8 sub = 2-3 ngày. (G6)

### 2.6.4. Phase 3 dash (G1)

- [ ] **Không áp dụng** — Nga My không có skill dash/melee/jump. Cả `KNpc.cpp::CastMeleeSkill` switch (line 1829-1891), `ModSkills.txt` (toàn bộ `IsMelee=0` cho ID 77-94), và `emei.lua` đều không tham chiếu `Melee_Jump/JumpAndAttack/RunAndAttack` cho Nga My. 16 skill đều là ranged magic missile (5 skill — 80/82/85/88/91) hoặc ranged surround self-buff (8 skill — 81/83/84/86/89/90/92/93) hoặc passive (3 skill — 77/79/87).

### 2.6.5. Phase 4 event chain (G6)

> Toàn bộ Nga My 77-94 **đều thiếu addskilldamage1-4 chain** — đây là G6 tổng quát. Có 8 attack skill với 4-6 sub-missile mỗi skill. Pattern giống Võ Đang 163 Nhân Kiếm Hợp Nhất (xem section 2.1.5 để biết chi tiết event chain integration).

- [ ] **ID 80 — addskilldamage1→91 (L20: 35) + addskilldamage2→380 (L20: 75) + addskilldamage3→1062 (L20: 63)**: thêm 3 sub-missile definition. PC `addskilldamageN` dùng level-gated damage, không phải StartEvent. Phase 6 effort: nửa ngày
- [ ] **ID 82 — addskilldamage1→331 (L10-19: 76) + addskilldamage2→1062 (L20: 20)**: thêm 2 sub-missile. Effort: 2 giờ
- [ ] **ID 85 — addskilldamage1→328 (L20: 60) + addskilldamage2→88 (L20: 35) + addskilldamage3→1061 (L20: 50) + addskilldamage4→1091 (L20: 50)**: thêm 4 sub-missile. Effort: 1 ngày
- [ ] **ID 88 — addskilldamage1→328 (L20: 70) + addskilldamage2→1061 (L20: 60)**: thêm 2 sub-missile. Effort: 3 giờ
- [ ] **ID 91 — addskilldamage2→380 (L20: 80) + addskilldamage3→1062 (L20: 65)**: thêm 2 sub-missile. Effort: 3 giờ
- [ ] **ID 80/82/85/88/91 visual** (G7 visual): thêm 5 cases vào `ConfigureEMeiVisuals` cho addskilldamage pre-cast/missile visual. PC dùng chung `mag_em_13_施魔法.spr` preCast cho 80/82/85/88, riêng 91 có `佛光普照.wav` sound + `mag_em_13` riêng. Effort: 2 giờ

### 2.6.6. Trạng thái

- [x] Catalog scan xong (16 skill: 3 passive, 5 ranged single attack, 8 surround self-buff)
- [ ] **Quick-win phase merged** (Phase 1 — 10 items, priority: registry radius 5/5 + 93 heal + 84 anti-missile + schema drift 5 surround)
- [ ] Dash phase merged: **không áp dụng** (Nga My 100% ranged magic, không có dash)
- [ ] Event chain phase merged (Phase 4 — 6 items, 8 attack × 2-4 sub-missile = ~15 sub-missile definitions, 2-3 ngày effort)
- [ ] Tuning coverage 38% → 100% (cần thêm 3 entries vào `PcSkillTuningRegistry.EMeiId` cho 90/93/94 nếu runtime cần radius — 94 là passive nên không cần)

### 2.6.7. Tổng kết

- **Skill chính ưu tiên sửa**:
  1. **Registry radius 5 entries sai** (PcSkillTuningRegistry 80/82/85/88/91) — 30 phút, runtime AOE fix ngay
  2. **ID 93 Từ Hàng Phổ Độ** — sai `ManaReplenishV` → `LifeReplenishV` — 30 phút, restore heal HP
  3. **ID 84 Phong Vũ Phiêu Hương** — sai `AddDefenseV` (defense buff) → `SlowMissleB` (anti-missile debuff) — 2 giờ, restore debuff mechanic đúng
  4. **Schema drift 5 surround** (81/83/86/89/92) — sai `MagicAttributeKind` — 1 ngày, restore team buff combat stats
- **Skill ưu tiên #2**: **G6 addskilldamage chain** (8 attack × 2-4 sub = 15+ sub-missile) — 2-3 ngày, restore 25-50% damage mỗi skill
- **Skill passive**: 77/79/87 OK, không cần sửa
- **Skill ranged single attack**: 80/82/85/88/91 — registry radius sai + thiếu chain. Fix theo thứ tự ưu tiên
- **Skill surround self-buff**: 81/83/84/86/89/90/92/93 — schema drift 5/8. Fix theo thứ tự ưu tiên
- **Test plan**: 
  - Unit test cho `MagicAttributeKind` resolve (81→StaminaReplenishV, 84→SlowMissleB, 86→FastWalkRunP, 89→LifeReplenishV, 92→LifeMaxP, 93→LifeReplenishV)
  - Visual test cast 93 ở L20, verify heal HP = 750 (không phải mana 750)
  - Visual test cast 84 ở gần enemy, verify enemy bị slowmissle debuff (không phải mình được defense buff)
  - Runtime test đo AOE radius 80/82/85/88 ở L20, verify == 384/416/384/512 (sau khi sửa registry)
  - Regression test cast 80 ở L20, verify fire 1 main missile + 91 + 380 + 1062 (sau khi thêm chain)
- **Không cần làm dash/event chain StartEvent/FlyEvent** cho range 77-94 (Nga My 100% ranged, không có Melee_Jump, không có event chain). Addskilldamage chain là G6 riêng.
- **Phase 5 (future)**: port 150-tier Nga My sub-form:
  - `sane_jixue` (三峨霁雪, ID 1086) — `skill_startevent→329` + `addskilldamage1→1061`
  - `fengshuang_suiying` (风霜碎影, ID 1087) — cold AOE 770/1000, `skill_startevent→331` + `addskilldamage1→1062`
  - `qianfo_qianye` (千佛千叶, ID 1088) — `skill_startevent→380`
  - `jianemei150` + `jianemei150_2` (剑峨眉150, ID 1089-1090) — `skill_startevent→1089`
  - `zhangemei150` + `zhangemei150_2` (掌峨眉150, ID 1091-1092) — multi-missile `skill_misslenum_v {{1,1},{10,1},{20,3}}`
  - `jinding_foguang` (金顶佛光, ID ~1093) — multi-missile 1→1→3→3
  - `yuquan_xichen` (玉泉洗尘, passive) + `emei120` (峨嵋120, passive `appendskill→86`)

Tất cả 6 active 150-tier đều có `skill_startevent` chain thật (khác với range 77-94 chỉ dùng addskilldamage). Đây là event chain phức tạp nhất VLTK Mobile, effort 1-2 tuần.

## 2.5. Ngũ Độc (PC: WuDu, ID range 60-76)

> **Nguồn PC**:
> - `/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser_bachkim_6.0/server1/script/skill/wudu.lua` (GB2312, 613 dòng, 25+ skills)
> - `/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser_bachkim_6.0/server1/script/skill/wudu/*.lua` (26 file per-skill: 9 pinyin + 17 Chinese GBK)
> - `Assets/StreamingAssets/Reference/ModSkills.txt` (canonical TCVN3, ID 60-76 trừ 61)
> - `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs` line 1704-1932 (`CreateWuDuSkills` 16 skill)
>
> C++: `KNpc.cpp::CastMeleeSkill` switch (line 1829-1891) — **không có** WuDu skill nào thuộc nhánh `Melee_Jump/JumpAndAttack/RunAndAttack`. Toàn bộ WuDu là poison ranged.
> Tóm tắt: Ngũ Độc = **poison ranged thuần**, không có dash, không có event chain trong range 60-76. Gap nặng nhất: **G5 (ID 73 đổi nhầm attribute class)**, **G7 magnitude sai lớn** (poison damage 3-14× off do schema 3-element bị nén sai, passive mastery sai 4-11×), **G4 radius lệch** (ID 63 sai 53%), **G7 tuning coverage 0%** (không có `PcSkillTuningRegistry.RadiusCurves[WuDuId]` và `ConfigureWuDuVisuals`).

### 2.5.1. Catalog scan

| ID | Tên | Loại PC | Vai trò |
|---:|---|---|---|
| 60 | 五毒刀法 Ngũ Độc Đao Pháp | Passive mastery | Buff physics dmg + crit |
| 62 | 五毒掌法 Ngũ Độc Chưởng Pháp | Passive mastery | Buff poison magic V |
| 63 | 毒砂掌 Độc Sa Chưởng | Ranged missile độc | Active — base poison missile (ReqLv 10) |
| 64 | 冰蓝玄晶 Băng Lam Huyền Tinh | Ranged AOE state | Active — debuff self cold res (ReqLv 10) |
| 65 | 血刀毒杀 Huyết Đao Độc Sát | Ranged missile độc | Active — phys + poison (ReqLv 10) |
| 66 | 杂难药经 Tạp Nan Dược Kinh | Passive mastery | Buff poison res P (ReqLv 20) |
| 67 | 九霄狂雷 Cửu Thiên Cuồng Lôi | Ranged AOE state | Active — debuff self lighting res (ReqLv 20) |
| 68 | 幽冥骷髅 U Minh Khô Lâu | Ranged missile độc | Active — poison + series (ReqLv 30) |
| 69 | 无形蛊 Vô Hình Độc | Ranged surround độc | Active — poison + movement speed (ReqLv 30) |
| 70 | 赤焰蚀天 Chích Dương Thệ Thiên | Ranged AOE state | Active — debuff self fire res (ReqLv 30) |
| 71 | 天罡地煞 Thiên Cương Địa Sát | Ranged surround độc | Active — big poison + series (ReqLv 60) |
| 72 | 穿心毒刺 Xuyên Tâm Độc Thích | Ranged AOE state | Active — debuff self poison res (ReqLv 20) |
| 73 | 万毒蚀心 Vạn Độc Thực Tâm | Ranged AOE state | Active — **extend poison duration on target** (ReqLv 20) |
| 74 | 朱蛤清鸣 Chu Cáp Thanh Minh | Ranged missile độc | Active — phys+poison+series (ReqLv 60) |
| 75 | 五毒奇经 Ngũ Độc Kỳ Kinh | Passive mastery | Buff poison magic V + cast speed V (ReqLv 60, MaxLv 30) |
| 76 | 移花接木 Di Hoa Tiếp Ngọc | Self buff | Active — return ranged damage (ReqLv 50) |

**Không catalog trong mobile** (PC `wudu.lua` có, mobile thiếu — ngoài range 60-76): 26 per-skill files nằm ngoài scope, bao gồm sub-form 150-tier `zhangwudu150` / `daowudu150` / `xuanyin_zhan` (1096) / `yinfeng_shigu` (1094) / `baidu_chuanxin` (1095) / `xueshou_dusha` (sister 65) + 120-tier `wudu120` (có `autoattackskill` = 719*256+1 → caster buff). Phase 5.

**ID 61** (PC ModSkills.txt "Ngũ Độc Bổng Pháp ##" — disabled/bỏ). Mobile đã exclude đúng (line 599 `IsWuDuSkill id != 61`).

### 2.5.2. Gap table

> **Cột "Hành vi mobile"**: `CreateWuDuSkills` line 1704-1932.
> **Cột "Hành vi PC"**: `wudu.lua` (curves per level) + per-skill files (`*.lua` subdir) + `ModSkills.txt` (radius/IsMelee/anim) + `wudu.lua::SKILLS.<key>.skill_attackradius` (radius theo level).

| ID | Tên | Hành vi mobile | Hành vi PC | Gap | Severity | Effort |
|---:|---|---|---|---|---|---|
| 63 | Độc Sa Chưởng | `radius=180, child=5, num=1, PoisonDamageV 15/150, SeriesDamageP 1/10, cost=10` | `poisondamage_v={{1,2},{20,40}},{{1,60},{20,60}},{{1,10},{20,10}}` (3-element), `seriesdamage_p={{1,1},{20,10}}`, `skill_attackradius={{1,320},{20,384}}` → L20=**384** | **G4 radius 180 vs PC 384 (-53%)** + **G7 PoisonDamageV schema 3-element nén thành 2-element sai** | Cao | 1 giờ |
| 65 | Huyết Đao Độc Sát | `radius=400 ✓, PhysicsEnhanceP 10/100, PoisonDamageV 15/150, SeriesDamageP 1/10, cost=20` | `physicsenhance_p={{1,15},{20,65}}`, `poisondamage_v={{1,4},{20,11}},{{1,60},{20,60}},{{1,10},{20,10}}`, `skill_attackradius={{1,320},{20,384}}` | **G7 PhysicsEnhanceP 10/100 vs PC 15/65 (1.5× off L20)** + **G7 PoisonDamageV 15/150 vs PC 4/11 (L20 14× off — schema nén sai)** + **G7 mất `physicsdamage_v` raw 30→70 (per-skill `xueshou-dusha.lua`)** | Cao | 2 giờ |
| 68 | U Minh Khô Lâu | `radius=400, PoisonDamageV 30/250, SeriesDamageP 5/30, cost=15` | `poisondamage_v={{1,11},{20,40}},{{1,60},{20,60}},{{1,10},{20,10}}`, `seriesdamage_p={{1,5},{20,30}}`, `skill_attackradius={{1,384},{20,448}}` → L20=**448** | **G4 radius 400 vs PC 448 (-11%)** + **G7 PoisonDamageV 30/250 vs PC 11/40 (L20 6× off — schema nén sai)** | Cao | 2 giờ |
| 69 | Vô Hình Độc | `radius=100, PoisonDamageV 25/220, AttackSpeedV 5/30, cost=15` | `poisondamage_v={{1,5},{20,25}}` + `fastwalkrun_p={{1,-10},{25,-50}}` (movement speed buff). Per-skill `wuxing-gu.lua`: `Getfastwalkrun_p=-5-level, -20, -25`. `skill_attackradius` không set. | **G7 AttackSpeedV 5/30 ≠ PC fastwalkrun_p -10/-50 (MOVEMENT speed ≠ ATTACK speed — đổi nhầm class attribute)** + **G7 PoisonDamageV 25/220 vs PC 5/25 (L20 9× off)** + **G4 radius 100 không có PC anchor** | **Cao** (gameplay cốt lõi "tàng hình lao tới") | 2 giờ |
| 71 | Thiên Cương Địa Sát | `radius=420, PoisonDamageV 50/385, SeriesDamageP 10/50, cost=20→40` | `poisondamage_v={{1,50},{20,135}}` (L20 max 135), `seriesdamage_p={{1,10},{20,50}}` ✓, `skill_attackradius={{1,448},{20,480}}` → L20=**480** | **G4 radius 420 vs PC 480 (-12%)** + **G7 PoisonDamageV 50/385 vs PC 50/135 (L20 ~3× off)** | Cao | 1 giờ |
| 74 | Chu Cáp Thanh Minh | `radius=400, PhysicsEnhanceP 80/385, PoisonDamageV 50/385, SeriesDamageP 10/50, cost=25` | `physicsenhance_p={{1,30},{20,392}}` ✓, `poisondamage_v={{1,16},{20,53}}` (L20=53, mobile 385 = **7× off**), `seriesdamage_p={{1,10},{20,50},{21,52}}` ✓, `skill_attackradius={{1,448},{20,512},{21,512}}` → L20=**512** | **G4 radius 400 vs PC 512 (-22%)** + **G7 PoisonDamageV 50/385 vs PC 16/53 (7× off)** | Cao | 2 giờ |
| 62 | Ngũ Độc Chưởng Pháp (passive) | `AddPoisonDamageV 15/515, baseLv=10` | `addpoisonmagic_v={{1,15},{20,45}}` (L20=45) + per-skill `wudu-zhangfa.lua` `Getaddpoisonmagic_v=5+level*2` (L1=7, L20=45). PC còn `addphysicsdamage_p 13+7*lv` raw. | **G7 AddPoisonDamageV L20=515 vs PC 45 (11× off — magnitude CỰC LỚN)** + **G7 mất `addphysicsdamage_p` raw 13+7*lv (per-skill)** | **Cao** (passive mastery sai 11× → cả đời tăng sai damage) | 1 giờ |
| 75 | Ngũ Độc Kỳ Kinh (passive) | `AddPoisonDamageV 20/200, CastSpeedV 5/30, baseLv=60, maxLv=30` | `addpoisonmagic_v={{1,5},{30,45}}` (L30=45) + `poisonenhance_p={{1,12},{30,50}}` (poison damage % enhance) + `castspeed_v={{1,1},{30,25}}` (L30=25) | **G7 AddPoisonDamageV 20/200 vs PC 5/45 (L30 4.4× off)** + **G7 mất `poisonenhance_p` 12→50 (poison damage %)** + **G7 CastSpeedV 5/30 vs PC 1/25** | Trung bình-cao | 2 giờ |
| 73 | Vạn Độc Thực Tâm | `radius=440, PoisonResP -10/-40 (debuff self poison res), cost=20` | `poisontimereduce_p={{1,-200},{20,-300}},{{1,18*45},{20,18*120}}` — **EXTEND poison duration on target** (PC comment line 132-135: "âm càng nhiều = thời gian dính độc càng lâu"). Per-skill `wangu-shixin.lua` THÊM `poisonres_p` = `-floor(log10(level+1)/2*60)` (L1=-9, L20=-23). | **G5 — ĐỔI NHẦM ATTRIBUTE CLASS: PC `poisontimereduce_p` (kéo dài độc trên target) vs mobile `PoisonResP` (debuff self res). Tên "Vạn Độc Thực Tâm" = target ăn độc lâu hơn, KHÔNG phải debuff res.** + **G7 mất `poisontimereduce_p` HOÀN TOÀN** + **G7 poisonres_p value -10/-40 vs per-skill -9/-23** | **Cao (G5 đổi class gameplay)** | 2 giờ (cần thêm `PoisonTimeReduceP` enum) |
| 60 | Ngũ Độc Đao Pháp (passive) | `AddPhysicsDamageP 15/215, DeadlyStrikeEnhanceP 6/25, baseLv=10` | `addphysicsdamage_p={{1,20},{20,180}}` + `deadlystrikeenhance_p={{1,6},{20,25}}` | **G7 AddPhysicsDamageP 15/215 vs PC 20/180 (L1 sai 25%, L20 sai 19%)** | Thấp-TB | 30 phút |
| 64 | Băng Lam Huyền Tinh | `radius=440, ColdResP -5/-25, cost=20, baseLv=10` | `coldres_p={{1,-9},{20,-49}}` | **G7 ColdResP -5/-25 vs PC -9/-49 (L20 2× weaker)** | Thấp | 15 phút |
| 66 | Tạp Nan Dược Kinh (passive) | `PoisonResP 10/60, baseLv=20` | `poisonres_p={{1,9},{20,39}}` | **G7 PoisonResP 10/60 vs PC 9/39 (L20 1.5× off)** | Thấp | 15 phút |
| 67 | Cửu Thiên Cuồng Lôi | `radius=440, LightingResP -5/-25, cost=20, baseLv=20` | `lightingres_p={{1,-9},{20,-49}}` | **G7 LightingResP -5/-25 vs PC -9/-49 (L20 2× weaker)** | Thấp | 15 phút |
| 70 | Chích Dương Thệ Thiên | `radius=440, FireResP -5/-25, cost=20, baseLv=30` | `fireres_p={{1,-9},{20,-49}}` | **G7 FireResP -5/-25 vs PC -9/-49 (L20 2× weaker)** | Thấp | 15 phút |
| 72 | Xuyên Tâm Độc Thích | `radius=440, PoisonResP -5/-25, cost=20, baseLv=20` | `poisonres_p={{1,-29},{20,-49}}` | **G7 PoisonResP -5/-25 vs PC -29/-49 (L1 sai 5.8× — magnitude lệch LỚN nhất trong 4 state debuff)** | Thấp-TB | 15 phút |
| 76 | Di Hoa Tiếp Ngọc | `radius=400, RangeDamageReturnP 10/50, cost=30, targetSelf` | ModSkills.txt: `radius=0, isMelee=7 (state buff)`. PC per-skill `rangedamagereturn_p` trong `/wudu/` (filename GBK) cho thấy effect đúng. | **G4 radius 400 vs PC 0 (buff không cần radius — mobile dùng để AOE state)** + **G7 magnitude chưa verify** | Thấp | 30 phút |
| 63/65/68/71/74 | (ranged missile) | mobile dùng `childSkillId=5, childSkillNum=1, baseSkill=true` | PC có `addskilldamage1-5` referencing 353/354/355/71/1066/1094/1096 (skill-interaction multiplier) | **G6/G7 — `addskilldamage` mechanism MISSING toàn cục mobile. Damage nhân hệ số 1.5-2× nếu caster có skill hỗ trợ.** | TB (gameplay) | Phase 5 (toàn dự án) |
| **Tất cả 7 ranged AOE (63/64/65/67/68/70/71/72/73/74)** | `PcSkillTuningRegistry.RadiusCurves[WuDuId]` | **0%** — line 1704-1932 không gọi registry | PC có 9 active attack cần curve | **G7 — Tuning coverage 0%** | TB | 1 ngày |
| 63/65/68/69/71/74 | `ConfigureWuDuVisuals` | **0%** — không có visual config function | PC có pre-cast SPR / missile SPR riêng cho mỗi skill wudu | **G7 — Visual coverage 0%** (mặc định SPR, mất cảm giác Ngũ Độc) | TB (visual) | 1-2 ngày |
| 1066 / 1067 | (PC sub-form 150-tier referenced as `addskilldamage` source) | KHÔNG CÓ trong mobile catalog | PC wudu.lua line 24/71/94/141/162/220: tham chiếu 1066/1067 làm boost source. 1066 = "Hình Tiêu Cốt Lập", 1067 = "U Hồn Phệ Ảnh" (per task brief) — sub-form 150-tier, ngoài scope 60-76. | **Không phải gap trong range 60-76** — Phase 5. | N/A | Phase 5 |
| 71/73/67/70 (task brief focus) | (dash/event chain check) | N/A — không có dash; không có event chain trong range 60-76 | PC 71/73/67/70 không có `skill_startevent/flyevent/collideevent/showevent`; 71/74/68/65/63 missile không có sub-event (sub-form 150-tier mới có) | **G1 N/A** + **G6 N/A** cho range 60-76 | N/A | — |

### 2.5.3. Phase 1 quick wins

> **Highest priority**: 73 (G5 attribute class swap), 62 (passive 11× off), 69 (movement ≠ attack), 71/74/65/68 (poison damage 3-14× off), 63 (radius sai 53%).

- [ ] **ID 73 Vạn Độc Thực Tâm — G5 attribute swap (CRITICAL)**: thay `PoisonResP -10/-40` → `PoisonTimeReduceP -200/-300` (PC `poisontimereduce_p`, âm = kéo dài độc trên target). Cần verify `MagicAttributeKind.PoisonTimeReduceP` đã có; nếu chưa thêm enum + runtime. (G5, 2 giờ)
- [ ] **ID 62 Ngũ Độc Chưởng Pháp — passive sai 11×**: sửa `AddPoisonDamageV 15/515` → `Link(lv, (1, 5, ""), (20, 45, ""))` (per-skill `wudu-zhangfa.lua`). Thêm `addphysicsdamage_p 13+7*lv` raw (per-skill line 19). (G7, 1 giờ)
- [ ] **ID 69 Vô Hình Độc — đổi nhầm class (CRITICAL)**: thay `AttackSpeedV 5/30` → `FastWalkRunP -10/-50` (PC `fastwalkrun_p` line 121-122, âm = tăng movement speed). Cần verify enum. Sửa `PoisonDamageV 25/220` → `Link(lv, (1, 5, ""), (20, 25, ""))`. (G7, 2 giờ)
- [ ] **ID 71 Thiên Cương Địa Sát — radius + poison damage**: sửa `radius=420` → `480`. Sửa `PoisonDamageV 50/385` → `Link(lv, (1, 50, ""), (20, 135, ""))`. (G4+G7, 1 giờ)
- [ ] **ID 74 Chu Cáp Thanh Minh — radius + poison damage**: sửa `radius=400` → `512`. Sửa `PoisonDamageV 50/385` → `Link(lv, (1, 16, ""), (20, 53, ""))`. (G4+G7, 2 giờ)
- [ ] **ID 65 Huyết Đao Độc Sát — poison damage schema 3-element nén sai**: sửa `PoisonDamageV 15/150` → dùng `Link(lv, (1, 4, ""), (20, 11, ""))` cho P1, P3=duration=10. Sửa `PhysicsEnhanceP 10/100` → `15/65`. (G7, 2 giờ)
- [ ] **ID 68 U Minh Khô Lâu — radius + poison damage**: sửa `radius=400` → `448`. Sửa `PoisonDamageV 30/250` → `Link(lv, (1, 11, ""), (20, 40, ""))`. (G4+G7, 2 giờ)
- [ ] **ID 63 Độc Sa Chưởng — radius sai 53% (CRITICAL)**: sửa `radius=180` → `384`. (G4 Cao, 30 phút)
- [ ] **ID 60 Ngũ Độc Đao Pháp — passive physics damage**: sửa `AddPhysicsDamageP 15/215` → `Link(lv, (1, 20, ""), (20, 180, ""))`. (G7 thấp, 30 phút)
- [ ] **ID 75 Ngũ Độc Kỳ Kinh — passive thiếu 2 attributes**: thêm `PoisonEnhanceP 12/50`; sửa `AddPoisonDamageV 20/200` → `Link(lv, (1, 5, ""), (30, 45, ""))`; sửa `CastSpeedV 5/30` → `Link(lv, (1, 1, ""), (30, 25, ""))`. (G7, 2 giờ)
- [ ] **ID 64/67/70/72 — magnitude sai 2× debuff**: sửa `ResP -5/-25` → `Link(lv, (1, -9, ""), (20, -49, ""))` (PC). Riêng 72: `Link(lv, (1, -29, ""), (20, -49, ""))`. (G7 thấp-TB, 30 phút tổng)
- [ ] **ID 66 Tạp Nan Dược Kinh — passive magnitude**: sửa `PoisonResP 10/60` → `Link(lv, (1, 9, ""), (20, 39, ""))`. (G7 thấp, 15 phút)
- [ ] **ID 76 Di Hoa Tiếp Ngọc — radius 0**: cân nhắc sửa `radius=400` → `0` (PC state buff). (G4 thấp, 5 phút)

### 2.5.4. Phase 2 tuning (G7 — 0% coverage)

- [ ] **Tuning coverage 0% → 100%**: thêm 10 entries vào `PcSkillTuningRegistry.RadiusCurves[WuDuId]`:
  - `[63]={(1,320),(20,384)}`
  - `[64]={(1,320),(20,384)}`
  - `[65]={(1,320),(20,384)}`
  - `[67]={(1,320),(20,384)}`
  - `[68]={(1,384),(20,448)}`
  - `[69]={(1,0),(20,0)}` (PC không set; mobile 100 sai)
  - `[70]={(1,320),(20,384)}`
  - `[71]={(1,448),(20,480)}`
  - `[72]={(1,320),(20,384)}`
  - `[73]={(1,320),(20,384)}`
  - `[74]={(1,448),(20,512),(21,512)}`
  - (G7, 1 ngày — tham chiếu template `WuDangId` / `TianWangId`)

### 2.5.5. Phase 3 dash (G1)

- [ ] **Không áp dụng** — Ngũ Độc không có skill dash/melee-jump. `KNpc.cpp::CastMeleeSkill` switch (line 1829-1891) và `wudu.lua` (range 60-76) đều không tham chiếu `Melee_Jump/JumpAndAttack/RunAndAttack`. Tất cả 16 skill là passive/ranged, không có dash.

### 2.5.6. Phase 4 event chain (G6)

- [ ] **Không áp dụng** cho range 60-76 — PC `wudu.lua` không có `skill_startevent/flyevent/collideevent/showevent` cho 60-76. Chỉ sub-form 150-tier `yinfeng_shigu` (1094) + `xuanyin_zhan` (1096) + `zhangwudu150` + `daowudu150` mới có event chain — ngoài scope task này. Phase 5.
- [ ] **Tuy nhiên**: cả 5 skill missile độc (63/65/68/71/74) đều có `addskilldamage*` referencing 353/354/355/71/1066/1094/1096. Cơ chế `addskilldamage` MISSING toàn cục mobile. Cần implement engine support. Effort: Phase 5 (toàn dự án).

### 2.5.7. Trạng thái

- [x] Catalog scan xong (16 skill: 4 passive mastery + 1 self buff + 5 ranged missile + 2 ranged surround độc + 4 ranged AOE state debuff)
- [ ] Quick-win phase merged (Phase 1 — 13 items, priority: 73 G5 + 69 G7 class swap + 62 passive 11× off + 71/74/65/68 magnitude + 63 radius 53%)
- [ ] Dash phase merged: **không áp dụng** (Ngũ Độc không có dash)
- [ ] Event chain phase merged: **không áp dụng** cho range 60-76 (sub-form 150-tier Phase 5)
- [ ] Tuning coverage 0% → 100% (cần thêm 10 entries vào `PcSkillTuningRegistry.RadiusCurves[WuDuId]`)
- [ ] Visual coverage 0% → có `ConfigureWuDuVisuals` (5 missile + 4 AOE state debuff)

### 2.5.8. Tổng kết

- **3 gap NGHIÊM TRỌNG NHẤT (Cao + đổi class gameplay)**:
  1. **ID 73 G5** — "Vạn Độc Thực Tâm" đổi nhầm `PoisonTimeReduceP` (kéo dài độc trên target) → `PoisonResP` (debuff self res). Tên gợi "target ăn độc lâu hơn" — implement hiện tại sai class hoàn toàn. Effort: 2 giờ.
  2. **ID 69 G7** — "Vô Hình Độc" đổi `FastWalkRunP` (movement speed buff = "tàng hình lao tới") → `AttackSpeedV` (attack speed). Gameplay cốt lõi mất. Effort: 2 giờ.
  3. **ID 62 G7** — Passive `Ngũ Độc Chưởng Pháp` `AddPoisonDamageV 15/515` vs PC `5/45` (L20 sai 11×). Effort: 1 giờ.
- **5 gap magnitude poison damage** (63/65/68/71/74): schema 3-element `{{1,2},{20,40}},{{1,60},{20,60}},{{1,10},{20,10}}` (per-tick min, per-tick max, duration) bị nén thành 2-element `(1,X,20,Y)` sai semantics. Cần verify mobile schema; nếu chưa có 3-element thì refactor.
- **G4 radius lệch**: 63 (180 vs 384 = -53%), 74 (400 vs 512 = -22%), 71 (420 vs 480 = -12%), 68 (400 vs 448 = -11%). Cao nhất là 63.
- **G7 state debuff magnitude** (64/67/70/72): -5/-25 vs PC -9/-49 (riêng 72 L1 -29). L20 sai 2×, không ảnh hưởng gameplay lớn nhưng nhất quán.
- **G7 passive 60/75/66**: 1.5-4.4× off magnitude. Fix dễ.
- **G7 tuning coverage 0%**: 10 entries radius thiếu trong `PcSkillTuningRegistry`. Cần thêm function `ConfigureWuDuTuning` (theo template `ConfigureWuDangTuning`).
- **G7 visual coverage 0%**: không có `ConfigureWuDuVisuals`. Cần thêm function này, tham chiếu wudu sub-spr pack.
- **Phase 5 (future)**: port sub-form 150-tier `wuxing-gu` (sister 69) / `xueshou-dusha` (sister 65) / `xuanyin_zhan` (1096) / `yinfeng_shigu` (1094) / `baidu_chuanxin` (1095) / `zhangwudu150` / `daowudu150` / `wudu120` từ PC `wudu.lua` line 142-401. Có event chain (yinfeng_shigu có `skill_flyevent`+`skill_vanishedevent`+`skill_showevent`; xuanyin_zhan có `skill_collideevent`+`skill_showevent`; zhangwudu150/daowudu150 có `skill_collideevent`+`skill_showevent`). Cần check ID trong `PcSkills.txt` (ngoài range 60-76 — có thể 700+).


## 2.3. Thiếu Lâm (PC: Shaolin 少林, ID range 3-21, loại trừ 5, 7)

> **Nguồn PC**:
> - `Assets/StreamingAssets/Reference/ModSkills.txt` + `PcSkills.txt` (canonical, SkillId 3-21 loại 5/7)
> - PC gốc: `/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser_bachkim_6.0/server1/script/skill/shaolin.lua` (GB2312, 450 dòng, 11 skill chính + 9 sub-form 150-tier)
> - PC cập nhật: `script/skill1/shaolin.lua` (TCVN3-pinyin, 450 dòng, dữ liệu mới với damage sub-skill cao hơn — "saolin mới")
> - Per-skill cũ: `script/skill/shaolin/*.lua` (11 file pinyin TCVN3)
> - Per-skill mới: `script/skill/saolin/*.lua` (19 file GB2312, không có `mohe-wuliang.lua` — skill 19)
> - C++: `KNpc.cpp::CastMeleeSkill` switch (line 1834) — **KHÔNG** có Thiếu Lâm skill nào thuộc nhánh `Melee_Jump/JumpAndAttack/RunAndAttack`. Toàn bộ Shaolin 3-21 là: passive mastery (3,4,6,8,9,12,21), self buff/state (13,15,16,18), hoặc **ranged missile** (10,11,14,17,19,20). Do đó **G1 (dash) KHÔNG áp dụng** cho Thiếu Lâm range 3-21.
> - Tóm tắt: Thiếu Lâm 3-21 là môn phái **ranged + buff + passive**. Gap nặng nhất: **G4 — sub-skill `addskilldamage*` bị bỏ** (mobile chỉ fire 1 childSkill, PC có 2-6 addskilldamage per skill). **G7 — `PcSkillTuningRegistry.ShaolinId` sai 3/5 entry** (registry [10]=90 vs PC 54, [19]=200 vs PC L20=512, missing 17/20).

### 2.3.1. Catalog scan

| ID | Tên (suy ra) | PC `shaolin.lua` key | Loại PC | Vai trò |
|---:|---|---|---|---|
| 3 | Thiếu Lâm Kiếm Pháp | `shaolin_gunfa` (sao chép từ gun) — passivity | Passive mastery | Buff physics dmg + atk rating + deadly (kiếm phái) |
| 4 | Thiếu Lâm Côn Pháp | `shaolin_gunfa` | Passive mastery | Buff physics dmg + atk rating + deadly (côn phái) |
| 6 | Thiếu Lâm Đao Pháp | `shaolin_daofa` | Passive mastery | Buff physics dmg + deadly (đao phái) |
| 8 | Thiếu Lâm Quyền Pháp | `shaolin_quanfa` | Passive mastery | Buff physics dmg (×2 khác slot) + atk rating + deadly (quyền phái) |
| 9 | Hỗn Nguyên Nhất Khí Công | (không có trong shaolin.lua — `addstaminamax_p` mobile-only) | Passive (stamina) | Buff stamina max (mobile: 20+10*lv) |
| **10** | Kim Cang Phục Ma | `jingang_fumo` | Ranged magic | Active — 6 sub-damages (321,319,11,19,1056,1057), L1/55 phys |
| **11** | Hoành Tảo Lục Hợp | `hengsao_liuhe` | Ranged surround | Active — 2 sub-damages (319,1056), surround AOE cold |
| 12 | Kim Cang Hộ Thể | (không có) | Passive (defense) | Buff defense (22+8*lv) |
| 13 | Lập Địa Thành Phật | (không có) | Self buff | Bad-status reduce (2+lv), 18-frame pulse |
| **14** | Hàng Long Bất Vũ | `xinglong_buyu` | Ranged magic | Active — 6 sub-damages (318,317,271,272,1083,1055) |
| 15 | Bất Động Minh Vương | `budong_mingwang` | Self buff | Atk rating + def V (18*120→18*180 ticks = 120→180 sec) |
| 16 | La Hán Trận | `luohan_zhen` | Self aura | Buff party (aura) + meleedamagereturn/rangedamagereturn |
| **17** | Long Trảo Hổ Trảo | `longzhao_huzhua` | Ranged magic | Active — 4 sub-damages (318,317,1083,1055) + stun + ignoredefense |
| 18 | Huệ Nhãn Chú | (không có — `attackratingenhance_p` 38+lv*10.5) | Self buff | Buff atk rating (38+lv*10.5, dur 960+lv*960 ticks) |
| **19** | Ma Ha Vô Lượng | `mohe_wuliang` | Ranged fan | Active — 2 sub-damages (321,1057), fan AOE cold |
| **20** | Sư Tử Hống | `shizi_hou` | Ranged surround | Active — stun + physicsdamage_v, has `skill_eventskilllevel` |
| 21 | Dịch Cân Kinh | `yijin_jing` | Passive (res) | All-res + meleedamagereturn + rangedamagereturn |

**Không catalog trong mobile** (PC `shaolin.lua` có, mobile thiếu — ngoài range 3-21): `damo_dujiang` (Đạt Ma Độ Giang — 150-tier), `rulai_qianye` (Như Lai Thiên Diện Thủ — passive 30-tier, 6 attrs HP+atk+life+stamina), `quanshaolin150` (拳少林150), `hengsao_qianjun` (Hoành Tảo Thiên Quân — 150-tier), `gunshaolin150` (棍少林150), `wuxiang_zhan` (Vô Tướng Trảm — 150-tier), `daoshaolin150` (刀少林150 — **CÓ `skill_startevent` + `skill_showevent`**), `dachengrulaizhou` (Đại Thành Như Lai Chú — passive 30-tier, autoreplyskill). Phase 5.

**Thiếu Lâm ID bị skip**: 5, 7 không thuộc Thiếu Lâm (`IsShaolinSkill` exclude rõ trong catalog line 595). Không phải gap.

### 2.3.2. Gap table

> **Chú thích**: "Mobile" = `CreateShaolinSkills()` trong `PcCombatCatalogFactory.cs:814-833` + 17 method tương ứng. "PC" = `skill/shaolin.lua` (gốc) + per-skill pinyin file + `skill1/shaolin.lua` (saolin cập nhật).

| ID | Tên | Hành vi mobile | Hành vi PC | Gap | Severity | Effort |
|---:|---|---|---|---|---|---|
| 3 | Thiếu Lâm Kiếm Pháp | `AddPhysicsDamageP (1,25)→(20,215)` slot=0 (kiếm — sai), `AttackRatingEnhanceP (1,15)→(20,72)`, `DeadlyStrikeEnhanceP (1,6)→(20,25)` | PC `shaolin_gunfa` (côn, shaolin.lua line 43-47) — `addphysicsdamage_p (1,25)→(20,100)` slot=**2** (gun), `attackratingenhance_p (1,35)→(20,275)`, `deadlystrikeenhance_p (1,6)→(20,45) Conic`. Per-skill `shaolin-quanfa.lua` cũng `slot=9` (quyền) cho quyền pháp. Kiếm (slot=0) chỉ có trong per-skill `少林剑法.lua` với `addphysicsdamage_p (1,15+10*lv)→(20,15+10*20=215)` slot=**0**, `attackratingenhance_p (1,12+3*lv)→(20,12+60=72)`, `deadlystrikeenhance_p (1,5+lv)→(20,5+20=25)`. | **G7 — mobile đang dùng values của per-skill `shaolin-quanfa.lua` (slot 9, attackrating 35→275) cho skill 3 (kiếm)** | TB (4× attackrating, slot sai) | 30 phút |
| 4 | Thiếu Lâm Côn Pháp | `addphysicsdamage_p (1,25)→(20,100)` slot=**2** ✓, `attackratingenhance_p (1,35)→(20,275)` (mobile) vs PC per-skill `少林棍法.lua` `(1,12+3*lv)→(20,72)` | PC `shaolin_gunfa` main: `addphysicsdamage_p (1,25)→(20,100)` slot=2 ✓, `attackratingenhance_p (1,35)→(20,275)` (main) vs `(1,35)→(20,272)` (skill1/shaolin.lua line 45) | **G7 — attackratingenhance_p 275 (mobile) vs 272 (skill1 saolin)** — sai số 3/275 ≈ 1%, OK; per-skill `少林棍法.lua` cho (1,12+3*lv)=72 thấp hơn 275 rất nhiều → main file đúng | Thấp (275 vs 272, 1%) | 15 phút verify |
| 6 | Thiếu Lâm Đao Pháp | `addphysicsdamage_p (1,25)→(20,215)` slot=1 ✓, `deadlystrikeenhance_p (1,5)→(20,15)` | PC `shaolin_daofa` main: `addphysicsdamage_p (1,25)→(20,215)` slot=1 ✓, `deadlystrikeenhance_p (1,5)→(20,15)` | OK (match) | — | — |
| 8 | Thiếu Lâm Quyền Pháp | `addphysicsdamage_p (1,25)→(20,415)` slot=9, `addphysicsdamage_p (1,25)→(20,215)` slot=9 (2 entries), `attackratingenhance_p (1,35)→(20,275)`, `deadlystrikeenhance_p (1,6)→(20,45) Conic` | PC `shaolin_quanfa` main: cùng schema — 415 + 215 + 275 + 45 | OK (match) | — | — |
| 9 | Hỗn Nguyên Nhất Khí Công | `StaminaMaxP (20+10*lv)` (mobile) | PC không có `hunyuan_yiqi` trong `shaolin.lua`. Per-skill `罗汉伏魔.lua` (La Hán Phục Ma = saolin #1) có `staminamax_p (20+10*lv)` (L1=30, L20=220). ID 9 mobile thực chất có thể tương ứng `luohan_fumo` (La Hán Phục Ma), không phải "Hỗn Nguyên Nhất Khí Công" | **G5 — id↔name swap** — mobile gán tên "Hỗn Nguyên Nhất Khí Công" cho ID 9, PC có `luohan_fumo` cho ID 9; mobile `huixian_xiangyi`/`hunyuan` không có trong PC base 3-21 | TB (name sai, gameplay OK nếu đúng stat) | 1 giờ verify |
| **10** | Kim Cang Phục Ma | `radius=400` (catalog) / registry [10]=**90** (PcSkillTuningRegistry line 36), `childSkillId=1056, childSkillNum=1` (mobile chỉ fire 1 sub-skill) | PC `jingang_fumo`: `skill_attackradius={{1,54},{20,54}}` (PC=54, mobile catalog 400 sai 7.4×); `addskilldamage1=321, addskilldamage2=319, addskilldamage3=11, addskilldamage4=19, addskilldamage5=1056, addskilldamage6=1057` (PC có **6** sub-damages); `missle_speed_v={{1,18},{20,18}}`, `missle_lifetime_v={{1,4},{20,4}}` | **G4 — childSkillId=1056 chỉ fire 1/6 sub-skill** (5 miss: 321, 319, 11, 19, 1057) + **G4 — radius 400 vs PC 54 (sai 7.4×)** + **G7 — registry [10]=90 vs catalog 400 vs PC 54** | Cao (mất 5/6 sub-hit + radius sai lớn) | 2 giờ |
| **11** | Hoành Tảo Lục Hợp | `radius=96` (catalog) / registry [11]=**90** (PcSkillTuningRegistry line 37), `childSkillId=319, childSkillNum=1` (mobile fire 1 sub) | PC `hengsao_liuhe`: `skill_attackradius={{1,96},{20,96}}` (PC=96, mobile catalog ✓), `addskilldamage1=319, addskilldamage2=1056` (PC có **2** sub-damages), `skill_eventskilllevel` ✓ (per-skill file) | **G4 — childSkillId=319 chỉ fire 1/2 sub-skill** (1056 miss) + **G7 — registry [11]=90 vs PC 96 (off 6%)** + **G6 — `skill_eventskilllevel` không handle** (per-skill file line 23 có `Getskill_eventskilllevel`) | TB (mất 1 sub-skill, registry lệch) | 1 giờ |
| 12 | Kim Cang Hộ Thể | `AddDefenseV (22+8*lv)` | PC không có trong `shaolin.lua`. Per-skill `金刚护体.lua` (Kim Cang Hộ Thể) confirm: `adddefense_v (22+8*lv)` (L1=30, L20=182) | OK (mobile match per-skill) | — | — |
| 13 | Lập Địa Thành Phật | `BadStatusTimeReduceV (2+lv)`, `cost=(5+lv/10)`, targetSelf+Ally, `radius=400` | PC không có trong `shaolin.lua`. Per-skill `清心梵音.lua` (Thanh Tâm Phạn Âm — saolin `BadStatusTimeReduceV (2+lv)`, cost=`5+lv/10`). Mobile dùng tên "Lập Địa Thành Phật" — tên này không có trong PC `shaolin.lua`. | **G5 — name drift** — "Lập Địa Thành Phật" không có trong PC. Thanh Tâm Phạn Âm mới đúng. Có thể là name custom VLTK mobile. | Thấp (cosmetic) | 30 phút verify |
| **14** | Hàng Long Bất Vũ | `radius=90` (catalog) / registry [14]=**90** ✓, `childSkillId=66, childSkillNum=1` (mobile fire 1 main missile, 0 sub-skill) | PC `xinglong_buyu`: **không có `skill_attackradius`** (mobile default 90 OK), `addskilldamage1=318, addskilldamage2=317, addskilldamage3=271, addskilldamage4=272, addskilldamage5=1083, addskilldamage6=1055` (PC có **6** sub-damages). Main missile id chưa rõ (mobile uses 66, không thấy trong addskilldamage list). | **G4 — childSkillId=66 có thể sai** (không khớp PC addskilldamage pattern; 66 không phải 1 trong 6 sub-ids) + **G4 — 6/6 sub-skill MISSING** (318, 317, 271, 272, 1083, 1055) | Cao (mất toàn bộ sub-skill chain) | 2-3 giờ (cần check missles.txt id=66, refactor childSkillId) |
| 15 | Bất Động Minh Vương | `attackratingenhance_p (1,28)→(20,275)`, `adddefense_v (1,15)→(20,250)`, `dur=18*120→18*180` (120→180 sec) | PC `budong_mingwang`: `attackratingenhance_p (1,28)→(20,275)`, `adddefense_v (1,15)→(20,250)`, `dur=18*120→18*180` | OK (match exactly) | — | — |
| 16 | La Hán Trận | `addphysicsdamage_p (1,11)→(20,135)`, `meleedamagereturn_p (1,1)→(20,20)→(25,25)`, `rangedamagereturn_p (1,1)→(20,20)→(25,25)`, `adddefense_v (1,40)→(20,800)`, `stateSpecialId=45` | PC `luohan_zhen` main: cùng schema, cùng values. Per-skill `luohan-zhen.lua` (skill/shaolin) **khác**: `meleedamagereturn_p if(level<10) then 1 else 5+2*level` (L10=25, L20=45) — **L10+ gate** trong per-skill, main file không có. **CONFLICT** giữa main và per-skill. | **G7 — main shaolin.lua thắng (mobile match) nhưng per-skill file có L10+ gate** (collision risk nếu per-skill dùng) | Thấp (chọn main file) | 1 giờ verify |
| **17** | Long Trảo Hổ Trảo | `radius=78` (catalog) — **KHÔNG có registry entry**, `childSkillId=218, childSkillNum=1` (mobile fire 1 sub) | PC `longzhao_huzhua`: `skill_attackradius={{1,78},{20,78}}` (PC=78, mobile ✓), `addskilldamage1=318, addskilldamage2=317, addskilldamage3=1083, addskilldamage4=1055` (PC có **4** sub-damages). `ignoredefense_p (1,9)→(20,85)`, `stun_p (1,1)→(20,5)`, `deadlystrike_p (1,5)→(20,40)`, `colddamage_v (1,10)→(20,56)` | **G4 — childSkillId=218 chỉ fire 1/4 sub-skill** (318, 317, 1083, 1055 miss) + **G7 — registry thiếu entry 17** (mặc dù PC có data) | TB (mất 3 sub-skill) | 1 giờ |
| 18 | Huệ Nhãn Chú | `attackratingenhance_p (38+lv*10.5)` (mobile), `dur=960+lv*960` | PC không có trong `shaolin.lua`. Per-skill `慧眼咒.lua` (skill/saolin): `attackratingenhance_p (1,38+lv*10.5)→(20,38+20*10.5=248)`, `dur=960+lv*960` ✓; **nhưng** per-skill `skill/shaolin/huiyan-zhou.lua` (pinyin): `attackratingenhance_p (1,15+13*lv)→(20,275)`, `dur=960+195*lv` — **SAI**. Main per-skill file (saolin GB2312) đúng, shaolin pinyin sai. | **G7 — mobile match per-skill saolin** (đúng), **NHƯNG** shaolin pinyin (cũ) có data sai → verify version | Thấp (mobile dùng file đúng) | 1 giờ verify |
| **19** | Ma Ha Vô Lượng | `radius=512` (catalog) / registry [19]=**200** (PcSkillTuningRegistry line 39), `childSkillId=61, childSkillNum=2` (mobile fire 2 main) | PC `mohe_wuliang`: `skill_attackradius={{1,448},{20,512}}` (PC L1=448, L20=512), `addskilldamage1=321, addskilldamage2=1057` (PC có **2** sub-damages — 321 với damage 92/2=46 L20, 1057 với 38 L20), `missle_speed_v={{1,28},{20,32}}` | **G4 — childSkillId=61 không khớp PC** (PC sub-damages là 321 và 1057, không phải 61) + **G7 — registry [19]=200 vs PC L20=512** (sai 156%) | Cao (childSkillId sai + registry sai 156%) | 2 giờ |
| **20** | Sư Tử Hống | `radius=90` (catalog) — **KHÔNG có registry entry**, `childSkillId=77, childSkillNum=1`, `stun_p (1,15)→(20,65)`, `physicsdamage_v (1,45)→(20,140)`, `cost (1,10)→(20,60)` | PC `shizi_hou`: **không có `skill_attackradius`**, `stun_p (1,15)→(20,65)→(21,66)`, `physicsdamage_v (1,45)→(20,140)`, `cost (1,10)→(20,60)`, `skill_eventskilllevel` ✓. Per-skill `shizi-hou.lua` (skill/shaolin): `stun_p (1,13+lv)→(20,33)`, `time (1,8+lv/2)→(20,18)` (SAI vs main); per-skill `shizi-hou.lua` (skill/saolin): `stun_p (1,18+lv)→(20,38)`, `time (1,10+lv/2)→(20,20)` (SAI vs main) | **G6 — `skill_eventskilllevel` không handle** (per-skill file có, mobile không fire) + **G7 — registry thiếu entry 20** + per-skill file conflict (mobile dùng main shaolin.lua values, đúng) | TB (event chain miss, registry miss) | 1-2 giờ |
| 21 | Dịch Cân Kinh | `allres_p (1,1)→(20,20)`, `meleedamagereturn_p (1,1)→(20,20)→(25,25)`, `rangedamagereturn_p (1,1)→(20,20)→(25,25)` | PC `yijin_jing` main: cùng schema, cùng values. Per-skill `yijin-jing.lua`: `allres_p (1,level)→(20,20)` (L1=1, L20=20) ✓ | OK (match) | — | — |
| **All 6 active** | `PcSkillTuningRegistry.RadiusCurves[ShaolinId]` cover 5/6 active (10, 11, 14, 16, 19) | PC có 6 active skills cần tuning | **G7 — Tuning coverage 83%** (thiếu ID 17, 20) | Thấp | 15 phút |
| **All 6 active** | registry [10]=90 vs catalog radius=400 vs PC 54; registry [19]=200 vs catalog 512 vs PC L20=512 | PC match | **G7 — registry-catalog-PC mismatch 2/5** (10 sai 7.4×, 19 sai 156%) | TB (inconsistency runtime vs catalog) | 30 phút |
| **All 4 ranged** (10, 11, 14, 17) | `ConfigureShaolinVisuals` cover 5/6 active visual case (10, 11, 14, 17, 19) | PC có 6 active + 1 self-buff-visual (15, 18) | **G7 — Visual coverage 6/6 active, OK** | — | — |

### 2.3.3. Phase 1 quick wins (sub-skill restoration + registry fix)

- [ ] **ID 10 Kim Cang Phục Ma** (priority cao — 6 sub-skills bị mất 5): thay `childSkillId=1056, childSkillNum=1` bằng `childSkillId=321, childSkillNum=1, baseSkill=true, childChain=[319,11,19,1056,1057]`. Hoặc dùng `PcCombatCatalogFactory` extension `s.childSkillIds = [321, 319, 11, 19, 1056, 1057]` để fire tất cả 6 sub-skill. Cần check `missles.txt` cho 11 (= Kim Cang Phục Ma main), 19 (= chính là main missile, có thể conflict). (G4, 2 giờ — cần test sequence 6 sub-missile cùng cast)
- [ ] **ID 19 Ma Ha Vô Lượng**: thay `childSkillId=61, childSkillNum=2` bằng `childSkillId=321, childSkillNum=1, baseSkill=true` + chain `[1057]`. **Verify** missle id=61 trong `missles1.txt` — nếu 61 là 1 loại missile khác, sửa ngay. (G4, 1 giờ)
- [ ] **ID 14 Hàng Long Bất Vũ**: thay `childSkillId=66` bằng `childSkillId=318, childSkillNum=1, baseSkill=true` + chain `[317, 271, 272, 1083, 1055]`. **Verify** missle id=66 trong `missles1.txt` — 66 có thể là "main missile" (không phải sub), trong trường hợp đó cần tách main + sub. (G4, 2-3 giờ)
- [ ] **ID 17 Long Trảo Hổ Trảo**: thêm `childSkillId=318, childSkillNum=1, baseSkill=true` + chain `[317, 1083, 1055]` (4 sub-skills bị mất 3). (G4, 1 giờ)
- [ ] **ID 11 Hoành Tảo Lục Hợp**: thêm `childSkillId=319, childSkillNum=1, baseSkill=true` + chain `[1056]` (1 sub bị mất). (G4, 1 giờ)
- [ ] **ID 10 radius fix (G4 nghiêm trọng)**: catalog `radius=400` → `54` (PC `skill_attackradius`). Hoặc thêm `s.useRegistryRadius = true` để runtime dùng `PcSkillTuningRegistry` (cần sửa registry trước). (G4, 5 phút)
- [ ] **ID 19 registry fix (G7 nghiêm trọng)**: `PcSkillTuningRegistry.ShaolinId[19]` từ `{(1,200),(20,200)}` → `{(1,448),(20,512)}` (PC `skill_attackradius` 448→512). (G7, 5 phút)
- [ ] **ID 10 registry fix**: `PcSkillTuningRegistry.ShaolinId[10]` từ `{(1,90),(20,90)}` → `{(1,54),(20,54)}` (PC). (G7, 5 phút)
- [ ] **ID 11 registry fix**: `PcSkillTuningRegistry.ShaolinId[11]` từ `{(1,90),(20,90)}` → `{(1,96),(20,96)}` (PC). (G7, 5 phút)
- [ ] **ID 17, 20 thêm registry**: `PcSkillTuningRegistry.ShaolinId[17] = new[] { (1, 78), (20, 78) }`; `PcSkillTuningRegistry.ShaolinId[20] = new[] { (1, 90), (20, 90) }` (mobile default — PC không có attackradius cho 20). (G7, 15 phút)
- [ ] **ID 9 name verify (G5)**: cross-check `PcSkills.txt` ID 9 — nếu là `luohan_fumo` thì đổi tên mobile "Hỗn Nguyên Nhất Khí Công" → "La Hán Phục Ma". Nếu là skill khác thì OK. (G5, 30 phút)
- [ ] **ID 13 name verify (G5)**: "Lập Địa Thành Phật" không có trong PC — có thể là custom VLTK name. Per-skill `清心梵音.lua` = "Thanh Tâm Phạn Âm" (saolin). Cân nhắc đổi. (G5, 30 phút)
- [ ] **ID 3 slot verify (G7)**: `addphysicsdamage_p` slot=0 (kiếm) trong per-skill `少林剑法.lua` confirm; **NHƯNG** mobile chỉ có 1 addphysicsdamage_p, không phải pattern "2 entries slot=9" như quyền. Catalog `AddPhysicsDamageP, Link(lv, (1, 25, ""), (20, 215, "")), -1, 0)` — slot=0 ✓. OK nếu per-skill file đúng. Verify với test 1 hit = 215 phys enh L20. (G7, 30 phút verify)

### 2.3.4. Phase 3 dash (G1)

- [ ] **Không áp dụng** — Thiếu Lâm range 3-21 không có skill dash/melee-jump. Cả `KNpc.cpp::CastMeleeSkill` switch (line 1834), `skill/shaolin.lua`, `skill1/shaolin.lua` đều không có `Melee_Jump/JumpAndAttack/RunAndAttack` cho ID 3-21. Toàn bộ 6 active skill là **ranged magic/missile**, không phải melee dash. Phase 3 bỏ qua cho Thiếu Lâm range 3-21. (Damo Dujiang 150-tier — Phase 5 — CÓ thể có dash, cần verify KNpc.cpp cast khi port).

### 2.3.5. Phase 4 event chain (G6)

- [ ] **ID 11 Hoành Tảo Lục Hợp** (`hengsao_liuhe` per-skill `skill_eventskilllevel` ✓): thêm runtime path để fire sub-skill event tại level `eventSkillLevel` = `level`. Cần check PC `skill_startevent`/`skill_flyevent`/`skill_collideevent` cho 11 — `skill/shaolin.lua` không define (chỉ per-skill file có `skill_eventskilllevel`). Mobile hiện không có runtime hook. (G6, 1-2 giờ)
- [ ] **ID 20 Sư Tử Hống** (`shizi_hou` per-skill `skill_eventskilllevel` ✓): tương tự 11. (G6, 1-2 giờ)
- [ ] **ID 16 La Hán Trận — `stateSpecialId=45`**: mobile đã set ✓. Cần verify `MapEnemy/buff system` xử lý state 45 → apply aura cho đồng đội. (G6 verify, 30 phút)
- [ ] **Phase 5 (future)**: port `daoshaolin150` (CÓ `skill_startevent` + `skill_showevent` chain — ẩn ý event chain cho Thiếu Lâm 150-tier).

### 2.3.6. Trạng thái

- [x] Catalog scan xong (17 skill: 7 passive mastery, 4 self buff/state, 6 ranged active)
- [ ] **Quick-win phase merged** (Phase 1 — 12 items, priority: 10/19/14/17 sub-skill chain + 10/19 radius fix)
- [ ] Dash phase merged: **không áp dụng** (Thiếu Lâm không có dash 3-21)
- [ ] Event chain phase merged (Phase 4 — 3 items, ID 11/20/16)
- [ ] Tuning coverage 83% → 100% (cần thêm 17, 20 + sửa 10, 11, 19)

### 2.3.7. Tổng kết

- **Skill chính ưu tiên sửa**: **10 Kim Cang Phục Ma** (6 sub-skills bị mất 5, radius sai 7.4×) → fix G4 trước (2 giờ, có hiệu ứng damage lớn) → rồi G7 registry (5 phút, fix radius runtime). Một mũi tên trúng 2 đích.
- **Skill ưu tiên #2**: **19 Ma Ha Vô Lượng** (childSkillId sai, registry sai 156%) → fix G4 + G7 (1-2 giờ, cảm giác fan AOE tăng rõ).
- **Skill ưu tiên #3**: **14 Hàng Long Bất Vũ** (6 sub-skills bị mất 6) → fix G4 (2-3 giờ, cần check missles.txt id=66).
- **Skill thứ cấp**: 11, 17 — sub-skill chain đơn giản, fix 1 giờ mỗi skill.
- **Passive/buff verify**: 9 (G5 name), 13 (G5 name), 16 (state 45), 18 (per-skill conflict), 21 (OK). Effort 30 phút mỗi skill verify, giá trị thấp (cosmetic).
- **Test plan**: unit test cho sub-skill chain (cast 10 L1, expect 6 sub-missile fires trong cùng 1 PC tick, total damage = 6× base); visual test cast 19 ở 3 cự ly (radius 448/480/512 theo level); regression test cast 14 confirm 6 sub-missile ID 318, 317, 271, 272, 1083, 1055 đều fire.
- **Không cần làm dash** cho range 3-21.
- **Event chain** (G6) cho 11/20/16 — thấp-trung bình severity, có thể Phase 5.
- **Phase 5 (future)**: port 150-tier `damo_dujiang` (Đạt Ma Độ Giang — có thể có dash), `daoshaolin150` (CÓ event chain), `quanshaolin150` (拳少林150), `gunshaolin150` (棍少林150), `wuxiang_zhan` (Vô Tướng Trảm), `hengsao_qianjun` (Hoành Tảo Thiên Quân), `rulai_qianye` (Như Lai — 6 attrs HP+atk+life+stamina), `dachengrulaizhou` (Đại Thành Như Lai Chú — autoreplyskill). Cần check ID trong `PcSkills.txt` (ngoài range 3-21 — có thể 700+ hoặc 800+ tier).
## 2.7. Thúy Yên (PC: CuiYan 翠烟, ID range 95-114)

> **Nguồn PC**:
> - `/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser_bachkim_6.0/server1/script/skill/cuiyan.lua` (GB18030, 23 skills incl. 150-tier + 120-tier)
> - Per-skill files: `cuiyan/{cuiyan-daofa,cuiyan-shuangdao,binggu-xuexin,bingxin-qianying,fengjuan-canxue,muye-liuxing,taxue-wuhen,xueying}.lua` (8 file, mỗi file override 1 attribute)
> - `Assets/StreamingAssets/Reference/ModSkills.txt` (canonical, TCVN3, SkillId 95-114, 19 skills — trừ 96/98 cùng pattern passive mastery)
> - C++: `KNpc.cpp::CastMeleeSkill` switch (line 1829-1891). **CuiYan 100% IsMelee=0** (xác nhận từ ModSkills.txt) → không thuộc nhánh Melee_Jump/JumpAndAttack/RunAndAttack. **G1 (dash) KHÔNG áp dụng** cho CuiYan.
> - Tóm tắt: Thúy Yên là môn phái **băng + song đao ranged** (active skills đều `SkillStyle=0/2/4` + child missile 6-13), đa số là **Missiles + Surround** + childSkillId 6-13 + StartEvent chain (102→398, 111→112). Gap nặng nhất: **G4 (childSkillId sai toàn bộ)**, **G7 (passive 95/97/100/109 sai effect hoàn toàn)**, **G6 (event chain 102→398, 111→112 missing)**, **G7 (PC `addskilldamage` sub-skills 336/337/338/382/398/1063/1064/1065/1093/381 chưa register)**.

### 2.7.1. Catalog scan

| ID | Tên | Loại PC | Vai trò |
|---:|---|---|---|
| 95 | Thúy Yên Đao pháp (passive) | Passive mastery | Buff physics dmg + crit (theo `cuiyan-daofa.lua`) |
| 96 | Thúy Yên Kiếm pháp (passive ##) | Passive mastery | Alt variant — **không catalog mobile** |
| 97 | Thúy Yên Song đao (passive) | Passive mastery | Buff **cold magic** theo PC `cuiyan-shuangdao.lua` (mobile sai: physics + crit) |
| 98 | Bích Yên Kiếm pháp (passive ##) | Passive mastery | Alt variant — **không catalog mobile** |
| 99 | Phong Hoa Tuyết Nguyệt | Ranged + missile 6 | Active — child=6 + 4 addskilldamage (336/108/1063/1064) |
| 100 | Hộ Thể Hàn Băng | Self buff | PC `huti_hanbing`: meleedamagereturn_p + rangedamagereturn_p. Mobile sai: ColdResP + AddDefenseV |
| 101 | Trị liệu thuật | Ranged heal self | PC `bingxin_qianying`: lifereplenish_v 130→700. Mobile sai: ManaReplenishV (sai effect) |
| 102 | Phong Quyển Tàn Tuyết | Ranged + StartEvent=398 | Active — child=7 + StartSkillId 398 (event chain **chưa fire**) |
| 103 | Thiên Lý Băng Phong | Self buff | PC `taxue_wuhen` empty (file per-skill có `fastwalkrun_p`). Mobile sai: ColdResP + AllResP |
| 104 | Băng Hồn | Passive | **không catalog mobile** |
| **105** | **Vũ Đả Lê Hoa** | Ranged + child=8, num=4 | **PC childSkillNum=4 (mất 4-hit). Mobile: 1.** |
| 106 | Băng Tung Vô Ảnh 111 | Teleport/blink (Style=4) | PC MslsGenerate=15, AttackRadius=400. **không catalog mobile** |
| 107 | Nhiếp Tâm Thuật | Ranged self (LRSkill=2) | PC child=6, num=1, AttackRadius=180. **không catalog mobile** |
| 108 | Mục Dã Lưu Tinh | Ranged + child=9 | Active — child=9 + 3 addskilldamage (336/1063/1064). Mobile: radius 420 vs PC 480 |
| 109 | Tuyết Ảnh | Self buff | PC `xueying`: attackspeed_v + fastwalkrun_p. Mobile sai: AllResP + AddDefenseV |
| 110 | Ngũ hành độn | Ranged self | PC child=6, num=1, AttackRadius=180. **không catalog mobile** |
| **111** | **Bích Hải Triều Sinh** | Ranged + StartEvent=112 | **Active — child=10 + StartSkillId 112 (sub-skill 112 missing in mobile catalog)** |
| **112** | **Bích Hải Triều Sinh b** | Ranged AOE (StartSkillId cho 111) | **PC child=11, num=16, MslsGenerate=5. MISSING trong mobile** — event chain dead |
| 113 | Phù Vân Tán Tuyết | Ranged + child=12 | Active — child=12 + 3 addskilldamage (338/1065/1093). Mobile: cost 20 vs PC 50 |
| 114 | Băng Cốt Tuyết Tâm (passive 30) | Passive mastery | PC maxLevel=30; 7 attributes (addcoldmagic_v + addcolddamage_v + addphysicsmagic_v + deadly + fasthitrecover + coldenhance + lifemax). Mobile: 2 attributes, bỏ 5 cái |

**Không catalog trong mobile** (PC `cuiyan.lua` có, mobile thiếu):
- **96, 98** — passive mastery alt-variant "##" (faction-specific, có thể MOD)
- **104** — "Băng Hồn" passive (no LvlSet active)
- **106** — "Băng Tung Vô ảnh" teleport/blink (MslsGenerate=15, AttackRadius=400) — 150-tier-like active
- **107** — "Nhiếp Tâm Thuật" (child=6 self, AttackRadius=180)
- **110** — "Ngũ hành độn" (child=6 self, AttackRadius=180)
- **112** — "Bích Hải Triều Sinh b" (16-missile AOE; **critical: StartSkillId cho event chain 111**)
- 1063, 1064, 1065, 1093, 381 — MOD sub-skills referenced trong `addskilldamage1-4` của 99/102/105/108/111/113 (Băng Tước Hoạt Kú, Băng Ngưng Hàn Yên, Thủy Anh Man Tố, etc.) — **không có catalog nào cho 5 ID này**
- 336, 337, 338, 382, 398 — missile IDs referenced trong `addskilldamage1-2` (Băng Tung Vô Ảnh / Phong Tuyết Băng Thiên / Băng Tâm Ngọc Lăng) — có thể là missle ID, cần check `Missles.txt`
- 150-tier: `bingzong_wuying`, `bingxin_yuling`, `daocuiyan150`, `daocuiyan150_2`, `bingxin_xuelian`, `bingxin_xianzi`, `fengxue_bingtian`, `neicuiyan150`, `neicuiyan150_2` — ngoài scope task 95-114 (Phase 5)
- 120-tier: `cuiyan120` (hide + skill_mintimepercast_v) — ngoài scope (Phase 5)

### 2.7.2. Gap table

> **Chú thích cột "Hành vi mobile"**: lấy từ `CreateCuiYanSkills()` trong `PcCombatCatalogFactory.cs` line 1934-2127.
> **Chú thích cột "Hành vi PC"**: lấy từ `ModSkills.txt` (AttackRadius, ChildSkillId, ChildSkillNum, CharAnimId, MisslesForm, MslsGenerate, StartEvent/StartSkillId, ReqLevel, MaxLevel) + `cuiyan.lua` (damage curves per level) + per-skill file (formula override).

| ID | Tên | Hành vi mobile | Hành vi PC | Gap | Severity | Effort |
|---:|---|---|---|---|---|---|
| 95 | Thúy Yên Đao pháp | `AddPhysicsDamageP(15→215) + DeadlyStrikeEnhanceP(6→25 Conic)` | PC `cuiyan-daofa.lua`: `addphysicsdamage_p=13+7*level` (L1=20, L20=153) + `deadlystrikeenhance_p=5+level` (L20=25). Per-skill file override cuiyan.lua. | **G7 — L1 sai lớn (15 vs 20), L20 sai lớn (215 vs 153, +40%)** | TB (passive mastery sai damage) | 30 phút |
| 96 | Thúy Yên Kiếm pháp (##) | **không catalog** | PC ModSkills.txt: passive mastery (SkillStyle=3, charAnimId=14) — alt-variant | **G — Missing trong mobile catalog** | TB (faction variant, MOD) | 30 phút |
| 97 | Thúy Yên Song đao | `AddPhysicsDamageP(15→215) + DeadlyStrikeEnhanceP(6→25 Conic)` | PC `cuiyan-shuangdao.lua`: `addcoldmagic_v=13+7*level, time=-1,5` (L20=153, time=-1, flag=5) | **G7 — Sai effect hoàn toàn** (physics dmg + crit vs **cold magic**). Also flag 5 ≠ 0. | **Cao** (passive mastery sai school effect — băng thành vật lý) | 30 phút |
| 98 | Bích Yên Kiếm pháp (##) | **không catalog** | PC ModSkills.txt: passive mastery — alt-variant | **G — Missing trong mobile catalog** | TB | 30 phút |
| 99 | Phong Hoa Tuyết Nguyệt | `childSkillId=70, childSkillNum=1, baseSkill=true, charAnimId=2, radius=360, WaitTime=0` | PC: child=**6**, num=1, baseSkill=1, charAnimId=**11**, MisslesForm=1, AttackRadius=360, WaitTime=**5**, LvlSet `fenghua_xueyue`: physicsenhance_p(5→85), seriesdamage_p(1→10), 4×addskilldamage (336/108/1063/1064). | **G4 — childSkillId 70 vs PC 6 (sai hoàn toàn)** + **G4 charAnimId 2 vs PC 11** + **G4 waitTime 0 vs PC 5** + **G7 — mobile bỏ physicsenhance_p(5→85)** + **G — 4 addskilldamage sub-skills 336/108/1063/1064 missing** | **Cao** (multi-gap: child sai + animation sai + bỏ 1 attribute + 4 sub-skills missing) | nửa ngày |
| 100 | Hộ Thể Hàn Băng | `InitiativeNpcState, targetSelf, charAnimId=2, ColdResP(10→50) + AddDefenseV(50→450), 1200+1200*lv, cost 20, AttackRadius=400` | PC `huti_hanbing`: `meleedamagereturn_p(5→20, 18*120 dur) + rangedamagereturn_p(5→20, 18*120 dur) + skill_cost_v(40→60)`. PC AttackRadius=0 (buff, không cần). CharAnimId=11. | **G7 — Sai effect hoàn toàn** (ColdResP + AddDefense vs **damage return shield**) + **G7 cost sai (20 vs PC 40-60)** + **G4 charAnimId 2 vs PC 11** + **G4 AttackRadius 400 vs PC 0** (waste) | **Cao** (passive mastery sai data — băng trở thành tank thuần) | 1 giờ |
| 101 | Trị liệu thuật | `Missiles, childSkillId=5, num=1, baseSkill=true, charAnimId=2, targetSelf, targetAlly, ManaReplenishV(100→450), cost 50, AttackRadius=400` | PC `bingxin_qianying`: `lifereplenish_v(130→700, time=20) + skill_cost_v(20+level)`. PC child=**13**, charAnimId=**11**, WaitTime=**5**, LRSkill=2 (self). | **G7 — Sai effect hoàn toàn** (ManaReplenishV vs **LifeReplenish** = HEAL) + **G4 childSkillId 5 vs PC 13** + **G4 charAnimId 2 vs PC 11** + **G7 cost sai (50 vs PC 21-40)** + **G7 life curve sai (100-450 vs PC 130-700)** | **Cao** (heal thành mana — sai semantics gameplay) | 1 giờ |
| 102 | Phong Quyển Tàn Tuyết | `childSkillId=71, num=1, charAnimId=2, ColdDamageV(30→300, 0, 40→400), cost 15, radius=360, WaitTime=0` | PC `fengjuan_canxue`: `physicsdamage_v(25→235, 0, 25→375) + seriesdamage_p(1→10) + 4×addskilldamage (337/111/1065/1093)`. PC child=**7**, charAnimId=**11**, WaitTime=**5**, StartEvent=**1**, StartSkillId=**398**. | **G4 — childSkillId 71 vs PC 7** + **G4 charAnimId 2 vs PC 11** + **G4 waitTime 0 vs PC 5** + **G7 — Sai damage type (Cold vs Physics)** + **G7 curve sai (cold 30-300 vs PC phys 25-235)** + **G6 — StartEvent=1, StartSkillId=398 chưa fire (sub-skill 398 missing)** + **G — 4 addskilldamage sub-skills missing** | **Cao** (multi-gap: 3 G4 + 2 G7 + 1 G6 + sub-skills) | nửa ngày |
| 103 | Thiên Lý Băng Phong | `InitiativeNpcState, targetSelf, charAnimId=2, ColdResP(15→75) + AllResP(5→25), 1200+1200*lv, cost 25, AttackRadius=400` | PC `taxue_wuhen` empty trong cuiyan.lua, per-skill `taxue-wuhen.lua`: `fastwalkrun_p(15+2*level, time=1080+135*level) + skill_cost_v(20+level*4)`. PC AttackRadius=0, charAnimId=11. | **G7 — Có thể sai effect** (ColdResP + AllResP vs **fastwalkrun_p** = move speed) + **G4 charAnimId 2 vs PC 11** + **G4 AttackRadius 400 vs PC 0** + **G7 cost sai (25 vs PC 20+4*lv=24-100)** | **Cao** (nếu là move speed, gameplay thay đổi hoàn toàn — Thúy Yên thiếu kỹ năng tăng tốc) | 1 giờ verify |
| 104 | Băng Hồn | **không catalog** | PC ModSkills.txt: passive (SkillStyle=3, charAnimId=14) | **G — Missing** | Thấp (no active effect) | 30 phút |
| **105** | **Vũ Đả Lê Hoa** | `childSkillId=72, **num=1**, charAnimId=2, PhysicsEnhanceP(10→100) + ColdDamageV(30→250, 0, 30→250), cost 20, radius=300, WaitTime=0, MslsGenerate=0` | PC `yuda_lihua`: `physicsenhance_p(10→140) + seriesdamage_p(5→30) + 2×addskilldamage (382/1064)`. PC child=**8**, **num=4**, charAnimId=**11**, WaitTime=**5**, MisslesForm=6, MslsGenerate=**3**, MslsGenerateData=**10**, AttackRadius=300, LRSkill=0. | **G4 — childSkillId 72 vs PC 8** + **G4 childSkillNum=1 vs PC=4 — MẤT 4-HIT pattern** + **G4 charAnimId 2 vs PC 11** + **G4 waitTime 0 vs PC 5** + **G4 MslsGenerate=0 vs PC=3 (multi-missile)** + **G7 physicsenhance_p L20 sai (100 vs 140, -29%)** + **G — 2 addskilldamage sub-skills 382/1064 missing** | **Cao** (mất 4-hit + multi-missile — cốt lõi Vũ Đả Lê Hoa) | nửa ngày |
| 106 | Băng Tung Vô ảnh 111 | **không catalog** | PC ModSkills.txt: **SkillStyle=4** (InitiativeNpcState variant), AttackRadius=400, MslsGenerate=**15**, MslsForm=1, LRSkill=2, charAnimId=11 | **G — Missing** (teleport/blink 15-missile AOE) | TB (gap lớn, PC có 15 missile) | 2 giờ |
| 107 | Nhiếp Tâm Thuật | **không catalog** | PC ModSkills.txt: SkillStyle=0, child=6, num=1, baseSkill=1, AttackRadius=180, LRSkill=2 (self), WaitTime=5, charAnimId=11 | **G — Missing** (self-targeted ranged, 180 radius) | Thấp (small effect) | 1 giờ |
| 108 | Mục Dã Lưu Tinh | `childSkillId=73, num=1, charAnimId=2, ColdDamageV(50→385, 0, 50→385) + SeriesDamageP(10→50), cost Link(20→40), radius=420, WaitTime=0, MslsGenerate=1` | PC `muye_liuxing`: `seriesdamage_p(10→50) + physicsenhance_p(30→271) + colddamage_v(20→246, 0, 20→426) + 3×addskilldamage (336/1063/1064)`. PC child=**9**, charAnimId=**11**, AttackRadius=448→**480** (L20), MisslesForm=6, LRSkill=0. PC cost=30→40. | **G4 — childSkillId 73 vs PC 9** + **G4 charAnimId 2 vs PC 11** + **G4 radius 420 vs PC 480 (-12%)** + **G7 — bỏ PhysicsEnhanceP(30→271) hoàn toàn** + **G7 colddamage_v L20 sai (385 vs PC 246, +56%)** + **G7 cost L1 sai (20 vs PC 30, -33%)** + **G — 3 addskilldamage sub-skills missing** | **Cao** (bỏ 1 attribute + sai 2 cái khác + radius sai) | nửa ngày |
| 109 | Tuyết Ảnh | `InitiativeNpcState, targetSelf, charAnimId=2, AllResP(5→25) + AddDefenseV(50→350), 1200+1200*lv, cost 30, AttackRadius=400` | PC `xueying`: `attackspeed_v(12→65, 23,73, 25,90, 28,99, 42,111, 43,119, 44,122) + fastwalkrun_p(17→55) + skill_cost_v(40→140)`. PC AttackRadius=0, charAnimId=11, WaitTime=5, LRSkill=2. | **G7 — Sai effect hoàn toàn** (AllResP + AddDefense vs **attackspeed_v + fastwalkrun_p** = cast/atk/move speed) + **G4 charAnimId 2 vs PC 11** + **G4 waitTime 0 vs PC 5** + **G4 AttackRadius 400 vs PC 0** + **G7 cost sai (30 vs PC 40-140)** | **Cao** (passive sai data — mất cảm giác "tuyết ảnh" = move speed) | 1 giờ |
| 110 | Ngũ hành độn | **không catalog** | PC ModSkills.txt: SkillStyle=0, child=6, num=1, baseSkill=1, AttackRadius=180, LRSkill=2 (self), WaitTime=5, charAnimId=11 | **G — Missing** (self-targeted ranged, 180 radius) | Thấp | 1 giờ |
| **111** | **Bích Hải Triều Sinh** | `childSkillId=74, num=1, charAnimId=2, ColdDamageV(40→350, 0, 40→350) + SeriesDamageP(10→50), cost 25, radius=72, WaitTime=0` | PC `bihai_chaosheng`: `seriesdamage_p(10→50) + physicsdamage_v(20→200, 0, 20→200) + colddamage_v(43→704, 0, 43→1214) + 4×addskilldamage (337/338/1065/1093)`. PC child=**10**, charAnimId=**11**, AttackRadius=72, MisslesForm=7, LRSkill=0, StartEvent=**1**, StartSkillId=**112**. PC cost=65. | **G4 — childSkillId 74 vs PC 10** + **G4 charAnimId 2 vs PC 11** + **G4 waitTime 0 vs PC 1** (StartEvent) + **G7 colddamage_v L20 SAI LỚN (350 vs PC 704, -50%)** + **G7 — bỏ PhysicsDamageV(20→200)** + **G7 cost sai (25 vs PC 65, -62%)** + **G6 — StartEvent=1, StartSkillId=112 chưa fire** + **G — 4 addskilldamage sub-skills missing** | **Cao** (damage sai 50% + bỏ physics + event chain dead) | nửa ngày |
| **112** | **Bích Hải Triều Sinh b** | **không catalog** | PC ModSkills.txt: SkillStyle=0, child=**11**, num=**16**, baseSkill=1, charAnimId=11, MslsGenerate=**5**, MslsGenerateData=**1**, LRSkill=2. **Đây là StartSkillId của 111 — event chain** | **G — Missing critical** (16-missile AOE sub-skill for 111) | **Cao** (event chain dead nếu thiếu) | 1 giờ |
| 113 | Phù Vân Tán Tuyết | `childSkillId=75, num=1, charAnimId=2, PhysicsEnhanceP(40→200) + ColdDamageV(20→200, 0, 20→200) + SeriesDamageP(5→25), cost 20, radius=400, WaitTime=0` | PC `fuyun_sanxue`: `colddamage_v(40→375, 0, 40→575) + seriesdamage_p(5→30) + 3×addskilldamage (338/1065/1093)`. PC child=**12**, charAnimId=**11**, WaitTime=**5**, AttackRadius=384→**416** (L20), MisslesForm=6, LRSkill=0. PC cost=50. | **G4 — childSkillId 75 vs PC 12** + **G4 charAnimId 2 vs PC 11** + **G4 waitTime 0 vs PC 5** + **G4 radius 400 vs PC 416 (-4%)** + **G7 — Thừa PhysicsEnhanceP(40→200)** (PC không có) + **G7 colddamage_v L20 sai (200 vs PC 375, -47%)** + **G7 cost sai (20 vs PC 50, -60%)** + **G — 3 addskilldamage sub-skills missing** | **Cao** (thừa attribute + 2 cái sai + radius lệch) | nửa ngày |
| 114 | Băng Cốt Tuyết Tâm (passive 30) | `PassivityNpcState, charAnimId=14, AddColdDamageV(20→200) + CastSpeedV(5→30), MaxLevel=30` | PC `binggu_xuexin` (maxLevel=**30**): `addcoldmagic_v(60→315) + addcolddamage_v(30→275) + addphysicsmagic_v(30→275) + deadlystrikeenhance_p(5→45 Conic) + fasthitrecover_yan_v(5→49) + coldenhance_p(8→80) + lifemax_yan_p(21→20)`. | **G7 — Mobile chỉ có 2 attributes, PC có 7**: bỏ `addcoldmagic_v` (cold magic dmg), `addphysicsmagic_v` (physics magic), `deadlystrikeenhance_p` (crit), `fasthitrecover_yan_v` (smoke), `coldenhance_p` (cold enhance), `lifemax_yan_p` (life max smoke) + **G7 AddColdDamageV L30 sai (200 vs 275, -27%)** | **Cao** (mastery passive mất 5/7 attribute — game-breaking) | nửa ngày |
| **All 6 active** (99, 102, 105, 108, 111, 113) | — | `childSkillId` dùng 70-75 (mobile) | PC dùng 6, 7, 8, 9, 10, 12 | **G4 — childSkillId sai toàn bộ** (mobile internal ID ≠ PC child missile ID — visual/runtime reference sai) | **Cao** (cốt lõi missile chain) | 1 giờ (sửa 6 dòng) |
| **All 6 active** | — | `charAnimId=2` (mobile default) | PC yêu cầu **11** (tất cả) | **G4 charAnimId sai toàn bộ** | TB (animation) | 30 phút |
| **All buff** (100, 103, 109) | — | `AttackRadius=400` (mobile default) | PC AttackRadius=**0** (buff, không cần) | **G4 radius waste** (không ảnh hưởng gameplay nhưng sai data) | Thấp | 15 phút |
| **All 6 active** | — | `WaitTime=0` (mobile default) | PC WaitTime=5 (cho 99/102/105/107/108/109/110/113) | **G4 waitTime sai toàn bộ** | TB (multi-frame timing) | 30 phút |
| 102, 111 | — | `StartEvent=0` (mobile default) | PC `StartEvent=1, StartSkillId=398/112` (event chain) | **G6 — event chain chưa fire** | **Cao** (102→398 + 111→112) | 2-3 giờ |
| **Tất cả active 6** | — | `PcSkillTuningRegistry.CuiYan[ID]` cover 99/102/105/108/111/113 (6/13 active catalog) | PC có 7 active attack cần tuning (thiếu 101) | **G7 — Tuning coverage 86%** (thiếu ID 101, 15 phút) | Thấp | 15 phút |
| **Tất cả sub-skill reference** | — | Mobile **không register** 1063, 1064, 1065, 1093, 381, 336, 337, 338, 382, 398 | PC `addskilldamage1-4` references tất cả 10 ID này | **G7 — sub-skill 10 ID missing** (cần register + runtime support cho addskilldamage) | **Cao** (gameplay mất damage bonus) | 1-2 ngày |

### 2.7.3. Phase 1 quick wins (CRITICAL — childSkillId + passives)

> **Đây là phase quan trọng nhất** cho Thúy Yên. Sai childSkillId toàn bộ = 6 skill active (99, 102, 105, 108, 111, 113) không spawn đúng missile. 4 passive (95, 97, 100, 109) sai effect hoàn toàn = mastery băng → vật lý/tank.

- [ ] **ID 99** Phong Hoa Tuyết Nguyệt: sửa `childSkillId=70` → `6`; `charAnimId=2` → `11`; `waitTime=0` → `5`. (G4, 15 phút)
- [ ] **ID 101** Trị Liệu Thuật: sửa `childSkillId=5` → `13`; `charAnimId=2` → `11`; `waitTime=0` → `5`; `ManaReplenishV(100→450)` → `LifeReplenishV(130→700)` (theo PC `bingxin_qianying`); `cost 50` → `Link(lv,(1,21,""),(20,40,""))`. (G4 + G7, 1 giờ)
- [ ] **ID 102** Phong Quyển Tàn Tuyết: sửa `childSkillId=71` → `7`; `charAnimId=2` → `11`; `waitTime=0` → `5`; `ColdDamageV(30→300, 0, 40→400)` → `PhysicsDamageV(25→235, 0, 25→375)` (theo PC `fengjuan_canxue`); `cost 15` → `Link(lv,(1,20,""),(20,20,""))`. (G4 + G7, 1 giờ)
- [ ] **ID 105** Vũ Đả Lê Hoa: sửa `childSkillId=72` → `8`; `childSkillNum=1` → `4`; `charAnimId=2` → `11`; `waitTime=0` → `5`; `MslsGenerate=0` → `3`; `MslsGenerateData=0` → `10`; `PhysicsEnhanceP(10→100)` → `Link(lv,(1,10,""),(20,140,""))`. (G4, 30 phút)
- [ ] **ID 108** Mục Dã Lưu Tinh: sửa `childSkillId=73` → `9`; `charAnimId=2` → `11`; `radius=420` → `480`; thêm `PhysicsEnhanceP Link(lv,(1,30,""),(20,271,""))`; sửa `ColdDamageV(50→385)` → `Link(lv,(1,20,""),(20,246,""))` min, `Link(lv,(1,20,""),(20,426,""))` max; sửa `cost Link((1,20),(20,40))` → `Link((1,30),(20,40))`. (G4 + G7, 1 giờ)
- [ ] **ID 111** Bích Hải Triều Sinh: sửa `childSkillId=74` → `10`; `charAnimId=2` → `11`; `waitTime=0` → `1`; `ColdDamageV(40→350)` → `Link(lv,(1,43,""),(20,704,""))` min, `Link(lv,(1,43,""),(20,1214,""))` max; thêm `PhysicsDamageV Link(lv,(1,20,""),(20,200,""))`; sửa `cost 25` → `65`. (G4 + G7, 1 giờ)
- [ ] **ID 113** Phù Vân Tán Tuyết: sửa `childSkillId=75` → `12`; `charAnimId=2` → `11`; `waitTime=0` → `5`; **XÓA** `PhysicsEnhanceP(40→200)` (PC không có); sửa `ColdDamageV(20→200)` → `Link(lv,(1,40,""),(20,375,""))` min, `Link(lv,(1,40,""),(20,575,""))` max; sửa `cost 20` → `50`. (G4 + G7, 1 giờ)
- [ ] **ID 95** Thúy Yên Đao pháp: sửa `AddPhysicsDamageP(15→215)` → `13+7*level` (formula theo `cuiyan-daofa.lua::Getaddphysicsdamage_p`). (G7, 30 phút)
- [ ] **ID 97** Thúy Yên Song đao: thay toàn bộ — `AddPhysicsDamageP + DeadlyStrikeEnhanceP` → `AddColdMagicV Link(lv, (1, 13+7*level, ""), (20, 153, ""))` (theo `cuiyan-shuangdao.lua::Getaddphysicsdamage_p` trả về Param2String(13+7*level, -1, 5) — magic_v với time=-1, flag=5). **Critical: băng thành vật lý**. (G7, 1 giờ)
- [ ] **ID 100** Hộ Thể Hàn Băng: thay toàn bộ — `ColdResP + AddDefenseV` → `MeleeDamageReturnP(5→20, 18*120 dur) + RangeDamageReturnP(5→20, 18*120 dur)` (theo PC `huti_hanbing`); `cost 20` → `Link(lv,(1,40,""),(20,60,""))`; `charAnimId=2` → `11`; `radius=400` → `0`. (G4 + G7, 1 giờ)
- [ ] **ID 103** Thiên Lý Băng Phong: **VERIFY FIRST** — PC `taxue_wuhen` empty trong cuiyan.lua nhưng `taxue-wuhen.lua` có `fastwalkrun_p(15+2*level, time=1080+135*level)`. Nếu 103 thực sự là move speed → thay toàn bộ. Có thể khác 109 (cũng có buff). Ưu tiên check với `PcSkills.txt` column `SkillDesc`. (G7, 1 giờ verify + 30 phút fix)
- [ ] **ID 109** Tuyết Ảnh: thay toàn bộ — `AllResP + AddDefenseV` → `AttackSpeedV Link(lv,(1,12,""),(20,65,""))` + `FastWalkRunP Link(lv,(1,17,""),(20,55,""))` (theo PC `xueying`); `cost 30` → `Link(lv,(1,40,""),(20,140,""))`; `charAnimId=2` → `11`; `waitTime=0` → `5`; `radius=400` → `0`. (G4 + G7, 1 giờ)
- [ ] **ID 114** Băng Cốt Tuyết Tâm: thêm 5 attributes bị thiếu — `AddColdMagicV(60→315)` + `AddPhysicsMagicV(30→275)` + `DeadlyStrikeEnhanceP(5→45 Conic)` + `FastHitRecover(5→49)` + `ColdEnhanceP(8→80)` + `LifeMaxP(21→20)`; sửa `AddColdDamageV(20→200)` → `(30→275)`. (G7, 1 giờ)
- [ ] **ID 101 Tuning** (registry thiếu): thêm `[101] = new[] { (1, 400), (20, 400) }` vào `PcSkillTuningRegistry.CuiYanId`. (G7, 5 phút)

### 2.7.4. Phase 3 dash (G1)

- [ ] **Không áp dụng** — Thúy Yên không có skill dash/melee-jump. Cả `KNpc.cpp::CastMeleeSkill` switch (line 1834) và `ModSkills.txt` (IsMelee=0 cho toàn bộ ID 95-114) đều không tham chiếu `Melee_Jump/JumpAndAttack/RunAndAttack`. Toàn bộ active skill là Ranged + Missiles, không phải dash. Phase 3 bỏ qua cho Thúy Yên.

### 2.7.5. Phase 4 event chain (G6) + Missing catalog (G)

- [ ] **G6 — ID 102 → 398 StartEvent**: trong `SkillEffectVisualService.PlaySkillCast` cho 102, sau khi missile spawn, gọi `SpawnStartEvent(fx, 398)` — fire sub-skill 398 ngay khi cast. PC `fengjuan_canxue` không có `skill_startevent` trong cuiyan.lua, nhưng ModSkills.txt cột StartEvent=1, StartSkillId=398. Cần verify sub-skill 398 có tồn tại trong `Missles.txt` (có thể là sub-missile). Effort: 2 giờ
- [ ] **G6 — ID 111 → 112 StartEvent + Add to catalog**: tạo `BichHaiTrieuSinhB(112)` (child=11, num=16, charAnimId=11, MslsGenerate=5, MslsGenerateData=1, SkillMissileForm.Surround, targetEnemy). Trong `PlaySkillCast` cho 111, gọi `SpawnStartEvent(fx, 112)` ngay khi cast — fire 16-missile AOE. PC `bihai_chaosheng` không có `skill_startevent` trong cuiyan.lua, nhưng ModSkills.txt cột StartEvent=1, StartSkillId=112. Effort: 2-3 giờ (cần test 16-missile AOE pattern)
- [ ] **G — ID 96, 98**: tạo 2 passive mastery alt-variant (theo schema SkillStyle=3, charAnimId=14, không LvlSetting — chỉ metadata). Có thể map AddPhysicsDamageP hoặc AddColdMagicV tùy variant. Effort: 1 giờ
- [ ] **G — ID 104 Băng Hồn**: tạo passive (SkillStyle=3, charAnimId=14). PC không có LvlSetting active (chỉ metadata). Effort: 30 phút
- [ ] **G — ID 106 Băng Tung Vô Ảnh 111**: tạo active (SkillStyle=4, AttackRadius=400, MslsGenerate=15, MslsForm=1, childSkillId=chưa biết, charAnimId=11). PC `taxue_wuhen` empty trong cuiyan.lua — cần check `cuiyan-150/120.lua` hoặc tương đương. Nhiều khả năng đây là teleport/blink 150-tier (Phase 5) chứ không thuộc range 95-114. Effort: 2 giờ (nếu Phase 5 → skip)
- [ ] **G — ID 107 Nhiếp Tâm Thuật**: tạo active (child=6, num=1, AttackRadius=180, LRSkill=2, WaitTime=5, charAnimId=11). Effort: 1 giờ
- [ ] **G — ID 110 Ngũ hành độn**: tạo active (child=6, num=1, AttackRadius=180, LRSkill=2, WaitTime=5, charAnimId=11). Effort: 1 giờ
- [ ] **G — sub-skills 1063, 1064, 1065, 1093, 381**: tạo 5 sub-skill entry (Băng Tước Hoạt Kú, Băng Ngưng Hàn Yên, Thủy Anh Man Tố, etc.) trong catalog. Cần check Vietnamese MOD source để biết chính xác LvlSetting. Effort: nửa ngày (5 skill + verify với `ModSkills.txt` 1000+ range)
- [ ] **G — missile refs 336, 337, 338, 382, 398**: register missile trong `Missles.txt` reference + `SkillEffectVisualService` nếu cần visual riêng. Effort: 2-3 giờ (5 missile + visual + test addskilldamage runtime)

### 2.7.6. Trạng thái

- [x] Catalog scan xong (19 skill PC: 4 passive mastery + 2 passive ## + 1 heal + 1 state buff + 6 active attack + 1 teleport + 2 ranged self + 1 ranged AOE 16-missile + 1 ranged 13-skill; mobile: 13 skills — thiếu 96/98/104/106/107/110/112 = 7 IDs)
- [ ] Quick-win phase merged (Phase 1 — 13 items, priority: childSkillId sửa 6 skill + passive 95/97/100/109/114 + tuning coverage)
- [ ] Dash phase merged: **không áp dụng** (Thúy Yên không có dash)
- [ ] Event chain phase merged (Phase 4 — 2 items, 102→398 + 111→112)
- [ ] Missing catalog merged (7 IDs + 5 sub-skills + 5 missile refs) (Phase 4)
- [ ] Tuning coverage 86% → 100% (chỉ thiếu ID 101, fix 5 phút)

### 2.7.7. Tổng kết

- **Skill chính ưu tiên sửa** (theo thứ tự):
  1. **99, 102, 105, 108, 111, 113** (6 active attack) — sửa childSkillId 70-75 → 6-12 (PC). Một sửa mở ra toàn bộ missile chain. **Effort: 1 giờ tổng (6 dòng)**
  2. **105 Vũ Đả Lê Hoa** — sửa childSkillNum 1→4 + MslsGenerate 0→3 (mất 4-hit + multi-missile). **Effort: 30 phút**
  3. **95, 97, 100, 109, 114** (5 passive mastery) — sửa effect: cold magic / damage return / attackspeed / 7-attribute. **Effort: 4 giờ tổng**
  4. **101 Trị liệu thuật** — sửa ManaReplenish → LifeReplenish (heal sai thành mana regen). **Effort: 1 giờ**
  5. **108, 111, 113** (damage curve) — sửa colddamage_v L20 sai lớn + bỏ physicsenhance_p. **Effort: 2 giờ tổng**
- **Skill event chain** (G6 — quan trọng): 111→112 missing catalog → event chain dead. Cần tạo sub-skill 112 trước (16-missile AOE), rồi wire StartEvent vào `PlaySkillCast` cho 111. Tương tự 102→398 (sub-skill 398, 1-missile sub). **Effort: nửa ngày**
- **Skill passive sai data nghiêm trọng**: 97 (cold magic → physics dmg), 100 (damage return → cold res + def), 109 (atk speed + move speed → all res + def). Fix để Thúy Yên cảm giác đúng là "song đao băng" thay vì "tank vật lý".
- **Sub-skills MOD missing** (5 IDs: 1063, 1064, 1065, 1093, 381): referenced trong 6 active skill `addskilldamage1-4`. Cần register catalog + runtime support. Nếu runtime không hỗ trợ addskilldamage pattern, gameplay sẽ thiếu damage bonus từ 5 sub-skill này.
- **Test plan**: unit test cho childSkillId resolution (cast 99 → expect missile ID 6 spawn); visual test cast 105 ở close range (expect 4 hit pattern); regression test cast 111 với StartSkillId 112 (expect 16 AOE missile + 1 main); verify passive 100 (cast, take melee/range damage, expect 5-20% return).
- **Không cần làm dash** (Thúy Yên 100% IsMelee=0, không có Melee_Jump).
- **Phase 5 (future)**: port 150-tier sub-form (9 entries: bingzong_wuying, bingxin_yuling, daocuiyan150, daocuiyan150_2, bingxin_xuelian, bingxin_xianzi, fengxue_bingtian, neicuiyan150, neicuiyan150_2) + 120-tier (cuiyan120 hide skill). Ngoài range 95-114, cần check ID trong `PcSkills.txt` (có thể 700+ hoặc 1500+).

## 2.4. Đường Môn (PC: TangMen 唐门, ID range 43-58)

> **Nguồn PC**:
> - `Assets/StreamingAssets/Reference/PcSkills.txt` (canonical, TCVN3, 8 active + 2 buff + 1 mastery + 1 resist + 1 passive = 10 skill ID 43-58)
> - `/var/www/vltksource_new/vl_update_27/Server 6.0/.../script/skill/tangmen.lua` (GB18030, 749 dòng, 25 skills kể cả 80/150-tier)
> - Per-skill files: `script/skill/tangmen/*.lua` (2 file pinyin) + `script/skill/tangmeng/*.lua` (19 file GB2312 bao gồm trùng lặp)
> - C++: `KNpc.cpp::CastMeleeSkill` switch (line 1834) — **không có** TangMen skill nào thuộc `Melee_Jump/JumpAndAttack/RunAndAttack` (tất cả `IsMelee=0`, thuần ranged). Do đó **G1 (dash) không áp dụng** cho Đường Môn.
> - Tóm tắt: Đường Môn là môn phái **ranged ám khí / poison thuần**, 5 active attack (45/47/50/54/58) đều dùng `PcSkillStyle.Missiles` + `childSkillId/childSkillNum` cho sub-missile. Gap nặng nhất: **G4 (waitTime=5 bị bỏ ở 5/5 attack)** + **G4 (req level sai ID 47/54)** + **G4 (ID 54 missile form sai Single vs Fan)** + **G4 (ID 50 MslsGenData=4 bị bỏ)** + **G6 (ID 58 CollideEvent 1→227 thiếu)** + **G7 (radius flat curve sai PC, lighting res formula sai PC cho 51)**.

### 2.4.1. Catalog scan

| ID | Tên (mobile + PC) | PC Skills.txt cols | PC tangmen.lua | Vai trò |
|---:|---|---|---|---|
| 43 | Đường Môn Ám Khí (唐门暗器) | `ReqLevel=10, MaxLevel=20, IsMelee=0, CharAnimId=0, Form=7, ChildId=0` | `tangmen_anqi` (passive) | Buff `addphysicsdamage_p` 25→215 + atk rating 7 |
| 45 | Tích Lịch Đơn (霹雳弹) | `ReqLevel=10, MaxLevel=20, IsMelee=0, CharAnimId=11, Form=1, ChildId=35, ChildNum=1, Wait=5, Cost=0, Time=0, EqtLimit=-2, HorseLimit=0` | `pili_dan` (VanishEvent 1113 L20, ShowEvent 8) | Active missile + poison V 1→5 + 9 addskilldamage |
| 47 | Đoạt Hồn Tiêu (夺魂镖) | **`ReqLevel=30` (mobile: 10), MaxLevel=20, IsMelee=0, CharAnimId=11, Form=1, ChildId=116, ChildNum=1, Wait=5, EqtLimit=100 (Phi tiêu), HorseLimit=1** | `duohun_biao` (rad 384→448, speed 24→28, cost 5→16) | Active missile + poison V 3→8 + deadly 2→12 |
| 48 | Tâm Nhãn (心眼) | `ReqLevel=60, MaxLevel=30, IsMelee=0, CharAnimId=14, Form=7, ChildId=0` | `xinyan` (addcold 10→110, addpoison 1→10, addphys 15→115, deadly 8→26, atkspd 29→106) | Passive mastery (5 buff gộp) |
| 50 | Truy Tâm Tiễn (追心箭) | `ReqLevel=30, MaxLevel=20, IsMelee=0, CharAnimId=11, Form=1, ChildId=37, ChildNum=2, **MslsGen=2, MslsGenData=4**, Wait=5, EqtLimit=101 (Phi đao), HorseLimit=1` | `zhuixin_jian` (rad 384→448, speed 24→28, cost 20) | Active 2 base + 2 gen + 2 gen = 6 missile, poison 3→8 |
| 51 | Thanh Mộc (青木) | `ReqLevel=30, MaxLevel=20, IsMelee=0, CharAnimId=14, Form=7, ChildId=0` | `青木功.lua` `lightingres_p = floor(log10(level+1)/2*60)` | Passive resist lightning |
| 54 | Mạn Thiên Hoa Vũ (漫天花雨) | **`ReqLevel=30` (mobile: 50), MaxLevel=20, IsMelee=0, CharAnimId=11, Form=6 (Fan), ChildId=38, ChildNum=1, Wait=5, EqtLimit=102, HorseLimit=1** | `mantian_huayu` (rad 384→416, cost 40 flat) | Active Fan missile + poison 3→8 |
| 55 | Thối Độc Thuật (淬毒术) | `ReqLevel=40, MaxLevel=20, IsMelee=0, CharAnimId=11, Form=7, ChildId=0` | per-skill file `tangmeng/淬毒术.lua` (addpoison 2→25, dur 1200-15600) | Self/ally poison buff (no missile) |
| 57 | Băng Phách Hàn Quang (冰魄寒光) | `ReqLevel=50, MaxLevel=20, IsMelee=0, CharAnimId=11, Form=7, ChildId=0` | per-skill file `tangmeng/冰附加.lua` (addcold 2→25, dur 1200-15600) | Self/ally cold buff (no missile) |
| 58 | Thiên La Địa Võng (天罗地网) | `ReqLevel=60, MaxLevel=20, IsMelee=0, CharAnimId=11, Form=1, ChildId=67, ChildNum=1, Wait=5, EqtLimit=102, HorseLimit=0`, **`CollideEvent=1, CollidSkillId=227` (PC: Vạn Lý Truy Tâm tiểu phi đao)**, `Param1=0` | `tianluo_diwang` + `tianluo_diwang1` (rad 448→512, cost 45→65, speed 26→28) | Active single missile + collide sub 227 |

**Không catalog trong mobile** (PC tangmen.lua có, mobile thiếu — Phase 5):
- **80-tier sub-skill (player)**:
  - 249 (Tiểu Lý Phi Đao 小李飞刀) — `ReqLevel=60, EqtLimit=101, Cost=50, rad=350, ChildId=106, Wait=5, CharAnim=11, Attrib=302, Icon=\spr\Ui\技能图标\icon_sk_小李飞刀.spr`
  - 250 (Tiểu Lý Phi Đao pc variant) — same icon
  - 302 (Bão Vũ Lê Hoa 暴雨梨花) — `ReqLevel=80, EqtLimit=102, Cost=0, rad=470, ChildId=96, FlyEvent=301 L20, FlyEventTime=30, Form=6, CharAnim=11` — có `skill_flyevent` chain
  - 340 (Ngân Đao Xạ Nguyệt 银刀射月) — `ReqLevel=80, EqtLimit=101, rad=400, ChildId=150`
  - 341 (Tản Hoa Tiêu 散花镖) — `ReqLevel=60, EqtLimit=100, ChildNum=5, rad=400, Form=2 (Fan)`
  - 342 (Cửu Cung Phi Tinh 九宫飞星) — `ReqLevel=80, EqtLimit=100, ChildNum=5, rad=360, Form=2 (Fan), CharAnim=11`
- **150-tier sub-skill (player)**:
  - 1069 (Vô Ảnh Xuyên 无影穿) — `ReqLevel=150, EqtLimit=101, rad=360, ChildId=331, ChildNum=1, Wait=3, CollidSkillId=1097` (sub 1097 = Truy Tâm Tọa Mệnh)
  - 1070 (Thiết Liên Tứ Sát 铁链四杀) — `ReqLevel=150, EqtLimit=102, rad=470, ChildId=332, ChildNum=1, Wait=5, **FlyEvent=1, FlySkillId=1098, FlyEventTime=18**, Form=6` (sub 1098 = Thiết Sa Xạ Tinh, fire mỗi 18 tick bay)
  - 1071 (Càn Khôn Nhất Trích 乾坤一掷) — `ReqLevel=150, EqtLimit=100, rad=360, ChildId=333, ChildNum=5, Wait=2, Form=2 (Fan), MslsGen=5`
  - 1097, 1098, 1099, 1100 (sub-evt visual / chain)

**IsTangMenSkill exclude**: 44, 46, 49, 52, 53, 56 không thuộc Đường Môn (ModSkills.txt confirm các ID này là shared/common skill khác). Không phải gap.

### 2.4.2. Gap table

> **Chú thích cột "Hành vi mobile"**: lấy từ `CreateTangMenSkills` trong `PcCombatCatalogFactory.cs` line 1073-1230. Mọi skill attack Đường Môn dùng `PcSkillStyle.Missiles` + `childSkillId` (sub-missile sprite) + `childSkillNum` (số lượng missile).
>
> **Chú thích cột "Hành vi PC"**: lấy từ `PcSkills.txt` (ReqLevel, ChildId, ChildNum, MslsGen, MslsGenData, CharAnimId, WaitTime, CostValue, EqtLimit, HorseLimit, CollideEvent) + `tangmen.lua` (damage curves, radius) + per-skill files trong `tangmen/` + `tangmeng/`.

| ID | Tên | Hành vi mobile | Hành vi PC | Gap | Severity | Effort |
|---:|---|---|---|---|---|---|
| 43 | Đường Môn Ám Khí | `BaseSkill(43, ..., 10, 20, 0, ...)` + `AddPhysicsDamageP Link(lv,(1,25,""),(20,215,""))` + atk rating 7 | `tangmen_anqi` — `addphysicsdamage_p={{1,25},{20,215}},{{1,-1},{2,-1}},{{1,7},{2,7}}` | **OK** (mobile set `d.state.Add(...,AddPhysicsDamageP,...,7)` đúng cả 3 tuple `[1][2][3]`) | — | — |
| **45** | Tích Lịch Đơn | `BaseSkill(45, ..., 10, 20, 400, Single)`; `childSkillId=35, childSkillNum=1`; `charAnim=11`; `phys 20→80, poison V 1→5/60s/10, deadly 1→8, series 1→10, cost=12`; **`s.waitTime` KHÔNG set** (default 0) | `pili_dan` — `physicsenhance_p 20→80, poisondamage_v 1→5 (60s dur 10), deadlystrike_p 1→8, seriesdamage_p 1→10, skill_cost_v 12 flat` + **`WaitTime=5`** + `VanishedEvent 1,1113` (vanish sub-skill 1113 = Tích Lịch Loạn Hoàn Hãm Tĩnh, level-gated) + `ShowEvent 8` | **G4 waitTime=5 bị bỏ** (cast cadence sai PC: mỗi missile fire sau 5 PC tick thay vì instant). **`G6 VanishedEvent 1113 bị bỏ`** (missile 45 fire sub-skill 1113 khi vanish — visual explosion chain) + `ShowEvent 8` bị bỏ. | Cao (cadence + visual chain mất) | 2 giờ |
| **47** | Đoạt Hồn Tiêu | `BaseSkill(47, ..., 10, 20, 450, Single)`; `childSkillId=116, childSkillNum=1`; `phys 25→115, poison 3→8, deadly 2→12, series 5→30, cost Link(1,5,20,16)`; **`s.waitTime` KHÔNG set**; `req=10`, `horseLimit=0` (mặc định) | `duohun_biao` — `physicsenhance_p 25→115, poisondamage_v 3→8, deadlystrike_p 2→12, seriesdamage_p 5→30, skill_cost_v 5→16, missle_speed_v 24→28, skill_attackradius 384→448` + **`ReqLevel=30` (mobile sai: 10)** + **`WaitTime=5`** + **`EqtLimit=100 (Phi tiêu)`** (mobile không set) + **`HorseLimit=1`** (mobile không set) + `CharAnimId=11` ✓ | **G4 req 10 vs PC 30** (cao hơn 20 level sai, gameplay progression sai) + **G4 waitTime=5 bị bỏ** + **G4 horseLimit=0 vs PC 1** (player cưỡi ngựa dùng được Đoạt Hồn Tiêu trên PC, mobile cấm) + **G7 EqtLimit 100 (Phi tiêu) bị bỏ** (không enforce vũ khí) + radius hardcode 450 vs PC curve 384→448 | Cao (req sai = progression broken + weapon check miss) | 1 giờ |
| 48 | Tâm Nhãn | `BaseSkill(48, ..., 30, 30, 0, ...)`; passive 5-attr: addcold 10→110 (∞), addpoison 1→10/10t (∞), addphys 15→115/7 (∞), deadly 8→26 (∞), atkspd 29→106 (∞) | `xinyan` — `addcolddamage_v 10→110, addpoisondamage_v 1→10 (10 dur), addphysicsdamage_p 15→115 (7), deadlystrikeenhance_p 8→26, attackspeed_v 29→106 → 33→113 → 35→149 → 38→156 → 39→159 → 40→162` + **`lifemax_yan_p 21→20 (L35-36)`** (bị bỏ) | **G4 attackspeed_v curve thiếu điểm giữa 30/35/38/40** (PC có nhiều breakpoint) + **G4 lifemax_yan_p 21→20 L35-36 bị bỏ** (smoke bonus) | Trung bình | 1 giờ |
| **50** | Truy Tâm Tiễn | `BaseSkill(50, ..., 30, 20, 360, Single)`; `childSkillId=37, childSkillNum=2`; `missilesGenerateData=0` (mặc định) | `zhuixin_jian` — `ChildNum=2, MslsGen=2, MslsGenData=4` (PC: 2 base missiles + 2 generated missiles × 4 = 8 sub-missile slots, runtime sinh 2-4 thêm) + `WaitTime=5` + `EqtLimit=101` + `HorseLimit=1` | **G4 MslsGenData=4 bị bỏ** (PC: missile 50 generate thêm 4 sub-missile sau khi va chạm — mobile chỉ fire 2 base) + **G4 waitTime=5 bị bỏ** + **G7 EqtLimit 101 (Phi đao) bị bỏ** + **G4 horseLimit=0 vs PC 1** | Cao (mất cảm giác 6-8 phi tiêu "truy tâm") | 2 giờ |
| **51** | Thanh Mộc | `PassiveResist(51, ..., 30, LightingResP)` — formula `Floor(Log10(lv+5)/2*50)` | `tangmeng/青木功.lua` — formula `floor(log10(level+1)/2*60)` | **G7 lighting res formula sai PC** (mobile L1: `Floor(Log10(6)/2*50)=19`, PC L1: `floor(log10(2)/2*60)=9` → 2.1× sai; mobile L20: `Floor(Log10(25)/2*50)=34`, PC L20: `floor(log10(21)/2*60)=39` → 13% sai). Sai vì dùng generic `PassiveResist` helper thay vì PC per-skill formula. | Trung bình (resistance sai) | 1 giờ (override formula cho ID 51) |
| **54** | Mạn Thiên Hoa Vũ | `BaseSkill(54, ..., 50, 20, 400, Single)`; `childSkillId=38, childSkillNum=1` | `mantian_huayu` — `Form=6 (Fan)`, `ReqLevel=30`, `EqtLimit=102`, `HorseLimit=1`, `WaitTime=5`, `skill_attackradius 384→416` | **G4 req 50 vs PC 30** (cao hơn 20 level sai) + **G4 missile form Single vs Fan (6)** (mất hiệu ứng "mưa hoa rải đều") + **G4 waitTime=5 bị bỏ** + **G7 EqtLimit 102 bị bỏ** + **G4 horseLimit=0 vs PC 1** + **G4 EventSkillLevel=1 bị bỏ** (PC dùng `EvtLvl=1`, mobile không set) | Cao (form Fan mất = cốt lõi "mạn thiên hoa vũ" tan tành) | 2 giờ |
| 55 | Thối Độc Thuật | `BaseSkill(55, ..., 30, 20, 400, Surround)`; `targetSelf=true, targetAlly=true`; `effectSourceId=\spr\skill\天忍\mag_tr_16_施魔法.spr`; `addpoison 2→25 dur 1200-15600` | per-skill `tangmeng/淬毒术.lua` — `addpoisondamage_v 2→25, dur 1200 + 1200*lv, time 10` + `skill_cost_v 20` | **OK** (mobile matches PC formula) | — | — |
| 57 | Băng Phách Hàn Quang | `BaseSkill(57, ..., 30, 20, 400, Surround)`; `addcold 2→25 dur 1200-15600` | per-skill `tangmeng/冰附加.lua` — `addcolddamage_v 2→25, dur 1200 + 1200*lv, time 0` + `skill_cost_v 20` | **OK** (mobile matches PC formula) | — | — |
| **58** | Thiên La Địa Võng | `BaseSkill(58, ..., 50, 20, 520, Single)`; `childSkillId=67, childSkillNum=1`; `cost Link(1,45,20,65)` | `tianluo_diwang` — `ReqLevel=60, EqtLimit=102, HorseLimit=0, WaitTime=5, EventSkillLevel=1, **CollideEvent=1, CollidSkillId=227** (PC: Vạn Lý Truy Tâm tiểu phi đao — sub-skill 227 fire khi missile 58 va chạm NPC) | **G4 req 50 vs PC 60** (thấp hơn 10 level sai) + **G4 waitTime=5 bị bỏ** + **G4 EventSkillLevel=1 bị bỏ** + **G7 EqtLimit 102 bị bỏ** + **`G6 CollideEvent 1→227 bị bỏ`** (mất sub-skill 227 = Vạn Lý Truy Tâm visual) | Cao (mất CollideEvent chain + req sai + weapon check miss) | 2 giờ |
| **Tất cả 5 attack (45/47/50/54/58)** | — | `s.waitTime` KHÔNG set (default 0). So sánh `DamageSkillNew` factory (line 523-528) có set `s.waitTime = 5; s.timePerCast = 2;` | PC: tất cả 5 attack đều `WaitTime=5` (5 PC tick ~ 280ms giữa mỗi missile trong cùng cast). PC: `TimePerCast=0` cho ranged (không set ở ranged, mobile 0 ok) | **G4 waitTime=5 bị bỏ ở 5/5 attack skills** — cadence sai PC. `DamageSkillNew` factory đã set, nhưng `TangMenXxx()` factories không set. Pattern tương tự `TianWang` (cùng thiếu waitTime). | Cao (cast cadence sai toàn bộ Đường Môn) | 30 phút (sửa 5 dòng) |
| **Tất cả 5 attack (45/47/50/54/58)** | — | `s.horseLimit` KHÔNG set (default 0) | PC: 45=0, 47=1, 50=1, 54=1, 58=0 | **G4 horseLimit thiếu 3/5 attack** (47/50/54 sai — player cưỡi ngựa sẽ không dùng được skill trên mobile, dù PC cho phép) | Trung bình | 15 phút |
| **Tất cả 5 attack (45/47/50/54/58)** | — | `s.missilesGenerateData` KHÔNG set (default 0) | PC: 45=0, 47=0, **50=4 (MslsGenData=4)**, 54=0, 58=0 | **G4 missilesGenerateData thiếu 1/5** (ID 50 quan trọng nhất) | Trung bình (50 only) | 15 phút |
| **Tất cả 5 attack (45/47/50/54/58)** | — | Radius hardcode 400/450/360/400/520, registry flat `(1,X),(20,X)` | PC curves: 45 default 400, 47 `{{1,384},{20,448}}`, 50 `{{1,384},{20,448}}`, 54 `{{1,384},{20,416}}`, 58 `{{1,448},{20,512}}` | **G7 — RadiusCurves[TangMenId] flat 5/5 sai PC** (PC có curve, registry hardcode 1 mức). L1 sai 4-17% tuỳ skill. | Trung bình (radius lệch nhẹ) | 30 phút (sửa registry 5 dòng) |
| **Tất cả 5 attack** | — | `PcSkillTuningRegistry.RadiusCurves[TangMenId]` cover 5/5 active (100%) | PC có 5 active cần tuning | **OK coverage 100%** — chỉ vấn đề là curves flat, không phải coverage | — | — |
| **Tất cả 5 attack** | — | `s.ChildSkillLevel` KHÔNG set (default 0) | PC: 45/47/50/54/58 đều `ChildSkillLevel=-1` (lấy level của parent). Mobile mặc định 0 → có thể gây sai khi tính damage sub-missile | **G7 — ChildSkillLevel=-1 bị bỏ 5/5** (sub-missile damage sẽ dùng L1 thay vì L_parent — sai damage) | Trung bình (sub-damage sai) | 15 phút (set `s.childSkillLevel = -1` cho 5 skill) |
| **ID 50 mobile `radius=360`** | `BaseSkill(50, ..., 30, 20, 360, Single)` | PC: `skill_attackradius={{1,384},{20,448}}` (L1=384, L20=448) | **G4 radius 360 flat vs PC 384→448** (sai 6.3% L1, 19.6% L20) | Trung bình | (covered ở row radius curves) |
| **ID 50 mobile `MslsGenData=0`** | (default) | PC: `MslsGen=2, MslsGenData=4` | **G4 MslsGenData=4 bị bỏ** (mobile chỉ spawn 2 base, PC spawn 2 base + 2 gen + 2 gen = 6) | (covered ở row missilesGenerateData) |
| **ID 58 mobile `ColEvt` missing** | (không set) | PC: `CollideEvent=1, CollidSkillId=227` (Vạn Lý Truy Tâm tiểu phi đao — sub-skill 227 với 1 sub-missile `MslsGen=0, ChildId=36, ChildNum=1` fire khi missile 58 va chạm NPC) | **G6 — CollideEvent chain 58→227 bị bỏ** (mất visual + damage 2nd wave "Vạn Lý Truy Tâm") | (covered ở row ID 58) |

### 2.4.3. Phase 1 quick wins (5 attack skills — casting cadence + weapon check + req level)

> **Phase này ưu tiên sửa 5 attack skills (45/47/50/54/58) cùng lúc** — pattern giống nhau. Toàn bộ là edit trong `TangMenPiLiDan()` / `TangMenDuoHunBiao()` / `TangMenZhuiXinJian()` / `TangMenManThienHoaVu()` / `TangMenThienLaDiaVong()` factories.

- [ ] **Tất cả 5 attack — set `s.waitTime = 5`**: sửa 5 factory methods thêm `s.waitTime = 5;` sau `s.targetEnemy = true;` (PC `WaitTime=5` cho 5/5 attack — cadence sai toàn bộ Đường Môn). (G4, 15 phút)
- [ ] **Tất cả 5 attack — set `s.childSkillLevel = -1`**: thêm `s.childSkillLevel = -1;` để sub-missile damage lấy level của parent (PC `ChildSkillLevel=-1`). (G7, 15 phút)
- [ ] **ID 47**: sửa `BaseSkill(47, ..., 10, ...)` → `..., 30, ...` (PC ReqLevel=30). Thêm `s.horseLimit = 1;` (PC HorseLimit=1). Effort 15 phút. (G4)
- [ ] **ID 47 — EqtLimit 100 (Phi tiêu)**: thêm field `s.weaponType = 100;` hoặc tương đương vào `SkillDefinition` (cần xem `SkillDefinition.cs` field schema — hiện chưa thấy field EqtLimit). Nếu không có field, đăng ký vào `PcSkillTuningRegistry.WeaponRequirements[TangMenId][47] = 100`. (G7, 1 giờ nếu cần refactor schema)
- [ ] **ID 50**: thêm `s.missilesGenerateData = 4;` (PC MslsGenData=4). Thêm `s.horseLimit = 1;`. Effort 15 phút. (G4)
- [ ] **ID 50 — EqtLimit 101 (Phi đao)**: tương tự ID 47. (G7, 30 phút)
- [ ] **ID 54**: sửa `BaseSkill(54, ..., 50, 20, 400, SkillMissileForm.Single)` → `..., 30, 20, 400, SkillMissileForm.Fan` (PC ReqLevel=30, Form=6 Fan). Thêm `s.horseLimit = 1;` + `s.eventSkillLevel = 1;`. Effort 15 phút. (G4)
- [ ] **ID 54 — EqtLimit 102**: tương tự ID 47. (G7, 30 phút)
- [ ] **ID 58**: sửa `BaseSkill(58, ..., 50, ...)` → `..., 60, ...` (PC ReqLevel=60). Thêm `s.eventSkillLevel = 1;`. Effort 15 phút. (G4)
- [ ] **ID 58 — EqtLimit 102**: tương tự ID 47. (G7, 30 phút)
- [ ] **Tất cả radius curves** (registry `PcSkillTuningRegistry.cs` line 53-60): sửa curves flat thành PC curves:
  - 45: `[45] = new[] { (1, 400), (20, 400) }` ✓ (PC default, OK)
  - 47: `[47] = new[] { (1, 384), (20, 448) }` (PC 384→448)
  - 50: `[50] = new[] { (1, 384), (20, 448) }` (PC 384→448)
  - 54: `[54] = new[] { (1, 384), (20, 416) }` (PC 384→416)
  - 58: `[58] = new[] { (1, 448), (20, 512) }` (PC 448→512)
  Effort 15 phút. (G7)
- [ ] **ID 51 Thanh Mộc formula override**: thêm helper mới hoặc override trong `TangMenThanhMoc()` (line 1163-1166) dùng PC formula `floor(log10(level+1)/2*60)` thay vì generic `PassiveResist`. Effort 1 giờ. (G7)
- [ ] **ID 48 Tâm Nhãn — bổ sung attackspeed_v curve 30/35/38/40 + lifemax_yan_p 21→20**: sửa `AddLevels` thêm breakpoints `(33, 113, ""), (35, 149, ""), (38, 156, ""), (40, 162, "")` cho `MagicAttributeKind.AttackSpeedV`. Thêm `MagicAttributeKind.LifeMaxYanP` với `Link(lv, (1, 21, ""), (35, 20, ""), (36, 20, ""))`. Effort 1 giờ. (G4)

### 2.4.4. Phase 3 dash (G1)

- [ ] **Không áp dụng** — Đường Môn không có skill dash/melee-jump. Cả `KNpc.cpp::CastMeleeSkill` switch (line 1834), `PcSkills.txt` (`IsMelee=0` cho tất cả 43-58), và `tangmen.lua` đều không tham chiếu `Melee_Jump/JumpAndAttack/RunAndAttack` cho ID 43-58. Toàn bộ 5 active skill là ranged pure missile, không phải dash. Phase 3 bỏ qua cho Đường Môn. **Tương tự Võ Đang / Thiên Vương.**

### 2.4.5. Phase 4 event chain (G6)

- [ ] **ID 45 Tích Lịch Đơn — VanishedEvent 1→1113** (PC `skill_vanishedevent [1]={{1,1},{20,1}}, [3]={{1,1113},{20,1113}}`): thêm `s.vanishedSkillId = 1113; s.vanishedEvent = 1;` vào `TangMenPiLiDan()`. Trong `SkillEffectVisualService.ConfigureTangMenVisuals` (line 585), thêm handler `case 45:` → `SetupPcVanishSubEffect(fx, 1113)` (fire sub-skill 1113 = Tích Lịch Loạn Hoàn Hãm Tĩnh visual khi missile 45 vanish). ShowEvent 8 (animation id 8) cũng cần map. Effort 2 giờ.
- [ ] **ID 58 Thiên La Địa Võng — CollideEvent 1→227** (PC `skill_collideevent [1]={{1,0},{10,0},{10,1},{20,1}}, [3]={{1,227},{20,227}}`): thêm `s.collideSkillId = 227; s.collideEvent = 1;` vào `TangMenThienLaDiaVong()`. Trong `SkillEffectVisualService.ConfigureTangMenVisuals`, thêm handler `case 58:` → `SpawnCollideSubEffect(fx, mp)` cho sub-skill 227 (Vạn Lý Truy Tâm — `physicsenhance_p=tianluo_diwang1, missle_speed_v=tianluo_diwang1` = `physicsenhance_p={{1,40},{20,120}}` half-damage từ parent). Effort 2 giờ.
- [ ] **Verify** CollideEvent fire đúng level: PC `skill_collideevent [1]={{1,0},{10,0},{10,1},{20,1}}` — fire L11+ only. Nếu cần gate, dùng pattern tương tự 357/389 (line 146-155 trong `CombatRuntimeService`). Effort 30 phút verify.

### 2.4.6. Trạng thái

- [x] Catalog scan xong (10 skill: 4 passive/buff/resist, 6 active 5 missile + 1 passive 5-attr)
- [ ] Quick-win phase merged (Phase 1 — 14 items, priority: `waitTime=5` 5/5 + req level 47/54/58 + `MslsGenData=4` ID 50 + Fan form ID 54 + EqtLimit 4/5 + radius curves 5/5 + ID 51 formula + ID 48 curve bổ sung)
- [ ] Dash phase merged: **không áp dụng** (Đường Môn không có dash)
- [ ] Event chain phase merged (Phase 4 — 2 items, ID 45 VanishedEvent→1113 + ID 58 CollideEvent→227)
- [ ] Tuning coverage 100% (5/5 radius curves) — chỉ thiếu chất lượng curves (flat → PC curve)

### 2.4.7. Tổng kết

- **Skill chính ưu tiên sửa** (cao nhất):
  1. **ID 54 Mạn Thiên Hoa Vũ** — sai MissileForm (Single vs Fan) là mất cốt lõi "mưa hoa rải đều" + sai req level. 15 phút fix.
  2. **ID 50 Truy Tâm Tiễn** — mất `MslsGenData=4` (chỉ spawn 2 base thay vì 6). 15 phút fix.
  3. **ID 58 Thiên La Địa Võng** — mất CollideEvent→227 (Vạn Lý Truy Tâm visual) + sai req level. 30 phút fix (event chain cần `SkillEffectVisualService` integration).
- **Skill ưu tiên #2** (cadence + weapon):
  1. **5/5 attack missing waitTime=5** — toàn bộ Đường Môn fire missile quá nhanh so với PC. 15 phút fix (5 dòng).
  2. **3/5 attack missing horseLimit=1** (47/50/54) — sai cưỡi ngựa behavior. 15 phút fix.
  3. **4/5 attack missing EqtLimit** (47/50/54/58) — không enforce vũ khí (Phi tiêu / Phi đao). Effort phụ thuộc schema field có sẵn.
- **Skill passive/buff cần chỉnh formula**:
  - **ID 51 Thanh Mộc** — formula sai 2.1× ở L1 (mobile 19 vs PC 9). 1 giờ fix (override `PassiveResist`).
  - **ID 48 Tâm Nhãn** — thiếu 5 breakpoint `attackspeed_v` (PC có 30/35/38/40) + `lifemax_yan_p` L35-36. 1 giờ fix.
- **Skill ưu tiên #3** (radius tuning):
  - **5/5 radius curves flat sai PC** — registry dùng hardcode 1 mức, PC có curve `{{1,X},{20,Y}}`. 30 phút fix 5 dòng.
- **Test plan**:
  - Unit test: cast 50 ở L1 vs L20, expect 6 missiles thay vì 2.
  - Unit test: cast 54 ở L30, expect Fan form (8 fan missiles) thay vì Single.
  - Unit test: cast 58 ở L60, expect 2nd-wave missile 227 (Vạn Lý Truy Tâm) sau khi missile 58 va chạm NPC.
  - Unit test: cast 51 ở L1, expect lighting res = 9 (PC) thay vì 19 (mobile bug).
  - Visual test: cast 45 ở L20, expect vanish visual 1113 (Tích Lịch Loạn Hoàn Hãm Tĩnh) khi missile 45 biến mất.
  - Regression test: cast 47/50/54 ở trên ngựa, expect vẫn cast được (mobile hiện cấm do `horseLimit=0` default).
- **Không cần làm dash** cho Đường Môn (ranged pure missile, không có dash).
- **Phase 5 (future)**: port 80-tier sub-skills (249, 250, 302, 340, 341, 342) + 150-tier sub-skills (1069, 1070, 1071, 1097, 1098, 1099, 1100) + per-skill files `feidaotang150` / `nutang150` / `biaotang150` / `xiaoli_feidao` / `baoyu_lihua` / `shehun_yueying` / `tangmen120` / `tangmen150` (8 sub-form) vào mobile catalog. Effort: 1-2 tuần. Cần check ID trong `PcSkills.txt` đã có sẵn, chỉ cần thêm `CreateTangMenXxx()` factory.
## 2.8. Thiên Nhẫn (PC: TianRen 天忍, ID range 131-150 + 361-364 + 1075-1076)

> **Nguồn PC**:
> - `/var/www/vltksource_new/vl_update_27/Server 6.0/.../script/skill/tianren.lua` (GBK, 247 dòng, 16 skill + 150-tier `zhanren150` có `randmove` + `missle_missrate` → dash-pattern)
> - `/var/www/vltksource_new/vl_update_27/Server 6.0/.../script/skill/tianren/*.lua` (26 file per-skill; 6 pinyin ASCII: `limo-duohun`, `sanmei-zhenhuo`, `shigu-xueren`, `tianmo-jieti`, `wuxing-zhen`, `zhiyan`; 20 GBK TCVN3)
> - `Assets/StreamingAssets/Reference/ModSkills.txt` (TCVN3, canonical SkillId 131-150 + 361-364 + 1075-1076, full columns)
> - `Assets/StreamingAssets/Reference/ModMissles.txt` (child missile 54-58, 69-71, 169-171, 226, 337, 363, 366, 192 — `mv=0` stationary fire, `mv=1` homing)
> - C++: `KNpc.cpp::CastMeleeSkill` switch (line 1829-1891) — **TianRen KHÔNG có** skill nào thuộc `Melee_Jump/JumpAndAttack/RunAndAttack` (PC `IsMelee=1` cho 139/142/147/361/1075 nhưng chỉ là melee-missile, không phải dash). Tuy nhiên `zhanren150` có `randmove` → missile có thể "lunge" (chưa thấy trong PC C++ reference, có thể nằm ngoài scope task).
> - Tóm tắt: Thiên Nhẫn là môn phái **fire damage + debuff ranged/missile** thuần, **không có dash** (G1 N/A). Gap nặng nhất: **G6 event chain (sub-skill 361/362/363/364/1075/1076 MISSING toàn bộ)** + **G4 attackRadius sai nhiều skill** + **G7 lifemax_p dấu ngược (ID 150)** + **G7 PcSkillTuningRegistry thiếu 136/137/139/140/142/143/147/149/150**.

### 2.8.1. Catalog scan

| ID | Tên (Việt / Chinese) | Loại PC | Style PC | IsMelee | AttackRadius PC / Mobile | childId/N | Vai trò |
|---:|---|---|---|---:|---|---|---|
| 131 | Thiên Nhẫn Đao Pháp / 天忍刀法 | Passive mastery | 3 (passive) | 0 | 0 / 0 | 0/0 | Buff addfiremagic_v (L1: 15 → L20: 215) |
| 132 | Thiên Nhẫn Thương Pháp / 天忍矛法 | Passive mastery | 3 (passive) | 0 | 0 / 0 | 0/0 | Buff addphysicsdamage_p + attackrating + deadly |
| 133 | Thiên Nhẫn Phủ Pháp (##) | Passive mastery | 3 (passive) | 0 | 0 / — | — | **THIẾU mobile catalog** (placeholder `##`) |
| 134 | Thiên Nhẫn Chùy Pháp | Passive mastery | 3 (passive) | 0 | 0 / — | — | **THIẾU mobile catalog** |
| 135 | Tàn Dương Như Huyết / 残阳如血 | Active ranged | 0 (Missiles) | 0 | 270 / 320 | 54/1 | Base fire skill, addskilldamage1→361, addskilldamage2→142, addskilldamage3→1075 |
| 136 | Hỏa Liên Phần Hoa / 火莲焚华 | Active state | 0 (Missiles) | 0 | 440 / 400 | 20/1 | Buff: meleedamagereturn_p (âm), stateSpecialId=22 |
| 137 | Ảo Ảnh Phi Hồ / 幻影飞狐 | Active state | 0 (Missiles) | 0 | 440 / 400 | 20/1 | Buff: attackratingenhance_p (âm), stateSpecialId=26 |
| 138 | Thôi Sơn Điền Hải / 推山填海 | Active ranged | 0 (Missiles) | 0 | 400 / 350 | 55/10 | **MISSILES STAGGER** — PC timePerCast=40 + MslsGen=5/5 (mobile mất) |
| 139 | Hỗn Thủy Mạc Ngư / ?水?鱼 | Active melee-missile | 0 | **1 (melee)** | 60 / 320 | 70/1 | **RADIUS SAI LỚN** (5×) + melee bị Missiles |
| 140 | Phi Hồng Vô Tích / 飞鸿无迹 | Active state | 0 (Missiles) | 0 | 440 / 400 | 20/1 | Buff: adddefense_v (âm), stateSpecialId=27 |
| 141 | Liệt Hỏa Tình Thiên / 烈火情天 | Active ranged | 0 (Missiles) | 0 | 72 / 384 | 56/16 | **RADIUS SAI 5×** (72 vs 384), MissilesForm=3 (Surround) nhưng mobile=Single |
| 142 | Thâu Thiên Hoán Nhật / 偷天换日 | Active melee-missile | 0 | **1 (melee)** | 60 / 78 | 69/1 | Melee bị Missiles; steal life/mana; missle_lifetime_v=4 (mobile không có) |
| 143 | Lịch Ma Đoạt Hồn / 厉魔夺魄 | Active state | 0 (Missiles) | 0 | 440 / 400 | 20/1 | Buff: addphysicsdamage_p (âm), stateSpecialId=28 |
| 144 | Minh Tôn Bản Sinh (##) | Passive | 3 (passive) | 0 | 0 / 0 | 0/0 | Buff: fireres_p (Log10 công thức có thể sai) — placeholder `##` |
| 145 | Đơn Chỉ Liệt Diệm / 单指烈焰 | Active ranged | 0 (Missiles) | 0 | 280 / 320 | 57/1 | Fire damage pure |
| 146 | Ngũ Hành Trận / 五行阵 | Active aura | 2 (Initiative) | 0 | 180 / 180 | 226/1 | Aura stateSpecialId=29; mobile đã có ✓ |
| 147 | Huyền Minh Hấp Tinh / ?冥?星 | Active melee-missile | 0 | **1 (melee)** | 60 / 320 | 71/1 | **RADIUS SAI 5×** (60 vs 320) + melee bị Missiles |
| 148 | Ma Diệm Thất Sát / 魔炎七杀 | Active ranged | 0 (Missiles) | 0 | 570 / 320 | 58/1 | **RADIUS SAI LỚN** (320 vs 570); **EVENT CHAIN startSkill=192 (Ngự Phong thuật) MISSING** |
| 149 | Thực Cốt Huyết Nhận / ?骨?刃 | Active state | 2 (Initiative) | 0 | 0 / 0 | 0/0 | Buff: addfiredamage_v; stateSpecialId=25 (mobile dùng 11 cho animId, state id riêng) |
| 150 | Thiên Ma Giải Thể / 天魔解体 | Active state | 2 (Initiative) | 0 | 0 / 0 | 0/0 | Buff multi-attr; **lifemax_p DẤU NGƯỢC** (mobile +21%/+20% vs PC -11%/-20%) |
| 361 | Vân Long Kích / ?龙? | Sub-skill (added dmg from 135/141/142) | 0 | 1 (melee) | 60 / — | 169/1 | **MISSING MOBILE** — child missile 169 mv=1 life=6 speed=32 (homing dash?) |
| 362 | Thiên Ngoại Lưu Tinh / 天外流星 | Sub-skill (from 138/145) | 0 | 0 | 420 / — | 171/1 | **MISSING MOBILE** — child 171 mv=0 stationary life=14; **EVENT CHAIN vanishSkill=363 MISSING** |
| 363 | Nghiệp Hỏa Phần Thành / 业火焚城 | Sub-skill (from 148) | 0 | 0 | 570 / — | 170/1 | **MISSING MOBILE** — child 170 mv=0 stationary life=54 speed=2 (fire spread!) |
| 364 | Bi Tô Thanh Phong / 碧?清风 | Sub-skill (from ?) | 0 | 0 | 440 / — | 20/1 | **MISSING MOBILE** — stateSpecialId=58 |
| 1075 | Giang Hải Não Lan / 江海?兰 | Sub-skill 150-tier (from 135/141) | 0 | 1 (melee) | 60 / — | 337/1 | **MISSING MOBILE** — child 337 mv=1 life=6 speed=36; **EVENT CHAIN startSkill=1131 MISSING** |
| 1076 | Tật Hỏa Liệu Nguyên / 疾火燎原 | Sub-skill 150-tier (from 138/145/148) | 0 | 0 | 570 / — | 366/1 | **MISSING MOBILE** — child 366 mv=0 stationary life=37 (fire storm) |

**Sub-skill damage chain trong tianren.lua** (event chain G6 hoàn toàn MISSING):
- 135 (Tàn Dương) L1-2: cast 361 + cast 142 + cast 1075 (sub-skill)
- 138 (Thôi Sơn) L1-2: cast 362 + cast 1076
- 141 (Liệt Hỏa) L1-2: cast 361 + cast 1075
- 142 (Thâu Thiên) L1-2: cast 361 + cast 1075
- 145 (Đơn Chỉ) L1-2: cast 362 + cast 148 + cast 1076
- 148 (Ma Diệm) L1-2: cast 363 + cast 1076
- 1075 (Giang Hải) startSkill=1131 — event chain chưa rõ target
- 362 (Thiên Ngoại) vanishSkill=363 — fire spread khi missile bay xong

**Tuning coverage** (`PcSkillTuningRegistry.RadiusCurves[TianRenId]` line 77-84): chỉ cover 5/14 active (35%); missing 136/137/139/140/142/143/147/149/150.

### 2.8.2. Gap table

| ID | Tên | Hành vi mobile | Hành vi PC | Gap | Severity | Effort |
|---:|---|---|---|---|---|---|
| **361** | Vân Long Kích | **KHÔNG CÓ TRONG CATALOG** | PC ModSkills.txt: child=169, anim=10, isMelee=1, arc=60, mv=1 homing | **G6 — sub-skill hoàn toàn missing** — referenced as addskilldamage1 cho 135/141/142 (chained damage). L1-2 cast nên trigger cast 361 | **Cao** (cốt lõi chain damage) | 1 giờ (BaseSkill + child config) |
| **362** | Thiên Ngoại Lưu Tinh | **KHÔNG CÓ TRONG CATALOG** | PC: child=171, anim=11, arc=420, mv=0 stationary, **vanishSkill=363** (event chain fire spread) | **G6 — sub-skill missing + G6 event chain vanish→363 missing**. Mobile không có cách nào fire chain. | **Cao** | 1 giờ + thêm logic vanish event |
| **363** | Nghiệp Hỏa Phần Thành | **KHÔNG CÓ TRONG CATALOG** | PC: child=170, anim=11, arc=570, mv=0 stationary life=54 speed=2 (fire spread AoE); referenced by 148 chain | **G6 — sub-skill missing** (mất fire spread visual cốt lõi của Thiên Nhẫn) | **Cao** | 1 giờ |
| **364** | Bi Tô Thanh Phong | **KHÔNG CÓ TRONG CATALOG** | PC: child=20, arc=440, stateSpecialId=58 | **G7 — sub-skill missing** (chưa rõ reference nào dùng) | Trung bình | 30 phút |
| **1075** | Giang Hải Não Lan | **KHÔNG CÓ TRONG CATALOG** | PC: child=337, anim=10, isMelee=1, arc=60, mv=1 life=6 speed=36; **startSkill=1131** | **G6 — sub-skill 150-tier missing + G6 event chain startSkill=1131 missing**. Mobile không cast được. | **Cao** | 1 giờ + start event |
| **1076** | Tật Hỏa Liệu Nguyên | **KHÔNG CÓ TRONG CATALOG** | PC: child=366, anim=11, arc=570, mv=0 stationary life=37; referenced by 138/145/148 chain | **G6 — sub-skill 150-tier missing** (mất fire storm cuối game) | **Cao** | 1 giờ |
| **135** | Tàn Dương Như Huyết | `radius=320, child=54/1, charAnim=11, timePerCast=0, waitTime=0, addskilldamage1→361, addskilldamage2→142, addskilldamage3→1075` (declarations only, no runtime cast) | PC: radius=270, waitTime=5, addskilldamage1[1]={{1,361},{2,361}} L1-2 cast 361; [3]={{1,1},{20,45}} L3-20 dmg scales | **G4 radius sai (320 vs 270)** + **G4 waitTime=0 vs PC=5** (multi-cast no delay) + **G6 addskilldamage1/2/3 không fire sub-skill runtime** (chỉ là damage field, không cast sub-skill) | **Cao** (mất chain pattern L1-2 + radius sai) | 2 giờ |
| **136** | Hỏa Liên Phần Hoa | `radius=400, child=20/1, addfiredamage_v` (NOTE: code ghi `MeleeDamageReturnP` không phải AddFireDamageV) | PC: radius=440, stateSpecialId=22 | **G4 radius sai (400 vs 440)** + **G7 stateSpecialId=22 bị bỏ** + **G7 PcSkillTuningRegistry thiếu ID 136** | Trung bình | 1 giờ |
| **137** | Ảo Ảnh Phi Hồ | `radius=400, child=20/1, AttackRatingEnhanceP` | PC: radius=440, stateSpecialId=26 | **G4 radius sai (400 vs 440)** + **G7 stateSpecialId=26 bị bỏ** + **G7 Tuning thiếu** | Trung bình | 1 giờ |
| **138** | Thôi Sơn Điền Hải | `radius=350, child=55/10, timePerCast=0, waitTime=0` | PC: radius=400, child=55/10, **timePerCast=40**, **waitTime=10**, **MslsGenerate=5**, **MslsGenerateData=5** | **G4 radius sai (350 vs 400)** + **G4 timePerCast=0 vs PC=40** (missile bắn hết 1 phát thay vì 10 missiles staggered qua 40 ticks) + **G4 waitTime=0 vs PC=10** + **G7 MslsGenerate/MslsGenerateData bị bỏ** (cơ chế staggered spawn mất) | **Cao** (mất cảm giác "10 lưỡi cày theo đường" — đây là tên skill "Thôi Sơn Điền Hải") | 2-3 giờ (cần thêm logic spawn theo tick) |
| **139** | Hỗn Thủy Mạc Ngư | `radius=320, child=70/1, PhysicsEnhanceP 9+lv*10, StealStaminaP 2+lv` (L1=11, L20=219) | PC: **isMelee=1 (melee)**, **radius=60**, waitTime=0, IsPhysical=1. PC `physicsenhance_p` (chưa tìm thấy file per-skill trong scope, có thể tianren.lua định nghĩa) | **G4 radius SAI 5×** (320 vs 60 — overcast, player cast xa hơn PC 5×) + **G4 isMelee bị bỏ** (mobile Missiles, PC melee) + **G7 Tuning thiếu ID 139** + **G7 PhysicsEnhanceP sai công thức (11→219 vs PC ?)** | **Cao** (radius 5× sai = balance break + melee bị missile) | 1 giờ |
| **140** | Phi Hồng Vô Tích | `radius=400, AddDefenseV (âm)` | PC: radius=440, stateSpecialId=27 | **G4 radius sai (400 vs 440)** + **G7 stateSpecialId=27 bị bỏ** + **G7 Tuning thiếu** | Trung bình | 1 giờ |
| **141** | Liệt Hỏa Tình Thiên | `radius=384, child=56/16, SkillMissileForm.Single` | PC: **radius=72** (cast range), MisslesForm=**3 (Surround)**, MslsGenerate=1/5, waitTime=5 | **G4 radius SAI 5×** (384 vs 72 — cast xa hơn PC 5×) + **G4 MissilesForm sai** (Single vs PC Surround=3) + **G4 waitTime=0 vs PC=5** + **G7 MslsGenerateData=5 bị bỏ** | **Cao** (radius sai + hình dạng missile sai: 16 tia tỏa tròn vs mobile 16 missile thẳng hàng) | 1 giờ |
| **142** | Thâu Thiên Hoán Nhật | `radius=78, child=69/1, PhysicsEnhanceP 25+lv*231, StealLife/Mana, charAnim=9, timePerCast=0, waitTime=0` | PC: **isMelee=1 (melee)**, radius=60, IsPhysical=1, child=69/1 (mv=1 life=3 speed=24), addskilldamage1→361, addskilldamage2→1075 (chain) | **G4 radius sai (78 vs 60)** + **G4 isMelee bị bỏ** (mobile Missiles, PC melee) + **G4 MissilesForm=Single ✓ (PC=1)** + **G6 addskilldamage chain MISSING** + **G7 missle_lifetime_v=4 (mobile missile 69) chưa thấy khai báo** + **G7 Tuning thiếu ID 142** | **Cao** (chain damage mất + melee→missile) | 2 giờ |
| **143** | Lịch Ma Đoạt Hồn | `radius=400, AddPhysicsDamageP (âm)` | PC: radius=440, stateSpecialId=28 | **G4 radius sai (400 vs 440)** + **G7 stateSpecialId=28 bị bỏ** + **G7 Tuning thiếu** | Trung bình | 1 giờ |
| **145** | Đơn Chỉ Liệt Diệm | `radius=320, child=57/1, waitTime=0` | PC: radius=280, waitTime=5, IsPhysical=0 | **G4 radius sai (320 vs 280)** + **G4 waitTime=0 vs PC=5** | Thấp (15% sai) | 30 phút |
| **147** | Huyền Minh Hấp Tinh | `radius=320, child=71/1, PhysicsEnhanceP 25+lv*10, StealManaP 2+lv/2` | PC: **isMelee=1 (melee)**, **radius=60**, IsPhysical=1, child=71/1 (mv=1 life=3 speed=24) | **G4 radius SAI 5×** (320 vs 60 — overcast 5×) + **G4 isMelee bị bỏ** (mobile Missiles, PC melee) + **G7 Tuning thiếu ID 147** + **G7 missle_lifetime_v chưa khai báo** | **Cao** (radius 5× + melee bị missile) | 1 giờ |
| **148** | Ma Diệm Thất Sát | `radius=320, child=58/1, DeadlyStrikeP, charAnim=11, timePerCast=0, waitTime=0` | PC: **radius=570**, **startSkill=192** (Ngự Phong thuật — fireball start event), addskilldamage1→363, addskilldamage2→1076 | **G4 radius SAI LỚN** (320 vs 570, undercast 44%) + **G6 EVENT CHAIN startSkill=192 MISSING** (mất hiệu ứng "gió bùng lên" trước khi bắn) + **G6 addskilldamage chain MISSING** + **G4 timePerCast=0 vs PC=0 ✓** | **Cao** (event chain mất + radius sai lớn) | 2 giờ (cần thêm start event hook) |
| **149** | Thực Cốt Huyết Nhận | `AddFireDamageV 40+10*lv (L1=50, L20=240), 1080+162*lv (L1=1242, L20=4320), charAnim=11, targetEnemy` | PC: stateSpecialId=25, InitiativeNpcState, addfiredamage_v theo per-skill `shigu-xueren.lua` | **G7 stateSpecialId=25 bị bỏ** (mobile dùng 11 charAnimId, state id riêng) + **G7 AddFireDamageV 2 tham số (val1+val2) là per-skill `shigu-xueren.lua` (L1=50, L20=240) + duration 1080+162*lv (per-skill); PC tianren.lua ĐÃ COMMENT OUT** (xem line 199-204 `skill_startevent/showevent` commented). G7 verify cần check 150-tier data | Trung bình | 1 giờ |
| **150** | Thiên Ma Giải Thể | `LifeMaxP Link(lv,(1,21,""),(30,20,"")) dur=18*45→18*180` — **L1=+21%, L30=+20%** (positive, slight decrease) | PC tianren.lua line 170 (commented): `lifemax_p={{{1,-11},{20,-30},{30,-20},{40,10},{41,10}}}`. Per-skill `tianmo-jieti.lua::Getlifemax_p`: `result1 = -10-level; result2 = 600+level*200` → L1=-11, L20=-30, L30=-40 | **G7 lifemax_p DẤU NGƯỢC** (mobile +21→+20 vs PC -11→-40) — buff HP thay vì giảm HP (mobile mất cảm giác "giải thể" — tự hủy HP để tăng dame) | **Cao** (cốt lõi cảm giác "tự thiêu" của skill) | 30 phút (đổi dấu + curve) |
| **131** | Thiên Nhẫn Đao Pháp | `AddFireDamageV Link(1,15)→(20,215), skillStyle=PassivityNpcState, charAnim=14` | PC: style=3 (passive), addfiremagic_v tianren.lua line 26 | **G7** MagicAttributeKind.AddFireDamageV vs PC tianren.lua addfiremagic_v (MagicAttr addFireMagic vs addFireDamage) | Thấp (verify: AddFireDamageV + value ổn) | 30 phút verify |
| **132** | Thiên Nhẫn Thương Pháp | `AddPhysicsDamageP (15→215) -1 3, AttackRatingEnhanceP (35→272) -1 0, DeadlyStrikeEnhanceP (6→35) -1 0` | PC tianren.lua: `addphysicsdamage_p (15→215), attackratingenhance_p (35→272), deadlystrikeenhance_p (6→35)` | OK (match) | — | — |
| **133** | Thiên Nhẫn Phủ Pháp | **THIẾU mobile catalog** | PC ModSkills.txt: style=3 passive mastery, icon `\spr\Ui\...µ¶·¨.spr` | **G7 — missing** (có trong PC, mobile chưa catalog) | Thấp (passive mastery) | 30 phút (copy 131/132) |
| **134** | Thiên Nhẫn Chùy Pháp | **THIẾU mobile catalog** | PC ModSkills.txt: style=3 passive mastery, icon `\spr\Ui\...\Ë«´¸Ì×Â·.spr` | **G7 — missing** (có trong PC, mobile chưa catalog) | Thấp (passive mastery) | 30 phút (copy 131/132) |
| **144** | Minh Tôn Bản Sinh | `FireResP = Log10(lv+1)/2 * 70` (L1=10, L20=48) | PC ModSkills.txt: style=3 passive, isMelee=0 | **G7** công thức FireResP có thể sai (PC tianren.lua không định nghĩa `minhton_bansinh` per-skill) | Thấp | 1 giờ verify |
| **146** | Ngũ Hành Trận | `AddDefenseV (75→550) dur=18, stateSpecialId=226, targetSelf, isAura` | PC: stateSpecialId=**29** (mobile=226), charAnim=14, isAura=1 ✓ | **G5** stateSpecialId confusion (mobile=226 vs PC=29 — có thể là ID khác mapping) | Trung bình | 30 phút verify |
| **All 6 melee-missile** (139/142/147/361/1075 + isMelee=1) | `skillStyle=Missiles` (mobile default) | PC: IsMelee=1 (melee mode) | **G1 lite — melee mode MISSING** (PC melee-missile pattern, mobile map sang Missiles). Ở close range <60, PC chém cận chiến; mobile bắn missile xa hơn. Không phải dash, nhưng có melee range check. | Trung bình (feel khác PC) | 2 giờ (thêm IsMelee branch trong runtime) |
| **All 5 active attack** | `PcSkillTuningRegistry.RadiusCurves[TianRenId]` cover 5/14 (35%) | PC có 14 active skill cần tuning | **G7 — Tuning coverage 35%** (thiếu 9 IDs: 136/137/139/140/142/143/147/149/150) | Trung bình | 1 giờ (thêm 9 entries) |
| **All TianRen** | `ConfigureTianRenVisuals` **KHÔNG CÓ** trong SkillEffectVisualService | PC có data-driven visuals OK, không có switch-case | **G7 — không có visual case** (rely on ConfigureDataDrivenVisuals auto-resolve). OK cho hiện tại nhưng rủi ro nếu auto-mapper miss | Thấp | 1 giờ verify |
| **135 + 148** | tianren.lua line 199-204 commented: `skill_eventskilllevel/skill_startevent/skill_showevent` | PC có event chain tiềm năng cho 147/150 (`zhanren150.randmove`) | **G6 — showevent/startevent commented** (PC code path mất; nếu uncomment sẽ cần thêm 378, 1101 logic) | Thấp (đã commented) | nửa ngày |

### 2.8.3. Phase 1 quick wins

- [ ] **ID 361-364 + 1075-1076 — thêm 6 sub-skill vào catalog** (CRITICAL — chain damage G6). Copy từ `DamageSkillNew` template với child missile 169/170/171/337/366. Riêng 362 cần thêm `vanishSkill=363` event hook, 1075 cần `startSkill=1131` event hook. (G6, 1 giờ + 1 giờ event)
- [ ] **ID 148 Ma Diệm Thất Sát** — sửa `radius=320` → `570` (PC), thêm event hook `startSkill=192` (Ngự Phong thuật pre-cast fire effect). (G4+G6, 1 giờ)
- [ ] **ID 141 Liệt Hỏa Tình Thiên** — sửa `radius=384` → `72`, đổi `SkillMissileForm.Single` → `Surround`, thêm `waitTime=5`. (G4, 1 giờ)
- [ ] **ID 139, 142, 147, 361, 1075 melee-mode** — sửa `radius=320/78/320/60/60` → `60/60/60/60/60` (PC melee range). (G4, 1 giờ tổng 5 skill)
- [ ] **ID 150 Thiên Ma Giải Thể lifemax_p DẤU NGƯỢC** — đổi `Link(lv,(1,21,""),(30,20,""))` → `Link(lv,(1,-11,""),(30,-40,""))` (PC per-skill tianmo-jieti.lua line 47-51). Đây là bug gameplay lớn nhất — buff HP thay vì tự hủy. (G7, 30 phút)
- [ ] **ID 135 Tàn Dương** — sửa `radius=320` → `270`, `waitTime=0` → `5`. (G4, 15 phút)
- [ ] **ID 136, 140, 143 Hỏa Liên / Phi Hồng / Lịch Ma** — sửa `radius=400` → `440` (3 skill). (G4, 30 phút)
- [ ] **ID 145 Đơn Chỉ** — sửa `radius=320` → `280`, `waitTime=0` → `5`. (G4, 15 phút)
- [ ] **ID 138 Thôi Sơn** — sửa `radius=350` → `400`. (G4, 15 phút) **[DEFER — staggered spawn fix xem G7]**
- [ ] **PcSkillTuningRegistry.RadiusCurves[TianRenId]** — thêm 9 entries: 136, 137, 139, 140, 142, 143, 147, 149, 150 (mỗi entry flat 1 dòng). (G7, 1 giờ)
- [ ] **ID 133 + 134 Phủ Pháp + Chùy Pháp** — thêm 2 passive mastery tương tự 131/132 (copy từ `TianRenPassiveDaofa`). (G7, 30 phút)
- [ ] **ID 149 Thực Cốt Huyết Nhận** — thêm `stateSpecialId=25` để runtime bind buff state. (G7, 15 phút)

### 2.8.4. Phase 3 dash (G1)

- [ ] **N/A — Thiên Nhẫn không có dash/lunge**. `KNpc.cpp::CastMeleeSkill` switch (line 1829) + `tianren.lua` (không thấy Melee_Jump/JumpAndAttack/RunAndAttack) đều không tham chiếu. PC `zhanren150.randmove` ở dòng 242 có `randmove={{1,1},{20,1}},{{1,1},{20,5}}` → missile 197 có random scatter path (chưa rõ PC C++ impl); ngoài range 131-150 task. Phase 3 bỏ qua cho Thiên Nhẫn.

### 2.8.5. Phase 4 event chain (G6)

- [ ] **Sub-skill 361-364, 1075-1076 catalog (G6 critical)** — Phase 1 đã cover (6 entries, mỗi cái BaseSkill + child config)
- [ ] **ID 148 startSkill=192** (Gió bùng lên trước khi bắn Ma Diệm) — Phase 1 đã cover (1 hook)
- [ ] **ID 362 vanishSkill=363** (fire spread khi Thiên Ngoại Lưu Tinh missile bay xong) — Phase 1 đã cover (1 hook)
- [ ] **ID 1075 startSkill=1131** (Giang Hải Não Lan pre-cast event — chưa rõ ID 1131 là gì, cần check `ModSkills.txt` ID 1131) — Phase 1 đã cover (1 hook, 30 phút verify 1131)
- [ ] **ID 135/138/141/142/145/148 addskilldamage1/2/3 chain** (chained damage L1-2 cast sub-skill 361/362/363/1075/1076) — cần thêm runtime path: nếu `skillLevel <= 2` thì gọi `ApplySkillCast(caster, subSkillId)` thay vì chỉ add damage field. Effort: nửa ngày (cần uncomment path trong CombatRuntimeService + test không infinite-loop)

### 2.8.6. Trạng thái

- [x] Catalog scan xong (18 skill: 4 passive mastery, 1 passive (144), 1 aura (146), 12 active attack)
- [x] Per-skill files: 6 pinyin ASCII decoded (`limo-duohun`, `sanmei-zhenhuo`, `shigu-xueren`, `tianmo-jieti`, `wuxing-zhen`, `zhiyan`) + 20 GBK TC names decoded
- [ ] **Quick-win phase merged** (Phase 1 — 12 items; ưu tiên: 361-364/1075-1076 sub-skill + 148 event + 141 radius + 150 lifemax_p)
- [ ] Dash phase merged: **không áp dụng** (Thiên Nhẫn không có dash)
- [ ] Event chain phase merged (Phase 4 — 5 hooks: 148/362/1075/1131 + addskilldamage chain runtime)
- [ ] Tuning coverage 35% → 100% (cần thêm 9 entries: 136/137/139/140/142/143/147/149/150)

### 2.8.7. Tổng kết

- **Skill chính ưu tiên sửa**: **148 Ma Diệm Thất Sát** (event chain mất + radius sai 44%) → fix 1 giờ → mở ra hiệu ứng gió bùng lên đặc trưng Thiên Nhẫn.
- **Skill sub-form ưu tiên**: **361-364, 1075-1076** (toàn bộ MISSING trong mobile) → 1 giờ/cái = 6 giờ tổng → mở ra chain damage L1-2 pattern (cốt lõi Thiên Nhẫn).
- **Bug gameplay nghiêm trọng #1**: **150 Thiên Ma Giải Thể lifemax_p DẤU NGƯỢC** — mobile buff HP, PC tự hủy HP. Fix 30 phút, ảnh hưởng lớn đến cảm giác "giải thể" (đây là tên skill).
- **Bug gameplay nghiêm trọng #2**: **141 Liệt Hỏa Tình Thiên** radius 384 vs PC 72 (5× overcast) — cast xa hơn PC 5×, balance break.
- **Bug gameplay #3**: **139, 147** radius 320 vs PC 60 (5× overcast) — same as 141.
- **Skill passive**: 133, 134 missing (chỉ là copy từ 131/132, 30 phút).
- **G1 dash**: **không áp dụng** (Thiên Nhẫn melee-missile thuần, không dash).
- **G6 event chain**: 5 hooks cần thêm (148 startSkill=192, 362 vanishSkill=363, 1075 startSkill=1131, addskilldamage1/2/3 chain runtime cho 6 skill) — tổng effort ~nửa ngày.
- **Test plan**: unit test cho sub-skill 361-364/1075-1076 cast chain; visual test cast 148 (expect gió bùng 192 → 363 fire spread); regression test cast 150 xem HP giảm (-10/L1) thay vì tăng; radius test cast 141 ở khoảng cách 100 (>72 PC) — expect fail cast ở PC, success ở mobile.
- **Phase 5 (future)**: port `zhanren150` 150-tier (có `randmove`+`missle_missrate`); port 4 missing per-skill skill (`huanying_feihu`/`feihong_wuji`/`huolian_fenhua`/`limo_duopo` đã có trong catalog rồi; còn lại `canyang_ruxue`/`tuishan_tianhai`/`liehuo_qingtian`/`toutian_huanri`/`tanzhi_lieyan`/`moyan_qisha`/`wuxing_zhen`/`beisu_qingfeng` per-skill cần verify).

---

## Tổng kết gap (ID, gap, severity, effort) — yêu cầu format

| ID | Gap | Severity | Effort |
|---:|---|---|---|
| 361 | Sub-skill MISSING (chain damage cốt lõi) | Cao | 1 giờ |
| 362 | Sub-skill MISSING + event vanish→363 | Cao | 1 giờ + event |
| 363 | Sub-skill MISSING (fire spread visual) | Cao | 1 giờ |
| 364 | Sub-skill MISSING | Trung bình | 30 phút |
| 1075 | Sub-skill 150-tier MISSING + event startSkill=1131 | Cao | 1 giờ + event |
| 1076 | Sub-skill 150-tier MISSING | Cao | 1 giờ |
| 135 | radius sai 320 vs 270, waitTime 0 vs 5, addskilldamage chain MISSING | Cao | 2 giờ |
| 136 | radius sai 400 vs 440, stateSpecialId=22 bị bỏ, Tuning thiếu | Trung bình | 1 giờ |
| 137 | radius sai 400 vs 440, stateSpecialId=26 bị bỏ, Tuning thiếu | Trung bình | 1 giờ |
| 138 | radius sai 350 vs 400, timePerCast=0 vs 40, MslsGenerate staggered MISSING | Cao | 2-3 giờ |
| 139 | radius SAI 5× (320 vs 60), isMelee bị bỏ, Tuning thiếu | Cao | 1 giờ |
| 140 | radius sai 400 vs 440, stateSpecialId=27 bị bỏ, Tuning thiếu | Trung bình | 1 giờ |
| 141 | radius SAI 5× (384 vs 72), MissilesForm Single vs Surround, waitTime 0 vs 5 | Cao | 1 giờ |
| 142 | radius sai 78 vs 60, isMelee bị bỏ, addskilldamage chain MISSING, Tuning thiếu | Cao | 2 giờ |
| 143 | radius sai 400 vs 440, stateSpecialId=28 bị bỏ, Tuning thiếu | Trung bình | 1 giờ |
| 145 | radius sai 320 vs 280, waitTime 0 vs 5 | Thấp | 30 phút |
| 147 | radius SAI 5× (320 vs 60), isMelee bị bỏ, Tuning thiếu | Cao | 1 giờ |
| 148 | radius SAI LỚN (320 vs 570), event startSkill=192 MISSING, addskilldamage chain MISSING | Cao | 2 giờ |
| 149 | stateSpecialId=25 bị bỏ | Trung bình | 15 phút |
| **150** | **lifemax_p DẤU NGƯỢC (+21% vs -11%/-40%)** | **Cao** | **30 phút** |
| 131 | AddFireDamageV vs PC addfiremagic_v (verify) | Thấp | 30 phút |
| 133 | Passive mastery MISSING | Thấp | 30 phút |
| 134 | Passive mastery MISSING | Thấp | 30 phút |
| 144 | FireResP công thức cần verify | Thấp | 1 giờ |
| 146 | stateSpecialId=29 vs mobile=226 confusion | Trung bình | 30 phút |
| All 6 melee-missile (139/142/147/361/1075) | isMelee mode MISSING (melee→Missiles sai pattern) | Trung bình | 2 giờ |
| All 5 active | Tuning coverage 35% (thiếu 9 entries) | Trung bình | 1 giờ |
| All 14 active | ConfigureTianRenVisuals MISSING (rely on auto-mapper) | Thấp | 1 giờ verify |

---

## 2.9. Côn Luân (PC: KunLun 昆仑, ID range 167-184 + ID 90)

> **Nguồn PC**:
> - `/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser_bachkim_6.0/server1/script/skill/kunlun.lua` (GB18030, 408 dòng, 23 skill — hàm `SKILLS{}` tên pinyin + 150-tier sub-form)
> - `/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser_bachkim_6.0/server1/script/skill/kunlun/*.lua` (33 file per-skill: 14 pinyin + 19 Chinese GBK)
> - `Assets/StreamingAssets/Reference/ModSkills.txt` (canonical TCVN3, ID 167-184 + ID 90)
> - `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs` line 2403-2673 (`CreateKunLunSkills` 18 entry)
> - `Assets/Scripts/Sandbox/PcSkillTuningRegistry.cs` line 108-114 (`KunLunId` radius curves — chỉ 4/18 entry)
> - C++: `KNpc.cpp::CastMeleeSkill` switch (line 1829-1891) — **không có** KunLun skill nào thuộc nhánh `Melee_Jump/JumpAndAttack/RunAndAttack`. Toàn bộ KunLun range 167-184 là passive/buff/self-state/ranged missile (lighting + q.heal).
> - Tóm tắt: Côn Luân = **lighting/buff thuần**, không có dash, không có event chain phức tạp trong range 167-184. Gap nặng nhất: **G7 magnitude sai lớn** (nhiều skill damage 1.5-3× off), **G5 ID 90 misplaced ở EMei** (PC là KunLun), **G7 Tuning coverage 22%** (chỉ 4/18 entry + 3/4 giá trị Registry SAI so với PC main file), **G4 radius lệch** (−14% đến +122% sai số).

### 2.9.1. Catalog scan

| ID | Tên (suy ra) | Loại PC | Vai trò |
|---:|---|---|---|
| 90 | 迷踪幻影 Mê Tung Ảo Ảnh | Self-buff missile | **G5 — ID 90 thuộc KunLun trong PC (`lvlSetScript=\script\skill\kunlun.lua`), nhưng mobile đặt ở `EMeiMeTungAoAnh` (line 1408). PC `emei.lua` KHÔNG có `mizhong_huanying`. Cần move 90 về `CreateKunLunSkills` + sửa `IsEMeiSkill(90)=true → false` (exclude 90 như 78).** Active state: `freezetimereduce_p` + `stuntimereduce_p` (-5→-50) trên bản thân |
| 167 | 昆仑刀法 Côn Lôn Đao Pháp | Passive mastery | Buff physics dmg + atk rating + crit (PC: `addphysicsdamage_p=(35,215)`, `deadlystrikeenhance_p=(6,50 Conic)`) |
| 168 | 昆仑剑法 Côn Lôn Kiếm Pháp | Passive mastery | Buff lighting magic V (PC: `addlightingmagic_v=(19,215)`; mobile dùng `AddLightingDamageV` ✓) |
| 169 | 呼风法 Hô Phong Pháp | Ranged missile | Active — base lighting missile (ReqLv 10, child=14, num=1, waitTime=5) |
| 170 | 大浪淘沙 Đại Lãng Thực Không | Self buff | Buff fire res P (PC: `fireres_p` với curve; mobile: `FireResP(50+10*lv, 1200+1200*lv)` flat — formula PC `dalang_taosha` không có sẵn trong `kunlun.lua`, dùng per-skill chung `kunlun-jianfa.lua` ratio 12+lv) |
| 171 | 清风符 Thanh Phong Phù | Self buff missile | Buff fastwalkrun (PC: `fastwalkrun_p=(22,60)`, dur=18*120) |
| **172** | 天际迅雷 **Thiên Tế Tấn Lôi** | Ranged missile | Active — **PC có `StartEvent=1/399`** (ModSkills.txt line "1/399"). Mobile catalog thiếu event chain — cần thêm `SpawnStartEvent` cho 172. (G6) |
| 173 | 天清地浊 Thiên Thanh Địa Trọc | Self buff | Buff 4 res (lighting+cold+fire+physics), 30s (2160) |
| 174 | 羁绊符 Ki Bán Phù | Ranged missile | Debuff fastwalkrun (negative), child=20 num=1, targetEnemy |
| 175 | 欺寒傲雪 Khi Hàn Ngạo Tuyết | Ranged missile | Debuff cast speed (PC: `castspeed_v=(-6,-39,-50)`, dur 18*45→18*120) |
| **176** | 狂风骤电 **Cuồng Phong Sậu Điện** | Ranged missile | Active — lighting + stun (PC: `stun_p=(5,15,15)`, `physicsenhance_p=(55,386)`, `addskilldamage1 → 373` (Khiếu Phong Tam Liên Kích), `addskilldamage2 → 1108`. radius L1=448, L20=512) |
| 177 | 百川纳海 Bách Xuyên Nạp Hải | Self buff | Buff cold res + physics res, 30s (2160) |
| 178 | 一气三清 Nhất Khí Tam Thanh | Self buff | Buff phys dmg P + deadly strike, 30s (2160) (PC: `addphysicsdamage_p=(35,215)`, `deadlystrikeenhance_p=(16,35 Conic)`) |
| 179 | 狂雷震地 Cuồng Lôi Chấn Địa | Ranged missile | Active — base lighting (PC: `lightingdamage_v=(27,315)`, `addskilldamage1 → 375` (Lôi Động Cửu Thiên), `addskilldamage2 → 182`, `addskilldamage3 → 1109`, `skill_attackradius L1=320, L20=352`) |
| 180 | 木珠兵解 Độc Tê Tị Tà | Self buff | Buff poison res (120+18*lv) |
| 181 | 弃心符 Khí Tâm Phù | Ranged missile | Stun (PC per-skill: `stun_p=(15+lv, 16+lv)`, 100 mana) |
| 182 | 五雷正法 Ngũ Lôi Chánh Pháp | Ranged AOE (4 fan) | Active — 4-missile fan (MslsGenerate=3, MslsGenerateData=5, child=18 num=4), big lighting (PC: `lightingdamage_v=(25,937)`, `addskilldamage1 → 375`, `addskilldamage2 → 1109`, radius L1=448, L20=480) |
| **183** | 岁月无情 **Tuế Nguyệt Vô Tình** | Ranged missile | **G5 — PC `kunlun.lua` KHÔNG có `suiyue_wuqing` (main missing). Per-skill `suiyue-wuqing.lua` khai báo `slowmissle_b` (10+lv*25, 270+lv*140) — `MagicAttributeKind` mobile KHÔNG có `SlowMissle`. Mobile dùng `AttackSpeedV + CastSpeedV` (Log10 formula) — attribute class MISMATCH (target missile speed ≠ target atk/cast speed). ModSkills.txt ID 183 radius=180, mobile radius=400 (+122% SAI).** |
| 184 | 金蝉脱壳 Kim Thiền Thoát Xác | Self buff | Buff physics res (120+20*lv) |

**Không catalog trong mobile** (PC `kunlun.lua` có, mobile thiếu — ngoài scope ID 167-184 + 90): `wusuo_kunlun` (雾锁昆仑, 150-tier, có `skill_collideevent → 375` + `skill_showevent` L10+) + `leidong_jiutian` (雷动九天, ID 375 ở ModSkills.txt, có `skill_collideevent → 387` + `skill_showevent` L10+) + `aoxue_xiaofeng` (傲雪啸风, ID 372, `skill_collideevent → 373` + `addskilldamage1 → 1080`) + `xiaofeng_sanlianji` (啸风三连击, ID 373) + `shuangao_kunlun` (霜傲昆仑, passive mastery 30) + `shufu_zhou` (束缚咒) + `beiming_daohai` (北冥到海) + `zuixian_cuogu` (醉仙错骨) + `jiankunlun150` (剑昆仑150) + `jiankunlun150fu` (剑昆仑150符, có `skill_vanishedevent → 1109`) + `daokunlun150` (刀昆仑150, có `skill_collideevent → 1108`) + `daokunlun150_2` (刀昆仑150第2) + `pingdi_hanlei` (平地撼雷) + `yufeng_shu` (驭风术, ID 386) + `xuantianwuji` (玄天无极) + `kunlun120` (昆仑120级技能) + 7 sub-form khác. **Tất cả nằm ngoài scope task này** (range 80-150, ID 372-1109). Phase 5.

### 2.9.2. Gap table

> **Cột "Hành vi mobile"**: `CreateKunLunSkills` line 2403-2673 (mỗi skill 1 factory `KunLunXxx()`).
> **Cột "Hành vi PC"**: `kunlun.lua` (curves per level — canonical PC source) + per-skill files (formula override từng skill) + `ModSkills.txt` (radius L1, childId, childNum, mslsForm, charAnimId, mslsGen, startEvent, collideEvent, vanishedEvent) + `KNpc.cpp::CastMeleeSkill` (KunLun không có dash).
> **Quy ước cờ (PC curve 3 phần tử)**: PC `{{1,X},{20,Y},{{1,A},{2,A}},{{1,B},{2,B}}}` = (value, durFrame, conicFlag, physFlag). Mobile dùng `Link(lv, (1,X,""), (20,Y,""))` cho value, hardcode duration, `Conic` flag trong 3rd arg của Link nếu có.

| ID | Tên | Hành vi mobile | Hành vi PC | Gap | Severity | Effort |
|---:|---|---|---|---|---|---|
| **90** | Mê Tung Ảo Ảnh | Đặt trong `EMeiMeTungAoAnh` (line 1408): `s.skillStyle = Missiles, child=20, num=1, targetSelf+targetAlly, BadStatusTimeReduceV (1, 30) dur=1200+1200*lv` | PC `kunlun.lua::mizhong_huanying` (line 168): `freezetimereduce_p=(-5,-50)` + `stuntimereduce_p=(-5,-50)`, dur=18*45→18*120, cost 30→40. ModSkills.txt: `lvlSetScript=\script\skill\kunlun.lua` → ID 90 thuộc KunLun. `emei.lua` KHÔNG có `mizhong_huanying`/`迷踪幻影`. Mobile `IsEMeiSkill(90)=true` (do 90 ∈ [77,93]) | **G5 — id↔name swap: ID 90 thuộc KunLun, mobile đặt ở EMei.** Sai sect + sai `lvlSetScript` reference + sai cost (mobile 20 vs PC 30→40) | **Cao** (sai sect identity, có thể ảnh hưởng faction routing) | 1 giờ (move factory + sửa `IsEMeiSkill` exclude 90 hoặc tạo `IsKunLunSkill` include 90) |
| 169 | Hô Phong Pháp | `radius=320, child=14, num=1, baseSkill=true, waitTime=5, LightingDamageV 15/180, PhysicsEnhanceP 5/75, SeriesDamageP 1/10, cost=15` | PC `hufeng_fa` (kunlun.lua line 11): `lightingdamage_v=(15,180)`, `physicsenhance_p=(5,75)`, `seriesdamage_p=(1,10)`, `addskilldamage1→372/2`, `addskilldamage2→176/2`, `addskilldamage3→1080/2`, `missle_speed_v=(20,24)`, `skill_attackradius L1=320, L20=384`, `skill_cost_v=15`. ModSkills.txt: `radius=300, child=14, num=1, mslsForm=1, waitTime=5, mslsGen=2, mslsGenData=1` ✓ | **OK** damage + radius (PC L1=320, mobile 320) + cost + child. **Minor: PC có 3 addskilldamage (372/176/1080) — mobile KHÔNG xử lý `addskilldamage` mechanism (toàn dự án thiếu).** | Thấp (addskilldamage toàn cục) | 30 phút verify; addskilldamage = Phase 5 toàn dự án |
| 170 | Đại Lãng Thực Không | `InitiativeNpcState, targetSelf, FireResP(50+10*lv, 1200+1200*lv, 0), cost=18+2*lv` | PC: `dalang_taosha` KHÔNG có trong `kunlun.lua` (line search). Per-skill không tìm thấy (chỉ 33 file, không có `dalang-taosha.lua`). ModSkills.txt: `radius=0, isMelee=0, mslsForm=7` (none). | **G7 — magnitude sai**: PC không có anchor chính xác. Per-skill `tianqing-dizhuo.lua` (cùng schema `fireres_p 12+level`) cho L20=32. Mobile 50+10*20=250 (×8 off vs PC 32). Cost 18+2*20=58 PC, 50/60 cho sister skills 70/73/80/90 theo kunlun.lua. | Cao (gameplay buff sai 8×) | 1 giờ — tìm PC anchor (`dalang_taosha` main file hoặc per-skill); nếu không tìm được, dùng sister `tianqing_dizhuo` pattern 12+lv |
| 171 | Thanh Phong Phù | `radius=400, child=19, num=1, targetSelf, FastWalkRunP(22, 60) dur=2160, cost=40` | PC `qingfeng_fu` (kunlun.lua line 31): `fastwalkrun_p={{{1,22},{20,60}},{{1,18*120},{20,18*120}}}`, `skill_cost_v=40`. Per-skill `qingfeng-fu.lua`: `Getfastwalkrun_p=3*level, 400+50*level` → L1: (3, 450), L20: (60, 1400) — **per-skill DIVERGE từ main (per-skill value 3*lv vs main 22 L1)**. ModSkills.txt: `radius=440` (mobile 400 = -9%) | **G4 radius 400 vs PC 440 (-9%)** + **G7 per-skill vs main conflict: mobile dùng main (22, 60) đúng convention; per-skill có formula khác (3*lv) — không phải gap nếu mobile đã chọn main** | Thấp-TB | 15 phút (sửa 400→440) |
| **172** | Thiên Tế Tấn Lôi | `radius=384, child=15, num=1, baseSkill=true, waitTime=5, targetEnemy, LightingDamageV 25/550, SeriesDamageP 1/30, cost=15/35` | PC `tianji_xunlei` (kunlun.lua line 92): `lightingdamage_v=(25,550)`, `addskilldamage1→375/2`, `addskilldamage2→1109/2`, `seriesdamage_p=(1,30)`, `missle_speed_v=(24,28)`, `skill_attackradius L1=384, L20=448` → L20=448, `skill_cost_v=15/35`. ModSkills.txt: `radius=360, child=15, num=1, mslsForm=1, **start=1/399** (StartEvent → fire sub-skill 399 ngay khi cast)` | **G4 radius 384 vs PC L20=448 (-14%)** + **G6 — StartEvent → 399 THIẾU runtime**. Mobile không có `SpawnStartEvent` cho 172 trong `CombatRuntimeService` / `SkillEffectVisualService` (chỉ có `SpawnCollideSubEffect` cho 1073→1072). | **Cao** (radius + event chain — visual feel mất + damage cộng dồn từ sub-skill 399 mất) | nửa ngày (1 giờ sửa radius + 2-3 giờ thêm `SpawnStartEvent` switch case 172→399) |
| 173 | Thiên Thanh Địa Trọc | `PassivityNpcState, targetSelf, radius=400, LightingResP(13,32)+ColdResP(13,32)+FireResP(9,28)+PhysicsResP(9,28) dur=2160, cost=12/90` | PC `tianqing_dizhuo` (kunlun.lua line 80): `lightingres_p=(13,32)`, `fireres_p=(9,28)`, `coldres_p=(13,32)`, `physicsres_p=(9,28)`, dur=18*120=2160 ✓, `skill_cost_v=(12,90)`. Per-skill `tianqing-dizhuo.lua` (different formula: lightingres 12+lv, dur 1080+135*lv) — per-skill DIVERGE từ main. Mobile đúng convention (dùng main). ModSkills.txt: `radius=440, mslsForm=6 (Surround)`. | **G4 radius 400 vs PC 440 (-9%)** + **OK magnitude** (mobile match PC main 13/32 + 9/28). | Thấp | 15 phút (sửa 400→440) |
| 174 | Ki Bán Phù | `radius=400, child=20, num=1, targetEnemy, FastWalkRunP(-22,-52) dur=360→1620, cost=60` | PC `jiban_fu` (kunlun.lua line 33): `fastwalkrun_p={{{1,-22},{20,-52}},{{1,18*20},{20,18*90}}}` (dur 360→1620 ✓), `skill_cost_v=60`. ModSkills.txt: `radius=440` (mobile 400 = -9%) | **G4 radius 400 vs PC 440 (-9%)** + **OK magnitude + duration** | Thấp | 15 phút |
| 175 | Khi Hàn Ngạo Tuyết | `radius=400, child=20, num=1, targetEnemy, CastSpeedV(-6,-39) dur=810→2160, cost=30/40` | PC `qihan_aoxue` (kunlun.lua line 164): `castspeed_p={{{1,-6},{20,-39},{30,-50},{31,-50}},{{1,18*45},{20,18*120}}}` (dur 810→2160 ✓), `skill_cost_v=(30,40)`. Per-skill `qihan-aoxue.lua` (DIFFERENT formula `fasthitrecover_v 4+floor(lv*0.8)` — per-skill DIVERGE). Mobile dùng main ✓. ModSkills.txt: `radius=440, property='Hç trî phßng ngù - bÞ ®éng'` (note: PC property "bị động" nhưng thực tế là active debuff missile). | **G4 radius 400 vs PC 440 (-9%)** + **OK magnitude**. Note: PC property field "bị động" không phải `skillStyle` thật — skill vẫn active missile. | Thấp | 15 phút |
| **176** | Cuồng Phong Sậu Điện | `radius=448, child=16, num=1, baseSkill=true, waitTime=5, targetEnemy, PhysicsEnhanceP 55/386, LightingDamageV 45/532, StunP 5/15 dur 1→20, SeriesDamageP 10/50, cost=25` | PC `kuangfeng_zhoudian` (kunlun.lua line 110): `physicsenhance_p=(55,386) ✓`, `lightingdamage_v=(45,532) ✓`, `stun_p=(5,15,15) dur=(1,20,20) ✓`, `seriesdamage_p=(10,50,52) ✓`, `missle_speed_v=(28,32,32)`, `skill_attackradius L1=448, L20=512` → **L20=512** (mobile 448 = PC L1 = -13% off L20), `skill_cost_v=25 ✓`. ModSkills.txt: `radius=180, child=16, num=1, mslsForm=1, horseLimit=1, mslsGen=0` (ModSkills.txt radius 180 là base L1, PC main L1=448, L20=512 — KHÔNG khớp nhau). | **G4 radius 448 vs PC L20=512 (-13%)** + **G7 addskilldamage1→373 + addskilldamage2→1108 THIẾU** (toàn dự án) | TB (radius) | 15 phút (radius); addskilldamage = Phase 5 |
| 177 | Bách Xuyên Nạp Hải | `InitiativeNpcState, targetSelf, radius=400, ColdResP(13,32)+PhysicsResP(9,28) dur=2160, cost=12/50` | PC `baichuan_nahai` (kunlun.lua line 27): `coldres_p=(13,32) ✓`, `physicsres_p=(9,28) ✓`, dur=18*120=2160 ✓, `skill_cost_v=(12,50) ✓`. Per-skill `baichuan-nahai.lua` (cùng formula `12+level, 1080+135*level`) — per-skill match main. ModSkills.txt: `radius=0 (buff)`. | **G4 radius 400 vs PC 0** (buff không cần radius, nhưng dùng cho AOE state 400 vẫn OK) + **OK magnitude**. | Thấp | 15 phút (sửa 400→0 nếu cần strict) |
| 178 | Nhất Khí Tam Thanh | `PassivityNpcState, targetSelf, radius=400, AddPhysicsDamageP(35,215)+DeadlyStrikeEnhanceP(16,35 Conic) dur=2160, cost=80` | PC `yiqi_sanqing` (kunlun.lua line 36): `addphysicsdamage_p={{{1,35},{20,215}},{{1,18*120},{20,18*120}},{{1,1},{2,1}}}` (Conic flag 1) ✓, `deadlystrikeenhance_p=(16,35 Conic) ✓`, dur 2160 ✓, `skill_cost_v=80 ✓`. Per-skill `yiqi-sanqing-fu.lua` (DIFFERENT: addphysicsdamage_p 20+8*lv, deadlystrike 15+lv, rangedamagereturn_v 15+lv — per-skill DIVERGE từ main). Mobile dùng main ✓. ModSkills.txt: `radius=440, child=21, num=1` (ModSkills có child=21 mà mobile child=21 ✓). | **G4 radius 400 vs PC 440 (-9%)** + **OK magnitude** (mobile match main). | Thấp | 15 phút (sửa 400→440) |
| 179 | Cuồng Lôi Chấn Địa | `radius=320, child=17, num=1, baseSkill=true, waitTime=5, targetEnemy, LightingDamageV 27/315, SeriesDamageP 1/10, cost=15` | PC `kuanglei_zhendi` (kunlun.lua line 67): `lightingdamage_v=(27,315) ✓`, `seriesdamage_p=(1,10) ✓`, `addskilldamage1→375/2`, `addskilldamage2→182/2`, `addskilldamage3→1109/2`, `skill_attackradius L1=320, L20=352` → **L20=352** (mobile 320 = PC L1, -9% off L20), `skill_cost_v=15 ✓`. ModSkills.txt: `radius=400, mslsForm=6 (Surround)` — **MAJOR CONFLICT**: ModSkills.txt=400 (Surround) vs PC main L1=320, L20=352 (Single form? hay mslsForm khác?). `missle_speed_v` không khai báo. Per-skill `kuanglei-zhendi.lua`: `Getlightingdamage_v=34+lv*5, 110+lv*17` → L20: (134, 450) — **per-skill DIVERGE từ main (134 vs 27, 450 vs 315)**. | **G4 radius 320 vs PC main L20=352 (-9%)** + **G7 addskilldamage 3 entries THIẾU** (375/182/1109) + **G4 ModSkills.txt mslsForm=6 (Surround) vs mobile default** | TB (addskilldamage toàn cục) | 15 phút radius; ModSkills mslsForm cần check `missle_speed_v` + `MslsGenerateData` |
| 180 | Độc Tê Tị Tà | `InitiativeNpcState, targetSelf, PoisonResP(120+18*lv, 1200+1200*lv), cost=18+2*lv` | PC `muzhubing_jie` KHÔNG có trong `kunlun.lua` (line search). Per-skill file name GBK (không rõ tên chính xác). ModSkills.txt: `radius=0, isMelee=0, mslsForm=7` (none). | **G7 — magnitude không có anchor PC chính xác**. 120+18*20=360 (mobile L20), per-skill sister `tianqing-dizhuo.lua` ratio 12+lv → L20=32. Mobile 360 = ×11 off nếu dùng per-skill anchor. | TB-Cao (magnitude không có PC anchor) | 1 giờ — tìm PC anchor (main file có thể bị mất tên) |
| 181 | Khí Tâm Phù | `radius=400, child=22, num=1, targetEnemy, StunP(16,35) dur 5→36, cost=100` | PC `qixin_fu` (kunlun.lua line 88): `stun_p={{{1,16},{20,35}},{{1,5},{20,36}}} ✓✓`, `skill_cost_v=100 ✓`. Per-skill `qixin-fu.lua` (DIFFERENT: `stun_p 15+lv, 16+lv` — per-skill DIVERGE). Mobile dùng main ✓. ModSkills.txt: `radius=440, child=22, num=1, waitTime=2` (mobile waitTime=0, PC waitTime=2) | **G4 radius 400 vs PC 440 (-9%)** + **G4 waitTime 0 vs PC 2 (-100% — multi-stun timing mất)** + **OK magnitude** | Thấp-TB | 15 phút radius + 5 phút waitTime |
| 182 | Ngũ Lôi Chánh Pháp | `radius=448, child=18, num=4, baseSkill=true, targetEnemy, LightingDamageV 25/937, SeriesDamageP 10/50, cost=50/90` | PC `wulei_zhengfa` (kunlun.lua line 130): `lightingdamage_v=(25,937) ✓`, `seriesdamage_p=(10,50,52) ✓`, `addskilldamage1→375/2`, `addskilldamage2→1109/2`, `skill_attackradius L1=448, L20=480` → **L20=480** (mobile 448 = -7% off L20), `skill_cost_v=(50,90) ✓`. ModSkills.txt: `radius=470, child=18, num=4, mslsForm=6 (Surround), mslsGen=3, mslsGenData=5, waitTime=0` (mobile waitTime=0 ✓). Per-skill `wulei-zhengfa.lua`: `Getlightingdamage_v=153+lv*28, 247+lv*42` → L20: (713, 1087) — per-skill DIVERGE. Mobile dùng main ✓. | **G4 radius 448 vs PC L20=480 (-7%)** + **G4 ModSkills mslsForm=6 (Surround) vs mobile default (Single?)** + **G7 addskilldamage 2 entries THIẾU** (375/1109) | TB | 15 phút radius; addskilldamage = Phase 5 |
| **183** | Tuế Nguyệt Vô Tình | `radius=400, child=23, num=1, targetEnemy, AttackSpeedV + CastSpeedV (Log10(lv+1)/2*60) dur=300+240*lv, cost=150` | PC `kunlun.lua` KHÔNG có `suiyue_wuqing` main entry (search). Per-skill `suiyue-wuqing.lua`: `GetSkillLevelData` xử lý `slowmissle_b` (10+lv*25, 270+lv*140) — **PC chính là missile slow debuff (giảm missile speed của target), KHÔNG phải atk/cast speed debuff**. ModSkills.txt: `radius=180` (mobile 400 = **+122% SAI LỚN**), `mslsForm=6 (Surround), waitTime=5`. MagicAttributeKind enum KHÔNG có `SlowMissle` (line 36-82). | **G5 — attribute class MISMATCH (G5 cao nhất)**: PC `slowmissle_b` ≠ mobile `AttackSpeedV+CastSpeedV`. Tên "Tuế Nguyệt Vô Tình" = "Years-Moon Heartless" = slow missile speed (chậm đạn), KHÔNG phải debuff atk/cast. **G4 radius 400 vs PC 180 (+122% SAI LỚN NHẤT)** + **G4 ModSkills waitTime=5 (mobile 0)** + **G7 magnitude sai: PC dur 18*45→18*120 (810→2160), mobile 540→5100 (formula khác)**. | **Cao** (sai attribute class, sai radius lớn, sai magnitude) | 1-2 ngày — cần thêm `MagicAttributeKind.SlowMissileB` enum + runtime support + sửa radius + sửa magnitude. Hoặc đánh dấu skill "TBA" cho tới khi PC anchor (main file `suiyue_wuqing`) tìm thấy |
| 184 | Kim Thiền Thoát Xác | `InitiativeNpcState, targetSelf, PhysicsResP(120+20*lv, 1200+1200*lv), cost=18+2*lv` | PC `jinchan_tuoke` KHÔNG có trong `kunlun.lua`. Per-skill không tìm thấy. ModSkills.txt: `radius=0, isMelee=0, mslsForm=7` (none). | **G7 — magnitude không có anchor PC**: 120+20*20=520 (mobile L20), sister `baichuan_nahai` per-skill ratio 12+lv → L20=32. Mobile 520 = ×16 off nếu dùng anchor. | TB-Cao (magnitude không có PC anchor) | 1 giờ — tìm PC anchor |
| 90, 167, 168 | (KunLun ở EMei) | `IsEMeiSkill(90)=true` (do 90 ∈ [77,93]). `IsKunLunSkill` chỉ check 167-184. | PC `kunlun.lua` line 168 `mizhong_huanying` cho ID 90. ModSkills.txt ID 90 `lvlSetScript=kunlun.lua`. `emei.lua` KHÔNG có 90. | **G5 — 90 misplaced ở EMei (xem trên)** + 167/168 OK (passive mastery, mobile dùng main file formula). | (covered trên) | — |
| 167 | (per-skill) | `addphysicsdamage_p Link(1,35,20,215), deadlystrikeenhance_p Link(1,6,20,50 Conic)` | PC main `kunlun_daofa` (line 35): `addphysicsdamage_p=(35,215) ✓`, `deadlystrikeenhance_p=(6,50 Conic) ✓`. Per-skill `kunlun-jianfa.lua` (file chung! `addphysicsdamage_p 13+7*lv, attackratingenhance_p 12+3*lv, addlightingmagic_v 35+lv*14, deadlystrikeenhance_p 5+lv`) — **per-skill có nhiều attribute hơn main (attackratingenhance_p, addlightingmagic_v)**. Mobile thiếu `AttackRatingEnhanceP` và `AddLightingMagicV` cho 167 (mặc dù 167 là đao pháp mastery, có thể không cần lighting). | **G7 — thiếu 2 attribute từ per-skill** (AttackRatingEnhanceP 12→72, AddLightingMagicV 49→315). Tuy nhiên per-skill `kunlun-jianfa.lua` dùng chung cho 167+168, có thể intentional. Cần verify. | Thấp-TB | 1 giờ verify (per-skill thật sự áp dụng cho 167?) |
| 168 | (per-skill) | `addlightingmagic_v Link(1,19,20,215)` (mobile dùng `AddLightingDamageV` ✓) | PC main `kunlun_jianfa` (line 39): `addlightingmagic_v=(19,215) ✓`. Per-skill giống 167 (chung file). | **OK** (mobile match main; per-skill có extra nhưng nằm ngoài convention). | — | — |
| **Tất cả 7 ranged missile (169/172/176/179/181/182/183)** | `PcSkillTuningRegistry.RadiusCurves[KunLunId]` (line 108-114) chỉ cover 4/18: 169=400, 172=570, 175=400, 178=570 | PC cần curve cho 7 ranged missile active | **G7 — Tuning coverage 22%** (4/18 entry). 3/4 entry SAI so với PC main: 169 registry=400 vs PC L1=320 (sai +25%), 172 registry=570 vs PC L20=448 (sai +27%), 178 registry=570 vs PC L1=440 (sai +30%). Riêng 175 registry=400 vs PC 440 = sai +10% (lỗi này 175 không khai báo radius trong main, dùng ModSkills.txt=440). | **Cao** (Tuning coverage rất thấp + 3/4 entry sai số) | 1 ngày (thêm 14 entry đúng giá trị PC main) |
| **Tất cả 8 ranged missile** | `ConfigureKunLunVisuals` **không tồn tại** trong `SkillEffectVisualService.cs` (line 281, 290, 501, 625, 1036, 1079 — chỉ có CaiBang/WuDang/EMei/TianWang). Mọi visual dựa vào `ConfigureDataDrivenVisuals` (auto-mapper line 57). | PC có pre-cast SPR / missile SPR riêng cho mỗi KunLun skill. | **G7 — Visual coverage 0%** (không có per-skill visual config, dùng data-driven fallback). Có thể data-driven đủ cho một số skill nhưng các skill hiếm (172/176/182) có thể cần hardcoded UID. | TB (visual feel) | 1-2 ngày (tạo `ConfigureKunLunVisuals` switch case cho 8 ranged missile) |
| **Tất cả 18 entry** | `charAnimId` mobile: 14 cho 167/168 (passive), 11 cho tất cả active/buff/cast-in-place | ModSkills.txt: 14 cho passive, 11 cho active/buff. Per-skill file convention. | **OK** (charAnimId match PC). | — | — |
| **Tất cả 18 entry** | `isMelee=0` (default, ranged + buff) | ModSkills.txt: tất cả `isMelee=0`. | **OK** (KNpc.cpp::CastMeleeSkill không cover KunLun range 167-184). **G1 N/A** — KunLun không có dash. | N/A | — |
| 169/172/176/179/181/182 | (event chain check) | N/A — không có sub-event trong range 167-184 (chỉ 172 có StartEvent 1/399, đã liệt kê trên) | PC 167-184 không có `skill_collideevent`/`skill_vanishedevent`. Riêng 172 có `skill_startevent=1/399` (ModSkills.txt). | **G6 — 172 StartEvent → 399 THIẾU runtime** (xem 172 trên) | Cao | nửa ngày (1 case trong SpawnStartEvent) |

### 2.9.3. Phase 1 quick wins (CRITICAL — G5 ID 90 misplaced + radius registry fixes)

> **Highest priority**: 90 (G5 misplaced ở EMei), 172 (G6 StartEvent + G4 radius), 183 (G5 attribute class mismatch + G4 radius lớn +122%), 7/7 ID 169/172/175/178 radius Registry SAI.

- [ ] **ID 90 — G5 move từ EMei về KunLun (CRITICAL)**: trong `PcCombatCatalogFactory.cs` line 35, sửa `EMeiMinSkillId=77, EMeiMaxSkillId=93` → exclude 90 (`id != 78 && id != 90`). Hoặc tạo `KunLunMinSkillId=90, KunLunMaxSkillId=184` cho `IsKunLunSkill` check. Move `EMeiMeTungAoAnh` (line 1408) → `KunLunMeTungAoAnh` và thêm vào `CreateKunLunSkills` list. Sửa cost 20 → 30, magnitude `BadStatusTimeReduceV(1,30)` → `BadStatusTimeReduceV(-5,-50)` (PC `freezetimereduce_p + stuntimereduce_p` là âm). Sửa targetSelf+targetAlly (giữ nguyên OK). (G5, 1 giờ)
- [ ] **ID 183 — G5 attribute class + G4 radius + G7 magnitude (CRITICAL)**: tìm PC anchor chính xác cho `suiyue_wuqing` (search `Assets/StreamingAssets/Reference/PcSkills.txt` + `pak_unpacked/` Settings/Skill/); thêm `MagicAttributeKind.SlowMissileB` enum + runtime support; sửa `AttackSpeedV+CastSpeedV` → `SlowMissileB(10+lv*25, 270+lv*140)` (per-skill anchor). Sửa `radius=400` → `180` (ModSkills.txt). Sửa `waitTime=0` → `5` (ModSkills.txt). Nếu không tìm được PC anchor, đánh dấu skill "TBA" và patch comment trong catalog. (G5+G4+G7, 1-2 ngày)
- [ ] **ID 172 — G6 StartEvent → 399 (CRITICAL)**: thêm `case 172 => 399` vào `SpawnStartEvent` switch trong `SkillEffectVisualService.cs` (tương tự `SpawnCollideSubEffect` line 455-466). Sub-skill 399 chưa có trong catalog → cần thêm `KunLunSub399` factory (rất nhỏ, chỉ visual flash). Sửa `radius=384` → `448` (PC L20). (G6+G4, nửa ngày)
- [ ] **ID 90/170/180/184 — magnitude PC anchor missing**: search `PcSkills.txt` cho `Mê Tung Ảo Ảnh` + `Đại Lãng Thực Không` + `Độc Tê Tị Tà` + `Kim Thiền Thoát Xác` để tìm PC main formula. Nếu không tìm được, dùng sister skill anchor (vd 170 dùng `tianqing_dizhuo` ratio 12+lv). (G7, 2-4 giờ tổng)
- [ ] **ID 171/173/174/175/178 — G4 radius lệch -9% (-40 PC)**: sửa `radius=400` → `440` cho 171/173/174/175/178 (5 skill, 5 dòng catalog). 1 phút.
- [ ] **ID 179 — G4 radius lệch -9% (-32 PC)**: sửa `radius=320` → `352` (PC L20). 1 phút.
- [ ] **ID 181 — G4 radius + waitTime**: sửa `radius=400` → `440`; sửa `waitTime=0` → `2`. 1 phút.
- [ ] **ID 182 — G4 radius lệch -7% (-32 PC)**: sửa `radius=448` → `480` (PC L20). 1 phút.
- [ ] **ID 176 — G4 radius**: sửa `radius=448` → `512` (PC L20). 1 phút.
- [ ] **ID 177 — G4 radius optional**: sửa `radius=400` → `0` (buff không cần, ModSkills.txt confirm). 1 phút.
- [ ] **ID 167 — G7 AttackRatingEnhanceP thiếu từ per-skill**: thêm `AttackRatingEnhanceP(12+3*lv, -1, 0)` (per-skill `kunlun-jianfa.lua` line 14). Cần verify per-skill thật sự áp dụng cho 167 (chung file với 168). Nếu chỉ áp dụng cho 168, không thêm. (G7, 1 giờ verify)
- [ ] **Tuning coverage 22% → 100% (CRITICAL)**: thêm 14 entry vào `PcSkillTuningRegistry.RadiusCurves[KunLunId]` (line 108): 170, 171, 173, 174, 176, 177, 179, 180, 181, 182, 183, 184 + sửa 3/4 entry hiện tại. Sửa 169: (1,320),(20,384). Sửa 172: (1,384),(20,448). Sửa 175: (1,440),(20,440). Sửa 178: (1,440),(20,440). Thêm 176: (1,448),(20,512). Thêm 179: (1,320),(20,352). Thêm 181: (1,440),(20,440). Thêm 182: (1,448),(20,480). Thêm 183: (1,180),(20,180). Thêm 171: (1,440),(20,440). Thêm 173: (1,440),(20,440). Thêm 174: (1,440),(20,440). (G7, 1 ngày)
- [ ] **Visual coverage 0% → 100%**: tạo `ConfigureKunLunVisuals` switch trong `SkillEffectVisualService.cs` cho 8 ranged missile (169/172/176/179/181/182/183/90). Mỗi case map sang pre-cast SPR + missile SPR UID từ PC `missles.txt` (settings) + `kunlun_*.spr` (PC `pak_unpacked/Spr/Ui3/技能图标/`. Cần tool `/var/www/vltktool/resolve_uid.py` để tra UID từ path. (G7, 1-2 ngày)

### 2.9.4. Phase 3 dash (G1)

- [ ] **Không áp dụng** — Côn Luân không có skill dash/melee-jump. Cả `KNpc.cpp::CastMeleeSkill` switch (line 1829-1891) và `kunlun.lua` đều không tham chiếu `Melee_Jump/JumpAndAttack/RunAndAttack` cho ID 167-184 + 90. Toàn bộ KunLun range là passive/buff (10 skill: 167/168/170/173/177/178/180/184 + 90 ở EMei) hoặc ranged missile/active (8 skill: 169/171/172/174/175/176/179/181/182/183). Tất cả `isMelee=0` trong `ModSkills.txt`. Phase 3 bỏ qua cho Côn Luân.

### 2.9.5. Phase 4 event chain (G6)

- [ ] **ID 172 — Thiên Tế Tấn Lôi StartEvent → 399 (HIGHEST PRIORITY G6)**: thêm `SpawnStartEvent(fx, 399)` switch trong `SkillEffectVisualService.cs` (tương tự `SpawnCollideSubEffect` line 455). PC `ModSkills.txt` confirm `start=1/399` (line "Thiªn TÕ TÊn L«i" 172 có StartSkillId=399). Sub-skill 399 chưa có trong catalog → cần thêm entry nhỏ (visual flash + minor damage). Effort: nửa ngày (1 giờ switch case + 1-2 giờ sub-skill 399 factory + 2 giờ test).
- [ ] **Tất cả skill khác (90, 167-171, 173-184) — không có event chain trong PC range 167-184 + 90**: riêng các sub-form 80-tier (372, 373, 375, 376, 386, 387) + 150-tier (1080, 1081, 1108, 1109) mới có `skill_collideevent` / `skill_vanishedevent` / `skill_showevent`. Đó là Phase 5 (ngoài scope).

### 2.9.6. Trạng thái

- [x] Catalog scan xong (18 skill: 2 passive mastery, 7 self buff/state, 8 ranged active missile, 1 G5 misplaced ở EMei)
- [ ] **Quick-win phase merged** (Phase 1 — 11 items, priority: 90 misplaced + 183 attribute swap + 172 StartEvent + 14 registry entries)
- [ ] Dash phase merged: **không áp dụng** (Côn Luân không có dash)
- [ ] Event chain phase merged: 1 item (ID 172 StartEvent → 399)
- [ ] Tuning coverage 22% → 100% (cần thêm/sửa 14 entry trong `PcSkillTuningRegistry.RadiusCurves[KunLunId]`)
- [ ] Visual coverage 0% → 100% (cần tạo `ConfigureKunLunVisuals` switch case cho 8 ranged missile)

### 2.9.7. Tổng kết

- **Skill chính ưu tiên sửa**:
  1. **ID 90 (G5 — ID misplaced ở EMei)**: PC xác nhận ID 90 = KunLun (`lvlSetScript=kunlun.lua`). Mobile đặt nhầm ở EMeiMeTungAoAnh. Fix: move factory + sửa `IsEMeiSkill` exclude 90 + sửa cost + magnitude. Effort: 1 giờ.
  2. **ID 183 (G5 — attribute class MISMATCH)**: PC `suiyue_wuqing` per-skill khai báo `slowmissle_b` (missile slow debuff), nhưng mobile dùng `AttackSpeedV + CastSpeedV` (sai class). Tên "Tuế Nguyệt Vô Tình" gợi ý slow effect. Fix: thêm `SlowMissileB` enum + sửa radius 400 → 180. Effort: 1-2 ngày.
  3. **ID 172 (G6 — StartEvent → 399 THIẾU)**: PC có `skill_startevent=1/399`, mobile runtime không handle. Visual + damage mất hoàn toàn. Fix: thêm switch case `172 → 399` trong `SpawnStartEvent` + tạo sub-skill 399 factory. Effort: nửa ngày.
  4. **Tuning coverage 22% (3/4 entry SAI)**: `PcSkillTuningRegistry.RadiusCurves[KunLunId]` chỉ có 169/172/175/178 với 3/4 giá trị sai. Fix: thêm 14 entry + sửa 3 entry sai. Effort: 1 ngày.
- **Skill thứ cấp (radius lệch -7% đến -14%)**: 7 skill ranged missile có radius sai — 169, 171, 172, 173, 174, 175, 178, 181 (400 vs 440), 176, 179, 182 (PC L20 lớn hơn L1). Fix 1 dòng / skill. Effort: 30 phút tổng.
- **Skill magnitude có vấn đề (không có PC anchor chính xác)**: 170, 180, 184 (mobile dùng per-skill sister schema). Có thể sai 8-16×. Effort: 2-4 giờ tổng (research PC anchor).
- **Visual coverage 0%**: không có `ConfigureKunLunVisuals`. Data-driven fallback có thể không đủ cho các skill hiếm. Effort: 1-2 ngày.
- **Test plan**:
  - Unit test: cast 90 ở L20 với Côn Luân player, expect debuff `BadStatusTimeReduceV=-50` trên self, faction routing KunLun (không phải EMei).
  - Unit test: cast 172 ở L1, expect sub-skill 399 fire ngay tại frame 0 (StartEvent).
  - Unit test: cast 183 ở L20 với target đang cast missile, expect target missile speed giảm (sau khi fix SlowMissileB).
  - Unit test: cast 179 ở L20, expect radius = 352 PC (sau khi fix registry + catalog).
  - Visual test: cast 176 ở L20, expect pre-cast SPR cho 176 (sau khi tạo ConfigureKunLunVisuals).
  - Regression test: cast 90 từ Côn Luân player, verify skill được route qua `IsKunLunSkill` thay vì `IsEMeiSkill`.
- **Không cần làm dash/event chain** cho range 167-184 (KunLun là ranged pure + buff, không có dash, chỉ 172 có 1 StartEvent).
- **Phase 5 (future)**: port 80-tier (372, 373, 375, 376, 386, 387) + 150-tier (1080, 1081, 1108, 1109) + 14 sub-form khác (wusuo_kunlun, leidong_jiutian, aoxue_xiaofeng, xiaofeng_sanlianji, shuangao_kunlun, shufu_zhou, beiming_daohai, zuixian_cuogu, jiankunlun150, jiankunlun150fu, daokunlun150, daokunlun150_2, pingdi_hanlei, yufeng_shu, xuantianwuji, kunlun120) + implement `addskilldamage` mechanism toàn cục. Effort: 2-3 tuần.

---

# Phần III — Implementation status (2026-06-15)

> **Báo cáo tiến độ cuối ngày** — branch `port/all-sect-dash-skills`, worktree `.harness/baocao-all-sect-skills.md`.

## Commits đã merge

| SHA | Phase | Scope | Files |
|---|---|---|---|
| `ad62cf734` | [SECT-QUICKWIN] đợt 1 | Cái Bang G2 + TianWang charAnimId×9 + TianWang 33/42 duration bug + WuDang 162 damage 14× + EMei 93 HEAL swap + TianRen 150 lifemax_p sign | 3 files, +1512/-35 |
| `00793b891` | [SECT-QUICKWIN] đợt 2 | TangMen 45/47/50/54 (waitTime, req, MslsGenData, Fan form) + WuDu 73 magnitude + KunLun 90 faction | 1 file, +27/-5 |
| `673a2d812` | [SECT-DASH] đợt 1 | Cái Bang 357+128 JUMP (MeleeType enum + NewJump logic + DamageSkillNew factory param) | 4 files, +56/-3 |

## Gap đã fix theo môn phái

| Môn phái | Gap Cao đã fix | Gap Trung bình | Còn lại |
|---|---|---|---|
| **Cái Bang** | G2 (357→389 sub-skill fire mọi level) + G1 (357+128 MeleeType=JumpAndAttack + NewJump runtime) | — | G3 (rend slash visual), G4 (128 childSkillNum 15→2), G6 (event chain) |
| **Thiên Vương** | G7 (33+42 duration 50× bug) + G7 (42 fireres_p sign sai) + G4 (8 active charAnimId 2→9/10) | 36 thiếu attribute, 40/41 multi-hit childSkillNum (blocked by G2 root cause) | G2 root cause, các passive magnitude sai |
| **Võ Đang** | G7 (162 damage 14× off) | — | 163 event chain, 165 childSkillNum 16→8, 164 radius, visual 163 |
| **Đường Môn** | G4 (45/47/50/54 waitTime, req, MslsGenData=4, Fan form) | — | 58 CollideEvent 1→227, 51 formula sai, EqtLimit weapon check |
| **Ngũ Độc** | — | G7 (73 magnitude -9/-23 theo per-skill) | 73 G5 attribute class swap (cần PoisonTimeReduceP enum), 69 G7 class swap, 62 passive 11× off |
| **Nga My** | G7 (93 ManaReplenishV → LifeReplenishV heal) | — | 95/97/100/109/114 passive sai effect, 84/86/89 schema swap, 80/82/85/88/91 addskilldamage chain |
| **Thúy Yên** | — | — | 99/102/105/108/111/113 childSkillId 70-75→6-12 (mobile internal ID sai PC), passive 95/97/100/109/114 |
| **Thiên Nhẫn** | G7 (150 lifemax_p DẤU NGƯỢC: buff → tự hủy) | — | 148 StartEvent=192, sub-skill 361-364/1075-1076 missing, 139/141/147 radius 5× off |
| **Côn Luân** | G5 (90 Mê Tung Ảo Ảnh faction misplaced EMei→KunLun) | — | 183 attribute class swap, 172 StartEvent=399, 167/172/178 radius sai, tuning 22% coverage |

## Còn gap cần fix tiếp (Phase 1 quick wins + Phase 4 event chain + Phase 5 sub-form)

### Phase 1 quick wins (chưa làm)
- **TianWang G2 root cause** (PREREQUISITE) — `CombatRuntimeService.SpawnProjectiles` line 249 skip Melee → chặn 9 multi-hit + 5 multi-shadow. 1-2 ngày.
- **TianWang 30/35/40/41** childSkillNum register (chờ G2 fix).
- **TianWang 36** missing 3 attribute (lifemax_p, lifemax_yan_p, attackspeed_v).
- **WuDang 163** event chain StartEvent 1→371, CollideEvent 1→162.
- **WuDang 165** childSkillNum 16→8, radius 400→512.
- **TangMen 58** CollideEvent 1→227 (Vạn Lý Truy Tâm).
- **CuiYan 99/102/105/108/111/113** 6 active childSkillId 70-75→6-12 (mobile internal ID sai).
- **CuiYan 95/97/100/109/114** 5 passive sai effect (cold magic, damage return, attack speed, 7-attribute).
- **TianRen 148** StartEvent=192 (Gió bùng), 6 sub-skill 361-364/1075-1076 missing.
- **KunLun 183** SlowMissleB vs AttackSpeedV (attribute class swap).
- **KunLun 172** StartEvent=399.
- **Thiếu Lâm 10** childSkillId 1056 fire 1/6 sub-skill (addskilldamage chain thiếu toàn cục).
- **WuDu 69** FastWalkRunP vs AttackSpeedV (class swap).
- **WuDu 62** passive 11× off magnitude.

### Phase 4 event chain (chưa làm)
- TangMen 45 VanishedEvent 1→1113 (Tích Lịch Loạn Hoàn Hãm Tĩnh).
- TangMen 58 CollideEvent 1→227.
- TianRen 148 StartEvent=192.
- TianRen 362 vanishSkill=363 (fire spread).
- TianRen 1075 startSkill=1131.
- CuiYan 102 StartEvent=398.
- CuiYan 111 StartEvent=112 (Bích Hải Triều Sinh b — MISSING trong catalog, 16-missile AOE).
- WuDang 163 StartEvent 1→371, CollideEvent 1→162, ShowEvent 1/5.

### Phase 3 dash (Cái Bang đã có skeleton, follow-up cần)
- Lerp + obstacle check (PC NewJump từng step + SubWorld.GetBarrier).
- Camera follow snap (SandboxPlayerController.FollowCamera).
- Input lock khi dash (MoveInput clear + cờ).
- Visual desync (SkillEffectVisualService getCurrentCasterPos callback).
- Còn 5 môn phái khác có melee: TianWang (TianWang 9 active — không có dash per PC) + WuDang (Kinh Lôi Trảm, Vô Ngã Vô Kiếm — verify). Tất cả đều `Melee_AttackWithBlur` mặc định, KHÔNG có Melee_Jump ngoài 357/128.

### Phase 5 (future, ngoài scope)
- Port 80-tier / 120-tier / 150-tier sub-form (Cái Bang 1073, 1074, Thiên Vương, Võ Đang 1078, 1079, ...).
- `addskilldamage` mechanism engine toàn cục.
- `PoisonTimeReduceP` enum (cho WuDu 73 fix đúng class).
- `SlowMissleB` attribute kind (cho KunLun 183).

## Verify checklist (sẽ chạy khi integration worker merge)

- [ ] Build: mở Unity Editor, compile `port/all-sect-dash-skills` worktree, expect 0 CS error.
- [ ] Test EditMode `CombatRuntimeServiceTests` cho 357: cast L1, L11, L20; expect sub-skill 389 fire mọi level.
- [ ] Test EditMode `TianWang33DurationTests`: cast 33 L1, expect buff expire sau ~120s (không phải 2.4s).
- [ ] Test EditMode `TianWang42FireresTest`: cast 42 L1, expect fire res = -5 (debuff, không phải +5 buff).
- [ ] Test EditMode `WuDang162DamageTest`: cast 162 L20, expect damage = (10, 100), không phải (144, 1476).
- [ ] Test EditMode `EMei93HealTest`: cast 93 L1, expect heal 275 HP (không phải mana regen 275).
- [ ] Test EditMode `TianRen150Test`: cast 150 L1, expect HP max giảm -11% (không phải +21%).
- [ ] Test EditMode `KunLun90FactionTest`: load skill 90, expect faction = KunLun (không phải EMei).
- [ ] Test visual: cast 357 ở full profile boot, expect player teleport tới enemy + slash effect.

## Effort summary (ngoài goal hiện tại)

- Phase 1 quick wins done: ~5 giờ (đã làm)
- Phase 3 dash skeleton: ~2 giờ (đã làm, MVP snap)
- Phase 3 dash full (lerp + obstacle + camera + input lock + visual): ~2-3 ngày
- Phase 4 event chain: ~1 ngày
- Phase 5 sub-form + tuning: ~2-3 tuần

