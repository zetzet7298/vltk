# ADR-003: Unity Mirror Và Go Canonical

| Trường | Giá trị |
|---|---|
| Mục đích | Khóa simulation/reward authority và replay contract |
| Trạng thái tài liệu | `design` |
| Trạng thái quyết định | `proposed` |
| Owner / approver | Server lead + client lead / product owner + technical reviewer |
| Evidence | `E-DHCD-SERVER-DECISION`; brief `E-PORT-BRIEF`; `E-UNITY` |
| Cập nhật | 2026-07-15 |

## Context và evidence

Backend target là contract mới bằng Go; không cần tương thích DHCD server. Online progression và reward không thể tin client-only, nhưng client vẫn cần simulation local đủ deterministic để điều khiển và hiển thị mượt.

## Options

| Option | Lợi ích | Rủi ro |
|---|---|---|
| Unity authoritative | Latency thấp | Reward forgery và replay yếu |
| Go chạy toàn bộ presentation simulation | Authority tập trung | Coupling/latency cao |
| Unity deterministic mirror + Go verifier | Prediction tốt, reward canonical | Cần cross-language vectors |

## Proposed decision

Unity C# mirror thu input và mô phỏng deterministic; Go cấp seed/config snapshot, xác minh sequence/hash/checkpoint và replay cuối run, rồi mới commit reward. Mismatch bị reject/quarantine. Replay đầy đủ và backward reader giữ tối thiểu 30 ngày.

## Consequences và rollback

Schema, RNG, rounding, tick, target tie-break và hash phải versioned. Mismatch block reward và release; rollback config chỉ áp dụng run mới/proposal pending, không đổi receipt đã commit.

## Trace

`OBJ-P0-03 -> REQ-P0-006/007/008 -> DOC-CLIENT-02, DOC-SRV-01/02/04/05 -> golden/replay/E2E gates`

## Acceptance

- [ ] Product owner, technical reviewer, server lead và client lead approve.
- [ ] Go/C# cùng pass golden vectors, replay negative tests và idempotent reward tests.
- [ ] Retention, quarantine/reprocess và config rollback invariants có artifact.
