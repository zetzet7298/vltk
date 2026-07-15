# Bộ Specs Port JX x DHCD

| Trường | Giá trị |
|---|---|
| Mục đích | Làm hợp đồng triển khai cho game mobile dọc: logic và nhịp chơi kiểu DHCD, identity/visual/asset JX |
| Bối cảnh | Hybrid: Unity client hiện hữu + corpus reverse DHCD + PC source JX canonical |
| Trạng thái | `design` |
| Owner | Product owner + technical lead |
| Reviewer | Orchestrator và các reviewer gameplay/JX/server |
| Cập nhật | 2026-07-15 |

## Cách đọc

`status.yaml` là ledger chuẩn cho trạng thái, ưu tiên, blocker, evidence và dependency; `document_provenance` ghi confidence, source version/hash, acceptance reference và ADR theo từng document ID, còn `document_record_defaults` chỉ là fallback cho field chưa override. Mỗi shard tách ba lớp:

1. **As-is/evidence**: điều đã quan sát được, luôn có đường dẫn tuyệt đối và giới hạn bằng chứng.
2. **To-be contract**: hành vi sản phẩm cần xây, không được trình bày như hành vi đã reverse.
3. **Verification**: test/golden/artifact phải tạo để chuyển trạng thái.

Không có shard nào được coi là hoàn thành chỉ vì đã có heading hoặc code skeleton. `verified` chỉ dùng khi evidence và acceptance test đã tồn tại; `provisional` dùng cho mapping cần audit; `design` là quyết định sản phẩm/kỹ thuật; `not_started` là chưa có artifact; `blocked` là có blocker ngoài phạm vi hiện tại.

## Source hierarchy

- JX identity, map, NPC, item, skill base, SPR/VFX/WAV: `/var/www/jx-source` theo [source-hierarchy](01-governance/source-hierarchy.md).
- DHCD combat loop, wave, drop, card/reroll và UX clue: corpus `/home/zet/Projects/dhcd`, chỉ trong phạm vi [evidence-register](01-governance/evidence-register.md).
- Reverse thiếu evidence: `/var/www/reverse-skill`, đầu ra phải cập nhật vào `/home/zet/Projects/dhcd`.
- Unity code hiện hữu: nguồn tái sử dụng và migration evidence, không thay thế PC source.
- PostgreSQL runtime target: dùng container ở `/var/www/tt-docker/` sau khi kiểm tra runtime, chỉ dùng database/role riêng cho game; đây chưa phải capability đã verified.

## Shard map

| Nhóm | Nội dung | File |
|---|---|---|
| Governance | source, evidence, thuật ngữ, ADR policy và ADR-001..005 | `01-governance/` |
| Product | vision, roster, mode, roadmap | `02-product/` |
| Gameplay | loop, skill, deck, wave, economy | `03-gameplay/` |
| UX | portrait, HUD, inventory, localization | `04-ux/` |
| JX parity | map, skill, item, NPC, asset | `05-jx-parity/` |
| Client | reuse, mirror, migration | `06-client/` |
| Server | architecture, API, data, replay | `07-server/` |
| Operations | Compose, secrets, telemetry, backup, CI | `08-operations/` |
| Quality | test, gates, performance, release | `09-quality/` |
| Research | reverse queue, unknowns, legal | `10-research/` |

## Traceability tối thiểu

Mọi feature phải đi theo chuỗi `OBJ -> REQ -> DESIGN/ADR -> IMPLEMENTATION -> TEST -> RELEASE SIGNAL`. ID ổn định dùng trong docs, issue và test; không dùng tên file làm ID. Bảng mapping thực tế nằm ở [traceability](01-governance/traceability.md).

Các quyết định gốc:

- [`ADR-001`](01-governance/adr-001-source-authority.md): JX là authority cho identity/visual; DHCD là evidence cho loop.
- [`ADR-002`](01-governance/adr-002-reuse-migration.md): reuse-first và migration gate.
- [`ADR-003`](01-governance/adr-003-deterministic-authority.md): Unity deterministic mirror + Go canonical verifier.
- [`ADR-004`](01-governance/adr-004-portrait-foundation.md): portrait adaptive 1080x1920.
- [`ADR-005`](01-governance/adr-005-legal-evidence-gate.md): legal/evidence gate trước pilot phân phối.
- `ADR-006`: wave ownership và co-op relay (đang proposed).

## Definition of Ready

Một shard chỉ được đưa vào implementation khi có owner, scope, evidence hoặc `[CẦN XÁC NHẬN]`, confidence, source version/hash hoặc `design-only`, dependency, acceptance test, ADR liên quan và blocker rõ trong `status.yaml`.

## Trạng thái hiện tại

| Nhóm | Đã làm | Chưa làm / blocker |
|---|---|---|
| Specs/governance | Đã tạo 58 Markdown shard, ledger YAML, source hierarchy, evidence register, traceability, ADR records và template | ADR-001..005 còn `proposed`; cần approval thật và cập nhật ledger khi artifact xuất hiện |
| Reverse DHCD | Đã lập evidence map và reverse queue có priority/owner/state | Card rule, pause, mode, reconnect và balance exact còn queued |
| JX parity | Đã khóa resolver/load-order/provenance và manifest schema | Map/NPC/item/skill/SPR candidate chưa audit đầy đủ; legal chưa cleared |
| Client | Đã inventory reuse và migration gates | Mirror C# deterministic, portrait runtime và visual golden chưa triển khai |
| Server/ops | Đã chốt Go/PostgreSQL/REST/WSS/replay/ops contract; PostgreSQL container `postgres` được quan sát healthy | Go code, schema, API, Compose game, TLS, telemetry, backup/CI chưa có artifact |
| Pilot | Đã chốt P0 deliverables và gate | Chưa có build pilot; mọi kênh pilot luôn internal-only và cần legal approval có scope/expiry |

## Definition of Done của pilot

Pilot chỉ xét các ID trong `status.yaml:pilot_deliverables`, không bắt buộc template/governance chuyển thành runtime `verified`. Exit nội bộ cần một arena đã audit collision, một build verified cho mỗi ba phái, portrait flow hoàn chỉnh, guest account, starter gear, Go verifier/replay, visual golden và restore drill. Mọi kênh pilot luôn internal-only và chỉ chạy khi legal owner cấp approval có phạm vi/thời hạn; public distribution là gate hậu pilot riêng.

## Acceptance

- [ ] Ledger có đủ document ID, path, owner, priority, dependency, evidence, acceptance reference và blocker.
- [ ] Mọi liên kết nội bộ trong shard map, traceability và status đều resolve được.
- [ ] Pilot chỉ chuyển sang exit review khi các deliverable P0 đạt gate tương ứng; tài liệu chưa tạo artifact vẫn giữ `design`, `not_started` hoặc `blocked`.

## Liên kết

- Ledger: [status.yaml](status.yaml)
- Chính sách ADR: [adr-policy](01-governance/adr-policy.md)
- Traceability: [traceability](01-governance/traceability.md)
- Gate phát hành: [acceptance-gates](09-quality/acceptance-gates.md)
- Queue reverse: [dhcd-reverse-queue](10-research/dhcd-reverse-queue.md)
