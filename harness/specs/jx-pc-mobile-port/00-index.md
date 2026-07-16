# Bộ specs CNPM port VLTK PC sang mobile

| Thuộc tính | Giá trị |
| --- | --- |
| Mã bộ tài liệu | `SPEC-JX-PC-MOBILE-001` |
| Ngôn ngữ | Tiếng Việt |
| Bối cảnh | Brownfield/hybrid: Unity client hiện hữu, Go server mới, PC corpus legacy |
| Phạm vi | Gameplay client, server, content, UI/UX và vận hành tối thiểu |
| Ngoài phạm vi | PaySys, launcher/patcher riêng, GM/backoffice, anti-cheat PC |
| Trạng thái | `SPECIFIED` |
| Nguồn canonical | Active Vietnamese PC client manifest và source-backed server evidence |

## Cách đọc

1. Đọc [01-yeu-cau.md](01-yeu-cau.md) để hiểu nhu cầu, quy định và tiêu chí sản phẩm.
2. Đọc [02-mo-hinh-yeu-cau.md](02-mo-hinh-yeu-cau.md) để hiểu chức năng, quyền và luồng dữ liệu.
3. Đọc [03-du-lieu.md](03-du-lieu.md) cùng `contracts/` để triển khai Go, PostgreSQL và Protobuf.
4. Đọc [04-giao-dien.md](04-giao-dien.md) để triển khai Unity UI/UX và input mobile.
5. Dùng `domains/` làm đặc tả implementation theo miền; dùng `registry/` và `as-is/` để truy vết.
6. Dùng [delivery/completion-audit.md](delivery/completion-audit.md) để kiểm từng yêu cầu nguồn và không nhầm premerge xanh với release-ready.

## Nguồn sự thật

- Bốn tài liệu CNPM ở root giữ nguyên cấu trúc template `$cnpm` và là bản tổng hợp chuẩn theo pha.
- `domains/<domain>/` là nguồn chuẩn cho invariant, behavior, failure mode và acceptance của từng miền; không lặp lại requirement statement.
- `registry/*.yaml` là nguồn chuẩn cho ID, owner, priority, phase, design và gate.
- `registry/traceability.csv` là nguồn chuẩn duy nhất cho các cạnh truy vết.
- `as-is/claims.yaml`, `evidence.yaml`, `contradictions.yaml` là nguồn chuẩn cho hiện trạng và provenance.
- `governance/mobile-targeting-research.md` ghi nguồn usability cho input mobile; không phải nguồn gameplay canonical.
- `governance/orchestration.md` khóa thứ tự pha, file ownership độc quyền và cross-review seam.
- `contracts/openapi/game.v1.yaml`, `contracts/proto/game/v1/game.proto` và
  `contracts/content/manifest.v1.schema.json` là contract máy đọc chuẩn; prose chỉ giải thích.

## Thứ tự ưu tiên triển khai

| Phase | Mục tiêu | Gate kết thúc |
| --- | --- | --- |
| `P0` | Combat Parity Lab, năm training NPC, novice và đủ 10 môn phái | Logic 100%, visual skill SSIM từng case `>= 0.99` |
| `P1` | Login, tạo nhân vật, Ba Lăng 53, combat, loot, túi/mặc đồ, cấp 1-200, persistence | Vertical slice client-Go-PostgreSQL chạy và phục hồi được |
| `P2` | PvE world, map, NPC, quest, mount, pet | Catalog/world behavior và content delivery đạt gate |
| `P3` | Party, chat, friend, trade, stall, guild | Social/economy atomic và moderation baseline |
| `P4` | PK/PvP, endgame, event, chuyển sinh | Replay, fairness, event rollback và load gate |

## Trạng thái và chất lượng

- Code hiện hữu chỉ được ghi `DISCOVERED / UNVERIFIED` cho tới khi có flow trace, test và evidence canonical.
- `FUNCTIONAL` không đồng nghĩa `PARITY_DONE`; `VISUAL_DEBT` không được ẩn bằng tolerance trung bình.
- Claim thiếu nguồn phải ghi `[CẦN XÁC NHẬN]`, `BLOCKED`, owner và điều kiện gỡ block; không được bịa fallback production.
- `/var/www/jx-source` là read-only. PAK/SPR/DAT/hash/encoding chỉ xử lý qua `~/Projects/vltktool`.

## Package liên quan

- Không sửa hoặc kế thừa trạng thái từ `../dhcd-jx-port/`; package đó là scope khác.
- `~/Projects/vltk` chỉ là reference UX/auto-combat, không phải canonical visual hay gameplay.
- Golden metadata lưu trong Git; PNG lossless/video gốc lưu MinIO riêng, content-addressed và kiểm SHA-256.
