# Validation

## Epic Completion Gate

Epic chỉ hoàn tất khi:

- Matrix phủ mọi player-facing skill của 10 phái và mọi category.
- Mỗi row có canonical source, Unity mapping, runtime owner và automated proof.
- Mỗi row có presentation trên PC phải phủ male/female, foot/mounted, concrete
  weapon variant, cast/body/hand pose, VFX lifecycle, audio, timing/speed và
  persistent buff/debuff/aura state; row không có PC presentation phải chứng minh
  Unity không fabricate.
- Không còn row `missing`, `source_only` hoặc blocker chưa xử lý trong player scope.
- Shared + per-faction verifiers đều pass fresh.
- Android packaged-data/device smoke pass.
- PC runtime golden xác nhận cùng direction/tick/camera cho các behavior/visual/
  audio được claim 99% hoặc cao hơn; catalog/path equality đơn lẻ không đủ.
- Final independent review không còn finding high-confidence.

## Inventory Phase (backlog #1)

Inventory phase được mark complete chỉ khi deterministic recomputation khớp, KHÔNG
force. Trạng thái hiện tại đã verify:

- `coverage-matrix.json` schema `vltk.all-faction.membership-matrix/v1`; deterministic
  canonical-PC-first row-level union.
- PC progression (`skills_table.lua`) + skillbook (`skillbook.lua`) = canonical
  learned-membership evidence; Unity `PcSkillPanelService` arrays = observed display
  only. UI order KHÔNG được infer.
- Global union = **242** ID; exact-byte vltktool slice
  `Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt` + provenance
  `PcAllFactionLearnedDisplaySkills.provenance.json` (schema `vltk.table-slice-provenance/v1`).
- Generator (`scripts/audit_skill_coverage.py`) chỉ parse slice, verify manifest
  source/slice bytes+hash, requested/selected IDs, source lines; có `--check` fail
  trên stale output/hash/provenance. Generator/test không tự read/hash full encoded
  `skills.txt`; source hash này chỉ được xác nhận qua checked vltktool provenance. Pin canonical hashes: skills.txt
  `c77892fb…`, skills_table.lua `7e46896c…`, skillbook.lua `4e5361a6…`.
- Mỗi faction có `membership_rows` cho mọi ID trong (PC-learned ∪ Unity-display),
  phân loại chính xác `shared` / `pc_learned_only` / `unity_display_only_unresolved`,
  kèm progression/skillbook evidence, observed-display boolean, canonical row/line,
  categories và direct relationship fields. Assert partitions/unions exactly.
- Summary counts: shared 140, pc_learned_only 65, unity_display_only_unresolved 37
  (= 242 union rows).
- Test recompute độc lập từng faction từ progression + skillbook + Unity display rồi
  đối chiếu exact learned/display/union và cả ba classification trong matrix.

## Risk-First Ranking (current evidence)

Chỉ loại completed waves sau khi oracle/membership artifact tương ứng pass pinned
hash + schema + scope checks (TangMen/KunLun/Shaolin/EMei/TianRen/WuDang/WuDu/TianWang/CuiYan learned-scope, Cái Bang static display-scope);
learned-scope proofs match observed display hiện tại. `SKL-CY-PROOF-001` verifies Thúy Yên
with gap 8 (PC-only `269,336,337,713,1063,1065`; Unity-only unresolved `101,103`).

Ranking hiện không còn candidate: cả 10 phái đã có verified exclusion scope. Tests fail nếu
pinned proof, current scope, hoặc deterministic recomputation lệch; không force.

## Next Implementable Wave

Không có membership-proof winner tiếp theo trong 10 phái hiện tại. Wave đang chạy
là shared player-presentation foundation `SKL-MOUNTED-CAST-001`; tiếp theo phải
sinh per-skill presentation inventory rồi rank các gap shared animation/VFX/audio/
buff lifecycle. PC learning flow remains membership evidence, không chứng minh UI
order hay presentation parity.

## Proof Status

- Cái Bang = `canonical_static_verified_display_scope` (display scope only; không claim
  learned-membership reconciliation).
- Đường Môn = `canonical_static_verified_learned_scope` (SKL-TM-PROOF-001 /
  SKL-TM-CATALOG-001 completed).
- Côn Luân = `canonical_static_verified_learned_scope` (SKL-KL-PROOF-001 completed;
  current observed display scope also verified).
- Thiếu Lâm = `canonical_static_verified_learned_scope` (SKL-S-PROOF-001 completed;
  current observed display scope also verified).
- Nga My = `canonical_static_verified_learned_scope` (SKL-EM-PROOF-001 completed;
  current observed display scope also verified).
- Thiên Nhẫn = `canonical_static_verified_learned_scope` (SKL-TR-PROOF-001 completed;
  current observed display scope also verified).
- Võ Đang = `canonical_static_verified_learned_scope` (SKL-WD-PROOF-001 completed;
  current observed display scope also verified).
- Ngũ Độc = `canonical_static_verified_learned_scope` (SKL-WDU-PROOF-001 completed;
  current observed display scope also verified).
- Thiên Vương = `canonical_static_verified_learned_scope` (SKL-TW-PROOF-001 completed;
  current observed display scope also verified).
- Thúy Yên = `canonical_static_verified_learned_scope` (SKL-CY-PROOF-001 completed;
  current observed display scope also verified; `101,103` remain Unity-only unresolved).
- Không còn phái `weak_or_partial` trong membership scope. Không claim runtime/platform parity.

## Runtime Correction Waves

- Thúy Yên skill 100 `Hộ Thể Hàn Băng`: canonical Lua emits melee and ranged
  damage return `5→20%` for 2160 ticks. The Unity catalog no longer fabricates
  cold resistance or defense for this skill. Focused runtime job
  `c289cd5997fa404ab94ad9315b31e91f` passes 4/4 for L1/L20 melee/range reflect
  and recast behavior; the all-faction static regression job
  `32ae78b1d695467b8ac57abb385a8112` passes 29/29. This is a bounded runtime
  correction, not proof that Thúy Yên or the epic is complete.

- Shared skill-effect lifecycle follow-up: active missile, stationary zone,
  persistent state aura, and passive/no-visual rows are now pinned by
  `SkillEffectVisualLifecycleParityTests`; nested sub-effects use the canonical
  cast path and fail closed when no PC visual exists. The focused Unity job
  passes 4/4 (`9921826edea44383a1cf001f26f6a771`); the related presentation batch
  passes 576/576 (`d8bcd414d55644bbb7bc1e776eba880a`). This closes a bounded
  lifecycle defect only; it does not close the all-faction inventory, PC golden,
  audio, or Android/device gates.

- Canonical parser/mounted cadence correction: `PcConfigParser` header-maps the
  shifted 114-column schema without dropping SkillId `720` when final
  `SkillDesc` is blank; legacy 113-column `PcSkills.txt` remains exactly 1.216
  rows. Runtime recast uses `TimePerCast` on foot, `TimePerCastOnHorse` exactly
  when mounted (including zero), and does not arm next-cast time for aura. All
  seven nonzero mounted rows are factory-pinned:
  `19=5,20=54,40=27,138=40,164=25,181=54,392=27`. Focused proof passes 19/19
  (`b3ff3f14087a42b29f8a489b857826cb`); final related EditMode group passes
  595/595 (`fd0ca38cee244ddebb2a647e873e4382`); PlayMode golden passes 2/2
  (`28cfe580ef0e48c3b871a3f7c9c1049c`).

- `PcMissileSourceAudit.json` pins exact vltktool-unpacked `slistcache.pak`
  `\settings\missles.txt` bytes (SHA `e893c7af…`, 513 unique rows) and
  model-aligned comparison counts. Presentation inventory full check passes at
  SHA `7aff74f5…`: 242 rows, BaseSkill-driven child partition
  `138 missile / 34 canonical_skill / 70 none`. Independent Herdr reproof
  recomputed all nonzero child links with zero mismatch. All 9.196 Unity mapping
  candidates deliberately remain `source_only`, not `verified`; Python
  audit/coverage passes 27/27. This is proof honesty, not parity closure.

## Residuals (epic-level, open)

- Android packaged-data/device smoke.
- PC runtime golden / curve / visual / audio parity cho mọi phái.
- Child/support/event resolution và UI deck order ngoài membership evidence.
- Per-skill male/female × mount × weapon-variant presentation closure; hiện mới
  có shared cast/body/hand foundation và một số Cái Bang/Đường Môn asset tests.
- 9.196 field-level Unity candidates vẫn cần dereference assignment/symbol thật;
  17 state visual mappings và 45 event targets vẫn mở trong inventory.

## Inventory Verify Command

```bash
cd /var/www/vltk-mobile
ids=$(jq -r '.requested_ids | join(",")' Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.provenance.json)
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-source/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids "$ids" --output Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt --manifest Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.provenance.json --check
python3 -m py_compile scripts/audit_skill_coverage.py scripts/test_audit_skill_coverage.py
python3 -m pytest scripts/test_audit_skill_coverage.py -q
python3 scripts/audit_skill_coverage.py --check
```

## Planned Verifiers

- Static canonical-row diff per faction.
- Formula/level-curve unit tests.
- Active/melee/missile/event integration tests.
- Passive/buff/debuff/aura state tests.
- Gender/mount/weapon pose and action-clock tests.
- VFX start/fly/collide/vanish plus cast/flight/impact audio lifecycle tests.
- Skill panel/deck exposure tests.
- Android resource smoke.
- PC runtime golden comparison.

Epic chưa runnable cho tới khi coverage matrix và child-story verifiers được tạo. Không
dùng một green proxy test để đóng epic.

## Gate 0 contract reconciliation (2026-07-19)

- Spec contract now consumes the dirty 242-row union as static scope only:
  `global_union_size=242`, `union_rows_total=242`, runtime/PC/Android parity still
  blocked.
- `game.v1` adds exact `ContentDigest`, `RuntimeSkillPolicy`, encounter preload
  ACK, active combat resync and lifecycle event kinds for recovery/fly/collide/
  vanish/status refresh/expire without reusing existing tags.
- Reconnect grace reconciled to 15 seconds across CNPM/model/data/delivery and
  SQL dictionary. No `PARITY_DONE`, PC golden pass or Android physical pass is
  claimed.
