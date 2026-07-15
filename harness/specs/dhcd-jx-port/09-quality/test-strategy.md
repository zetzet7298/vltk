# Test Strategy

| Trường | Giá trị |
|---|---|
| Mục đích | Kiểm chứng parity, deterministic và readiness theo layer |
| Trạng thái | `design` |
| Owner / reviewer | QA owner / technical lead |
| Cập nhật | 2026-07-15 |

## Matrix

| Layer | Test |
|---|---|
| Evidence/provenance | path/hash/UID/encoding/pack/decode manifest |
| Catalog | ID/name/skill/item/NPC/map referential integrity |
| Go | unit, property, SQL integration, API contract, replay verifier |
| Unity | EditMode, PlayMode, input/safe-area, pooling, scene/map |
| Cross-language | golden vectors, state/event/hash equality |
| Visual/audio | SPR action/direction/frame, VFX/WAV linkage, screenshots/golden |
| E2E | guest -> character -> run -> checkpoint -> reward -> inventory |
| Ops | compose, health, TLS rotation, backup restore, load |

## Bug classification

P0 data loss/security/reward forgery/crash/asset legal; P1 gameplay divergence/blocked input/visual wrong; P2 polish/content gap; P3 backlog.

Every failure links to evidence, build, config version and replay/trace where applicable.

## Acceptance

- [ ] Mỗi P0 requirement trong traceability có ít nhất một automated test hoặc golden.
- [ ] Negative security/replay, visual provenance và restore tests có artifact/hash.
- [ ] QA report nêu pass/fail, residual risk, owner và release gate.
