# Validation

## Epic Completion Gate

Epic chỉ hoàn tất khi:

- Matrix phủ mọi player-facing skill của 10 phái và mọi category.
- Mỗi row có canonical source, Unity mapping, runtime owner và automated proof.
- Không còn row `missing`, `source_only` hoặc blocker chưa xử lý trong player scope.
- Shared + per-faction verifiers đều pass fresh.
- Android packaged-data/device smoke pass.
- PC runtime golden hoặc equivalent oracle xác nhận các behavior/visual/audio được claim 100%.
- Final independent review không còn finding high-confidence.

## Inventory Phase (backlog #1)

Inventory phase được mark complete chỉ khi deterministic recomputation khớp, KHÔNG
force. Trạng thái hiện tại đã verify:

- `coverage-matrix.json` schema `vltk.all-faction.membership-matrix/v1`; deterministic
  canonical-PC-first row-level union.
- PC progression (`skills_table.lua`) + skillbook (`skillbook.lua`) = canonical
  learned-membership evidence; Unity `PcSkillPanelService` arrays = observed display
  only. UI order KHÔNG được infer.
- Global union = **245** ID; exact-byte vltktool slice
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
- Summary counts: shared 124, pc_learned_only 81, unity_display_only_unresolved 40
  (= 245 union rows).
- Test recompute độc lập từng faction từ progression + skillbook + Unity display rồi
  đối chiếu exact learned/display/union và cả ba classification trong matrix.

## Risk-First Ranking (current evidence)

Chỉ loại completed waves sau khi oracle/membership artifact tương ứng pass pinned
hash + schema + scope checks (TangMen learned-scope, Cái Bang static display-scope), rank 8
faction còn lại theo descending symmetric gap = `pc_learned_only` +
`unity_display_only_unresolved`, rồi descending relationship-bearing union-row count,
rồi stable faction key:

| Rank | Faction | Gap | PC-only | Unity-only | Rel rows |
| --- | --- | --- | --- | --- | --- |
| 1 | KunLun | 16 | 11 | 5 | 20 |
| 2 | Shaolin | 15 | 9 | 6 | 17 |
| 3 | EMei | 14 | 10 | 4 | 20 |
| 4 | TianRen | 12 | 7 | 5 | 20 |
| 5 | WuDang | 11 | 6 | 5 | 14 |
| 6 | WuDu | 9 | 8 | 1 | 18 |
| 7 | TianWang | 8 | 8 | 0 | 16 |
| 8 | CuiYan | 8 | 6 | 2 | 12 |

Winner = **KunLun** gap 16. Tests fail nếu deterministic recomputation lệch, không force.

## Next Implementable Wave

`SKL-KL-PROOF-001` — **Côn Luân canonical learned-membership and static catalog proof**:
reconcile 18 Unity display roots với PC progression + 90/120/150 skillbook grants
(union 29, 11 PC-only, 5 Unity-only unresolved) trước khi tạo oracle. PC learning flow
là membership evidence, không chứng minh UI order; không dùng Unity panel làm canonical
expected.

## Proof Status

- Cái Bang = `canonical_static_verified_display_scope` (display scope only; không claim
  learned-membership reconciliation).
- Đường Môn = `canonical_static_verified_learned_scope` (SKL-TM-PROOF-001 /
  SKL-TM-CATALOG-001 completed).
- 8 phái còn lại = `weak_or_partial`. Không claim runtime/platform parity.

## Runtime Correction Waves

- Thúy Yên skill 100 `Hộ Thể Hàn Băng`: canonical Lua emits melee and ranged
  damage return `5→20%` for 2160 ticks. The Unity catalog no longer fabricates
  cold resistance or defense for this skill. Focused runtime job
  `c289cd5997fa404ab94ad9315b31e91f` passes 4/4 for L1/L20 melee/range reflect
  and recast behavior; the all-faction static regression job
  `32ae78b1d695467b8ac57abb385a8112` passes 29/29. This is a bounded runtime
  correction, not proof that Thúy Yên or the epic is complete.

## Residuals (epic-level, open)

- Android packaged-data/device smoke.
- PC runtime golden / curve / visual / audio parity cho mọi phái.
- Child/support/event resolution và UI deck order ngoài membership evidence.

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
- Skill panel/deck exposure tests.
- Android resource smoke.
- PC runtime golden comparison.

Epic chưa runnable cho tới khi coverage matrix và child-story verifiers được tạo. Không
dùng một green proxy test để đóng epic.
