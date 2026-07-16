# ADR-001: Authority JX Và DHCD

| Trường | Giá trị |
|---|---|
| Mục đích | Khóa source authority theo loại claim, tránh port nhầm logic hoặc visual |
| Trạng thái tài liệu | `design` |
| Trạng thái quyết định | `proposed` |
| Owner / approver | Product owner / technical reviewer + JX/DHCD reviewers |
| Evidence | `/home/zet/.codex/attachments/19ad23fc-a9f3-4549-9e80-41e12e77df01/pasted-text-1.txt`; `E-JX-ROOT`; `E-DHCD-MAP` |
| Cập nhật | 2026-07-15 |

## Context và evidence

Brief sản phẩm yêu cầu visual/identity JX nhưng logic và nhịp chơi DHCD. PC source không được dùng để ép gameplay thành bản sao 99%, còn corpus reverse DHCD không được dùng làm nguồn asset hoặc JX identity.

## Options

| Option | Lợi ích | Rủi ro |
|---|---|---|
| Port gần như toàn bộ JX PC | Nhiều source PC | Sai mục tiêu gameplay portrait/DHCD |
| Clone DHCD cả logic lẫn visual | Loop thống nhất | Vi phạm yêu cầu dùng asset JX |
| Hybrid authority theo claim | Đúng brief, audit được | Cần provenance và parity gate chặt |

## Proposed decision

Chọn hybrid: JX là authority cho identity, map, NPC, item, skill base và SPR/VFX/WAV; DHCD reverse corpus chỉ là evidence cho combat loop, wave, drop/XP, card/reroll và UX portrait. Gap phải reverse bằng `/var/www/reverse-skill`; không được suy diễn. Deviation cần ADR riêng.

## Consequences và rollback

Asset/config thiếu provenance phải fail closed. Nếu authority conflict, tắt slice bằng feature flag và quay lại source audit; không dùng art hoặc behavior tự tạo để giữ tiến độ.

## Trace

`OBJ-P0-01/02 -> REQ-P0-001/002/003/005 -> DOC-GOV-01/02, DOC-JX-01..08, DOC-GAME-01..05 -> provenance/golden gates`

## Acceptance

- [ ] Product owner, technical reviewer và hai domain reviewer approve.
- [ ] Source hierarchy, resolver manifest và reverse queue không mâu thuẫn quyết định.
- [ ] Một conflict test chứng minh feature fail closed thay vì fallback.
