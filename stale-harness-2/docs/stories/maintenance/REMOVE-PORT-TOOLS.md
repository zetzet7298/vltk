# REMOVE-PORT-TOOLS — Remove unwanted providers

## Status

implemented

## Lane

normal

## Product Contract

Harness registry chỉ giữ provider người dùng muốn dùng cho project.

## Acceptance Criteria

- Gỡ `gitnexus`, `semble`, `agent-browser`, `worker`, `review`, `security-review`, `simplify`, `deepwiki`.
- Không gỡ hoặc sửa provider khác.
- `tool check` không có provider `missing` sau thay đổi.

## Design Notes

Dùng `harness-cli tool remove`; không sửa schema hoặc policy.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Không áp dụng |
| Integration | Tám tên không còn trong bảng `tool` |
| E2E | Không áp dụng |
| Platform | Tool còn lại scan bình thường |
| Release | Không áp dụng |

## Harness Delta

Chỉ thay đổi local tool registry trong `harness.db`.

## Evidence

- Tám provider chỉ định không còn trong bảng `tool`.
- Registry còn 8 provider: 6 `present`, 2 `unknown`, 0 `missing`.
- Verification của `REMOVE-PORT-TOOLS` và `REGISTER-PORT-TOOLS` đều pass.
