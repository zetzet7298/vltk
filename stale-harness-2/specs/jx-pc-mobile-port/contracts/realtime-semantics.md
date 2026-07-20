# Realtime semantics game.v1

Một WSS binary message chứa đúng một Protobuf `ClientEnvelope` hoặc
`ServerEnvelope`, subprotocol `game.v1`; frame text bị từ chối. Server xử lý
`client_seq` tăng đơn điệu trong `session_epoch`, dedupe sequence đã thấy và yêu
cầu resync khi gap vượt window.

## ACK và kết quả nghiệp vụ

- `ack_server_seq` và `last_processed_client_seq` chỉ phục vụ transport,
  retransmission và cắt replay buffer. ACK không chứng minh command hợp lệ,
  combat đã áp dụng hay PostgreSQL đã commit.
- `CommandResult.outcome=COMMITTED` là business success cuối. `REJECTED` là lỗi
  cuối; `SCHEDULED` là pending và server phải phát kết quả cuối cùng cùng
  `command_id`.
- Economy chỉ hiển thị thành công khi cùng command nhận
  `EconomyEvent.transaction_state=POSTED`; event này chỉ phát sau commit ledger,
  wallet và outbox. Client không suy success từ balance prediction.

## Snapshot, delta và resume

`WorldSnapshot` thiết lập baseline đầy đủ. `WorldDelta` chỉ áp dụng khi
`baseline_tick`, `baseline_checksum` và sequence nối tiếp state cuối đã apply;
sau apply phải khớp `state_checksum`. Sai một điều kiện, client bỏ delta, gửi
`ResyncRequest` và chờ full snapshot, không tự vá state.

`ServerHello.resume_outcome` là kết quả authoritative: `DELTA_REPLAY` công bố
điểm bắt đầu replay; `FULL_SNAPSHOT` tạo baseline mới;
`GRACE_EXPIRED`/`BASELINE_MISMATCH`/`SESSION_REPLACED` giải thích fallback hoặc
rejection. Field `resumed` chỉ là compatibility hint, không điều khiển state. Grace chuẩn là 15 giây.

`ClientHello.accepted_content` và `ServerHello.active_content` phải khớp exact
content digest trước khi client coi preload/cast là hợp lệ. Mismatch dẫn tới
`RESUME_BASELINE_MISMATCH` hoặc content error ổn định; client không được fallback
filesystem hay tự đổi release.

Server không gửi delta tham chiếu baseline thuộc content release/session epoch
khác. Client không resume auto-combat, auto-path hoặc pending cast từ local cache;
chỉ state authoritative trong snapshot/event được khôi phục.

## Target, inventory và automation

`CastSkillInput.target_mode` dùng đúng matrix canonical trong domain combat.
`aim_x_q/aim_y_q` là integer đã quantize theo scale/rounding của content release
đang pin; server tự dựng candidate set và tie-break, không dùng tọa độ màn hình
hoặc thứ tự render. Field aim phải bằng zero ở mode không dùng aim.

Inventory move/swap/merge/sort/split là command riêng phát sinh từ chuỗi tap và
action sheet. `move` chỉ tới ô trống; destination có item phải dùng `swap` hoặc
`merge`; sort dùng policy content deterministic; split dùng ô trống có index nhỏ
nhất. Không có command drag/drop. Mỗi command nhận một `InventoryEvent` final;
reject không có delta và công bố revision/capacity hiện tại.

Use/drop/destroy/equip đều đi qua `InventoryCommand` với `expected_revision`;
`PlayerInput.use_item` chỉ là field deprecated, client mới không được gửi. Loot
do pickup command trả `InventoryEvent` loại `COMMAND_RESULT`; auto-loot do server
grant trả loại `LOOT_GRANTED`, `command_id` rỗng nhưng bắt buộc có `grant_id`,
`loot_entity_id`, delta và revision committed để dedupe/reconcile.

`AutomationEvent.state_code/stopped_reason` là authority UI. Leash breach chuyển
`RETURNING_TO_ANCHOR`, không phải terminal stop. Chỉ `STOPPED` mới có stopped
reason; full bag dùng `INVENTORY_FULL`, disconnect không tự resume automation.
Client chỉ gửi start/stop, preset key và expected world revision. Server bỏ qua
ba field anchor/leash deprecated, chụp anchor từ vị trí authoritative tại tick
accept và lấy hard leash từ signed content release/preset; giá trị hiệu lực chỉ
được công bố qua `AutomationEvent`.

Hủy reticle trước khi thả là local-only `CancelAim`, không phải wire command.
Hồi sinh dùng `ReviveState` revisioned và `ReviveCommand`; chỉ `CommandResult`
COMMITTED cùng `CombatEvent` REVIVE và snapshot authoritative mới đóng overlay.

Server có thể gửi `ActiveCombatResyncState` khi resync để phục hồi cast đang
recovery, missile đang fly và status còn hiệu lực. `CombatEvent.kind` lifecycle
mới cho recovery/fly/collide/vanish/status refresh/expire là semantic event;
transport ACK không thay thế được chúng. Encounter preload chỉ sẵn sàng sau
`EncounterPreloadAck` khớp `ContentDigest`.
