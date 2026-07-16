# ADR-002: Reuse-first Và Migration Gate

| Trường | Giá trị |
|---|---|
| Mục đích | Bảo toàn phần Unity dùng được và thay module qua gate có rollback |
| Trạng thái tài liệu | `design` |
| Trạng thái quyết định | `proposed` |
| Owner / approver | Client lead / product owner + technical reviewer |
| Evidence | `E-UNITY`; `/home/zet/.codex/attachments/19ad23fc-a9f3-4549-9e80-41e12e77df01/pasted-text-1.txt` |
| Cập nhật | 2026-07-15 |

## Context và evidence

Client hiện hữu là surface cần audit, không phải parity authority. Việc rewrite toàn bộ làm mất map/player/UI tooling; reuse không kiểm soát lại giữ runtime thời gian/reward/fallback không deterministic.

## Options

| Option | Lợi ích | Rủi ro |
|---|---|---|
| Rewrite toàn bộ | Boundary mới sạch | Mất asset/import/tooling và tăng blast radius |
| Reuse nguyên trạng | Nhanh ban đầu | Giữ behavior sai authority/determinism |
| Inventory rồi migrate theo slice | Đo được và rollback được | Cần shadow compare/feature flag |

## Proposed decision

Reuse-first theo inventory có exact path/revision/hash. Chỉ giữ module/component khi authority, dependency và test rõ; thay runtime qua adapter, shadow comparison, feature flag có owner/expiry, migration test và rollback. Không retire legacy path trước hai chu kỳ pilot không divergence.

## Consequences và rollback

`CityDefenceService` chỉ được reuse parser/import; `MapEnemyDatabase` không được dùng curated/default fallback trong pilot. Rollback bằng disable flag về last verified slice, không rollback schema destructive.

## Trace

`OBJ-P0-01/04 -> REQ-P0-004/006/009 -> DOC-CLIENT-01/03/04 -> migration tests -> rollout signal`

## Acceptance

- [ ] Client lead, product owner và technical reviewer approve.
- [ ] Mọi reuse row có source authority, revision/hash, flag, test và retirement criteria.
- [ ] Shadow compare và rollback drill pass trước cutover.
