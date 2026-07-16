# ADR-005: Legal Và Evidence Gate

| Trường | Giá trị |
|---|---|
| Mục đích | Chặn asset/source chưa đủ provenance hoặc quyền sử dụng khỏi release |
| Trạng thái tài liệu | `design` |
| Trạng thái quyết định | `proposed` |
| Owner / approver | Legal owner + evidence owner / product owner + technical reviewer |
| Evidence | Brief `E-PORT-BRIEF`; `E-JX-ROOT`; `E-DHCD-CORPUS` |
| Cập nhật | 2026-07-15 |

## Context và evidence

Filesystem access không chứng minh quyền dùng/phân phối. Asset JX/DHCD hiện chưa có legal clearance, trong khi pilot phải dùng exact JX visual và giữ provenance đầy đủ.

## Options

| Option | Lợi ích | Rủi ro |
|---|---|---|
| Ship khi file tồn tại | Nhanh | Rủi ro pháp lý và không audit được |
| Chặn toàn bộ tới public clearance | An toàn cao | Không có internal pilot |
| Internal-only có scoped approval, public gate riêng | Cho phép kiểm thử có kiểm soát | Cần expiry/CI enforcement |

## Proposed decision

Mọi pilot channel luôn internal-only. Internal pilot chỉ chạy khi approval ghi owner, scope, expiry và cấm public distribution. Public distribution là gate hậu pilot riêng, chỉ xét khi asset/corpus clearance đã `cleared` và có release approval. Asset thiếu provenance/hash/winner/legal state phải fail closed.

## Consequences và rollback

Approval hết hạn hoặc scope đổi chuyển ledger về `blocked` và disable distribution flag. Không copy/generate asset để lách evidence gate; rollback build/content manifest về last cleared set.

## Trace

`OBJ-P0-02/04 -> REQ-P0-001/009/010 -> DOC-GOV-02, DOC-JX-05/08, DOC-RES-03 -> provenance/legal/CI/release gates`

## Acceptance

- [ ] Legal owner, evidence owner, product owner và technical reviewer approve.
- [ ] Approval expiry scan, asset provenance scan và internal-only channel test pass trong CI.
- [ ] Public distribution fail khi thiếu clearance hoặc post-pilot release approval.
