# Gán nhanh skill từ panel quản lý vào combat deck

## Status

implemented

## Lane

normal — user-visible panel workflow thay đổi, nhưng dùng lại slot controller hiện có.

## Product Contract

Skill chủ động đã học, khi được chọn trong popup Kỹ năng võ công, có thể được gán trực
tiếp vào một trong năm ô của deck combat đang active. Passive và skill chưa học bị từ
chối.

## Relevant Product Docs

- `harness/docs/product/sandbox-runtime.md`

## Acceptance Criteria

- Detail của skill eligible hiện các nút `Ô 1`–`Ô 5`.
- Gán qua panel thay đúng slot của active deck và refresh hotbar.
- Không gán được skill chưa học hoặc passive; skill active chưa học hiện slot bị khóa
  cùng lý do thay vì im lặng ẩn action.

## Design Notes

`SkillContent` chỉ gửi request qua callback; `CombatSkillSlotController` kiểm tra lần
cuối và giữ ownership của active deck/visual refresh.

## Validation

- Unity compile pass.
- EditMode job `b2abbf50e2324daf9b2683b067b605bc`: 15/15 pass, gồm panel bridge mới
  và regression active-deck assignment.

## Harness Delta

Không thay đổi Harness.
