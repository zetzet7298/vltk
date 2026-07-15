# Asset Manifest Template

| Trường | Giá trị |
|---|---|
| Mục đích template | Chuẩn hóa selection và provenance của asset JX |
| Trạng thái tài liệu | `design` |
| Cập nhật template | 2026-07-15 |
| Asset ID / status | `ASSET-...` / `provisional` |
| Owner / reviewer | `...` / `...` |
| Logical role | `SPR` / `VFX` / `WAV` / `map` / `UI` |

| Field | Value |
|---|---|
| Original logical path |  |
| Candidate absolute paths (all) |  |
| Absolute selected path |  |
| Pack/version |  |
| Load-order winner |  |
| Hash_UID |  |
| Encoding |  |
| Normalized path bytes (hex) |  |
| Byte count |  |
| SHA-256 |  |
| Resolver evidence |  |
| `_labels.json:name_vi` cross-check |  |
| Decode/action/direction/frame check |  |
| Unity import settings |  |
| Legal clearance | `unknown` / `cleared` / `blocked` |
| Golden/test reference |  |

## Selection rule

Không chọn asset chỉ vì tên gần giống hoặc file tồn tại. Enumerate candidate, resolver, decode, golden rồi mới vendor exact bytes.

## Acceptance

- [ ] Có mọi candidate path, load-order, resolver bytes, label cross-check và hash.
- [ ] Usage, legal state và golden link tồn tại trước khi chọn `verified`.
