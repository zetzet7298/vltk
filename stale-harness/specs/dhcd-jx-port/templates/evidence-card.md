# Evidence Card Template

| Trường | Giá trị |
|---|---|
| Mục đích template | Chuẩn hóa provenance cho một claim |
| Trạng thái tài liệu | `design` |
| Evidence ID | `E-...` |
| Claim | Một câu có thể kiểm chứng |
| Evidence kind | `asset-pak` / `source` / `document` / `runtime` / `test` |
| Status / confidence | `verified` / `documented` / `provisional` / `unresolved` |
| Owner / reviewer | `...` |
| Captured at | `YYYY-MM-DD` |
| Cập nhật template | 2026-07-15 |

## Provenance bắt buộc

- Absolute source path:
- Source layer (`pak_unpacked`, legacy source, reverse corpus, runtime):
- Pack/version và load-order winner (chỉ `asset-pak`):
- Logical path (nếu có):
- Hash_UID (chỉ `asset-pak`):
- Encoding/path bytes (bắt buộc `asset-pak`, loại khác ghi `N/A`):
- `_labels.json:name_vi` cross-check (khi có):
- Byte count:
- SHA-256:
- Resolver/decode command và output:
- Golden/runtime test:

## Giới hạn

- Điều evidence chứng minh:
- Điều evidence không chứng minh:
- Assumption hoặc conflict:
- Follow-up ticket:

## Acceptance

- [ ] Evidence kind chọn đúng và fields conditional không bị bịa.
- [ ] Absolute path/hash/owner/confidence và giới hạn claim có đủ.
