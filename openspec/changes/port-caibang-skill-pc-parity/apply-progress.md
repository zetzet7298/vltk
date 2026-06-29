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
- Created SDD artifacts:
  - `proposal.md`
  - `spec.md`
  - `design.md`
  - `tasks.md`
- Created initial evidence matrix:
  - `evidence/caibang-skill-matrix.md`
- Loaded/used mandatory PC resource resolver workflow and project PC port rules.
- Researched required PC docs:
  - `/var/www/vltksource_new/docs/3_ky_nang_va_chieu_thuc.md`
  - `/var/www/vltksource_new/docs/client_port/03_skills.md`
  - `/var/www/vltksource_new/docs/backend_port/03_skills.md`
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
Phase 3:
- Move to buff/runtime slices for `Hoạt Bất Lưu Thủ` and `Túy Điệp Cuồng Vũ`.
- Verify movement speed consumes `FastWalkRunP` and buff states expire.
- Continue PC evidence/resource resolution for Đả Cẩu Trận, Thiên Hạ Vô Cẩu, and 150-skill variants.

## Blockers / Cautions
- Async scout subagents failed/staled in this session, so parent-created SDD artifacts are the authoritative planning state for now.
- Do not run full EditMode suite in the dev loop.
- Use fresh-context review before committing implementation changes.
