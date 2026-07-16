# Nghiên cứu UX chọn mục tiêu trên mobile

## Phạm vi và mức thẩm quyền

- Ngày rà soát: `2026-07-16`; công cụ: `ketch search --scrape`.
- Tài liệu này chỉ biện minh cách nhập liệu mobile. Logic combat, quan hệ địch-ta,
  range, LOS, skill và damage vẫn phải truy về PC canonical và golden test.
- Nguồn nền tảng/accessibility có thẩm quyền cao hơn bài hướng dẫn game cộng đồng.
  Không nguồn ngoài nào được dùng để đổi HUD geometry hoặc visual JX.

## Bằng chứng

| ID | Nguồn | Claim có thể dùng | Giới hạn |
| --- | --- | --- | --- |
| `RSCH-MOB-001` | [Android Accessibility Help - Touch target size](https://support.google.com/accessibility/android/answer/7101858) | Control chạm nên tối thiểu `48x48dp`, cách nhau `8dp`; hitbox có thể lớn hơn art. | Hướng dẫn UI chung, không định nghĩa target policy trong game. |
| `RSCH-MOB-002` | [W3C WCAG 2.1 - Understanding target size](https://www.w3.org/WAI/WCAG21/Understanding/target-size.html) | Touch là coarse input; target nhỏ khó kích hoạt chính xác; control tương đương nên ít nhất `44x44 CSS px`. | Chuẩn web/accessibility, dùng làm nguyên tắc human-factor. |
| `RSCH-MOB-003` | [Vortex Gaming - phân tích targeting Mobile Legends](https://vortexgaming.io/en/postdetail/555754) | Pattern thực tế gồm auto priority, explicit lock và giữ-kéo/manual aim; chase cần giới hạn để tránh hành vi ngoài ý người chơi. | Nguồn cộng đồng, chỉ tham khảo interaction pattern; không sao chép priority theo HP hoặc gameplay. |

## Quyết định áp dụng

- Không chạm trực tiếp actor nhỏ để chọn mục tiêu. Tap attack/skill chạy deterministic
  acquisition; nút lock là control lớn độc lập và lock không tự đổi theo HP.
- Giữ-kéo skill cho aim hướng/ground reticle; thả để gửi intent, kéo vào vùng hủy để
  bỏ. Server vẫn authoritative cho range, LOS và kết quả cast.
- Joystick là direct movement duy nhất. Auto-approach có hard leash và bị joystick,
  cancel hoặc target invalid hủy; không biến chase thành tap-to-move.
- Mọi control mới tuân `48x48dp` và khoảng cách `8dp`. SPR PC có thể giữ kích thước
  nhìn thấy nhỏ hơn nếu vùng hit vô hình đạt chuẩn và không chồng lấn.

## Trace

- Quyết định: `ADR-0004`; yêu cầu: `FR-TGT-001`, `FR-TGT-002`, `FR-CBT-003`.
- Thiết kế: `DOM-CBT`, `UI-HUD-WORLD`; kiểm thử: `TEST-UI-001`, `TEST-SKL-001`.
