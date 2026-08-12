# Cái Bang slistcache Migration (2026-07-15)

## User decision

User: "chỉ Phi Long đúng PC, các skill kia sai. Mọi skill chủ động/bị động/buff/hỗ trợ phải 100% giống PC."
Version-priority conflict giữa mobile snapshot và PC slistcache → **user chọn slistcache (newest PC)** làm PC truth.

## Version-priority resolution

- Mobile `Assets/StreamingAssets/Reference/gaibang.lua` (md5 `cb3ec6e8...`, 562 dòng) = snapshot VLTK-mobile ship, match `00.src-tinh-kiem`, `update03`, `dmjx06`, `Server` (OLD values).
- PC `pak_unpacked/slistcache/script/skill/gaibang.lua` (md5 `d82c8bbb...`, 590 dòng) = server list cache, **newest PC balance patch** (chỉ slistcache có `_yan_p` variants + `fatallystrike_p` + `anti_block_rate`).
- Rule `evidence/version-priority.md` (newest PC wins) + user confirmation → slistcache authoritative.

9/20 bảng Lua khác nhau: `dagou_zhen`, `huabu_liushou`, `zuidie_kuangwu`, `huaxian_weiyi`, `xiaoyao_gong`, `gaibang120`, `gaibang120zuzhou`, `zhanggaibang150`, `gungaibang150`.

## Changes applied

### 1. Lua reference (source-backed)
- `Assets/StreamingAssets/Reference/gaibang.lua` = byte-exact copy của slistcache (`d82c8bbb...`). Runtime `PcCaiBangLuaLevelService` đọc file này → radius/missile-count/form/speed tự khớp slistcache.

### 2. Engine: 9 new MagicAttributeKind (KMagicDesc.cpp canonical)
`PhysicsResYanP`, `FireResYanP`, `AllResYanP`, `ReturnResP`, `AntiDoHurtP`, `FatallyStrikeP`, `Me2MetalDamageP`, `Metal2MeDamageP`, `AntiBlockRate`.
- Enum: `CombatDefinition.cs` (additive, cuối enum).
- Parser: `PcConfigParser.cs` switch map 9 tên `_p`/`_rate`.
- Consumption: `CombatRuntimeService.ApplyDamage` sum yang-res variants (`PhysicsResYanP`/`FireResYanP`/`AllResYanP`) vào `targetResist` cùng base res (PC KMagicDesc.cpp:241/230/231).
- TODO (ponytail): `ReturnResP`/`AntiDoHurtP`/`FatallyStrikeP`/`Me2MetalDamageP`/`Metal2MeDamageP`/`AntiBlockRate` đã store trong state cho data parity; consumption logic đầy đủ (return-damage resist, fatal-strike ignore-def, metal series, anti-block) defer — cần engine extension riêng.

### 3. Catalog (`PcCombatCatalogFactory.cs`) — 9 bảng + wire 3 skill ID
- **124 dagou_zhen**: `DogArrayPassive` helper — addphysicsdamage_p 53→348 (+L21=369), lifemax_yan_p 1→50 [NEW].
- **127 huabu_liushou**: fastwalkrun_p 9→5, 66→33.
- **129 huaxian_weiyi**: meleedamagereturn_p 4→1, 46→55 (L30), +anti_block_rate 1→30 [NEW].
- **130 zuidie_kuangwu**: allres_p→allres_yan_p (L30=15), lifemax_yan_p 20→60 (L35, dur finite), +5 attr mới (me2metal/metal2me/returnres/anti_do_hurt/physicsres_yan).
- **360 xiaoyao_gong**: `XiaoyaoGongPassive` helper — speed đa breakpoint (L25/31/32/33), +addphysicsdamage_p 10→120, deadlystrikeenhance_p 1→20 Conic [NEW].
- **720 gaibang120zuzhou**: physicsres_p→physicsres_yan_p, fireres_p→fireres_yan_p, resmax giá trị slistcache, +fastwalkrun_p -9→-50 [NEW].
- **1073 zhanggaibang150**: `ZhangGaiBang150Skill` helper — seriesdamage_p 40→80, firedamage_v 24→275 / 720→13750 (L50), +fatallystrike_p 1→30 [NEW], MaxLevel 20→27.
- **1074 gungaibang150**: MaxLevel 20→27, +breakpoints L23/L26 (physicsenhance/firedamage).
- **714 gaibang120**: proc% `AutoAttackProcPercent` Lerp 1→6 thành 1→10 (slistcache autoattackskill[3] {1,1},{20,10},{21,10}).
- Wire **129**→huaxian_weiyi, **389**→longzhan_yuye, **1072**→zhanggaibang150_2 vào `PcCaiBangLuaLevelService.SkillIdToName`.

## Verification

- Compile: clean (0 error).
- EditMode `CaiBang` category: **106/106 passed** (job `fdd18fc97cde4431af074945e2354f85`).
- EditMode `Skill`+`Smoke` category: **16/16 passed** (cross-faction regression, job `1ec6a5868a5347f98d93b7e1556ef771`).

## Remaining gaps (deferred, reported)

- 6 attr mới có consumption logic đầy đủ chưa implement (data parity OK, runtime effect một phần). Cần engine extension riêng: return-damage resist, fatal-strike (ignore-def crit), metal-series damage mod, anti-block rate.
- `gaibang_server.lua` mobile (dead file, không load) vẫn cũ — không ảnh hưởng runtime.
- Visual/audio smoke (Phase 5 unchecked) + fresh-context review + commit (Phase 6 unchecked) theo `tasks.md`.
