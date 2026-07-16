# ADR-006: Wave Ownership Và Co-op

| Trường | Giá trị |
|---|---|
| Mục đích | Chốt authority của wave và co-op trước khi triển khai |
| Trạng thái tài liệu | `design` |
| Trạng thái quyết định | `proposed` |
| Owner / approver | Gameplay owner / product owner + technical reviewer |
| Cập nhật | 2026-07-15 |

## Context

DHCD evidence cho thấy wave/spawn/choice có state per level/player, nhưng chưa chứng minh global pause hay co-op semantics. Co-op tương lai cần tránh peer host và bảo toàn replay.

## Options

| Option | Ưu | Nhược |
|---|---|---|
| Client host | đơn giản prototype | gian lận, reconnect khó, không phù hợp Go canonical |
| Server owns wave | canonical, replay rõ | cần Go tick/relay và tải server |
| Hybrid client schedule | giảm tải | dễ divergence nếu config/version lệch |

## Proposed decision

Chọn server-owned wave/config và Go room/relay/verify khi làm co-op; Unity chỉ mirror/present. Normal solo P0 có thể chạy cùng state model local nhưng phải gửi input/checkpoint để verify. Không claim DHCD parity cho pause cho tới khi reverse hoàn tất; nếu evidence vẫn inconclusive, product ADR riêng chỉ được xét sau reverse và phải ghi rule đó là to-be design.

## Consequences

Build P1 cần WSS room, reconnect window, sequence ack, snapshot/checkpoint và load test. Nếu latency/scale không đạt, giảm co-op scope chứ không chuyển sang peer host âm thầm.

## Acceptance

- [ ] Product owner và technical reviewer approve ADR trước co-op implementation.
- [ ] Wave owner, input ordering, reconnect window và checkpoint schema có contract test.
- [ ] Không có peer-host fallback khi server relay/verify chưa đạt.
