# Domain: Combat và chọn mục tiêu

## Định danh và phạm vi

- Domain ID: `DOM-CBT`; DRI: Gameplay Combat; reviewer: QA Parity/Backend; phase sâu P0.
- Sở hữu target policy, relation/range/LOS, cast lifecycle, tick, damage/state/missile, death/revive combat và replay.

## Bằng chứng as-is

- `EVID-0022`: `KPlayer.cpp:668-683` có tìm NPC/object được chọn; policy mobile hiện có cạnh tranh nên canonical target policy phải hợp nhất, không suy diễn.
- `EVID-0023`: `KNpcTemplate.h:31-57` có AI radius, resist, attack/defend/damage và skill list phía server.
- `EVID-0024`: `KSkills.cpp`, `KNpc.cpp`, `KMissle.cpp`, `KNpcAttribModify.cpp` là seam combat tĩnh; runtime order/rounding/RNG cần trace + golden.
- `EVID-0025`: `KPlayerDBFuns.cpp:623-680` load fight/state skill; không chứng minh parity cast.

## Invariant và state

- Production server-authoritative đúng 18 Hz; client gửi intent, không gửi damage/result.
- Gameplay dùng integer/fixed-point; RNG deterministic bảo toàn xác suất PC. Exact seed/stream/rounding `BLOCKED` đến runtime golden.
- Input mobile: tap attack/skill tự chọn target, giữ-kéo theo hướng/ground reticle, không tap actor nhỏ; target lock; queue chỉ giữ pending cast mới nhất.
- Auto-combat dùng cùng target/cast validator như manual và không vượt hard leash.
- State: `Idle -> TargetLocked -> Aiming/Pending -> Casting -> Recovery -> TargetLocked`; reject/supersede không tạo side effect.

### Candidate set và khóa sắp xếp deterministic

Server tạo candidate set từ snapshot đúng tick: cùng world instance/phase, còn sống, targetable, relation được skill cho phép, trong acquisition radius với manual hoặc trong hard leash với auto, và thỏa LOS nếu skill yêu cầu. Client highlight chỉ là prediction; `actor_id` client gửi phải được server kiểm lại. Không có candidate hợp lệ thì reject có reason, không tự cast vào actor render gần con trỏ.

Mọi phép đo dùng tọa độ fixed-point đã quantize trước khi so. Khóa canonical là tuple tăng dần; `angle_error_q` là sai số góc tuyệt đối với hướng aim hoặc hướng mặt actor, `distance_sq_q` là bình phương khoảng cách, `actor_id` là định danh ổn định trong world session. Không dùng HP, tên, thứ tự render, thứ tự dictionary, thời điểm packet đến hay khoảng cách màn hình làm tie-break.

| Target mode | Candidate/ưu tiên | Khóa sắp xếp tăng dần | Không có candidate |
| --- | --- | --- | --- |
| `LOCKED_QUICK_CAST` | Chỉ target lock nếu còn hợp lệ với skill | Không sắp xếp; đúng `locked_actor_id` | Giải lock bằng event reason rồi xử lý như `SMART_QUICK_CAST` chỉ khi intent cho phép fallback |
| `SMART_QUICK_CAST` | Target lock hợp lệ trước; nếu không, candidate trong cone ưu tiên của skill | `(lock_rank, outside_preferred_cone, angle_error_q, distance_sq_q, actor_id)` | Reject `NO_VALID_TARGET`; không yêu cầu tap actor nhỏ |
| `DIRECTION_AIM` | Candidate giao với shape/cone theo vector aim quantized | `(angle_error_q, distance_sq_q, actor_id)`; skill multi-target lấy N đầu theo limit canonical | Cast hướng/rỗng chỉ khi skill schema cho phép; nếu không reject |
| `GROUND_AIM` | Không auto-pick actor; clamp điểm thả theo range/nav/skill rule | Điểm fixed-point duy nhất sau clamp | Reject nếu ground target/terrain/LOS không hợp lệ |
| `AUTO_COMBAT` | Giữ lock hợp lệ; nếu không, hostile trong hard leash tính từ anchor | `(lock_rank, out_of_cast_range, distance_sq_from_actor_q, actor_id)` | Giữ anchor, không nới leash; chờ scan tick kế tiếp |
| `CONTEXT_INTERACT` | NPC/object có primary action trong interaction radius | `(distance_sq_q, actor_id)`; nếu nhiều action cùng entity dùng priority từ content, rồi action ID | Hiển thị “Không có mục tiêu tương tác” |

`lock_rank=0` chỉ cho target lock hợp lệ, target khác là `1`; `outside_preferred_cone/out_of_cast_range` là `0/1`. Mọi hằng acquisition radius, cone, multi-target limit và content action priority phải nằm trong content version có source evidence; thiếu thì `BLOCKED`, không dùng magic number client.

### Pending cast, approach và hủy

1. Tap/nhả aim tạo `CastIntent` với mode, aim quantized, optional target, input sequence và expected world revision.
2. Server chọn/validate target theo bảng. Nếu ngoài cast range nhưng trong acquisition/leash và skill cho phép approach, server phát trạng thái approach tới biên cast; target lock không đổi.
3. Client chỉ giữ intent local chưa gửi mới nhất. Sau khi nhận, server mỗi actor chỉ giữ một pending cast chưa bắt đầu; intent có sequence mới hơn supersede intent cũ, sequence bằng/thấp hơn là duplicate/out-of-order và không side effect.
4. `CancelAim` chỉ hủy reticle local trước khi intent được gửi. Sau khi server nhận intent, joystick manual, target invalid/dead, hard leash, stun/death hoặc policy interrupt mới hủy approach/pending với reason. Cast đã bắt đầu chỉ bị interrupt theo skill/state rule PC, không theo UI đóng panel.
5. Auto-combat gọi đúng selector/validator trên, không có policy target riêng. Manual cast là soft override. Leash breach hủy approach/target và return-to-anchor; toggle/death/disconnect/transfer/full-bag là terminal stop. Stop policy này không được nhập nhằng với auto-path.

## Contract

- Wire commands: `SelectTarget`, `ClearTarget`, `CastIntent`, `ReviveCommand`; `CancelAim` là local-only trước gửi.
- Events: `TargetChanged`, `CommandResult`, `CombatEvent`, `ReviveState`, `WorldSnapshot`; loot grant map chuẩn sang `InventoryEvent.event_kind=LOOT_GRANTED`, không phải combat event tự do.
- Envelope bắt buộc `command_id`, actor, client tick/sequence, target hoặc aim, skill/level, expected world revision; error registry có recovery classification.

## P0 Combat Parity Lab

- 5 training NPC được pin máy đọc tại `delivery/fixtures/training-npcs.p0.json`: hai Bao cát template 415, hai Cọc gỗ template 413, một Mộc nhân template 414, instance `1000..1004`, đội hình ngũ giác tâm `(53246,-52041)`, bán kính `300`, HP `999999`, map 53/DevHarness. Đây là baseline source Unity hiện hữu, không phải PC oracle.
- Resistance, hitbox, collision, relation, reset/death và PC visual oracle được ghi rõ `BLOCKED` trong fixture vì `TrainingNpcSpawner` không cung cấp evidence đó. Không được thay `null` bằng mặc định để làm test xanh.
- Case phủ shared, novice và toàn bộ catalog 10 phái theo `delivery/case-matrices/skill-parity-p0.json`; schema bắt buộc ở `skill-parity-p0.schema.json`.
- Logic event/state/damage phải khớp 100%; frame sequence SSIM từng case >=0,99.
- `TEST-CBT-001`: deterministic replay cùng input/tick/seed cho cùng output byte-normalized.
- `TEST-CBT-002`: fuzz target/range/LOS/cooldown/cost và duplicate/out-of-order command.
- `TEST-CBT-003`: live PC capture đang `BLOCKED`; static evidence không được nâng quá `FUNCTIONAL`.
