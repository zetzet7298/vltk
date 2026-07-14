# Apply Progress: Cai Bang Skill PC Parity

## Status
Planning/evidence phase started. No implementation changes have been applied under this SDD change yet.

## Completed
- Completed Phase 1C Phi Long homing lane slice:
  - Verified canonical PC `gaibang.lua` in mobile reference, PC Client, PC Server, and unpacked PAK all has `feilong_zaitian` with `skill_misslesform_v L20=0` and `skill_misslenum_v L20=4`, but no `skill_param1_v`.
  - Used PC `skills.txt` row 357 `Param1=32` and missile 166 `MoveKind=5` as the source for parallel lane gap + homing behavior.
  - Updated `SkillEffectVisualService` so `luaCount>1` always configures `SetupPcPhiLongSpread` with fallback step `32` when Lua has no `skill_param1_v`.
  - Re-enabled/strengthened `CaiBang_357_HomingSpreadKeepsPerMissileOffsetsForLiveTarget`; it now asserts four lane offsets and live-target homing without `Assert.Ignore`.
  - Verified filtered CaiBang tests green: Unity EditMode job `b0ed18f3971f40cd865aaa62f9f7475e`, 82 total / 82 passed / 0 failed / 0 skipped.
- Completed Phase 2 Kháng Long spread evidence/test slice:
  - Resolved PC row 128: `MisslesForm=2`, child missile `48`, row `Param1=3`, Lua L20 `skill_misslenum_v=15`, `skill_param1_v=2`.
  - Resolved missile 48 resources in `evidence/khang-long-resource-evidence.md`; it shares dragon/impact SPRs with Phi Long but uses `MoveKind=1` non-homing.
  - Added `CaiBang_128_KhangLongUsesFanSpreadFromLuaMissileForm` verifying 15 fan targets, upward/downward lanes, center lane, and no Phi Long homing offsets.
  - Verified filtered CaiBang tests green: Unity EditMode job `46ef0c54113f4537895d1dd8a44cefb2`, 83 total / 83 passed / 0 failed / 0 skipped.
- Completed Phase 3 buff runtime evidence/test slice:
  - Verified Hoạt Bất Lưu Thủ `127` PC L20 `FastWalkRunP=66`, duration `3240`, cost `50`.
  - Verified Túy Điệp Cuồng Vũ `130` PC active buff durations at L20 (`2867` ticks for allres/fire/deadly states; `LifeMaxYanP=-1` sentinel), and cost `100`.
  - Fixed `PcCombatCatalogFactory` so `130` no longer uses permanent `-1` duration for all active buff states; only `LifeMaxYanP` keeps PC `-1` sentinel.
  - Added runtime cast tests for 127 and 130 buff application.
  - Verified filtered CaiBang tests green: Unity EditMode job `42335754025b435c9f17d4ed3578cc2c`, 85 total / 85 passed / 0 failed / 0 skipped.
  - Verified focused buff expiration runtime test green: Unity EditMode job `f0ce62a7e9744188af5f752b0ed158c7`, 1 total / 1 passed.
- Created SDD artifacts:
  - `proposal.md`
  - `spec.md`
  - `design.md`
  - `tasks.md`
- Created initial evidence matrix:
  - `evidence/caibang-skill-matrix.md`
- Loaded/used mandatory PC resource resolver workflow and project PC port rules.
- Researched required PC docs:
  - `/var/www/jx-source/docs/3_ky_nang_va_chieu_thuc.md`
  - `/var/www/jx-source/docs/client_port/03_skills.md`
  - `/var/www/jx-source/docs/backend_port/03_skills.md`
- Per project instruction, attempted external research through Exa and DeepWiki before implementation:
  - Exa: Unity 2D homing projectile/determinism references.
  - DeepWiki: UnityCsReference question about deterministic 2D projectile movement and testing caveats.
- Ran current CaiBang baseline tests and fixed stale/flaky tests:
  - Updated `CaiBangCombatParityTests.Catalog_LoadsNoviceAndAllCaiBangSkills` expected count from 39 to 40 because catalog now includes valid Phi Long collide child skill `389`.
  - Injected deterministic `DamageFormulaService.RollPercent = _ => true` in two damage-value tests so they do not randomly fail on PC hit/miss chance while testing damage values.
- Verified filtered CaiBang tests green: Unity EditMode job `f8bfebda6ad2430192fd6828e106a2a2`, 82 total / 81 passed / 0 failed / 1 ignored.
- Resolved initial Phi Long PC resource evidence with JX hash workflow in `evidence/phi-long-resource-evidence.md`:
  - Skill 357 PreCastSpr `b91ab706.spr`.
  - Missile 166 `MoveKind=5` homing.
  - Missile 166 dragon AnimFile2 `a31b9f04.spr`.
  - Missile 166 impact AnimFile4 `c33e96c2.spr`.
  - Missile 166 move SFX `3b6bb009.wav`.

## Important Current-Code Discovery
Some older `.agents` handoff recommendations are already implemented in the current codebase:
- `PcCaiBangLuaLevelService` already maps `127 -> huabu_liushou` and `130 -> zuidie_kuangwu`.
- `SkillEffectVisualService` already routes Cai Bang multi-missile setup through `GetMissileForm` and uses `SetupPcKangLongSpread` for form `2`.
- `ActiveSkillEffect.ResolveMissileTarget(int index)` already exists and is used by both `SkillEffectRenderer` and `SkillEffectWorldOverlay`.

Therefore, the next apply step should not blindly re-implement those old fixes. It should verify them against PC evidence and focus on remaining gaps.

## Next Recommended Apply Slice
Phase 4:
- Started active damage/chain version-priority slice:
  - Recorded user rule: when PC has multiple versions, newest/update override PC source wins (`evidence/version-priority.md`).
  - Compared mobile reference, `_slistcache`, `vltkdata`, Client 6.0, and Server skill-goc `gaibang.lua`; checked 125/359/1073/1074 active-chain attributes agree across these sources.
  - Corrected Lua mapping from stale `125 -> tianxia_wugou` to newest PC `125 -> bangda_egou`; `359/1539 -> tianxia_wugou`.
  - Updated 1539 catalog shape to match newest PC Thiên Hạ variant: child missile 168, single/homing form with Lua count override.
  - Updated `CombatRuntimeService` to support multiple `addskilldamageN` chain slots per parent and to use deterministic injected `DamageFormulaService.RollPercent` for chain tests.
  - Updated/add tests so 125 now verifies both PC chains (`359` chance 60 and `1074` chance 50) and deterministic cast expects 8 chain projectiles (`359` L20 count 3 + `1074` L20 count 5).
  - Verified filtered CaiBang tests green: Unity EditMode job `605e92791d464058a663cb3ec33faf17`, 85 total / 85 passed / 0 failed / 0 skipped.
- Continued defender-state damage slice:
  - Added PC evidence from `Assets/StreamingAssets/Reference/KNpc.cpp::CalcDamage`: defender current resist is read by damage type and applied via `nDamage -= nDamage * nRes / MAX_PERCENT`.
  - Added `evidence/defender-state-damage-evidence.md`.
  - Added `CaiBang_117_DefenderAllResStateReducesIncomingDamage`, proving active defender `AllResP` reduces incoming Cai Bang damage through `CombatRuntimeService.ApplyDamage` -> `DefenderStats`.
  - Verified filtered CaiBang tests green: Unity EditMode job `e961aace5c714d9aa8606aaf2a18c494`, 86 total / 86 passed / 0 failed / 0 skipped.
- Continued staff/dog-array version-priority slice:
  - Compared newest PC `skills.txt` row 124 across Client 6.0 and Server 6.0 variants. All agree `124` is `SkillStyle=3`, `AttackRadius=0`, `StateSpecialId=0`, `ChildSkillId=0`, `CharAnimId=11`.
  - Compared row 209 and confirmed it is the actual state projectile/aura row (`StateSpecialId=44`, `AttackRadius=180`, `TargetAlly=1`, `TargetSelf=1`, `ChildSkillId=92`).
  - Added `evidence/dagou-zhen-version-priority-evidence.md`.
  - Updated `PcCombatCatalogFactory` so `124` is passive `dagou_zhen` with PC Lua `AddPhysicsDamageP` L1=10 / L20=175 / duration=-1 / param3=2.
  - Rewrote `CaiBangDogArrayTests` and updated related skill-style/char-anim/panel/wait-time tests to remove stale `124` aura assumptions.
  - Verified focused updated tests green: Unity EditMode job `266cf06167c74324b4fa0e4e2b4b3380`, 16 total / 16 passed / 0 failed / 0 skipped.
  - Verified filtered CaiBang tests green after first pass: Unity EditMode job `a4eff245f6bb42b4ba6d0a17aa8a4162`, 86 total / 86 passed / 0 failed / 0 skipped.
- Continued dragon active-skill parity slice:
  - Added `CaiBang_359_TianxiaUsesPcDerivedDamageRangeAndMissileData` for Thiên Hạ Vô Cẩu.
  - Test locks newest PC `skills.txt` row 359 + `gaibang.lua::tianxia_wugou` evidence: child missile `168`, `WaitTime=5`, `CharAnimId=11`, L20 radius `512`, missile count `3`, cost `50`, `PhysicsEnhanceP=206`, `FireDamageV=285,0,432`, and `ConfuseP=60,-1,0`.
  - Test verifies runtime cast uses Lua-derived range/projectile count and spawns 3 child projectiles with skill id `168` while deterministic damage hits.
  - Verified filtered CaiBang tests green: Unity EditMode job `3e0333e30783495198988834fc4300fe`, 84 total / 84 passed / 0 failed / 0 skipped.
- Completed remaining Phase 4 active-data audit/fix slice:
  - Searched required PC source-of-truth path `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem`; it is absent in this environment, so used already-established SDD fallback evidence from `/var/www/jx-source/Client 6.0`, Server 6.0, `pak_unpacked`, and mobile Reference files.
  - Found stale mobile override for skill `358`: mobile treated it as Kháng Long/player `kanglong_youhui`, but newest PC `skills.txt` rows identify `358` as `Tiềm Long Tại Uyên` / `qianlong_zaiyuan`, `ChildSkillId=167`, `MisslesForm=7`, `AttackRadius=570`, `WaitTime=5`, `CharAnimId=11`.
  - Confirmed checked `gaibang.lua` sources keep `qianlong_zaiyuan` commented out; therefore row/default missile data wins and 358 must not borrow Kháng Long (128) Lua damage/count/form data.
  - Added `SkillMissileForm.Stationary = 7`, mapped `358 -> qianlong_zaiyuan`, updated 358 catalog to PC row/default data, and removed stale KLHH icon/comment overrides.
  - Added `CaiBang_358_TiemLongUsesNewestPcRowDefaultsWhenLuaTableIsCommented` to lock this behavior.
  - Verified filtered CaiBang tests green: Unity EditMode job `74b3bd3ad9a84714bdd7336599935665`, 84 total / 84 passed / 0 failed / 0 skipped.
  - Verified focused shared/mobile skill-slot tests green: Unity EditMode job `8f81799327b942568b21c2715a4d75c8`, 27 total / 27 passed / 0 failed / 0 skipped.
  - Fresh review timed out but surfaced one valid blocker: PC row `209` has `CharAnimId=11`, `WaitTime=0` while mobile still had `charAnimId=14`.
  - Fixed `CaiBangDogBeatingAuraChildSkill` and `CaiBangDogArrayTests` so row `209` now uses PC `CharAnimId=11` and `WaitTime=0`.
  - Re-verified filtered CaiBang tests green: Unity EditMode job `b5e917add01c4b41bccede6ce28fe9a9`, 84 total / 84 passed / 0 failed / 0 skipped.
  - Re-verified focused shared/mobile skill-slot tests green: Unity EditMode job `57e871b11e8c465880ca2ef6bd483f67`, 27 total / 27 passed / 0 failed / 0 skipped.
- Next Phase 4 work:
  - Phase 4 implementation tasks are complete; run final fresh-context review before commit/push if continuing to git operations.
  - Add visual/state-icon smoke for 127/130 after core runtime parity (Phase 5).

## Blockers / Cautions
- Async scout subagents failed/staled in this session, so parent-created SDD artifacts are the authoritative planning state for now.
- Do not run full EditMode suite in the dev loop.
- Use fresh-context review before committing implementation changes.

## Phase 5 — Visual/SFX resources + addskilldamage engine correction (2026-06-29)
- Read PC `docs/porting_guide.md` and loaded `jx-pc-resource-resolver`.
- Confirmed all core Cai Bang visual/SFX resources are already physically bound in the mobile
  runtime roots (no re-import needed); runtime signed-GB2312 hash reproduces the recorded UIDs:
  - dragon `a31b9f04`, impact `c33e96c2`, pre-cast `b91ab706`, Túy Điệp aura `7d34af1d`,
    Đả Cẩu Trận aura `202667bb`, Kháng Long icon `98055770`, Phi Long icon `d97b70ca` — all in `SpritesRuntime/`.
  - cast SFX `sound_k005/k010/k037.wav` present in `StreamingAssets/sound/skill` + `AudioRuntime/Skill`.
- Added evidence:
  - `evidence/phi-long-video-evidence.md` — quantitative analysis of user-supplied
    `pc-evidence/skills/phi_long_tai_thien.mp4` (warm orange hue ~34°, homing left→right convergence).
  - `evidence/caibang-visual-sfx-resource-evidence.md` — UID + presence table for all resources.
  - `evidence/addskilldamage-engine-semantics-evidence.md` — DECISIVE PC engine evidence.
- Added `Assets/Tests/EditMode/Sandbox/CaiBangVisualResourceParityTests.cs` (6 tests: state-aura
  metadata, signed-hash parity, SpritesRuntime existence, SFX existence, [Slow] decode-to-frames).
- ENGINE CORRECTION — `addskilldamage` is NOT a missile-spawning proc:
  - PC `KSkillList::GetAddSkillDamage` (KSkillList.cpp:895) + `KNpc::AppendSkillEffect`
    (KNpc.cpp:3017/3045/3119, MAX_PERCENT=100) prove it is a PASSIVE flat %-damage amplifier on
    the cast skill, summed from LEARNED skills whose addskilldamage targets it. No RNG, no spawn.
  - Replaced `CombatRuntimeService.TryFireAddSkillDamageChain/Slot` (rolled chance + spawned
    sub-skill missiles + applied separate damage) with `ComputeAddSkillDamagePercent` + an
    `addSkillDamagePercent` arg on `ApplyDamage` that scales the cast skill's own damage components.
  - Added `CombatCastReport.addSkillDamagePercent`.
  - Updated tests: `CaiBang_359` now passes (3 own missiles, was wrongly expecting 8 chain);
    `CaiBang_Cast_...125` asserts 0 sub-skill missiles + addP 0; added
    `CaiBang_AddSkillDamage_IsPassiveDamageAmp_NotChainSpawn` (cast 359 with 119+125 learned → +100%)
    and `CaiBang_AddSkillDamage_ZeroWhenGrantSkillNotLearned`.
  - Updated `CaiBangAddSkillDamageChainTests` header to passive-amp semantics (percent values unchanged).
- Verified filtered CaiBang tests green: Unity EditMode job `1e9843e321fc4a50b384f0d7c32ae268`,
  93 total / 93 passed / 0 failed / 0 skipped.
- Cross-faction smoke (WuDang/Shaolin/TangMen/EMei groups): 16 ran, 15 passed; the 1 failure
  `WuDangCombatCatalogTests.WuDangVisualService_UsesPcMissileSpeedLifeAndSpriteKeys` is a
  PRE-EXISTING SPR-key expectation mismatch (expects hash `3dfcabc2`, gets raw path) unrelated to
  this change (no SprPathToKey/WuDang code touched).

## Not committed yet
Holding commit for user review: this reverses the Phase 4 addskilldamage "chain spawn" model
(casting Bổng Đả Ác Cẩu 125 no longer launches extra 359/1074 dragons; instead 125 passively
buffs 359/1074 damage when those are cast), which is a visible gameplay/visual change.
