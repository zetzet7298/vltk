# REGISTER-PORT-TOOLS — Register port toolchain

## Status

implemented

## Lane

normal

## Product Contract

Harness registry phải công bố các tool thực sự dùng cho workflow port PC → Unity mobile, theo capability thay vì phụ thuộc tên tool trong prompt.

## Relevant Product Docs

- `docs/TOOL_REGISTRY.md`
- `specs/jx-pc-mobile-port/governance/orchestration.md`
- `specs/jx-pc-mobile-port/governance/source-authority.md`

## Acceptance Criteria

- Đăng ký code discovery, PC evidence, web/docs, Unity Editor, visual parity và orchestration tools đã chọn.
- Tool có local executable/config/skill path phải scan thành `present`.
- Runtime-only visual MCP không có scan surface được giữ `unknown`, không giả báo `present`.
- Không đăng ký Unity package như tool riêng; package được vận hành qua Unity MCP/project manifest.

## Design Notes

- Một row đại diện một provider/capability ổn định.
- `vltktool` đăng ký dạng toolchain/skill với scan directory; script cụ thể được chọn theo task.
- Unity MCP scan config project; agent vẫn phải xác nhận live session lúc dùng.
- `describe-image` và `vision-ui-diff` không có project-local scan target nên giữ `unknown`.
- Danh sách ban đầu được thu hẹp bởi story `REMOVE-PORT-TOOLS` theo chỉ định người dùng.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Không áp dụng |
| Integration | `tool check` và truy vấn registry |
| E2E | Không áp dụng |
| Platform | CLI/path/config/skill providers scan được |
| Release | Không áp dụng |

## Harness Delta

Thêm inbound registry rows trong local `harness.db`; không sửa Harness policy hay source hierarchy.

## Evidence

- Registry hiện giữ 8 provider sau story `REMOVE-PORT-TOOLS`.
- `tool check`: 6 `present`, 2 `unknown`, 0 `missing`.
- `unknown` có chủ đích: `describe-image`, `vision-ui-diff`; agent runtime xác nhận khi dùng.
