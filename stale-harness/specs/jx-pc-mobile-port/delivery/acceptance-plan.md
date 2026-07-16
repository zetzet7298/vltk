# Kế hoạch nghiệm thu theo gate

## Gate tuần tự

| Gate | Điều kiện bắt buộc | Bằng chứng |
| --- | --- | --- |
| `G0 CNPM` | Bốn template đủ heading/cột, ID và trace graph hợp lệ | Validator authoring/premerge |
| `G1 Source` | Pin source/package/tool; census 100%, không filesystem fallback | Catalog + SHA-256 + evidence ledger |
| `G2 Contract` | OpenAPI/Proto/data/content schemas tương thích và có negative cases | Contract test, golden vectors |
| `G3 Parity` | Deterministic simulation; skill logic 100%; visual gate đạt | Replay hashes, PC/mobile captures |
| `G4 Vertical slice` | P1 E2E, transaction, reconnect, Lua sandbox, recovery | Test report cùng Git/content revision |
| `G5 Scale` | 1000 CCU, AOI/latency/checkpoint/device FPS đạt | Load/soak/device manifests |
| `G6 Release` | Migration, restore, rollback, version window, approvals | Drill report và release manifest |

Gate fail phải đóng promotion; không được biến thành warning để phát hành. Test file tồn tại không chứng minh test pass: evidence phải có command, timestamp, revision, environment, result và artifact hash.

## Dependency phase và điều kiện promote

Các shard P2-P4 được phép đặc tả/census sớm, nhưng không được promote runtime wave
vượt dependency. Chuỗi dependency canonical là `P0 -> P1 -> P2 -> P3 -> P4`;
`G6` chỉ mở sau khi mọi phase trước đạt exit gate.

| Phase | Entry gate bắt buộc | Exit gate bắt buộc | Phase kế tiếp bị chặn khi |
| --- | --- | --- | --- |
| `P0` | `G0` CNPM/trace READY; `G1` source và `G2` contract phải đủ evidence cho case được chạy | `G3`: đủ shared/novice/10 phái, logic 100%, SSIM từng case >=0,99 và reviewer | G1/G2/G3 còn BLOCKED; P1 chỉ được dựng seam/dev harness, không được nghiệm thu |
| `P1` | P0 đạt `G3`; app/Go/content/protocol version compatible | `G4`: vertical slice Ba Lăng 53, economy/reconnect/Lua/recovery/security E2E PASS | Bất kỳ P1 TEST hoặc golden bắt buộc chưa PASS |
| `P2` | P1 đạt `G4`; G1 resolve content world/quest/mount tương ứng | `G4` domain P2: map/channel/quest/team/mount persistence và rollback PASS | Catalog/domain P2 còn unresolved hoặc test P2 chưa PASS |
| `P3` | P2 exit PASS; economy durability của P1 vẫn xanh | `G4` domain P3: chat/moderation/trade/guild/pet và crash/idempotency PASS | Moderation, ledger/trade hoặc companion recovery chưa PASS |
| `P4` | P3 exit PASS; event/rule/reward content được ký | `G3` parity combat + `G4` event/reward recovery + `G5` scale/device PASS | Fairness/replay/reward/load còn BLOCKED/UNVERIFIED |
| Release | P0-P4 exit gate cùng revision | `G6`: migration/restore/rollback/approval PASS | Có gate không READY/COMPLETED hoặc artifact khác revision |

Việc sửa lỗi P0/P1 có quyền chặn hoặc mở lại mọi phase sau. Không dùng trạng thái
catalog `DISCOVERED` hay một test PASS đơn lẻ để suy ra phase đã đạt.

## Acceptance theo phase

### P0 Combat Parity Lab

- Năm training NPC chỉ xuất hiện với DevHarness profile.
- Catalog đóng recursive graph cho novice và 10 phái: root skill, level, child/event, missile, buff/debuff, cost, cooldown, target mode, damage/status, audio.
- Cùng seed và intent sequence tạo authoritative state hash duy nhất trên Go. C#
  chỉ là test adapter/presentation oracle đọc event/snapshot để đối chiếu hash,
  tuyệt đối không chứa authoritative gameplay rule hoặc production simulation thứ hai.
- Tap nút attack/skill để auto-acquire, hold-drag, cancel zone, target lock, one-pending-intent và joystick override có test.
- Mọi frame skill đạt `min SSIM >= 0.99`; mismatch logic bằng 0.

### P1 Ba Lăng vertical slice

- Auth/character flow, soft-delete 7 ngày, single active session và reconnect grace 30 giây chạy E2E.
- Map ID 53 được load trực tiếp, không alias 79; coordinate/collision/spawn/portal/minimap cùng bundle hash.
- Loot, wallet, inventory 60 slot, equip/stat/consumable/NPC buy-sell và level 1-200 persist qua restart.
- Item/economy commit trước semantic ACK; retry không nhân đôi.
- Core+Ba Lăng first-playable <=1.5GB, P1 install <=3GB.

### P2-P4

- Mỗi catalog entity có disposition, phase, owner và acceptance ID dù deep spec được mở theo wave.
- Không mở phase nếu domain dependency còn `BLOCKED` hoặc contradiction ảnh hưởng authority.
