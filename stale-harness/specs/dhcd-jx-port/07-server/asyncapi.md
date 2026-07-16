# AsyncAPI Và WSS Contract

| Trường | Giá trị |
|---|---|
| Mục đích | Contract event/room cho reconnect và co-op |
| Trạng thái | `not_started` |
| Owner / reviewer | Server lead / API reviewer |
| Cập nhật | 2026-07-15 |

## Phase boundary

Ledger status của `DOC-SRV-03` áp dụng cho P0 base slice: authenticated WSS transport, reconnect/resume và run-verification events có AsyncAPI/JSON Schema. Room/co-op channels là P1 extension, được trace riêng qua `REQ-P1-003` và `ADR-006`; chúng không được bật chỉ vì P0 base slice đã verified.

## Channels

| Channel | Phase | Mục đích |
|---|---|---|
| `session/{session_id}/status` | P0 | auth/session status và server notice |
| `run/{run_id}/reconnect` | P0 | resume từ sequence/checkpoint đã xác minh |
| `run/{run_id}/verification` | P0 | checkpoint/final verification status |
| `room/{room_id}/snapshot` | P1 | co-op snapshot |
| `room/{room_id}/input_ack` | P1 | co-op input acknowledgement |
| `room/{room_id}/wave_event` | P1 | server-owned wave event |
| `room/{room_id}/choice_event` | P1 | choice ownership/event |
| `room/{room_id}/reconnect` | P1 | room resume |

P0 schema IDs: `SessionStatusV1`, `RunReconnectV1`, `VerificationV1`. P1 schema IDs: `RoomSnapshotV1`, `InputAckV1`, `WaveEventV1`, `ChoiceEventV1`, `RoomReconnectV1`.

Common envelope của mọi event gồm `schema_id`, `schema_version`, `session_id`, `sequence`, `server_tick`, `trace_id`, `payload` và signature/hash nếu cần. Scope identifiers không dùng một rule mơ hồ:

- session channel: bắt buộc `session_id`, cấm `run_id`/`room_id`;
- run channel: bắt buộc `session_id` + `run_id`, cấm `room_id`;
- room channel: bắt buộc `session_id` + `room_id`; `run_id` chỉ hiện diện khi room đã bind vào run và schema ID khai báo field đó.

Run schemas phải trace tới `runs`, `run_checkpoints`, `run_inputs`, `quarantine_cases`; room schemas phải trace tới room/run persistence contract của P1. Client ack và resume từ sequence cuối; server phát snapshot/checkpoint phù hợp khi có gap.

## Rules

- Server-owned ordering; client không broadcast event đã tự tạo như canonical.
- Duplicate ack idempotent; out-of-order input bị reject hoặc queue theo contract.
- Disconnect không tự kết thúc run trong reconnect window.
- Choice ownership/global pause chỉ implement sau ADR-006 và reverse gate.

## Deliverables

P0: `asyncapi.yaml`, JSON Schema, generated Go validators, Unity contract tests (`WSS-*`) và authenticated reconnect/load test cho ba channel P0. P1 bổ sung room schemas/tests sau `ADR-006`. Hiện chưa có artifact.

## Acceptance

- [ ] Ba channel P0 có schema ID/version, persistence/replay mapping, auth và contract test.
- [ ] Sequence gap, duplicate event, reconnect và unauthorized event có negative tests; P0 WSS load/reconnect artifact pass trước internal pilot.
- [ ] Room channels giữ feature flag off và chỉ pass P1 gate sau `ADR-006`, room schema/load test và reconnect evidence.
