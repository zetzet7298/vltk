# Glossary

| Trường | Giá trị |
|---|---|
| Mục đích | Từ điển dùng chung giữa product, Unity, Go và reverse |
| Trạng thái | `design` |
| Owner / reviewer | Product owner / all reviewers |
| Cập nhật | 2026-07-15 |

| Thuật ngữ | Định nghĩa |
|---|---|
| JX | Võ Lâm Truyền Kỳ PC source tại `/var/www/jx-source`; authority cho identity và visual |
| DHCD | Corpus client đã reverse tại `/home/zet/Projects/dhcd`; evidence cho loop, không phải runtime target |
| Source of truth | Nguồn được phép quyết định một loại claim cụ thể, theo source hierarchy |
| Candidate | Asset/map/config có thể dùng nhưng chưa qua resolver/decode/golden |
| Provenance | Chuỗi truy xuất từ logical path đến bytes, pack, UID, hash và test |
| Run | Một phiên battle từ `Start` tới `End` hoặc `Abort` |
| Wave | Nhóm spawn được điều phối bởi timeline; không đồng nghĩa map JX |
| Deck | Tập card/upgrade có trong một run |
| Timeline | Lịch tier và event card theo mode/difficulty |
| Mirror | Mô phỏng C# deterministic trên client, không có quyền canonical |
| Verifier | Go service kiểm tra input, hash, checkpoint và replay |
| Checkpoint | Snapshot canonical định kỳ gồm state hash và sequence |
| Replay | Chuỗi input/event đủ để tái dựng và xác minh run |
| Feature flag | Cờ rollout/migration có owner, expiry và rollback |
| Golden | Artifact cố định dùng so sánh behavior/visual/contract |
| Pilot | Build nội bộ giới hạn người dùng và không phân phối công khai |
| `[CẦN XÁC NHẬN]` | Unknown phải có owner và cách xác minh, không phải placeholder vô thời hạn |

## Không đồng nhất

`Normal solo` là target pilot, không phải fact đã reverse. `yanwuchang`, `jingjichang`, `shiliantang` là tên arena candidate, chưa phải map ID/collision winner. `MapEnemyDatabase` và mapping item/NPC hiện hữu là provisional cho tới khi có source evidence.

## Acceptance

- [ ] Product/Unity/Go/reverse docs dùng cùng định nghĩa cho run, wave, mirror, verifier và candidate.
- [ ] Thuật ngữ conflict tạo ADR/glossary update, không đổi nghĩa âm thầm.
