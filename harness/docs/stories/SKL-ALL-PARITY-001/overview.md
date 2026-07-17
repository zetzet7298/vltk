# Parity toàn bộ skill 10 phái

## Current Behavior

Cái Bang đã có Harness story riêng và verifier 136/136 pass. Các phái còn lại đang dựa nhiều vào catalog hard-code, test tự nhất quán và coverage không đồng đều. Chưa có matrix chứng minh mọi skill chủ động, bị động, buff, utility, aura, child/event và NPC/player variant khớp canonical `/var/www/jx-source`.

## Target Behavior

Mọi player-facing skill của 10 phái phải có chuỗi evidence:

```text
canonical jx-source row/Lua/C++/PAK
→ repo-local exact slice khi runtime cần
→ Unity catalog/model mapping
→ runtime behavior
→ automated proof
→ platform/runtime golden khi claim PARITY_DONE
```

Không đoán field, formula, asset, hash hoặc encoding. Trường chưa chứng minh phải fail-closed hoặc được ghi blocker.

## Scope

- 10 phái: Thiếu Lâm, Thiên Vương, Đường Môn, Ngũ Độc, Cái Bang, Thiên Nhẫn, Nga My, Thúy Yên, Võ Đang, Côn Luân.
- Tất cả category: active damage, melee/missile, passive, self/ally/enemy buff/debuff, aura/stance, utility, ultimate, child/start/fly/collide/vanish event.
- Player-facing stock + expansion/MOD skill đã expose trong Unity.
- Shared factory/parser/runtime/UI roots và per-sect tests.

## Non-Goals

- Không sửa canonical `/var/www/jx-source`.
- Không claim 100% visual/audio/frame parity khi chưa có PC runtime golden.
- Không port NPC-only skill nếu không ảnh hưởng player chain; ghi riêng trong matrix.
- Không migrate toàn bộ 1.554 rows một lần; chia wave theo shared root cause và faction.

## Inventory Status

Backlog #1 inventory phase đã complete (verified, không force): deterministic
row-level union matrix schema `vltk.all-faction.membership-matrix/v1`, exact-byte
vltktool slice `PcAllFactionLearnedDisplaySkills.txt` + provenance, global union
245 ID, partitions/unions asserted exactly, `--check` fail-on-stale. Risk-first
ranking chọn **SKL-KL-PROOF-001 (Côn Luân, gap 16)** làm wave kế tiếp. Cái Bang
(display-scope) và Đường Môn (learned-scope) đã complete canonical static wave;
hai exclusion này chỉ được áp dụng sau khi generator verify hash/schema/scope của
oracle và membership artifact tương ứng. Runtime/platform residuals vẫn mở cho toàn epic.
