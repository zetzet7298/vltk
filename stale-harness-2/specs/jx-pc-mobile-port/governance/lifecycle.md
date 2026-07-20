# Lifecycle, ownership và traceability

## Vòng đời

`DISCOVERED -> SOURCE_PROVEN -> SPECIFIED -> READY -> IMPLEMENTING -> FUNCTIONAL -> VERIFYING -> PARITY_DONE`

Trạng thái ngoại lệ: `BLOCKED`, `DEFERRED`, `OUT_OF_SCOPE`, `SUPERSEDED`. Cờ độc lập: `UNVERIFIED`, `VISUAL_DEBT`, `STALE`.

## Điều kiện promotion

- Mỗi record có đúng một DRI role và một parity reviewer.
- Mọi requirement có objective, source/evidence hoặc blocker, acceptance criteria và test ID.
- `FUNCTIONAL` yêu cầu flow chạy được nhưng có thể còn visual debt.
- `PARITY_DONE` yêu cầu source winner/hash, implementation anchor, test result cùng revision, golden nếu cần, reviewer và rollback; không có contradiction mở.
- Evidence/hash/tool revision đổi sẽ đặt verification thành `STALE` và hạ promotion cho tới khi chạy lại.

## ID ổn định

| Loại | Pattern |
| --- | --- |
| Mục tiêu | `OBJ-###` |
| Chức năng | `FR-###` |
| Phi chức năng | `NFR-<QUALITY>-###` |
| Quyết định | `ADR-####` |
| Rủi ro | `RISK-###` |
| Test | `TEST-<LAYER>-###` |
| Migration/debt | `MIG-###`, `DEBT-###` |
| Claim/evidence/contradiction | `CLAIM-####`, `EVID-####`, `CON-####` |
| Gap/parity/golden | `GAP-###`, `PAR-####`, `GOLD-####` |

ID không chứa phase, priority hoặc domain có thể đổi. ID đã phát hành không được tái sử dụng; record superseded vẫn được giữ.

## Chuỗi truy vết bắt buộc

`OBJ -> FR/NFR -> domain/ADR/contract -> TEST -> release gate`

Brownfield bổ sung: `CLAIM + EVID -> GAP/RISK/DEBT -> FR -> MIG -> TEST -> retirement`.
