# 26 — SkillDef data pipeline (parser JX + fail-closed + self-check)

**What to build:** Từ JX `PcSkills.txt` (GBK) + `PcAllFactionLearnedDisplaySkills.txt` (TCVN3) + `missles.txt` (441 missile), generate bộ `SkillDef` (ScriptableObject) riêng của Survivor đủ 10 phái player pool (~452) + boss/npc pool. Mỗi SkillDef resolve đúng sprite staged (`/SpritesRuntime`), chưa staged → KHÔNG gán (fail-closed). Sửa bug parser: faction = col 70, không dùng đường parse Sandbox lỗi (`LvlSetScriptCol=71`).

**Blocked by:** None — can start immediately.

**Status:** verified

- [x] Parser đọc 3 nguồn đúng encoding (GBK/TCVN3), col map: 2→Id, 70→Faction (LvlSetScript), 19→Form, 26→IsMelee, 20→ChildMissileId, 6→PreCastSprUid, 58/60→Fan Param1/2, 52/53→Req/MaxLevel, 71-110→LevelScaling
- [x] SkillDef generated đủ pool + supply subset tag (heal=`lifereplenish_v/lifemax_v`, bomb=`special/bomb.lua`, aura=`IsAura`, magnet=own)
- [x] Fail-closed: sprite chỉ gán khi hash staged (`SprRuntimeService.ComputePathUidHex` GB2312 signed+unsigned); danh sách skill chưa staged rõ (vd child không AnimFile: 20/408/274/1083-88)
- [x] Editor menu generate + log đếm kết quả (tổng skill, staged %, fail-closed list)
- [x] EditMode self-check xanh: col map, faction đúng LvlSetScript, fail-closed list

**Verification (orchestrator):** EditMode 84/84 PASSED (2026-08-03). Orchestrator fixes: parser encoding `Encoding.GetEncoding(28591)`/GB2312 (parser:303); test `Convert.FromHexString`→HexToBytes; `Assert.Contains(HashSet)`→`CollectionAssert.Contains`; dedupe data-lặp id 521 (giữ row đầu, `DuplicateIds` tracked) → counts 1216→1215, form7 652→651. Wave-30: `WaveRefresh.OnMonsterKilled` thêm `CheckFinish()` — kill cuối quota finish ngay (trước đây chỉ check trong Tick).
