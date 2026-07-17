# Côn Luân canonical learned-membership and static catalog proof

## Current Behavior

Unity quan sát 18 root skill Côn Luân (`167..184`) trong panel. Canonical PC
progression + skillbook chứng minh 24 learned skill: 13 ID shared, 11 PC-only và
5 Unity-only unresolved, tạo union 29 ID. Chưa có frozen membership artifact hay
static oracle độc lập cho Côn Luân; catalog/runtime hiện chỉ có proof yếu hoặc tự
nhất quán.

## Target Behavior

- Freeze exact PC-learned membership từ `skills_table.lua` + `skillbook.lua`.
- Dùng `vltktool` tạo exact-byte slice/provenance cho static rows; không tự decode,
  hash hoặc parse full encoded `skills.txt`.
- Sinh deterministic static oracle chỉ cho learned scope, kèm relationship closure.
- So production catalog với oracle bằng test không lấy expected từ Unity.
- Giữ `uiOrder = null`; progression tier và skillbook grant chỉ chứng minh
  membership, không chứng minh panel slot order.

## Affected Users

- Người chơi phái Côn Luân trên Unity/mobile.
- Maintainer port combat skill từ PC source.

## Affected Product Docs

- `harness/docs/stories/SKL-ALL-PARITY-001/`
- `harness/docs/stories/SKL-KL-PROOF-001/`

## Non-Goals

- Không claim runtime curve, projectile timing, visual/audio hoặc Android parity.
- Không tự thêm 5 Unity-only ID vào learned state khi PC evidence chưa chứng minh.
- Không sửa `/var/www/jx-source`.
- Không suy UI order từ progression/skillbook membership.
