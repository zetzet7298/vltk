# Localization

| Trường | Giá trị |
|---|---|
| Mục đích | Ship tiếng Việt trước, chuẩn bị English mà không đổi identity JX |
| Trạng thái | `design` |
| Owner / reviewer | Localization owner / product owner |
| Cập nhật | 2026-07-15 |

## Rules

- UI string dùng localization key; không lấy tên file/hash làm text hiển thị.
- Giữ mapping original JX name, ID và transliteration trong catalog; tiếng Việt là presentation layer.
- Config legacy encoding phải decode bằng resolver workflow; không tự sửa bytes nguồn.
- Placeholder/plural/number/date dùng locale formatter.
- Skill/item/NPC chưa có mapping Việt rõ phải đánh dấu `[CẦN XÁC NHẬN]`, không dịch sáng tạo.

## Acceptance

- Vietnamese coverage 100% P0; missing-key scan fail CI.
- Pseudo-localization không tràn portrait layout.
- English framework chạy được trên cùng key set, dù content có thể P1.
