# Domain: Tự tìm đường và tự đánh

## Định danh và phạm vi

- Domain ID: `DOM-AUTO`; DRI: Unity Gameplay; reviewer: Server Combat/QA; auto-combat sâu P1, auto-path P2.
- Sở hữu mobile orchestration/preset; không sở hữu authority move/target/cast/item.

## Bằng chứng as-is

- `EVID-0020`: `KPlayerAI.h:45-120` có active/fight mode, tự dùng HP/MP, radius/distance, support/fight/boss skill, target, danh sách NPC/object, loot/filter và follow.
- `EVID-0021`: `KNpcFindPath.cpp` và `KSubWorld::TestBarrier` là seam path/barrier; không chứng minh UX hoặc path parity mobile.
- `ADR-0004`: implementation tham khảo `~/Projects/vltk` chỉ dùng cho UX/auto-combat, không làm behavior authority.

## Invariant và policy

- Auto tạo cùng command contract như manual; server luôn validate. Không có endpoint `auto-damage` hay client loot authority.
- Hard leash là bất biến: target ngoài tâm+bán kính bị loại; vượt leash luôn chuyển `Returning`, không có preset đổi sang hành vi mơ hồ khác.
- Auto-path và auto-combat là hai state machine độc lập. Joystick, manual cast hoặc combat bắt đầu hủy auto-path ngay và không tự resume.
- Manual cast pause auto-combat tới hết recovery rồi resume `Scanning` nếu vẫn trong leash. Toggle off, death, transfer, disconnect hoặc túi đầy là terminal stop; leash breach chuyển `Returning`; không có target giữ `Scanning`, không tự stop.
- Target policy duy nhất dùng chung `DOM-CBT`; một pending cast mới nhất; không có hai bộ chọn mục tiêu cạnh tranh.
- Preference có thể persist, nhưng runtime target/path queue không phải source of truth sau reconnect.

## State và nghiệm thu

- Auto-path: `Off -> Resolving -> Navigating -> Arrived`; `joystick/manual-cast/combat/no-route/transfer -> Stopped(reason)` và không tự resume.
- Auto-combat: `Off -> Scanning -> Engaging -> Looting -> Returning -> Scanning`; manual cast tạo `PausedManual -> Scanning`, còn toggle/death/disconnect/transfer/bag-full tạo `Stopped(reason)`.
- `TEST-AUTO-001` P1: NPC trong/ngoài leash, target chết/despawn, cooldown/mana, manual override và reconnect.
- `TEST-AUTO-002` P1: loot filter/túi đầy, heal threshold; exact legacy threshold parity BLOCKED `[CẦN XÁC NHẬN]`; owner Gameplay/QA; gỡ block khi runtime golden + reviewer duyệt.
- `TEST-AUTO-003` P2: path no-route/barrier/map transfer; compare static source trước, runtime golden `BLOCKED`.
