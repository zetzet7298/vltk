# Legal Clearance

| Trường | Giá trị |
|---|---|
| Mục đích | Kiểm soát quyền dùng/phân phối asset và corpus |
| Trạng thái | `blocked` |
| Owner / reviewer | Legal owner / product owner |
| Cập nhật | 2026-07-15 |

## Hiện trạng

Quyền dùng và phân phối asset JX/DHCD chưa được xác minh trong repo. Đây là release blocker; pilot chỉ internal khi có approval ghi rõ phạm vi, người phê duyệt, thời hạn và không public distribution.

## Clearance record

| Corpus/asset | Quyền sở hữu/nguồn | Phạm vi được phép | Evidence/license | Expiry | Status |
|---|---|---|---|---|---|
| JX PC source/SPR/VFX/WAV | `[CẦN XÁC NHẬN]` | `[CẦN XÁC NHẬN]` | `[CẦN XÁC NHẬN]` | `[CẦN XÁC NHẬN]` | blocked |
| DHCD reverse corpus | `[CẦN XÁC NHẬN]` | internal analysis/runtime? | `[CẦN XÁC NHẬN]` | `[CẦN XÁC NHẬN]` | blocked |
| Generated/vendor bytes | `[CẦN XÁC NHẬN]` | repo/internal build | `[CẦN XÁC NHẬN]` | `[CẦN XÁC NHẬN]` | blocked |

Không ghi legal conclusion dựa trên file hiện hữu hoặc quyền truy cập filesystem. Legal owner phải attach văn bản/approval và cập nhật ledger trước release.

## Acceptance

- [ ] Mỗi corpus/asset có owner, văn bản/phê duyệt, phạm vi, expiry và status.
- [ ] Pilot channel luôn internal-only, không phụ thuộc trạng thái legal; internal-only flag kiểm tra approval hiện hành.
- [ ] Public distribution chỉ được xét ở gate hậu pilot khi clearance đã `cleared` và có release approval riêng.
- [ ] CI/release checklist kiểm tra clearance record và không log/copy secret.
