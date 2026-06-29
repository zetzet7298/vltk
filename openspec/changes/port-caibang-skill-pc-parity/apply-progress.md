# Apply Progress: Cai Bang Skill PC Parity

## Status
Planning/evidence phase started. No implementation changes have been applied under this SDD change yet.

## Completed
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
Phase 1C:
- Revisit the currently ignored `CaiBang_357_HomingSpreadKeepsPerMissileOffsetsForLiveTarget` test: it is ignored because current mobile `gaibang.lua::feilong_zaitian` lacks `skill_param1_v`. Determine from canonical PC client/server/pak source whether mobile reference is stale or whether Phi Long should use a different PC data path for level-20 four-dragon homing lanes.
- Add/strengthen tests proving `Phi Long Tại Thiên` level 20 uses four homing dragons against a moving target using live-target resolution.
- Run a visual smoke with PC SPRs for `a31b9f04.spr`/`c33e96c2.spr` once the runtime test gap is resolved.

## Blockers / Cautions
- Async scout subagents failed/staled in this session, so parent-created SDD artifacts are the authoritative planning state for now.
- Do not run full EditMode suite in the dev loop.
- Use fresh-context review before committing implementation changes.
