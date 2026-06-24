# Code Context

> **Note**: This run was a **gap-analysis subagent** for VLTK Mobile port. The `{{FACTION}}` placeholder in the task template was empty, so the next natural faction after Cái Bang (done) was picked: **Võ Đang / WuDang (武当)**. ID range 151-166. Section appended to report file.

## Files Retrieved

1. `/var/www/vltk-mobile.worktrees/all-sect-dash/.harness/baocao-all-sect-skills.md` (lines 1-350) — report file. Appended section "## 2.1. Võ Đang" starting after line 267.
2. `/var/www/vltk-mobile.worktrees/all-sect-dash/Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs` (lines 28-29, 274-449, 501-533, 585-588, 594, 102-107) — mobile catalog; `CreateWuDangSkills` list + helpers + faction enum + `IsWuDangSkill`.
3. `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/script/skill/wudang.lua` (413 lines, GB2312) — PC source-of-truth. Contains 14 skill entries: `nulei_zhi`, `wudang_jianfa`, `wudang_quanfa`, `canghai_mingyue`, `zuowang_wuwo`, `jianfei_jingtian`, `qingxing_zhen`, `tiyun_zong`, `boji_erfu`, `wuwo_wujian`, `taiji_shengong`, `sanhuan_taoyue`, `tiandi_wuji`, `qiwudang150`, `jianqi_zongheng`, `taiji_wuyi`, `nulei_lianhuanji`, `renjian_heyi`, `jianwudang150`, `jianwudang150_2`, `xuanyi_wuxiang`, `jianwudang150_3`, `wudang120`.
4. `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/script/skill/wudang/*.lua` (20 files, mostly TCVN3 + 3 pinyin) — per-skill level data formulas.
5. `/var/www/vltk-mobile.worktrees/all-sect-dash/Assets/StreamingAssets/Reference/KNpc.cpp` (lines 1829-1891) — `CastMeleeSkill` switch; confirms WuDang has NO dash/melee skills.
6. `/var/www/vltk-mobile.worktrees/all-sect-dash/Assets/Scripts/Sandbox/SkillEffectVisualService.cs` (lines 340-533) — `SpawnCollideSubEffect` (handles 1073 only) + `ConfigureWuDangVisuals` (handles 153/155/158/159/164/165 only — missing 163!).
7. `/var/www/vltk-mobile.worktrees/all-sect-dash/Assets/Scripts/Sandbox/PcSkillTuningRegistry.cs` (lines 102-107) — `RadiusCurves[WuDangId]` only covers 153/155/158.
8. No `PcWuDangModTuning.cs` exists (only `PcCaiBangModTuning.cs`).

## Key Code

### Mobile catalog — CreateWuDangSkills (PcCombatCatalogFactory.cs:274-317)
```csharp
public static List<SkillDefinition> CreateWuDangSkills() => new()
{
    WuDangPassiveJianFa(),      // 151 passive
    WuDangPassiveQuanFa(),      // 152 passive
    WuDangYinYangQi(),          // 154 passive
    WuDangLightningDamage(153, "怒雷指", "Nộ Lôi Chỉ", 10, 400, 24, 1, 11, ...),
    WuDangLightningDamage(155, "沧海明月", "Thương Hải Minh Nguyệt", 10, 480, 25, 1, 11, ...),
    WuDangChunYangXinFa(),      // 156 passive
    WuDangManaShield(),         // 157 self buff
    WuDangLightningDamage(158, "剑飞惊天", "Kiếm Phi Kinh Thiên", 30, 400, 26, 1, 11, ...),
    WuDangQiXingZhen(),         // 159 aura
    WuDangRunPassive(),         // 160 passive
    WuDangLiangYiXinFa(),       // 161 passive
    WuDangXuanYiWuXiang(),      // 162 active (SUSPECT G7)
    WuDangRenJianHeYi(),        // 163 active (SUSPECT G6)
    WuDangLightningDamage(164, "搏击二复", "Bác Cấp Nhi Phục", 50, 470, 28, 1, 11, ...),  // SUSPECT G4 radius
    WuDangLightningDamage(165, "无我无剑", "Vô Ngã Vô Kiếm", 50, 400, 29, 16, 11, ...),     // SUSPECT G4 childSkillNum
    WuDangTaiJiShenGong(),      // 166 passive
};
```

### Mobile catalog — 162 WuDangXuanYiWuXiang (line 405-407) — DAMAGE ~14× TOO HIGH
```csharp
private static SkillDefinition WuDangXuanYiWuXiang()
{
    var s = BaseSkill(162, "玄一无象", "Huyền Nhất Vô Tượng", 50, 20, 520, SkillMissileForm.Surround);
    s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 27; s.childSkillNum = 1;
    s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
    s.effectSourceId = Sprite("\\spr\\skill\\昆仑\\kl_16_魔法施放.spr");
    s.missileSpriteId = Sprite("\\spr\\skill\\武当\\wd_04_玄一无象.spr");
    AddLevels(s, lv => { var d = new SkillLevelData { level = lv };
        d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.LightingDamageV, 4 + lv * 7, 0, 296 + lv * 59));
        d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20 + lv * 3, 0, 0));
        return d; });
    return s;
}
```

### Mobile catalog — 163 WuDangRenJianHeYi (line 409-413) — CHILD WRONG, NO EVENT CHAIN HANDLER
```csharp
private static SkillDefinition WuDangRenJianHeYi()
{
    var s = BaseSkill(163, "人剑合一", "Nhân Kiếm Hợp Nhất", 50, 20, 90, SkillMissileForm.Surround);
    s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 215; s.childSkillNum = 1;  // 215 = no PC evidence
    s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
    s.effectSourceId = Sprite("\\spr\\skill\\昆仑\\kl_16_魔法施放.spr");
    // NO missileSpriteId set!
    AddLevels(s, lv => { ... }); return s;
}
```

### Mobile visual — ConfigureWuDangVisuals (SkillEffectVisualService.cs:501-533) — MISSING 163
```csharp
private void ConfigureWuDangVisuals(SkillDefinition skill, ActiveSkillEffect fx, int level)
{
    switch (skill.skillId)
    {
        case 153: SetupPcPreCast(...); SetupPcMissile(...); break;
        case 155: SetupPcPreCast(...); SetupPcMissile(...); break;
        case 158: SetupPcPreCast(...); SetupPcStationaryEffect(...); break;
        case 159: SetupPcMissile(...); break;
        case 164: SetupPcPreCast(...); SetupPcStationaryEffect(...); break;
        case 165: SetupPcPreCast(...); SetupPcMissile(...); SetupPcCircleOutwardMissiles(fx, Math.Max(1, skill.childSkillNum)); break;
        // NO case 162 (XuanyiWuxiang), NO case 163 (RenJianHeYi), NO case for passives
    }
}
```

### Mobile collision event — SpawnCollideSubEffect (SkillEffectVisualService.cs:455-473) — ONLY 1073
```csharp
private void SpawnCollideSubEffect(ActiveSkillEffect parentFx, Vector2 position)
{
    int subSkillId = parentFx.skillId switch
    {
        1073 => 1072,    // GB Thời Thặng Lục Long → Ngũ Diệu Càn Khôn
        _    => 0,       // 163 missing!
    };
    if (subSkillId == 0) return;
    ...
}
```

### PC source — renjian_heyi event chain (wudang.lua:271-321)
```lua
renjian_heyi={ --人剑合一
    ...
    skill_startevent={
        [1]={{1,0},{10,0},{10,1},{20,1}},     -- L10+ fires on cast
        [3]={{1,371},{20,371}}                  -- sub-skill 371
    },
    skill_collideevent={
        [1]={{1,0},{15,0},{15,1},{20,1}},     -- L15+ fires on collide
        [3]={{1,162},{20,162}}                  -- sub-skill 162 (XuanyiWuxiang)
    },
    skill_showevent={
        [1]={{1,0},{10,0},{10,1},{15,1},{15,5},{20,5}}   -- anim id 1 L10-14, id 5 L15-20
    },
}
```

### PC source — wuwo_wujian (wudang.lua:115-130)
```lua
wuwo_wujian={ --无我无剑  (ID 165)
    ...
    skill_misslenum_v={{{1,1},{20,8},{21,8}}},   -- MAX 8 missiles (mobile uses 16!)
    missle_speed_v={{{1,28},{20,32},{21,32}}},
    skill_attackradius={{{1,448},{20,512},{21,512}}},  -- L20=512 (mobile uses 400)
    ...
}
```

### PC source — xuanyi_wuxiang (wudang.lua:368-374) — PC MAIN TABLE
```lua
xuanyi_wuxiang={ --玄一无象  (ID 162)
    seriesdamage_p={{{1,20},{20,60},{21,62}}},
    lightingdamage_v={
        [1]={{1,1},{20,10}},     -- L20: min = 10
        [3]={{1,10},{20,100}}    -- L20: max = 100
    },
}
```

### PC per-skill file — xuanyi-wuxiang.lua — CONFLICTS WITH MAIN TABLE
```lua
function Getlightingdamage_v(level)
    result1 = 4+level*7       -- L20: 144
    result2 = 296+level*59    -- L20: 1476
    return Param2String(result1,0,result2)
end;
```
**Mobile uses the per-skill file formula (144/1476), but PC main table says 10/100. ~14× too high.**

### Mobile tuning — PcSkillTuningRegistry.cs:102-107 — COVERAGE 18%
```csharp
[CombatFactionExt.WuDangId] = new()
{
    [153] = new[] { (1, 400), (20, 400) },
    [155] = new[] { (1, 480), (20, 480) },
    [158] = new[] { (1, 400), (20, 400) },
    // MISSING: 162, 163, 164, 165
},
```

### C++ — KNpc.cpp:1829-1891 — NO WUDANG DASH
```cpp
BOOL KNpc::CastMeleeSkill(KSkill * pSkill) {
    switch(pSkill->GetMeleeType()) {
    case Melee_Jump: ...       // not used by WuDang
    case Melee_JumpAndAttack: ...  // not used by WuDang
    case Melee_RunAndAttack: ...  // not used by WuDang
    case Melee_ManyAttack: ...    // not used by WuDang
    }
}
```
Wudang.lua has no `MeleeType=Jump`/`JumpAndAttack` for any ID 151-166.

## Architecture

- **Catalog layer**: `PcCombatCatalogFactory.CreateWuDangSkills()` registers 16 skills. Each `BaseSkill` sets `skillId`, `reqLevel`, `attackRadius`, `missileForm`. Some call `WuDangLightningDamage()` helper.
- **Faction dispatch**: `IsWuDangSkill(id)` returns true iff `id ∈ [151, 166]`.
- **Tuning layer**: `PcSkillTuningRegistry.GetSkillSpec(skillId, level, factionId)` reads `RadiusCurves[WuDangId]` first, falls back to `PcCaiBangSkillTuning`, then scan-all-factions. **Coverage 18% for WuDang.**
- **Visual layer**: `SkillEffectVisualService.PlaySkillCast` dispatches to `ConfigureWuDangVisuals(skill, fx, level)` — switch covers 6 of 16 skills.
- **Event chain layer**: `SpawnCollideSubEffect(fx, position)` hard-coded to handle 1 case (1073). No generic `SpawnStartEvent` / `SpawnFlyEvent` / `SpawnVanishedEvent` runtime path.
- **PC pattern**: Wudang.lua declares per-skill event chains via `skill_startevent[3]`, `skill_collideevent[3]`, `skill_flyevent[3]`, `skill_showevent[3]`, `skill_vanishedevent[3]`. Mobile only handles `skill_collideevent` for 1 skill.
- **Per-skill files**: 20 `.lua` files in `script/skill/wudang/` — some have formulas that conflict with main `wudang.lua` table (xuanyi-wuxiang.lua vs wudang.lua). Authoritative source is the main `wudang.lua` table, per-skill files are obsolete per VLUpdate 27.

## Start Here

Another agent implementing these fixes should:
1. **Read the appended section** `/var/www/vltk-mobile.worktrees/all-sect-dash/.harness/baocao-all-sect-skills.md` lines 269-350 (section 2.1. Võ Đang).
2. **Verify per-skill conflict** for 162: read both `xuanyi-wuxiang.lua` and `wudang.lua` to confirm wudang.lua main table is canonical.
3. **Check `PcSkills.txt` 150-tier WuDang** to find IDs for `qiwudang150` / `jianwudang150` (likely 1105-1110 range based on wudang.lua's `skill_collideevent[3]={1,1105}`).
4. **Use `PcSkillTuningRegistry` pattern** to add 4 missing WuDang radius curves (162, 163, 164, 165).

## Cross-faction gap summary (Võ Đang)

| ID | Gap | Severity | Effort |
|---:|---|---|---|
| 162 | G7 — damage sai ~14× (per-skill file vs main table) | Cao | 1 giờ |
| 163 | G6 — thiếu event chain (StartEvent 371, CollideEvent 162, ShowEvent 1/5) | Cao | nửa ngày |
| 163 | G4 — childSkillId=215 không có trong PC | Cao (con of G6) | nửa ngày |
| 163 | G7 visual — thiếu case trong `ConfigureWuDangVisuals` | Cao | 1 giờ |
| 164 | G4 — radius sai (470 vs 416) | Trung bình | 30 phút |
| 165 | G4 — childSkillNum sai (16 vs 8) + radius sai (400 vs 512) | Cao | 30 phút |
| 153 | G4 — radius lệch nhẹ (400 vs 384) | Thấp | 30 phút |
| 155 | G4 — radius sai lớn (480 vs 384) | Trung bình | 30 phút |
| 166 | G4 — sai curve nội suy (bỏ điểm 33/35/38/41) | Trung bình | 1 giờ |
| 161 | G7 — không có per-skill file để verify | Thấp-TB | 1 giờ verify |
| Tuning | G7 — `PcSkillTuningRegistry.WuDangId` chỉ cover 3/16 (18%) | Trung bình | 1 ngày |
| Phase 5 | 10 sub-form/120/150-tier skill thiếu hoàn toàn | Trung bình | 2-3 ngày (Phase 5) |

**Võ Đang tổng kết**:
- **G1 dash**: 0 (faction không có dash, tương tự Thiếu Lâm/Đường Môn/Nga Mi)
- **G2 sub-skill gate**: 0
- **G3 visual close-range**: 0 (faction là ranged, không áp dụng)
- **G4 childSkillNum/radius sai**: 5 (165, 164, 155, 153, 166) — **nhiều nhất** trong các faction pure-magic
- **G5 id↔name swap**: 0
- **G6 event chain thiếu**: 1 (163 — nghiêm trọng)
- **G7 tuning coverage**: 1 (tuning 18%, cộng dồn 162 damage 14×)

**Tổng effort Phase 1 quick-win (Võ Đang)**: ~5 giờ. **Tổng effort Phase 4 event chain (163)**: nửa ngày. **Tổng effort Tuning coverage**: 1 ngày. **Tổng effort Phase 5 (150/120)**: 2-3 ngày.

**Võ Đang = faction "magic thuần" với 2 gap nặng**: (1) ID 162 damage quá lớn 14×, (2) ID 163 event chain hoàn toàn thiếu + visual case thiếu. Còn lại là sửa radius/curve nhỏ lẻ.
