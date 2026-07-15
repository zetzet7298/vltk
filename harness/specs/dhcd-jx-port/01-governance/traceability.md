# Traceability Matrix

| Trường | Giá trị |
|---|---|
| Mục đích | Nối objective/requirement với contract, implementation, test và release signal |
| Trạng thái | `design` |
| Owner / reviewer | Product owner / QA owner |
| Cập nhật | 2026-07-15 |

`status.yaml` là ledger trạng thái; bảng này là catalog ID. `implementation`, `test` và `release signal` ghi `TBD`/`not_started` cho tới khi artifact tồn tại, không coi link dự kiến là bằng chứng.

## Objectives

| ID | Outcome | Priority |
|---|---|---|
| `OBJ-P0-01` | Portrait battle loop chơi được một tay | P0 |
| `OBJ-P0-02` | Visual/identity JX có provenance và không bịa asset | P0 |
| `OBJ-P0-03` | Run/reward deterministic, Go canonical | P0 |
| `OBJ-P0-04` | Pilot online vận hành/rollback được | P0 |
| `OBJ-P1-01` | Mở rộng content và portrait inventory qua pipeline kiểm soát | P1 |
| `OBJ-P1-02` | Co-op server relay/verify sau reverse gate | P1 |
| `OBJ-P2-01` | Content/mode mở rộng chỉ bật sau evidence gate | P2 |
| `OBJ-P3-01` | Competitive/social/faction update có security/data gate | P3 |

## P0 requirement map

| REQ | Owner | Objective | Document IDs | Design/ADR | Contract/implementation target | Test/acceptance | Release signal | Status |
|---|---|---|---|---|---|---|---|---|
| `REQ-P0-001` source hierarchy/provenance | evidence-owner | OBJ-P0-02 | DOC-GOV-01/02, DOC-JX-05/08 | ADR-001, ADR-005 | `01-governance`, `05-jx-parity` manifests | resolver/decode/hash audit | asset gate | design |
| `REQ-P0-002` một arena có Region_C/collision | jx-map-owner | OBJ-P0-01 | DOC-PROD-03, DOC-JX-01/06 | ADR-001 | `arena-candidate-audit`, map importer | map/collision golden | pilot arena selected | not_started |
| `REQ-P0-003` 3 phái + starter gear verified | content-owner | OBJ-P0-01/02 | DOC-PROD-02, DOC-JX-02/03/04/05 | ADR-001 | roster/item catalog | catalog + visual golden | faction gate | provisional |
| `REQ-P0-004` portrait adaptive/HUD/input/localization | ux-owner | OBJ-P0-01 | DOC-UX-01/02/04, DOC-CLIENT-03 | ADR-004 | Unity portrait shell/HUD/localization keys | safe-area/touch/PlayMode/missing-key scan | portrait gate | design |
| `REQ-P0-005` DHCD-style wave/card/reward loop | gameplay-owner | OBJ-P0-01 | DOC-GAME-01/02/03/04/05, DOC-JX-07 | ADR-001; ADR-006 chỉ cho P1 co-op | wave/deck/reward config + audited NPC roster | state/property/spawn/replay cases | gameplay gate | provisional |
| `REQ-P0-006` C# mirror và golden vectors | client-lead | OBJ-P0-03 | DOC-CLIENT-02, DOC-QUAL-01 | ADR-003 | Unity simulation package | cross-language vector suite | verifier gate | not_started |
| `REQ-P0-007` Go verify/checkpoint/replay + WSS base contract | server-lead | OBJ-P0-03 | DOC-SRV-01/02/03/04/05, DOC-QUAL-02 | ADR-003 | `server/verify`, OpenAPI, AsyncAPI/JSON Schema, replay schema | REST/WSS contract + forged/altered/duplicate cases | security/transport gate | not_started |
| `REQ-P0-008` guest/progression/inventory transaction | server-lead | OBJ-P0-03 | DOC-SRV-01/02/04, DOC-UX-03 | ADR-003 | REST + PostgreSQL schema | E2E/idempotency/conflict | data gate | not_started |
| `REQ-P0-009` compose/telemetry/backup/restore | ops-owner | OBJ-P0-04 | DOC-OPS-01/02/03/04/05, DOC-QUAL-03/04 | ADR-005 | ops artifacts | load/health/restore drill | ops gate | not_started |
| `REQ-P0-010` internal/public legal boundary | legal-owner | OBJ-P0-04 | DOC-RES-03, DOC-QUAL-04 | ADR-005 | clearance record + flags | approval/expiry scan | distribution gate | blocked |
| `REQ-P0-011` reuse-first + feature-flag migration | client-lead | OBJ-P0-01/03 | DOC-CLIENT-01/03/04 | ADR-002/003/004 | module inventory, adapters, shadow compare, flags và retirement criteria | compile/PlayMode/save/replay/migration/rollback tests | migration gate | not_started |

## P1 requirement map

| REQ | Owner | Objective | Document IDs | Design/ADR | Contract/implementation target | Test/acceptance | Release signal | Status |
|---|---|---|---|---|---|---|---|---|
| `REQ-P1-001` mở rộng skill/item đã audit | content-owner | OBJ-P1-01 | DOC-JX-02/03/05/08, DOC-PROD-02 | ADR-001/005 | versioned skill/item bundle | catalog integrity + visual/audio golden | content bundle promoted | not_started |
| `REQ-P1-002` inventory/equipment portrait hoàn chỉnh | ux-owner | OBJ-P1-01 | DOC-UX-03, DOC-SRV-02/04, DOC-CLIENT-03 | ADR-002/004 | portrait inventory + server transactions | PlayMode + API/idempotency/E2E | inventory flag promoted | not_started |
| `REQ-P1-003` co-op Go room/relay/verify | gameplay-owner | OBJ-P1-02 | DOC-GAME-06, DOC-SRV-03/05, DOC-CLIENT-02, DOC-RES-01/02 | ADR-003/006 | WSS room schemas, relay, reconnect, verifier | reverse evidence + room/reconnect/load vectors | co-op flag promoted | blocked |
| `REQ-P1-004` performance hardening + content release pipeline | ops-owner | OBJ-P1-01 | DOC-OPS-05, DOC-QUAL-03/04, DOC-JX-05/08 | ADR-002/005 | reproducible signed content bundle + canary/rollback | provenance/legal/golden/load/regression gates | content bundle promoted | not_started |

## P2/P3 requirement map

| REQ | Owner | Objective | Document IDs | Evidence/ADR gate | Test/acceptance | Release signal | Status |
|---|---|---|---|---|---|---|---|
| `REQ-P2-001` boss/escort/tower và mode đặc biệt | gameplay-owner | OBJ-P2-01 | DOC-PROD-03, DOC-JX-01/04, DOC-RES-01/02 | R-DHCD-009 + mode/product ADR | mode lifecycle/AI/map/reward golden | mode flag promoted | blocked |
| `REQ-P2-002` mount/pet | content-owner | OBJ-P2-01 | DOC-PROD-02, DOC-JX-02/03/05/08, DOC-RES-01/02 | R-DHCD-011 + JX resolver manifests + product ADR nếu deviation | actor/equipment/skill/visual/replay golden | mount/pet flag promoted | blocked |
| `REQ-P2-003` thêm arena và item content | content-owner | OBJ-P2-01 | DOC-PROD-03, DOC-JX-01/03/05/06/08 | JX candidate/source audit + ADR nếu deviation | collision/item/visual golden + content pipeline | arena/item bundle promoted | blocked |
| `REQ-P3-001` PvP/royal | gameplay-owner | OBJ-P3-01 | DOC-PROD-03, DOC-SRV-03/05, DOC-RES-01/02 | R-DHCD-008 + product/security/economy ADR | authority/match/reward/anti-cheat/load tests | competitive flag promoted | blocked |
| `REQ-P3-002` social/guild/leaderboard | product-owner | OBJ-P3-01 | DOC-SRV-01/02/03/04, DOC-RES-01/02 | R-DHCD-010 + product/data/security ADR | privacy/auth/idempotency/moderation tests | social flag promoted | blocked |
| `REQ-P3-003` faction updates | content-owner | OBJ-P3-01 | DOC-PROD-02, DOC-JX-02/03/05/08, DOC-RES-01/02 | R-DHCD-010 + JX catalog audit + product ADR | migration/catalog/visual/regression tests | faction bundle promoted | blocked |

## Verification rule

Mỗi requirement chỉ chuyển `verified` khi có artifact thật, test result, build/config hash, reviewer và timestamp. `not_started`/`provisional` được phép tồn tại trong spec; không dùng status để che thiếu implementation.

## Acceptance

- [ ] Mọi `pilot_deliverables` trace được tới ít nhất một `REQ-P0-*`.
- [ ] Mỗi REQ có owner, design/ADR, target artifact, test và release signal.
- [ ] Mọi ID trong `status.yaml:phase_requirements` có row đúng priority (P0/P1/P2/P3), evidence và gate tương ứng; P1-P3 phải có feature-flag gate.
- [ ] Artifact chưa tồn tại giữ `not_started`/`TBD`, không dùng link dự kiến làm proof.
