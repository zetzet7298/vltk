# Rewards Và Balance

| Trường | Giá trị |
|---|---|
| Mục đích | Đặt boundary economy/reward và tránh suy diễn số liệu DHCD |
| Trạng thái | `design` |
| Owner / reviewer | Economy owner / gameplay reviewer |
| Cập nhật | 2026-07-15 |

## Evidence

DHCD evidence map ghi drop item/XP manager và actor death reaction; không có đủ bảng giá, exact drop weights, progression curve hay server authority. Mọi số phải là versioned product data, không gắn nhãn recovered.

## Reward pipeline

```text
NPC death -> drop/XP events -> run ledger -> Go replay verify
-> reward proposal -> inventory transaction (idempotent) -> receipt
```

- XP/drop event có source sequence, table version và owner.
- Reward proposal pending hết hạn nếu replay mismatch; config snapshot/version của run là immutable và được giữ trong retention. Revoke/rollback chỉ ngăn run mới hoặc proposal pending, không thay đổi receipt đã commit.
- Inventory commit dùng optimistic version + idempotency key.
- Không cấp reward từ client-only `OnBattleEnd`.

## Balance knobs

`wave_count`, `spawn_rate`, `npc_stat_table`, `xp_curve`, `drop_table`, `card_offer`, `reroll_cost`, `starter_gear` đều phải có schema/version/owner. Chưa đặt default numbers trong spec này.

## Acceptance

- Property test: không âm currency/item; duplicate request không nhân đôi reward.
- Golden run kiểm tra XP/drop/reward receipt.
- Config rollback không được làm thay đổi run đã commit; test phải chứng minh receipt bất biến. Proposal pending/retry dùng đúng snapshot đã ghi hoặc expire theo policy.
- Telemetry theo dõi fail/quarantine và reward latency.
