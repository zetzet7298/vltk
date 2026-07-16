# ADR-004: Portrait Adaptive Baseline

| Trường | Giá trị |
|---|---|
| Mục đích | Khóa orientation, responsive baseline và input target của client |
| Trạng thái tài liệu | `design` |
| Trạng thái quyết định | `proposed` |
| Owner / approver | UX owner + client lead / product owner + technical reviewer |
| Evidence | Brief `E-PORT-BRIEF`; DHCD portrait chỉ dùng khi có evidence card |
| Cập nhật | 2026-07-15 |

## Context và evidence

Sản phẩm phải chuyển từ client hiện tại sang màn hình dọc kiểu DHCD, ưu tiên chơi một tay trên Android. Baseline 1080x1920 là contract layout, không phải khóa resolution vật lý.

## Options

| Option | Lợi ích | Rủi ro |
|---|---|---|
| Giữ landscape | Ít migration | Sai brief và interaction target |
| Portrait fixed pixel | Dễ dựng một máy | Hỏng safe-area/aspect khác |
| Portrait adaptive 1080x1920 | Đúng brief và scale được | Cần responsive/golden matrix |

## Proposed decision

Android online-only, portrait adaptive baseline 1080x1920. HUD, camera crop, touch target, safe-area và modal phải scale theo aspect/device matrix; visual vẫn dùng JX asset. Internal pilot phải đạt gate 60 FPS trên device manifest máy tầm trung đã pin.

## Consequences và rollback

Portrait shell đi qua feature flag và migration slice; không duy trì landscape như alternate pilot UI. Nếu device matrix fail, rollback slice chứ không scale/crop thủ công per-device.

## Trace

`OBJ-P0-01 -> REQ-P0-004 -> DOC-UX-01/02/03/04, DOC-CLIENT-03 -> PlayMode/visual/performance gates`

## Acceptance

- [ ] Product owner, technical reviewer, UX owner và client lead approve.
- [ ] Safe-area/touch/camera/modal golden pass trên device matrix.
- [ ] Release build đạt gate 60 FPS và frame-time budget trên device manifest máy tầm trung đã pin.
