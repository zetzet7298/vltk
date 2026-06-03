# Cái Bang Skills 100% PC Audit

> Audit all 17+ Cái Bang skills (PC 115-130 + MOD 274, 277, 357, 359, 360, 389, 209, 714, 720, 1073, 1074, 1101-1103, 1539) against PC source (`PcSkills.txt`, `PcMissles.txt`, `gaibang.lua`) and current Unity code.

**Status legend:** ✅ Match | ⚠️ Minor gap | ❌ Major gap

---

## PC Source Data Summary

### Skills 115-130 (PcSkills.txt, gaibang.lua)

| ID | PC Name | Form | Child | ChildNum | BaseSkill | ReqLv | MaxLv | MslsGenData | Param1 |
|----|---------|------|-------|----------|-----------|-------|-------|-------------|--------|
| 115 | Cái Bang Bổng pháp | 7 (passive) | 0 | 0 | 0 | 10 | 20 | 0 | 0 |
| 116 | Cái Bang Chưởng Pháp | 7 (passive) | 0 | 0 | 0 | 10 | 20 | 0 | 0 |
| 117 | Đầu Thạch Vấn Lộ (Ném Đá) | 1 (Single) | 44 | 1 | 1 | 10 | 20 | 0 | 0 |
| 118 | Cô Mộc Độn Lôi | 6 (Surround stationary) | 49 | 1 | 1 | 10 | 20 | 0 | 0 |
| 119 | Diên Môn Thác Bát | 1 (Single) | 45 | 1 | 1 | 10 | 20 | 0 | 0 |
| 120 | Bôn Lưu Đáo Hải | 6 (Surround stationary) | 50 | 1 | 1 | 20 | 20 | 0 | 0 |
| 121 | Diệu Thủ Không Không | 6 (Surround) | 0 | 0 | 0 | 20 | 20 | 0 | 0 |
| 122 | Kiến Nhân Thần Thủ | 1 (Single) | 46 | 1 | 1 | 10 | 20 | 0 | 0 |
| 123 | Khuê Mộc Tinh Chiếu | 6 (Surround stationary) | 51 | 1 | 1 | 30 | 20 | 0 | 0 |
| 124 | Đả Cẩu bổng | 7 (aura) | 0 | 1 | 0 | 30 | 20 | 0 | 0 |
| 125 | Bổng Đả ác Cẩu (NPC) | 3 (Circle) | 47 | 16 | 1 | 50 | 20 | 5 | 0 |
| 126 | Kim Ô Ánh Tuyết | 6 (Surround stationary) | 52 | 1 | 1 | 40 | 20 | 0 | 0 |
| 127 | Hoạt Bất Lưu Thủ | 6 (Surround) | 0 | 0 | 0 | 10 | 20 | 0 | 0 |
| 128 | Kháng Long Hữu Hối | 2 (Fan) | 48 | 8 | 1 | 50 | 20 | 0 | 3 |
| 129 | Hóa Hiểm Vi Di | 7 (passive) | 0 | 0 | 0 | 20 | 20 | 0 | 0 |
| 130 | Túy Điệp Cuồng Vũ | 7 (passive) | 0 | 0 | 0 | 60 | 30 | 0 | 0 |

### MOD Skills (PcSkills.txt)

| ID | PC Name | Form | Child | ChildNum | BaseSkill | ReqLv | MaxLv | MslsGenData | Param1 |
|----|---------|------|-------|----------|-----------|-------|-------|-------------|--------|
| 209 | Đả Cẩu bổng (aura child) | 3 (Circle) | 92 | 1 | 1 | 50 | 20 | 0 | 0 |
| 274 | Giáng Long Chưởng | 7 (passive) | 0 | 0 | 0 | 30 | 20 | 0 | 0 |
| 277 | Hoạt Bất Lưu Thủ (MOD) | 6 (Surround stationary) | 114 | 1 | 1 | 40 | 20 | 0 | 0 |
| 357 | Phi Long Tại Thiên | 0 (Single/parallel) | 166 | 3 | 1 | 80 | 20 | 0 | 32 |
| 359 | Thiên Hạ Vô Cẩu (player) | 0 (Single/parallel) | 168 | 16 | 1 | 80 | 20 | 0 | 32 |
| 360 | Tiêu Diêu Công | 7 (passive) | 0 | 0 | 0 | 60 | 20 | 0 | 0 |
| 389 | Long Chiến Ư Dã | 0 (stationary) | 195 | 1 | 1 | 80 | 20 | 0 | 0 |
| 714 | Hỗn Thiên Khí Công | 7 (passive) | 0 | 0 | 0 | 120 | 20 | 0 | 0 |
| 720 | Hỗn Thiên Khí Công Quyết Chú | 6 (Surround) | 275 | 1 | 1 | 120 | 20 | 0 | 0 |
| 1073 | Thời Thặng Lục Long | 1 (Single) | 335 | 1 | 1 | 150 | 20 | 0 | 0 |
| 1074 | Bổng Hoành Lược Mã | 1 (Single) | 336 | 5 | 1 | 150 | 20 | 9 | 0 |
| 1101 | (1101 sub-skill) | 1 (Single) | 363 | 1 | 1 | 150 | 20 | 0 | 0 |
| 1102 | (1102 sub-skill) | 0 (Single/parallel) | 362 | 3 | 1 | 150 | 20 | 0 | 32 |
| 1103 | Thời Thặng Lục Long Hỏa | 7 | 344 | 1 | 1 | 150 | 20 | 0 | 1 |
| 1539 | Thiên Hạ Vô Cẩu (NPC variant) | 3 (Circle) | 47 | 16 | 1 | 1 | 60 | 5 | 0 |

### Missiles (PcMissles.txt)

| MslID | Name | MoveKind | LifeTime | Speed | Height | CollidRange | AnimFileInfo1 | Notes |
|-------|------|----------|----------|-------|--------|-------------|---------------|-------|
| 44 | Đầu Thạch Vấn Lộ | **7** (bouncing) | 40 | 14 | 10 | 2 | 1,1,1 | MoveKind=7 = bouncing stone |
| 45 | Diên Môn Thác Bát | 1 (straight) | 16 | 31 | 10 | 1 | 64,16,1 | Ladle projectile |
| 46 | Kiến Nhân Thần Thủ | 1 (straight) | 16 | 31 | 10 | 1 | 64,16,1 | Hand projectile |
| 47 | Bổng Đả ác Cẩu | 1 (straight) | 16 | 31 | 10 | 1 | 64,16,1 | Stick projectile (NPC version) |
| 48 | Kháng Long Hữu Hối | 1 (straight) | 16 | 10 | 10 | 1 | 80,16,1 | Dragon projectile (PC: slow Speed=10) |
| 49-53 | Buffs (5x) | 0 (stationary) | 12-19 | 0 | 10-20 | 1 | various | All stationary |
| 92 | Đả Cẩu aura child | 0 (stationary) | 2 | 0 | 2 | 10 | (empty) | Aura tick |
| 114 | Hoành Bách Lộ Thiên (MOD 277) | 0 (stationary) | 2 | 1 | 18 | 3 | 20,1,1 | Buff stationary |
| 166 | Phi Long Tại Thiên | **5** (homing) | 24 | 30 | 10 | 1 | 80,16,1 | Homing dragon (hành vi dí) |
| 167 | Long Chiến Ư Dã | 0 (stationary) | 15 | 0 | 10 | 3 | 15,1,1 | Sâu xé stationary |
| 168 | Thiên Hạ Vô Cẩu | **5** (homing) | 32 | 24 | 10 | 1 | 64,16,1 | Homing stick (player 359) |
| 195 | Tiềm Long Tại Uyên | 0 (stationary) | 15 | 0 | 10 | 3 | 6,1,1 | Sâu xé stationary |
| 275 | (Hỗn Thiên Khí Công Quyết child) | ? | ? | ? | ? | ? | ? | Need to look up |
| 334 | (1102 sub) | 0 (stationary) | 10 | 0 | 10 | 1 | 11,1,1 | Tick |
| 335 | Thời Thặng Lục Long | 1 (straight) | 16 | 30 | 10 | 3 | 16,16,1 | Slow AOE |
| 336 | Bổng Hoành Lược Mã | **5** (homing) | 24 | 28 | 10 | 1 | 16,16,1 | Homing 150-tier stick |
| 344 | (1103 sub) | ? | ? | ? | ? | ? | ? | Need to look up |
| 362 | (1102 child) | ? | ? | ? | ? | ? | ? | Need to look up |
| 363 | (1101 child) | 1 (straight) | 16 | 30 | 10 | 1 | 64,16,1 | Slow stick |
| 389 | Vòng tròn hỗ trợ đồng đội | 0 (stationary) | 2 | 0 | 2 | 8 | (empty) | Aura range |

### MoveKind Reference (PC)
- 0 = Stationary (stays at spawn)
- 1 = Straight line (no tracking)
- 2 = Sine wave
- 3 = Bouncing
- 4 = ?
- 5 = Target-tracking (dí, follows enemy)
- 6 = Curve
- 7 = Bouncing/ricochet (used for "Stone Throw" stones)

### gaibang.lua level tables (PC source of truth for level scaling)

| Skill | misslenum_v | misslesform_v | param1_v | speed_v | radius_v | cost_v |
|-------|-------------|---------------|----------|---------|----------|--------|
| 117 (yanmen_tuobo) | (none) | (none) | (none) | L1=20, L20=24 | L1=320, L20=384 | L1=10, L20=10 |
| 119/122 (jianren_shenshou) | (none) | (none) | (none) | L1=20, L20=24 | L1=320, L20=384 | L1=25, L20=25 |
| 125 (bangda_egou) | (none) | (none) | (none) | L1=28, L20=32 | L1=448, L20=512 | L1=28, L20=48 |
| 128 (kanglong_youhui) | L1=1, L10=1, L20=15, L25=18 | L1=1, L10=1, L10=2, L20=2 | L1=0, L10=0, L10=2, L20=2 | L1=28, L20=32 | L1=448, L20=512 | L1=10, L20=50 |
| 130 (zuidie_kuangwu) | (none) | (none) | (none) | (none) | (none) | L1=50, L20=100 |
| 357 (feilong_zaitian) | L1=1, L11=1, L12=2, L15=2, L16=3, L20=4 | L1=1, L11=1, L11=0, L20=0 | (none) | L1=20, L20=24 | L1=448, L20=512 | L1=10, L20=65 |
| 359 (tianxia_wugou) | L1=1, L20=3 | (none) | (none) | L1=20, L20=24 | L1=448, L20=512 | L1=20, L20=50 |
| 1073 (zhanggaibang150) | L1=1, L11=1, L12=2, L15=2, L16=2, L20=3 | (none) | (none) | L1=24, L20=40 | L1=448, L20=512 | L1=12, L20=78 |
| 1074 (gungaibang150) | L1=1, L20=5 | (none) | (none) | L1=24, L20=24 | L1=448, L20=512 | L1=20, L20=50 |

### Cost values vs current Unity catalog

| Skill | PC cost L1→L20 | Unity cost L1→L20 | Status |
|-------|----------------|-------------------|--------|
| 117 | 10 → 10 | 10 → 10 | ✅ |
| 119 | (no data) | 10 → 10 | ⚠️ PC: should be 10? check |
| 122 | 25 → 25 | 25 → 25 | ✅ |
| 125 | 28 → 48 | 28 → 48 | ✅ |
| 128 | 10 → 50 | 10 → 50 | ✅ |
| 357 | 10 → 65 | 10 → 65 | ✅ |
| 359 | 20 → 50 | 20 → 50 | ✅ |
| 1073 | 12 → 78 | 20 → 50 | ❌ WRONG (should be 12→78) |
| 1074 | 20 → 50 | 20 → 50 | ✅ |

### Damage values vs current Unity catalog

| Skill | PC firedamage L1→L20 | Unity fire L1→L20 | Status |
|-------|----------------------|-------------------|--------|
| 117 | L1=10, L20=100 (and L3 [3] = 150) | L1=10, L20=100 (and L3 = 150) | ✅ |
| 119 | (no data, =117) | L1=10, L20=100 (and L3 = 150) | ✅ |
| 122 | L1=15, L20=75 (L3 = 215) | L1=15, L20=75 (L3 = 120) | ❌ L3[3]=215, Unity has 120 |
| 125 | L1=70, L20=360 (L3 = 420) | L1=70, L20=360 (L3 = 420) | ✅ |
| 128 | L1=10, L20=536 | L1=10, L20=536 | ✅ |
| 357 | L1=10, L15=300, L20=750 | L1=10, L15=300, L20=750 | ✅ |
| 359 | L1=70, L15=150, L20=285 (L3 = 432) | L1=70, L15=150, L20=285 (L3 = 432) | ✅ |
| 1073 | L1=24, L15=720, L20=1800 (L3 = same) | L1=24, L15=720, L20=1800 | ✅ |
| 1074 | L1=60, L15=120, L20=230 (L3 = L1=160, L15=160, L20=345) | L1=60, L15=120, L20=230 (L3 = 160/160/345) | ✅ |

### Attack Radius vs current Unity catalog

| Skill | PC radius L1→L20 | Unity radius L1→L20 | Status |
|-------|------------------|---------------------|--------|
| 117 | 320 → 384 | 280 (static) | ❌ WRONG static 280 |
| 119 | 320 → 384 | 240 (static) | ❌ WRONG static 240 |
| 122 | 320 → 384 | 300 (static) | ❌ WRONG static 300 |
| 125 | 448 → 512 | 400 (static) | ❌ WRONG static 400 |
| 128 | 448 → 512 | 512 (static) | ⚠️ Should scale L1=448 |
| 357 | 448 → 512 | 400 (static) | ❌ WRONG static 400 |
| 359 | 448 → 512 | 400 (static) | ❌ WRONG static 400 |
| 1073 | 448 → 512 | 400 (static) | ❌ WRONG static 400 |
| 1074 | 448 → 512 | 400 (static) | ❌ WRONG static 400 |

> **Major issue**: Attack radius in Unity is hardcoded as a single value per skill (e.g. 280 for skill 117). PC scales it from 320→384. The runtime should pick the L1 value or interpolate by level.

### Missile speed vs current Unity visual config

| Skill | PC msl Speed (col12) | Unity `pcMissileSpeedPerTick` | Unity `missileSpeed` | Status |
|-------|----------------------|-------------------------------|----------------------|--------|
| 117 (msl 44) | **14** | 14 | 14×18=252 | ✅ (but MoveKind=7 bouncing, not straight!) |
| 119 (msl 45) | **31** | 16 | 16×18=288 | ❌ WRONG speed 16 vs 31 |
| 122 (msl 46) | **31** | 20 | 20×18=360 | ❌ WRONG speed 20 vs 31 |
| 125 (msl 47) | **31** | 12 | 12×18=216 | ❌ WRONG speed 12 vs 31 |
| 128 (msl 48) | **10** | 18 | 18×18=324 | ❌ WRONG speed 18 vs 10 |
| 357 (msl 166) | **30** | 20 | 20×18=360 | ❌ WRONG speed 20 vs 30 |
| 359 (msl 168) | **24** | 20 | 20×18=360 | ❌ WRONG speed 20 vs 24 |
| 1073 (msl 335) | **30** | 24 | 24×18=432 | ❌ WRONG speed 24 vs 30 |
| 1074 (msl 336) | **28** | 24 | 24×18=432 | ❌ WRONG speed 24 vs 28 |

> **Major issue**: 8/10 damage skills have wrong missile speed. Only 117 is correct. The visual service uses L1 missle_speed_v from gaibang.lua, but PC missles.txt Speed is the canonical per-tick speed (PC gaibang.lua missle_speed_v is a different concept — total speed across multiple ticks).

### Missile lifetime vs current Unity

| Skill | PC LifeTime (col11) | Unity `pcMissileLifeTicks` | Status |
|-------|---------------------|----------------------------|--------|
| 117 | 40 | 40 | ✅ |
| 119 | 16 | 15 | ⚠️ off by 1 |
| 122 | 16 | 15 | ⚠️ off by 1 |
| 125 | 16 | 34 | ❌ WRONG 34 vs 16 |
| 128 | 16 | 20 | ❌ WRONG 20 vs 16 |
| 357 | 24 | 20 | ❌ WRONG 20 vs 24 |
| 359 | 32 | 24 | ❌ WRONG 24 vs 32 |
| 1073 | 16 | 24 | ❌ WRONG 24 vs 16 |
| 1074 | 24 | 24 | ✅ |

### CollideEvent (sâu xé) - is homing/rend correctly wired?

| Skill | PC CollideEvent | CollidSkillId | Current homing? | Current sâu xé? | Status |
|-------|-----------------|---------------|-----------------|-----------------|--------|
| 117 | (none) | - | ❌ no | ❌ no | OK no event |
| 119 | (none) | - | ❌ no | ❌ no | OK no event |
| 122 | (none) | - | ❌ no | ❌ no | OK no event |
| 125 | (none) | - | ❌ no | ❌ no | OK no event |
| 128 | (none) | - | ❌ no | ❌ no | OK no event |
| 357 | L10=1, L20=1 (yes) | 389 | ✅ yes (msl 166 MoveKind=5) | ❌ NO 389 trigger | ⚠️ missing sâu xé |
| 359 | (none) | - | ✅ yes (msl 168 MoveKind=5) | ❌ no event | OK no event |
| 1073 | L10=1, L20=1 (yes) | 1072 | ❌ NO (msl 335 MoveKind=1) | ❌ NO 1072 | ❌ msl 335 is straight, not homing! |
| 1074 | (none) | - | ✅ yes (msl 336 MoveKind=5) | ❌ no event | OK no event |

> **Major issue**: 
> - 1073 (Thời Thặng Lục Long) uses missile 335 which is MoveKind=1 (straight), but the user said earlier it has a "rồng" (dragon) visual. PC actually has it as straight, so the current code is correct for straight. But the visual should still be the dragon SPR (which is mag_gb_05_亢龙有悔.spr, same as 128 Kháng Long).
> - 357 (Phi Long) is missing sâu xé trigger (CollideEvent[3] = 389, which should fire when missile hits enemy). The current code triggers sâu xé via proximity (rendRadius=5) but doesn't actually spawn skill 389 effect.
> - Need to verify whether PC actually spawns a child skill 389 OR just does proximity damage.

---

## Per-Skill Audit (current Unity state)

### Damage skills (need visual + speed + radius fix)

| ID | Name | Catalog OK? | Visual OK? | Data gaps | Priority |
|----|------|-------------|-----------|-----------|----------|
| 117 | Đầu Thạch Vấn Lộ (Ném Đá) | ✅ form=1, child=44 | ❌ MoveKind=7 not implemented, no bouncing/ricochet | radius static 280 (PC 320→384) | **HIGH** |
| 119 | Diên Môn Thác Bát | ✅ form=1, child=45 | ❌ speed 16 vs PC 31, lifeTicks 15 vs 16 | radius static 240 (PC 320→384) | **HIGH** |
| 122 | Kiến Nhân Thần Thủ | ✅ form=1, child=46 | ❌ speed 20 vs PC 31, L3[3]=120 vs PC 215 | radius static 300 (PC 320→384) | **HIGH** |
| 125 | Thiên Hạ Vô Cẩu (NPC) | ✅ form=3 Circle, child=47, num=16 | ❌ speed 12 vs PC 31, lifeTicks 34 vs 16 | radius static 400 (PC 448→512) | **HIGH** |
| 128 | Kháng Long Hữu Hối | ✅ form=2 Fan, child=48, num=15 (gaibang) | ❌ speed 18 vs PC 10 (much faster in Unity!), lifeTicks 20 vs 16 | radius static 512 (PC 448→512) | **HIGH** |
| 1539 | THVC NPC variant | ✅ same as 125 | (same issues as 125) | (same as 125) | HIGH |
| 357 | Phi Long Tại Thiên | ✅ form=0, child=166 | ⚠️ sâu xé missing CollidEvent 389 | radius static 400 (PC 448→512) | **MED** |
| 359 | Thiên Hạ Vô Cẩu (player) | ✅ form=0, child=168 | ❌ uses KangLong spread (fan), should be Phi Long parallel | radius static 400 (PC 448→512) | **MED** |
| 1073 | Thời Thặng Lục Long | ✅ form=1, child=335 | ❌ straight, no homing (PC: 3-phase event chain, 1101→1103→1072) | cost L1=20 vs PC 12, radius static 400 | **HIGH** |
| 1074 | Bổng Hoành Lược Mã | ✅ form=1, child=336 | ❌ speed 24 vs PC 28, mslCount wrong (L6=2, L12=3, L16=4 vs PC L1=1, L20=5 linear) | radius static 400 (PC 448→512) | **HIGH** |

### Buff skills (need radius fix + verify stationary visual)

| ID | Name | Catalog OK? | Visual OK? | Data gaps | Priority |
|----|------|-------------|-----------|-----------|----------|
| 118 | Cô Mộc Độn Lôi | ✅ form=6, child=49 | ✅ SetupPcStationaryEffect("9ba1b99d", 13, 1, 2, white) | (none) | LOW |
| 120 | Bôn Lưu Đáo Hải | ✅ form=6, child=50 | ✅ SetupPcStationaryEffect("3ab94121", 15, 1, 2, light blue) | (none) | LOW |
| 123 | Khuê Mộc Tinh Chiếu | ✅ form=6, child=51 | ✅ SetupPcStationaryEffect("ea9d621d", 15, 1, 2, magenta) | (none) | LOW |
| 126 | Kim Ô Ánh Tuyết | ✅ form=6, child=52 | ✅ SetupPcStationaryEffect("7770c465", 20, 1, 2, gold) | (none) | LOW |
| 129 | Hóa Hiểm Vi Di | ✅ form=7 (passive aura) | ✅ Aura | (none) | LOW |
| 127 | Hoạt Bất Lưu Thủ | ✅ form=6, targetSelf | ✅ Aura-style | (none) | LOW |
| 130 | Túy Điệp Cuồng Vũ | ✅ form=7, maxLv=30 | ✅ Aura | (none) | LOW |
| 277 | Hoành Bách Lộ Thiên (MOD 277) | ✅ form=6, child=114 | ✅ SetupPcStationaryEffect("7770c465", 20, 1, 1, gold) | (none) | LOW |
| 274 | Giương Long Chưởng | ✅ passive | ✅ no visual | (none) | LOW |
| 360 | Tiêu Dao Công | ✅ passive | ✅ no visual | (none) | LOW |
| 714 | Hỗn Thiên Khí Công | ✅ passive | ✅ no visual | (none) | LOW |
| 720 | Hỗn Thiên Khí Công Quyết Chí | ✅ form=6, child=275 | ✅ SetupSurroundMissiles(4) | ❌ no SPR setup, just generic 4-missile surround | MED |

### Utility skills

| ID | Name | Catalog OK? | Visual OK? | Data gaps | Priority |
|----|------|-------------|-----------|-----------|----------|
| 121 | Diệu Thủ Không Không | ✅ form=6, child=0 | ⚠️ SetupSurroundMissiles(4) generic, no PC visual | (none) | LOW |
| 124 | Đả Cẩu Trận | ✅ form=7, aura | ✅ Aura | (none) | LOW |
| 1539 | Thiên Hạ Vô Cẩu (NPC) | ✅ form=Surround, child=47 | ✅ case 1539, missile 47 (Speed=31, LifeTime=16) | (none) | LOW |

### Sub-skills (CollideEvent/SpawnEvent)

| ID | Name | Catalog OK? | Visual OK? | Data gaps | Priority |
|----|------|-------------|-----------|-----------|----------|
| 195 | Tiềm Long Tại Uyên | (not in catalog - is child) | n/a | n/a | n/a |
| 209 | Đả Cẩu Trận Tử Đạn | ✅ form=3, child=92 | ✅ SetupSurroundMissiles? actually missle 92 is stationary | (none) | LOW |
| 275 | Hỗn Thiên Khí Công Quyết child | ✅ form=6, child=275 | (uses 720) | (none) | LOW |
| 334 | 1072 sub | (not in catalog - is child) | n/a | n/a | n/a |
| 344 | 1103 sub | (not in catalog) | n/a | n/a | n/a |
| 362 | 1102 sub | (not in catalog) | n/a | n/a | n/a |
| 363 | 1101 sub | (not in catalog) | n/a | n/a | n/a |
| 389 | Long Chiến Ư Dã | ✅ form=0, child=195, maxLv=20 | ✅ SetupPcStationaryEffect("b91ab706", 6, 1, 1) | (none) | LOW |
| 1072 | Ngũ Diệu Càn Khôn | ✅ form=0, child=334, maxLv=20 | ✅ case 1072, missile 334 (Speed=0, LifeTime=10), spawn via SpawnCollideSubEffect on 1073 impact | (none) | LOW |
| 1101-1103 | (1073 event chain) | (not in catalog - sub-skill-only) | (not in visual - 1101/1102/1103 internal to 1073 flow) | sub-skill chain not exercised in current visual | LOW |

---

## Major Issues Summary

### Tier 1: Critical (DONE in Phase 1)
1. ✅ **Speed mismatch in visual service**: 9/10 damage skills have wrong missile speeds. Use `PcMissles.txt` Speed column directly.
2. ✅ **Attack radius hardcoded**: All damage skills use static radius. PC scales L1→L20 via `PcCaiBangSkillTuning`.
3. ⚠️ **Skill 117 MoveKind=7 (bouncing)**: Kept as straight line — Z-axis (Zspeed=10240, Zacc=2048) doesn't translate to 2D top-down. PC engine uses parabolic arc that has no equivalent in flat 2D.
4. ✅ **Skill 1073 CollideEvent 1072 trigger**: Now spawns `NguDieuCanKhonSkill()` (1072 → 334 stationary) via `SpawnCollideSubEffect()` on 335 missile arrival.
5. ✅ **Skill 122 L3[3] damage wrong**: PC 215, Unity 120. Fixed in catalog.
6. ✅ **Skill 1073 cost wrong**: PC L1=12, L20=78. Fixed in catalog.

### Tier 2: Important (DONE in Phase 2 + 3)
7. ✅ **Skill 357 missing CollideEvent 389 trigger**: Test `CaiBang_PhiLongAtLevel11_TriggersLongChienUYuye` already verified; runtime spawns 389→195 stationary chain.
8. ✅ **Skill 359 uses KangLong spread**: Now uses `SetupPcPhiLongSpread` with parallel offset 32 (matches PC param1=32 default).
9. ✅ **Skill 1074 mslCount wrong**: PC L1=1, L20=5 linear. Now `Mathf.RoundToInt(Mathf.Lerp(1f, 5f, (level - 1) / 19f))`.
10. ✅ **LifeTime wrong in 6 skills**: All use `PcMissles.txt` LifeTime column (verified by 4 new tests).

### Tier 3: Polish (PARTIAL)
11. ⚠️ **720 has no SPR**: Uses generic SetupSurroundMissiles. Real PC SPR not yet wired.
12. ⚠️ **1073 FlyEvent 1101/1103 not spawned**: Only CollideEvent 1072 is triggered. PC's pre-cast 1101 and mid-flight 1103 sub-effects are not visualized. Reason: 1101/1102/1103 are sub-skill-only (no catalog entry needed) and would require deep refactor of the visual service phase machine.
13. ✅ **Add tests for each skill**: 13 new EditMode tests added: `CaiBang_117_VisualServiceUsesPcMissile44SpeedAndLife`, `CaiBang_122_FireDamageMaxesAtPc215_AtLevel20`, `CaiBang_128_VisualServiceUsesGaibangLuaMissileSpeed`, `CaiBang_359_VisualServiceUsesPcMissile168HomingSpeed`, `CaiBang_1073_CollideEvent1072_RegisteredInCatalog`, `CaiBang_117_MoveKind7_HasLongerFlightTime_ThanStraightSkills`, `CaiBang_1074_MslCountInterpolatesLinearly_FromL1ToL20`, `CaiBang_1539_VisualServiceUsesPcMissile47Speed`, plus the 5 from Phase 1.

### Out of scope (intentionally skipped)
- 117 MoveKind=7 parabolic arc: PC `Zspeed=10240`, `Zacc=2048` represents a lobbed throw. Our 2D top-down renderer has no Z axis. Stone appears as a static sprite flying straight.
- 1101/1102/1103 sub-skill chain: PC engine (KNpc.cpp) handles these via `m_SkillParam1/2` casting. Our visual service has only 3 phases (PreCast → Missile → Impact). Adding 1101 (pre-cast) and 1103 (mid-flight) would require adding new phase states.
- 117 Y-bob simulation: Considered but rejected — would visually mislead in 2D top-down.

---

## Fix Plan (prioritized)

### Phase 1: Data correctness (catalog + visual service)
1. Fix missile speed: use PC missles.txt Speed value
2. Fix missile lifetime: use PC missles.txt LifeTime value
3. Add attack radius scaling: use Link() from PcCaiBangModTuning pattern, add to all damage skills
4. Fix 122 L3[3] damage: 120 → 215
5. Fix 1073 cost: L1=12, L20=78
6. Fix 1074 mslCount: linear L1=1, L20=5

### Phase 2: Visual behavior
7. Implement MoveKind=7 bouncing for skill 117 (Ném Đá)
8. Fix 357 CollideEvent 389: when missile arrives at target, spawn separate 389 effect at impact point
9. Fix 359 spread: use Phi Long parallel (perpendicular), not Kang Long fan
10. Add 1073 event chain: pre-cast 1101, mid-flight 1103, on-collide 1072

### Phase 3: Polish + tests
11. Add real PC SPR for skill 720
12. Add EditMode tests for each skill (damage, radius, cost, count per level, homing, sâu xé)
13. Add PlayMode visual tests

---

## Open Questions
1. **Skill 117 MoveKind=7**: Bouncing how? Stones bounce N times then expire? Or bounce back to caster? Need PC source code.
2. **Skill 1073 event chain**: Should all 3 phases (pre-cast/fly/collide) play? Or only the main missile?
3. **Skill 389 sâu xé**: Is it a separate child effect (rend), or just damage applied? Current code uses rend visual.

---

## File Locations
- PC source: `/var/www/vltk-mobile/Assets/StreamingAssets/Reference/{PcSkills.txt, PcMissles.txt, gaibang.lua, KNpc.cpp}`
- Unity catalog: `/var/www/vltk-mobile/Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs`
- Unity visual: `/var/www/vltk-mobile/Assets/Scripts/Sandbox/SkillEffectVisualService.cs`
- Unity overlay: `/var/www/vltk-mobile/Assets/Scripts/UI/SkillEffectWorldOverlay.cs`
- Unity tuning: `/var/www/vltk-mobile/Assets/Scripts/Sandbox/PcCaiBangModTuning.cs`
- Unity tests: `/var/www/vltk-mobile/Assets/Tests/EditMode/Sandbox/CaiBangCombatParityTests.cs`
