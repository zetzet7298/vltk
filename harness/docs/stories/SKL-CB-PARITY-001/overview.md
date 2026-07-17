# Audit và sửa parity skill Cái Bang

## Current Behavior

Baseline intake: người dùng test runtime và xác nhận bộ skill Cái Bang chỉ `Phi Long Tại Thiên` đúng. Audit xác nhận factory hard-code lệch canonical static rows, `ByMissle` timing sai, nested collision thiếu HUD callback, default deck chứa utility/inert/event skills và nhiều test dùng oracle cũ.

Implemented result: player Cái Bang catalog/runtime/deck hiện lấy canonical static rows + Lua curves, nested missile damage đi qua lifecycle riêng, và toàn bộ verifier Cái Bang pass. Xem `validation.md` cho evidence và residual gaps.

## Target Behavior

Mọi skill Cái Bang hiện được expose cho player trong Unity phải lấy đúng thuộc tính, level curve, child/collide relation và combat behavior từ canonical PC source. Trường thiếu evidence phải fail-closed hoặc ghi blocker, không dùng giá trị phỏng đoán.

## Affected Users

- Người chơi nhân vật Cái Bang.
- Gameplay/QA parity reviewer.

## Affected Product Docs

- `specs/jx-pc-mobile-port/domains/skills.md`
- `specs/jx-pc-mobile-port/governance/source-authority.md`
- `specs/jx-pc-mobile-port/delivery/case-matrices/skill-parity-p0.json`

## Non-Goals

- Không port phái khác.
- Không tuyên bố `PARITY_DONE` khi chưa có PC runtime golden.
- Không thay asset/SPR/audio nếu chưa được chọn bằng `vltktool`.
