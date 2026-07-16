# Protobuf fuzz contract game.v1

Fuzz target phải chạy trên decoder frame WSS, decoder `ClientEnvelope`, mọi
payload `oneof`, validator semantic và dispatcher, bằng corpus seed từ từng
message hợp lệ. Test không được gọi database/network thật; clock/RNG được inject.

| Lớp input | Biến thể bắt buộc | Kết quả bắt buộc |
| --- | --- | --- |
| Frame | rỗng, text frame, truncated length, > `max_frame_bytes`, nhiều envelope trong một frame | Từ chối bounded; không panic/OOM/dispatch partial |
| Protobuf wire | varint quá dài, length overflow, truncated field, duplicate scalar, unknown field/tag, invalid wire type | Unknown field được bỏ qua theo proto3; malformed trả protocol error và đóng `4422` khi cần |
| Envelope | thiếu payload, nhiều nhánh oneof trên wire, request/sequence cực dài/lớn, epoch 0/sai | Last-one-wins decode không được bypass semantic validator; không mutate state |
| Domain | axis/coordinate biên, target mode/aim không tương thích, quantity/amount overflow, inventory swap cùng item/merge khác stack/split khi đầy, enum unspecified/unknown, chuỗi UUID/UTF-8 lỗi, quest option không khớp action | Stable error; không integer overflow, SQL call hoặc side effect |
| Stateful | duplicate/reorder/gap sequence, reconnect epoch cũ, delta sai checksum/baseline, command duplicate | Dedupe/resync đúng contract; không double reward/economy |

Gate CI: libFuzzer/go fuzz chạy tối thiểu 60 giây/target ở premerge và corpus dài
ở nightly; zero crash, panic, race, timeout vượt budget hoặc memory growth không
bounded. Mọi regression lưu input tối thiểu hóa vào corpus, pin SHA-256 và thêm
negative test. Fuzzer phải chạy cả `-race` định kỳ và giới hạn frame trước khi
unmarshal để chống allocation bomb.
