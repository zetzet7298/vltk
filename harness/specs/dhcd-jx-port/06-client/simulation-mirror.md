# Deterministic Simulation Mirror

| Trường | Giá trị |
|---|---|
| Mục đích | Định nghĩa C# mirror tương thích với Go verifier |
| Trạng thái | `not_started` |
| Owner / reviewer | Client lead / server reviewer |
| Cập nhật | 2026-07-15 |

## Boundary

Unity chạy presentation, input collection và deterministic C# simulation. Go là canonical cho seed/config/input validation/checkpoint/replay/reward. Client không tự cấp reward hoặc sửa canonical progression.

## Determinism contract

- Integer/fixed-point hoặc quy ước rounding chung; cấm float không kiểm soát trong rules.
- Stable serialization field order, endian và hash algorithm.
- Seed + config version + input sequence là replay header.
- Tick rate, collision ordering, target tie-break, RNG stream và event sequence phải có golden vectors.
- Unity visual interpolation không đi vào canonical hash.

## Cross-language artifacts

`golden/vectors/*.json`, schema version, expected state hash, event sequence, reward proposal. Go và C# chạy cùng vectors trong CI; mismatch block release.

## Failure

Hash mismatch -> stop reward, upload diagnostic replay, mark run `quarantined`, cho phép support reprocess bằng server. Không retry mù làm thay đổi sequence.

## Acceptance

- [ ] C# và Go pass cùng golden vectors trên pinned toolchain.
- [ ] Hash mismatch/quarantine/reprocess giữ nguyên sequence và không cấp duplicate reward.
- [ ] Replay header có config snapshot/version, map conversion và simulation version.
