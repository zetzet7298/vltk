# Vision Và Scope

| Trường | Giá trị |
|---|---|
| Mục đích | Chốt sản phẩm cần port, không quay lại mục tiêu “copy 99% PC” |
| Trạng thái | `design` |
| Owner / reviewer | Product owner / technical lead |
| Cập nhật | 2026-07-15 |

## Vision

Một game VLTK mobile màn hình dọc, chơi online, dùng identity và visual JX chính xác nhưng có nhịp battle/wave/upgrade kiểu DHCD. Người chơi chọn phái JX, vào arena, tự động chiến đấu theo bộ skill/card được xác minh, nhận drop và tiến triển; UI, input và flow ưu tiên một tay trên Android.

## In scope P0

- Android portrait adaptive, baseline 1080x1920; safe-area và nhiều aspect ratio.
- Guest account, save progression tối thiểu, starter gear.
- Ba phái pilot: Đường Môn, Cái Bang, Võ Đang; catalog tree phải audit trước khi expose.
- Một arena JX exact geometry/collision được chọn sau audit.
- Normal solo là **target pilot**, không phải claim DHCD recovered.
- JX player/NPC/item/skill base và SPR/VFX/WAV exact sau resolver.
- DHCD-style wave, drop, XP, modal card/reroll và battle lifecycle trong phạm vi evidence.
- Unity C# deterministic mirror, Go canonical verifier, checkpoint/replay.

## Out of scope P0

- Tương thích DHCD server/wire protocol; server reverse chỉ làm khi scope đổi.
- PvP/royal, guild/social/leaderboard, live economy, mount/pet, boss/escort/tower chưa evidence-gated.
- Phân phối public khi legal clearance chưa hoàn tất.
- Tự sáng tạo art/effect thay cho asset JX đã có.

## Success metrics pilot

| Metric | Target | Cách đo |
|---|---:|---|
| Critical gameplay acceptance | 100% P0 gate | `09-quality/acceptance-gates.md` |
| Portrait input/flow completion | 100% trên thiết bị test | scripted PlayMode + visual golden |
| Frame rate | 60 FPS bắt buộc trên device manifest máy tầm trung đã pin | profiler capture 10 phút + frame-time percentile |
| Replay verification | 100% run hợp lệ pass; mismatch bị quarantine | Go integration test |
| Pilot concurrency | 100 CCU | load test server |
| Data loss | RPO <= 24h | restore drill |
| Recovery | RTO <= 4h | timed drill |

## Quyết định sản phẩm cần giữ

“Giống DHCD” chỉ áp dụng cho loop và UX đã có evidence. Khi evidence không đủ, reverse owner phải chạy task bằng `/var/www/reverse-skill`, cập nhật `/home/zet/Projects/dhcd` và ghi kết quả/failed methods trước. Nếu reverse vẫn inconclusive mà cần ship rule mới, product owner chỉ được chọn rule bằng ADR đã approve sau bước reverse; rule phải ghi rõ là design, không gọi là “DHCD parity”.

## Blocker

Legal clearance asset JX/DHCD là release blocker; catalog/map/asset provenance chưa đủ là implementation blocker cho từng slice.

## Acceptance

- [ ] P0 scope được trace tới `REQ-P0-001`...`REQ-P0-011`.
- [ ] Product owner approve ranh giới internal-only/public và out-of-scope.
- [ ] Metrics pilot có artifact đo, không chỉ target trên giấy.
