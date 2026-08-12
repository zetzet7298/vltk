# Parity toàn bộ skill 10 phái

## Current Behavior

Cái Bang đã có Harness story riêng và verifier 136/136 pass. Các phái còn lại đang dựa nhiều vào catalog hard-code, test tự nhất quán và coverage không đồng đều. Chưa có matrix chứng minh mọi skill chủ động, bị động, buff, utility, aura, child/event và NPC/player variant khớp canonical `/var/www/jx-pc`.

## Target Behavior

Mọi player-facing skill của 10 phái phải có chuỗi evidence:

```text
canonical jx-pc row/Lua/C++/PAK
→ repo-local exact slice khi runtime cần
→ Unity catalog/model mapping
→ runtime behavior
→ automated proof
→ platform/runtime golden khi claim PARITY_DONE
```

Không đoán field, formula, asset, hash hoặc encoding. Trường chưa chứng minh phải fail-closed hoặc được ghi blocker.

`99% như PC` là presentation gate đo được, không phải nhãn thủ công. Mỗi skill
có presentation phải chứng minh đúng pose/tốc độ/tick, VFX lifecycle, audio và
framebuffer so với live PC reference; skill thuần passive không có presentation
trên PC thì Unity cũng không được tự bịa hiệu ứng.

## Scope

- 10 phái: Thiếu Lâm, Thiên Vương, Đường Môn, Ngũ Độc, Cái Bang, Thiên Nhẫn, Nga My, Thúy Yên, Võ Đang, Côn Luân.
- Tất cả category: active damage, melee/missile, passive, self/ally/enemy buff/debuff, aura/stance, utility, ultimate, child/start/fly/collide/vanish event.
- Player-facing stock + expansion/MOD skill đã expose trong Unity.
- Shared factory/parser/runtime/UI roots và per-sect tests.
- Presentation axes cho mọi row có liên quan: male/female; foot/mounted;
  empty/hidden và mọi melee resource variant; cast/loop/hit/end pose; projectile
  start/fly/collide/vanish VFX; attached buff/debuff/aura overlay; cast/flight/
  impact audio; direction, timing, speed và camera composition.

## Non-Goals

- Không sửa canonical `/var/www/jx-pc`.
- Không claim 99%/100% visual/audio/frame parity khi chưa có PC runtime golden
  và Android/device playback evidence cho cùng skill matrix.
- Không port NPC-only skill nếu không ảnh hưởng player chain; ghi riêng trong matrix.
- Không migrate toàn bộ 1.554 rows một lần; chia wave theo shared root cause và faction.

## Inventory Status

Backlog #1 inventory phase đã complete (verified, không force): deterministic
row-level union matrix schema `vltk.all-faction.membership-matrix/v1`, exact-byte
vltktool slice `PcAllFactionLearnedDisplaySkills.txt` + provenance, global union
242 ID, partitions/unions asserted exactly, `--check` fail-on-stale. Cái Bang
(display-scope), Đường Môn, Côn Luân, Thiếu Lâm, Nga My, Thiên Nhẫn, Võ Đang, Ngũ Độc, Thiên Vương và Thúy Yên (learned-scope) chỉ bị loại sau khi
generator verify hash/schema/scope của proof artifact tương ứng; các proof learned-scope
còn verify observed display hiện tại. Sau `SKL-CY-PROOF-001`, cả 10 phái đã có verified
exclusion scope; ranking không còn candidate. Runtime/platform residuals vẫn mở cho toàn epic.
