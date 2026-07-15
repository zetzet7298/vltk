# SPR/VFX/WAV Manifest

| Trường | Giá trị |
|---|---|
| Mục đích | Theo dõi asset exact và linkage từ config tới runtime |
| Trạng thái | `not_started` |
| Owner / reviewer | Asset owner / JX reviewer |
| Cập nhật | 2026-07-15 |

## Manifest columns

`asset_id`, `kind`, `logical_path`, `candidate_absolute_paths`, `absolute_selected_path`, `pack_version`, `load_order_winner`, `hash_uid`, `encoding`, `normalized_path_bytes_hex`, `name_vi_cross_check`, `byte_count`, `sha256`, `resolver_evidence`, `decode_result`, `referencing_skill/npc/ui/event`, `unity_import`, `golden`, `legal_status`, `status`.

## Validation

- SPR: header/frame count, action, direction, anchor, alpha and frame ordering.
- VFX: source skill/missile/event, timing, layer/order, pooled cleanup.
- WAV: source config/event, sample rate/channel compatibility, mobile memory budget.
- Duplicate logical path: retain all candidates, select winner only with load-order evidence.

## Exit

No P0 skill/NPC/HUD can be marked `verified` until every referenced visual/audio entry is `verified` or intentionally absent with ADR.

## Acceptance

- [ ] Manifest rows có candidate/provenance/decode/linkage/legal fields.
- [ ] SPR/VFX/WAV validation artifacts và golden được lưu theo asset ID.
- [ ] Duplicate logical path chỉ chọn winner sau load-order evidence.
