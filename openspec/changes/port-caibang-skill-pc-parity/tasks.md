# Tasks: Cai Bang Skill PC Parity

## Phase 0 - Evidence and Baseline
- [ ] Create `openspec/changes/port-caibang-skill-pc-parity/evidence/caibang-skill-matrix.md` with one row per current mobile Cai Bang skill.
- [ ] Decode/verify PC names and skill rows from `Assets/StreamingAssets/Reference/PcSkill/skills.txt` for all matrix rows.
- [ ] Decode/verify missile rows from `Assets/StreamingAssets/Reference/PcAttrib/missles.txt` for all missile-based Cai Bang skills.
- [ ] Compare mobile `Assets/StreamingAssets/Reference/gaibang.lua` with canonical PC gaibang Lua files under `/var/www/vltksource_new/vl_update_27/Client 6.0/`, `Server 6.0/`, and `pak_unpacked/`.
- [ ] Record resource paths and hashed filenames for initial Phi Long assets using `jx-pc-resource-resolver`.

## Phase 1 - Phi Long Tại Thiên Homing First Slice
- [x] RED: Add/extend CaiBang EditMode test asserting `Phi Long Tại Thiên` level 20 resolves exactly 4 missiles/dragons from PC data.
- [x] RED: Add/extend CaiBang EditMode test simulating target movement after cast and asserting missile target resolution follows the live target, not only cast-time position.
- [x] RED: Add/extend visual/math test asserting each of the 4 dragons keeps lane-specific offsets and does not resolve all heads to one center target.
- [x] GREEN: Expose/use an index-specific missile target resolver on `ActiveSkillEffect` or equivalent runtime model.
- [x] GREEN: Update `SkillEffectVisualService` homing update so `Phi Long Tại Thiên` uses `liveTarget + laneOffset` for each dragon.
- [x] GREEN: Update `SkillEffectRenderer` and `SkillEffectWorldOverlay` to orient/select frames with each missile's resolved target.
- [x] Run filtered tests: `unityMCP_run_tests(mode="EditMode", category_names=["CaiBang"])`.

## Phase 2 - Kháng Long Hữu Hối Spread
- [ ] RED: Add/extend CaiBang test proving `skill_misslesform_v == 2` selects fan/radial spread for Kháng Long, not Phi Long parallel lanes.
- [ ] RED: Add test proving `skill_param1_v` is used as the fan angle step.
- [ ] GREEN: Ensure `PcCaiBangLuaLevelService.GetMissileForm` and parameter accessors return PC values for current level.
- [ ] GREEN: Route Cai Bang multi-missile setup to `SetupPcKangLongSpread` when missile form is fan/radial.
- [ ] Run filtered CaiBang tests.

## Phase 3 - Buff Skills and State Runtime
- [ ] RED: Add tests for `Hoạt Bất Lưu Thủ` (`127`) mapping to `huabu_liushou` and applying `fastwalkrun_p` at current level.
- [ ] RED: Add tests for `Túy Điệp Cuồng Vũ` (`130`) mapping to `zuidie_kuangwu` and exposing resistance/fire/deadly/life/cost values.
- [ ] RED: Add runtime test proving buff states expire after their PC duration.
- [ ] GREEN: Add/verify missing skill id mappings in `PcCaiBangLuaLevelService`.
- [ ] GREEN: Add typed Lua accessors for speed/resistance/fire/deadly/life/cost/duration values.
- [ ] GREEN: Apply PC-derived buff states in `CombatRuntimeService`.
- [ ] GREEN: Integrate active movement speed state into `SandboxPlayerController` or the authoritative movement path.
- [ ] GREEN: Add state ticking/expiration, using `BuffStateService` if compatible or a minimal runtime tick if safer.
- [ ] Run filtered CaiBang tests and relevant movement/runtime tests.

## Phase 4 - Active Damage and Defender State
- [ ] RED: Add CaiBang damage test proving defender resistance states affect incoming Cai Bang skill damage.
- [ ] RED: Add tests for at least one staff/dog-array skill and one dragon skill using PC-derived damage/range/missile data.
- [ ] GREEN: Populate `DefenderStats` from active defender states in `CombatRuntimeService.ApplyDamage`.
- [ ] GREEN: Ensure Cai Bang active skills use PC-derived level data instead of empty/default approximations.
- [ ] Run filtered CaiBang tests plus focused shared combat tests.

## Phase 5 - Visual/SFX Resources
- [ ] Resolve Phi Long missile SPR(s), impact SPR(s), icon, cast/precast, and SFX paths from PC config with hash evidence.
- [ ] Resolve Kháng Long and core buff/support visual resources with hash evidence.
- [ ] Import or bind resolved resources into mobile asset paths with deterministic metadata.
- [ ] RED/GREEN visual tests where practical: sprite existence, frame count/direction metadata, non-null SFX path, and resource reference wiring.
- [ ] Manual/Unity visual smoke: Phi Long level 20 moving-target homing, Kháng Long fan spread, Hoạt/Túy buff aura/status visuals.

## Phase 6 - Review and Final Gates
- [ ] Run `unityMCP_run_tests(mode="EditMode", category_names=["CaiBang"])`.
- [ ] If shared combat services changed, run focused shared combat/catalog tests.
- [ ] Run `lens_diagnostics(mode="all")` or equivalent diagnostics for edited files.
- [ ] Run a fresh-context reviewer before commit/push because this is multi-file combat work.
- [ ] Commit in small conventional commits by slice.
- [ ] Push only after final gate passes.

## Review Workload Forecast
This change should not be applied as one large diff. Each phase should target less than 400 changed lines when possible. If a phase exceeds that budget, split it into a separate commit/PR-sized slice before continuing.
