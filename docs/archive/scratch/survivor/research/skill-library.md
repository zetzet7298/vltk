# Research 04 — Full skill library mapping JX → survivor pool

Status: `done` (wayfinder inline — sub-agent aborted 3×, paseo transport unstable; resolved directly via shell probe of real data). Decision 01: parity = structure, KHÔNG numeric. Decision 02: tự author config.

Sources (read-only, verified):
- `Assets/StreamingAssets/Reference/PcSkills.txt` (609.775 B, TCVN3 via `PcText.ReadLinesTcvn3`, **113 cột**, 1.216 data rows)
- `Assets/StreamingAssets/Reference/PcAttrib/missles.txt` (441 data rows, 57 cột)
- `Assets/StreamingAssets/Reference/PcAttrib/missles1.txt`
- Parser: `Assets/Scripts/Combat/PcSkillFullParser.cs`, `Assets/Scripts/Combat/PcSkills1FullParser.cs`
- Model: `Assets/Scripts/Model/SkillDefinition.cs`
- Encoding: `Assets/Scripts/PortData/PcText.cs` (TCVN3 table, windows-1252 → unicode map)
- SPR runtime: `Assets/Scripts/Sprites/SprRuntimeService.cs` (root `/SpritesRuntime`, `ComputePathUidHex` GB2312 signed+unsigned) — per research 08
- Probes: `.scratch/survivor/research/_probe2.py`, `_probe3.py` (reproducible)

---

## 1. Inventory + breakdown

**Tổng: 1.216 skill** trong PcSkills.txt (curated reference). Server `settings/skills.txt` full = 1.555 row/114 col (parser comment) — rộng hơn, dùng khi cần bổ sung.

### Faction (col 70 `LvlSetScript`) — 10 phái chính ≈ 452 skill
| Faction (dir) | Skills | Ghi chú |
|---|---:|---|
| tangmen (Đường Môn) | 54 | + tangmeng 6 (GBK subdir) |
| cuiyan (Thúy Yên) | 53 | |
| emei (Nga Mi) | 51 | |
| tianwang (Thiên Vương) | 50 | + fengyun-jiang 2 |
| kunlun (Côn Lôn) | 50 | |
| shaolin (Thiếu Lâm) | 45 | + saolin 5 (GBK) |
| wudu (Ngũ Độc) | 40 | |
| wudang (Võ Đang) | 39 | + wudang GBK subdir |
| tianren (Thiên Nhẫn) | 37 | |
| gaibang (Cái Bang) | 33 | |

Remainder ≈ 764: `special` (417 — event/buff/supply/boss), `npc` (124 — monster/boss skill), `partner` (78 — pet), `battles` (47 — battlefield), `shipin` (25), misc. → **player-learnable pool = 10 phái; npc/partner/battles = monster & boss pool.**

### Cast form (col 19 `MisslesForm`, col 26 `IsMelee`, col 41 `ByMissle`)
| MisslesForm | Count | Nghĩa |
|---|---:|---|
| 7 | 652 | standard ranged (dominant) |
| 6 | 189 | |
| 1 | 174 | |
| 3 | 124 | |
| 2 | 23 | |
| **12** | **22** | **melee form** (AGENTS.md: MisslesForm=12 → child-missile visual, không cần PreCastSpr) |
| 0 | 19 | |
| 8/11/13/10/9/5 | 13 | misc |

- `IsMelee=1`: **104 skill** (melee — visual qua child missile + char anim, KHÔNG cần PreCastSpr).
- `ByMissle=1`: 224 (spawn missile).
- `ChildSkillId != 0`: **675** (có child missile → visual từ missles.txt AnimFile).
- `PreCastSpr` nonempty: **357/1216** (29%).
- `IsAura=1`: 41 (buff/aura — không phải attack skill).

### Visual portability (fail-closed — KHÔNG bịa)
- **PreCastSpr path** (col 6): GBK bytes → hash = `SprRuntimeService.ComputePathUidHex` (GB2312, thử signed + unsigned). Staged check = `SprRuntimeService.FindSprDataInRoot(hash)` against `/SpritesRuntime` (67.499 file, per research 08). **Chỉ 357 skill có PreCastSpr path** → chỉ những skill này có precast visual candidate; phần còn lại dựa vào **child missile AnimFile** (missles.txt col 29 `AnimFile1` … col 50 `AnimFile4`) hoặc **char anim** (melee).
- **Child missile visual**: 675 skill có `ChildSkillId` → tra `missles.txt[MissleId]` → `AnimFile1-4` (SPR anim path) → cùng hash/staged check. **NOTE**: AGENTS.md ghi nhiều child missile KHÔNG có cột sprite trong missles.txt (vd 20/408/274/1083..1088) → PC cũng không visual → **fail-closed đúng** (KHÔNG phải bug).
- **Không gán sprite chưa staged**. Proxy màu (P1) là fallback cho mọi skill chưa staged.

## 2. Schema mapping: PcSkills.txt → own `SkillDef`

> **BUG phát hiện**: `PcSkillFullParser.LvlSetScriptCol = 71` là SAI — header thực `LvlSetScript` ở **col 70**; col 71 = `LvlSetting1` (effect script). Parser đang đọc effect-script làm faction. **Phải sửa constant = 70 khi to-spec** (hoặc dùng header-name lookup). Đây là lý do breakdown faction lần đầu bị sai.

| PcSkills.txt cột | own SkillDef field | Ghi chú |
|---|---|---|
| 2 SkillId | `int Id` | PK |
| 0 SkillName (TCVN3) | `string Name` | decode qua PcText.ReadLinesTcvn3 |
| 70 LvlSetScript | `string Faction` | **col 70** — dir segment = faction key (shaolin/tianwang…) |
| 19 MisslesForm | `MissileForm Form` | enum; 12=melee (ngoài enum chuẩn per AGENTS.md) |
| 26 IsMelee | `bool IsMelee` | melee → child-missile visual |
| 41 ByMissle | `bool SpawnsMissile` | |
| 20 ChildSkillId | `int ChildMissileId` | → missles.txt lookup |
| 6 PreCastSpr (GBK bytes) | `string PreCastSprUid` | SprRuntimeService.ComputePathUidHex |
| 58 Param1 / 60 Param2 | `float FanAngleStep / FanOffset` | fan-spread (SKILL_MF_Spread): Param1=angle step 1/64 vòng, Param2=offset px |
| 4 SkillStyle / 3 Attrib | `int Style / Attrib` | |
| 11 IsAura | `bool IsBuff` | aura/buff skill |
| 14 AttackRadius | `int Range` | |
| 52 ReqLevel / 53 MaxLevel | `int ReqLevel / MaxLevel` | |
| 18 CharClass | `(discard)` | cặp phái, KHÔNG phải faction |
| 29 SkillCostType / 30 CostValue | `CostType / Cost` | |
| 31 TimePerCast | `float Cooldown` | |
| 33 IsPhysical | `bool IsPhysical` | |
| 35-40 Target* | `TargetFlags` | |
| 43-51 Start/Fly/Collide/Vanish Event+SkillId | `MissileEventHooks` | |
| 71-110 LvlSetting1-20 / LvlData1-20 | `LevelScaling[]` | 20 cấp — numeric OWN (per 01) |
| 112 LevelUpScript | `string LevelUpScript` | |
| 113 SkillDesc (TCVN3) | `string Desc` | i18n key nguồn |

**Numeric own-design** (per Decision 01 — dhcd blocked): `weight`, `level-scaling value`, `damage`, `cooldown tune`, `mana cost`. Schema-parity từ PcSkills.txt; balance = own.

## 3. missles.txt / missles1.txt schema (child-missile visual + behavior)

> **PRIORITY**: dùng **missles1.txt** (513 row, file Sandbox auto-mapper PcMissileFullVisualParser/Skills1FullCatalogService dùng) — đầy đủ hơn missles.txt (441). Hai file có cùng schema 57 col nhưng row-set khác nhau.
> **VISUAL**: AnimFile2 (col 32) = primary SPR path. AnimFile1 (col 29) = LUÔN trống. (Agent 04 late-return verified: 0 row có AnimFile1 trong cả 2 file.)

441 row, 57 cột. Quan trọng:
- 0 MissleId (FK từ PcSkills.ChildSkillId)
- 2 MoveKind, 3 FollowKind, 5 MissleHeight, 6 CollidRange, 10 LifeTime, 11 Speed, 12-13 Zspeed/Zacc
- 18 ResponseSkill (skill chạy khi collide — on-hit)
- 25-27 Param1/2/3
- **AnimFile2 (col 32) = PRIMARY missile visual** (412/513 row có data; **AnimFile1 col 29 LUÔN TRỐNG** — 0 row). AnimFile3/4 (col 35/38) = state variants (42/273). AnimFileInfo2 (col 33) = frame info. **PHẢI check AnimFile2 đầu tiên, KHÔNG AnimFile1** (verified cả missles.txt 353/441 + missles1.txt 412/513).
- 41-50 AnimFileB1-4 (burst/on-death anim)
- 31/34/37/40 SndFile1-4 (SFX)

→ own `MissileDef { Id, MoveKind, FollowKind, Speed, LifeTime, CollidRange, ResponseSkill, AnimFileUid[], SndFile[] }`. AnimFile = GBK path → `ComputePathUidHex` → staged check (fail-closed).

## 4. Fail-closed list (rule, không enumerate 1216)

KHÔNG enumerate từng skill (runtime check). Quy tắc cho to-spec:
1. Mỗi SkillDef có `PreCastSprUid` + `ChildMissileId`.
2. Runtime: `SprRuntimeService.FindSprDataInRoot(uid)` → null ⇒ **KHÔNG render sprite**, dùng proxy màu (P1).
3. Melee (Form=12/IsMelee=1): visual = char anim + child missile; PreCastSpr optional.
4. Child missile không có AnimFile trong missles.txt (vd id 20/408/274/1083-1088) ⇒ PC cũng không visual ⇒ **KHÔNG gán** (per AGENTS.md — fail-closed đúng, không bug).
5. Build staged-hash manifest 1 lần (tool `scan_required_spr.py` có sẵn trong vltktool) để pre-filter library trước ship.

## 5. Supply-skill subset (heal/bomb/magnet/full-clear)

Nguồn = faction `special` + effect-script (col 71 `LvlSetting1` từ probe1). Phân loại theo effect script:
- **Heal/HP**: `lifereplenish_v` (29), `lifemax_v` (36), `lifemax_p` (13), `lifereplenish` → supply-heal.
- **Damage/clear**: `physicsdamage_v` (28), `firedamage_v`(41), `lightingdamage_v`(39), `poisondamage_v`(35), `colddamage_v`(31) + `special\bomb.lua` (2 skill) → supply-bomb/AoE-clear.
- **Magnet/speed**: `fastwalkrun_p` (36) → movement; magnet = own-design (không có script trực tiếp — dhcd supply-magnet là LevelCollectItemMgr magnet radius, không phải skill).
- **Buff/aura**: IsAura=1 (41) + `allres_p`(27), `stun_p`(15) → supply-buff.

→ Supply pool = subset `special` + effect-script skills; heal/bomb có nguồn JX, **magnet = own** (thuộc collect mgr, không skill). Numeric (cooldown/effect) own.

## Verdict cho map.md

- **Skill library đủ clear để port**: 1.216 skill, 10 phái (~452 player pool) + npc/partner (boss/monster pool). Schema mapping chốt (bảng §2). missles.txt (441) cho child-missile visual.
- **Parser bug** LvlSetScriptCol 71→70: PHẢI sửa khi to-spec (ghi vào ticket implement).
- **Card pool composition law** (Not yet specified): giờ specifiable — pool per faction (10 phái) + supply subset; weight = own. → graduate fog.
- Fail-closed rule chốt (§4); KHÔNG enumerate, runtime check per skill.
- Numeric toàn bộ own (Decision 01).
