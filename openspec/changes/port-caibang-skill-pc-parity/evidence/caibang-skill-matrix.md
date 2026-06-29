# Cai Bang Skill Evidence Matrix

Status legend:
- `verified-current`: confirmed in current mobile code/tests during SDD planning.
- `needs-pc-audit`: requires direct PC row/resource extraction before implementation.
- `needs-runtime-audit`: mobile code exists but runtime behavior still needs proof against PC semantics.
- `needs-visual-audit`: asset/VFX/SFX path must be resolved via JX hash workflow.

| SkillId | Vietnamese name | PC Lua symbol / source mapping | Current mobile evidence | Current status | Next evidence/action |
| ---: | --- | --- | --- | --- | --- |
| 115 | Cái Bang Bổng Pháp | `gaibang_bangfa` | `PcCaiBangLuaLevelService.SkillIdToName` maps 115; CaiBang tests include 115 in panel/combat coverage. | needs-runtime-audit | Verify passive mastery values and whether runtime applies them to staff/dog skills. |
| 116 | Cái Bang Chưởng Pháp | `gaibang_zhangfa` | `PcCaiBangLuaLevelService.SkillIdToName` maps 116; combat test asserts `AddFireDamageV=275,-1,9` at L20. | needs-runtime-audit | Verify passive palm values affect relevant dragon/palm skills. |
| 117 | Đầu Thạch Vấn Lộ / related | `yanmen_tuobo` | Service maps 117; tests classify it in short-range skills. | needs-pc-audit | Decode exact PC Vietnamese name and `skills.txt` row; verify damage/range. |
| 119 | Duyên Môn Thác Bát | `yanmen_tuobo` | Service maps 119; addskilldamage tests mention chain behavior. | needs-pc-audit | Verify PC row and chain semantics. |
| 122 | Kiến Nhân Thân Thủ | `jianren_shenshou` | Service maps 122; addskilldamage test notes chain to 357 at L20 chance 50%. | needs-runtime-audit | Verify chain target/damage frequency with PC source. |
| 124 | Đả Cẩu Trận / dog array | not yet confirmed in service grep; tests cover dog array aura behavior | `CaiBangDogArrayTests` exists; style tests discuss state/aura. | needs-runtime-audit | Verify PC state style and ally propagation radius. |
| 125 | Thiên Hạ Vô Cẩu | `tianxia_wugou` | Service maps 125; `CaiBangTianXiaWuGouTests` exists; comments say form 5 zone + childNum 3. | needs-runtime-audit | Confirm PC missile/zone rows and impact logic. |
| 127 | Hoạt Bất Lưu Thủ | `huabu_liushou`; PC L20 `fastwalkrun_p=66`, duration `3240`, cost `50` | Current service maps 127; runtime cast test verifies `FastWalkRunP=66,3240,0`; `SandboxPlayerController` consumes `FastWalkRunP`; `GameplayLoopService` ticks durations. Evidence recorded in `caibang-buff-skill-evidence.md`. | verified-current for runtime/test evidence | Visual/state-icon smoke still needed. |
| 128 | Kháng Long Hữu Hối | `kanglong_youhui`; skill row `MisslesForm=2`, child missile `48`; Lua L20 count `15`, `skill_param1_v=2` | Current service maps 128; visual service calls `GetMissileForm` and routes `missileForm == 2` to `SetupPcKangLongSpread`. Test `CaiBang_128_KhangLongUsesFanSpreadFromLuaMissileForm` verifies fan targets and no Phi Long homing offsets. Resource evidence recorded in `khang-long-resource-evidence.md`. | verified-current for runtime/test/resource evidence; needs visual smoke | Visually verify L20 fan spread with PC SPRs. |
| 130 | Túy Điệp Cuồng Vũ | `zuidie_kuangwu`; PC allres/fire/deadly durations interpolate to `2867` at L20; lifemax_yan duration `-1`; cost `100` | Current service maps 130; char anim test asserts `charAnimId=43`; runtime cast test verifies finite PC durations and cost. Evidence recorded in `caibang-buff-skill-evidence.md`. | verified-current for runtime/test evidence | Visual/state-icon smoke still needed. |
| 357 | Phi Long Tại Thiên | `feilong_zaitian`; skill row child missile `166`; missile `166` has `MoveKind=5` homing; skill row `Param1=32` supplies lane gap | Current service maps 357; tests assert L20 missile count 4; visual service has live target accessor and per-missile `ResolveMissileTarget`; `CaiBangCombatParityTests` now asserts homing lane offsets without ignore. Resource evidence recorded in `phi-long-resource-evidence.md`; canonical Lua comparison recorded in `phi-long-canonical-lua-comparison.md`. | verified-current for runtime/test/resource evidence; needs visual smoke | Visually verify L20 four-dragon homing with PC SPRs. |
| 358 | Kháng Long Hữu Hối player variant | `kanglong_youhui` | Current service maps 358 to `kanglong_youhui`; cast sound tests include 358. | needs-runtime-audit | Verify 358 vs 128 alias semantics and default deck behavior. |
| 359 | Thiên Hạ Vô Cẩu / MOD variant | `tianxia_wugou` | Current service maps 359; tests cover sound/default slot interactions. | needs-pc-audit | Confirm whether 359 should be default deck or secondary skill relative to current user acceptance. |
| 1073 | Thời Thặng Lục Long | `zhanggaibang150` | Current service maps 1073; tests mention default deck/current catalog. | needs-pc-audit | Verify 150-skill source Lua/script and missile rows. |
| 1074 | Bổng Hoành Lược Mã | `gungaibang150` | Current service maps 1074; tests include it in long-range skills. | needs-pc-audit | Verify staff 150-skill source Lua/script and visuals. |
| 1101/1103/1161/1162 | NPC/MOD variants | `zhanggaibang150` / `gungaibang150` | Current service maps NPC/no-script variants; tests mention catalog count variants. | needs-pc-audit | Decide whether variants are in scope for player skill parity or only regression coverage. |
| 714/720 | 120-series utility | `gaibang120` / `gaibang120zuzhou` | Current service maps 714/720; panel tests include 714/720. | needs-pc-audit | Verify if user expects 120-series in this SDD or defer. |

## Current verified implementation facts

- `Assets/Scripts/Sandbox/PcCaiBangLuaLevelService.cs` currently maps `127 -> huabu_liushou`, `130 -> zuidie_kuangwu`, `357 -> feilong_zaitian`, `358 -> kanglong_youhui`, `359 -> tianxia_wugou`, `1073 -> zhanggaibang150`, and `1074 -> gungaibang150`.
- `Assets/Scripts/Sandbox/SkillEffectVisualService.cs` currently reads `PcCaiBangLuaLevelService.GetMissileForm(skill.skillId, level)` and calls `SetupPcKangLongSpread` for `missileForm == 2`.
- `ActiveSkillEffect.ResolveMissileTarget(int index)` currently exists and uses `getCurrentTargetPos()` plus `missileTargetOffsets[index]` for homing missiles with `pcMissileMoveKind == 5`.
- `Assets/Scripts/UI/SkillEffectRenderer.cs` and `Assets/Scripts/UI/SkillEffectWorldOverlay.cs` currently call `fx.ResolveMissileTarget(i)` for per-missile frame selection/orientation.
- Existing CaiBang test fixtures are already categorized with `[TestFixture, Category("CaiBang")]`.

## Open risks

- Current code presence does not prove exact PC parity; each behavior still needs PC row/resource evidence and runtime/visual verification.
- Movement speed buff, state expiration, defender stats, and actual VFX/SFX resource selection remain the highest-risk runtime areas.
