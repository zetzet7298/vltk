# Error contract v1

REST lỗi dùng `application/problem+json` RFC 9457 với `type`, `title`, `status`,
stable `code`, `requestId`, `retryable`; client không parse `detail`. WSS `Error`
dùng cùng code. Một code không được đổi ý nghĩa hoặc ánh xạ retry giữa patch
release; code mới là thay đổi additive.

## REST và lỗi nền tảng

| HTTP | Code | Retry |
| --- | --- | --- |
| 400 | `MALFORMED_REQUEST` | Không |
| 401 | `AUTH_INVALID_CREDENTIALS`, `AUTH_TOKEN_EXPIRED` | Chỉ sau refresh |
| 403 | `AUTH_ACCOUNT_DISABLED`, `REALM_ACCESS_DENIED`, `CHARACTER_OWNERSHIP_DENIED` | Không |
| 404 | `CHARACTER_NOT_FOUND` | Không |
| 409 | `CHARACTER_NAME_TAKEN`, `CHARACTER_SLOT_TAKEN`, `VERSION_CONFLICT`, `IDEMPOTENCY_CONFLICT`, `IDEMPOTENCY_IN_PROGRESS`, `CONTENT_RELEASE_MISMATCH`, `SESSION_REPLACED` | Theo code |
| 422 | `VALIDATION_FAILED` | Không |
| 426 | `CLIENT_UPGRADE_REQUIRED` | Sau upgrade |
| 429 | `RATE_LIMITED` | Theo `Retry-After` |
| 503 | `REALM_UNAVAILABLE`, `DEPENDENCY_UNAVAILABLE` | Có, backoff |
| 500 | `INTERNAL` | Có giới hạn |

## WSS gameplay

| Nhóm | Stable code | Retry/phục hồi |
| --- | --- | --- |
| Protocol/session | `PROTOCOL_MALFORMED_FRAME`, `PROTOCOL_FRAME_TOO_LARGE`, `PROTOCOL_UNSUPPORTED_VERSION`, `SEQUENCE_GAP`, `SESSION_EPOCH_MISMATCH`, `RESUME_BASELINE_MISMATCH`, `RESUME_GRACE_EXPIRED`, `CONTENT_DIGEST_MISMATCH`, `ENCOUNTER_PRELOAD_UNAVAILABLE` | Resync/reconnect theo code; malformed/oversize không retry cùng frame; content mismatch phải bootstrap lại, không fallback filesystem |
| World/move | `WORLD_NOT_READY`, `MAP_NOT_FOUND`, `MOVE_BLOCKED`, `TRANSFER_IN_PROGRESS`, `TRANSFER_DESTINATION_UNAVAILABLE`, `TRANSFER_EXPIRED` | Snapshot lại hoặc retry destination khi `retryable=true` |
| Target/combat | `TARGET_REQUIRED`, `TARGET_INVALID`, `TARGET_NOT_VISIBLE`, `TARGET_OUT_OF_RANGE`, `SKILL_NOT_LEARNED`, `SKILL_ON_COOLDOWN`, `SKILL_RESOURCE_INSUFFICIENT`, `CAST_AIM_INVALID`, `CAST_INTERRUPTED`, `COMBAT_STATE_FORBIDS_ACTION` | Chỉ retry khi state/tick/target đã đổi; không spam cùng command |
| Inventory/loot | `INVENTORY_FULL`, `INVENTORY_REVISION_CONFLICT`, `INVENTORY_STACK_INCOMPATIBLE`, `INVENTORY_QUANTITY_INVALID`, `ITEM_NOT_FOUND`, `ITEM_SLOT_CONFLICT`, `ITEM_LOCKED`, `ITEM_REQUIREMENT_FAILED`, `ITEM_COMBAT_LOCKED`, `LOOT_NOT_FOUND`, `LOOT_OWNERSHIP_DENIED`, `LOOT_EXPIRED` | Refresh revision/snapshot; ownership denial không retry |
| Economy/trade | `ECONOMY_INSUFFICIENT_FUNDS`, `ECONOMY_PRICE_CHANGED`, `ECONOMY_TRANSACTION_IN_PROGRESS`, `ECONOMY_TRANSACTION_FAILED`, `TRADE_REVISION_CONFLICT`, `TRADE_NOT_LOCKED`, `TRADE_COUNTERPARTY_UNAVAILABLE`, `STALL_LISTING_UNAVAILABLE` | Cùng idempotency key chỉ khi code cho phép; refresh quote/revision trước command mới |
| NPC/quest | `NPC_NOT_FOUND`, `NPC_OUT_OF_RANGE`, `NPC_INTERACTION_EXPIRED`, `QUEST_NOT_AVAILABLE`, `QUEST_INVALID_TRANSITION`, `QUEST_OPTION_INVALID`, `QUEST_DIALOGUE_REVISION_CONFLICT`, `QUEST_REWARD_ALREADY_GRANTED`, `QUEST_REWARD_FAILED` | Mở lại dialogue/refresh quest; reward duplicate trả trạng thái grant hiện có |
| Automation/social | `AUTOMATION_LEASH_VIOLATION`, `AUTOMATION_STOPPED`, `PARTY_PERMISSION_DENIED`, `PARTY_MEMBERSHIP_CONFLICT`, `GUILD_PERMISSION_DENIED`, `CHAT_RATE_LIMITED`, `SOCIAL_TARGET_UNAVAILABLE` | Theo state và `retry_after_ms` |

`last_processed_client_seq` chỉ là transport ACK, không phải business success.
Business success chỉ đến từ `CommandResult.outcome=COMMITTED`; với economy còn bắt
buộc `EconomyEvent.transaction_state=POSTED` sau commit PostgreSQL. `SCHEDULED`
không phải success cuối và phải có kết quả final cùng `command_id`.

Close code: `4401` auth, `4403` access, `4409` session/content conflict, `4422`
protocol, `4503` unavailable. Lỗi command có thể phục hồi không đóng socket;
không trả stack, SQL, secret hoặc nội dung credential.
