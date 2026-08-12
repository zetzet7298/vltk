# Ma trận case parity P0

## Artifact

| File | Vai trò |
| --- | --- |
| `skill-parity-p0.schema.json` | JSON Schema Draft 2020-12 cho một case skill/level, bắt buộc logic, target, RNG/state, visual/audio oracle và `blocked_fields` |
| `skill-parity-p0.json` | Ma trận expansion cho shared, novice và 10 phái; pin catalog input và liệt kê chiều biên bắt buộc |

## Cách sinh case

1. Reconciler phải điền `catalog_selection` tái lập được cho đúng 12 nhóm; hiện tất cả cố ý `BLOCKED`, vì `Skills.txt` đơn lẻ không chứng minh membership hoặc behavior.
2. Với mỗi skill canonical, sinh case cho từng level hợp lệ và mọi variant trong `required_variants`; bổ sung variant effect/missile/state riêng của skill.
3. Validate từng record bằng schema. Mỗi field chưa biết dùng `{status: "BLOCKED", value: null, evidence_refs: [...], blocker: "..."}` và đưa path vào `blocked_fields`.
4. `NOT_APPLICABLE` phải có rule/source chứng minh. Không bỏ case vì implementation hiện tại chưa hỗ trợ.
5. Gate logic so event/snapshot từng tick; gate visual so từng frame với SSIM `>=0,99`; catalog coverage, runtime golden và reviewer đều đạt mới được `PARITY_DONE`.

## Quy tắc chống false parity

Một screenshot cuối, tổng damage cuối, mô tả tiếng Việt, icon giống nhau hoặc deterministic replay nội bộ chỉ là evidence hẹp. Chúng không thay thế proc order, rounding/RNG draw, state/missile timeline và PC runtime golden.
