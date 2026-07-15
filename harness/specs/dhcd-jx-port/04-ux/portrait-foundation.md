# Portrait Foundation

| Trường | Giá trị |
|---|---|
| Mục đích | Chuyển client sang portrait một tay, adaptive theo safe area |
| Trạng thái | `design` |
| Owner / reviewer | UX owner / client reviewer |
| Cập nhật | 2026-07-15 |

## Contract

- Baseline logical resolution `1080x1920`; không assume thiết bị đúng tỉ lệ.
- Canvas dùng safe-area inset, scale theo width/height và anchor semantic.
- Playfield giữ vùng trung tâm; HUD không che joystick/target/skill button.
- Touch target tối thiểu 44dp; hỗ trợ notch, gesture navigation, rotate lock portrait.
- Text Việt có fallback font và localization key; không bake string vào SPR.
- Orientation state, pause, modal và reconnect phải khôi phục idempotent.

Portrait UX là product target; corpus DHCD hiện không đủ chứng minh layout gốc. Không dùng screenshot làm proof.

## Layout zones

| Zone | Nội dung |
|---|---|
| Top | HP/MP/EXP, run timer, connection/status |
| Center | JX map/playfield, camera bounds |
| Bottom-left | Joystick và movement state |
| Bottom-right | Skill/action buttons, auto-cast indicator |
| Overlay | card choice, inventory, reconnect/error |

## Acceptance

- Golden screenshots trên 16:9, 19.5:9, tablet narrow và display cutout.
- Touch automation không bị overlay chặn.
- 60 FPS target giữ trong battle; orientation không reset run.
