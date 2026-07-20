# ADR-0007: Gate 0 content/combat contract addendum

- **Trạng thái:** Chấp nhận
- **Quyết định:** `game.v1` thương lượng exact content digest gồm release, manifest SHA-256, source snapshot và 242-row skill union; runtime skill policy bắt buộc `vltktool`, cấm filesystem fallback và không claim PC/Android parity khi evidence còn `BLOCKED`. Reconnect grace là 15 giây. Combat lifecycle bổ sung recovery, fly, collide, vanish, status refresh/expire và ACK preload encounter bằng additive tags.
- **Hệ quả:** Go/Unity/compiler có thể đọc field mới khi hỗ trợ; N/N-1 client bỏ qua field unknown, server vẫn giữ trường cũ. Không đổi tag hiện hữu và không gắn `PARITY_DONE` nếu thiếu runtime golden.
