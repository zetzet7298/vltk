# Pin canonical oracle cho skill Cái Bang

## Current Behavior

`SKL-CB-PARITY-001` đã sửa catalog/runtime và có 136 test pass, nhưng expected còn phân tán trong fixture và implementation. Chưa có một artifact độc lập, deterministic, hash-pinned chứng minh toàn bộ 26 root skill player Cái Bang cùng static fields/relationship anchors.

## Target Behavior

Sinh một oracle JSON từ exact repo-local slice đã extract từ canonical PC `skills.txt`, bổ sung hai collide relationship có nguồn từ canonical `gaibang.lua`, pin SHA-256, và so toàn bộ root catalog Unity với oracle trong EditMode.

## Affected Users

- Người chơi Cái Bang.
- Gameplay/QA parity reviewer.
- Các wave parity 9 phái tiếp theo dùng cùng pattern proof.

## Lane

Normal với stronger validation: existing combat behavior + weak proof; không đổi public API hay persistence.

## Non-Goals

- Không sửa tuning/runtime behavior trong story proof này.
- Không tuyên bố visual/audio runtime `PARITY_DONE`.
- Không dùng oracle để thay thế PC runtime golden.
