# ADR-0006: PostgreSQL transaction trước semantic ACK

- **Trạng thái:** Chấp nhận
- **Quyết định:** Item, tiền, trade và receipt commit nguyên tử trước ACK; progression/quest/position checkpoint tối đa 5 giây.
- **Hệ quả:** Mọi command mutation có idempotency key; retry sau crash trả lại receipt cũ và không double-spend.
