# Client Reuse Inventory

| Trường | Giá trị |
|---|---|
| Mục đích | Lập inventory module hiện hữu trước khi thay thế |
| Trạng thái | `provisional` |
| Owner / reviewer | Client lead / technical lead |
| Cập nhật | 2026-07-15 |

## Classification

| Module/surface | Reuse candidate | Evidence cần chốt | Quyết định |
|---|---|---|---|
| `CityDefenceService` / parser | Chỉ parser/import wave data | `DateTimeOffset.UtcNow`, runtime state và reward grant hiện tại không deterministic/canonical | Chỉ reuse parser; thay runtime orchestration/reward bằng tick/input/mirror |
| `MapEnemyDatabase` | Có thể giữ generated roster lookup | Code hiện tại chứa curated template, fallback enemy ID/spawn và map registration | Cấm fallback/default/curated path trong pilot; chỉ lookup roster versioned đã audit |
| Existing Unity player/HUD | Rendering/input seam | JX visual/UI manifest, portrait golden | Reuse từng component |
| Existing harness SQLite | Test metadata | Không phải game persistence | Chỉ dùng test tooling |
| Unity MCP scripts | Editor automation | scene/script/test reproducibility | Reuse nếu không thay runtime contract |

## Inventory fields

`module_id`, exact path/line, revision/hash, owner, current behavior, dependencies, source authority, reuse/replace rationale, migration adapter, tests, feature flag, rollback, retirement criteria.

Không thay module chỉ vì “code cũ”; phải có ADR và migration test.

## Acceptance

- [ ] Mọi reuse decision có exact path/line, revision/hash và source authority.
- [ ] `CityDefenceService` runtime state/reward path và `MapEnemyDatabase` fallback path bị loại khỏi pilot.
- [ ] Adapter/shadow comparison/migration test được liên kết trước khi bật feature flag.
