# Battle HUD

| Trường | Giá trị |
|---|---|
| Mục đích | HUD dọc cho battle, tận dụng UI SPR JX đã resolve |
| Trạng thái | `design` |
| Owner / reviewer | UX owner / JX reviewer |
| Cập nhật | 2026-07-15 |

## Components

- Player HP/MP/EXP và status.
- Target name/HP/status.
- Minimap nếu map candidate có asset/geometry verified.
- Chat/status tối giản, không lấn input.
- Skill/action toolbar, auto-cast state, cooldown/cost.
- Run timer, wave progress, reward/choice modal.

## Asset rule

Icon, frame, bar, minimap và effect phải map tới JX UI/SPR manifest; không flip/overlap/missing icon bằng cách vẽ placeholder. Nếu chưa resolve, component hiển thị trạng thái unavailable trong internal build và chặn content gate.

## Interaction

- Joystick hit area độc lập skill buttons.
- Modal card serialize event theo player; global pause chưa được chứng minh.
- Reconnect overlay không cho gửi input cũ ngoài sequence window.

## Acceptance

- UI golden + sprite provenance cho mỗi visible asset.
- Touch matrix và safe-area test.
- HUD state tái tạo từ replay/checkpoint.
