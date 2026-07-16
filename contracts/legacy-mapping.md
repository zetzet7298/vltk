# Legacy C# REST/Mock to DevHarness mapping

## Nguyên tắc

`RestGameBackend` hiện gọi FastAPI-style JSON envelope `{code,message,data}`;
`MockGameBackend` mô phỏng offline cùng interface; `MockNetworkClient` chỉ echo
JSON sau latency ngẫu nhiên. Không surface nào là network contract production
cho DevHarness. Adapter migration được đặt phía Unity, không làm server mới phụ
thuộc C# DTO hay integer database ID cũ.

## REST mapping

| Legacy `IGameBackend` / endpoint | DevHarness v1 | Chuyển đổi |
| --- | --- | --- |
| `LoginAsync`, `POST /v1/account/login` | `POST /v1/auth/login` | `accName` -> `accountName`; thêm `realmId`; response token thật thay session bằng tên account |
| `ListRolesAsync`, `GET /v1/role/by-account/{accName}` | `GET /v1/characters` | Không nhận account từ path; lấy account từ bearer; `id:int` -> `id:uuid`, giữ `legacyRoleId` tạm |
| `GetPlayerStateAsync`, `GET /v1/player/by-role/{id}` | `GET /v1/characters/{uuid}` | Gộp role/player/position; `money` chuyển sang wallet/ledger, không nằm trong stats |
| `ListMapsAsync`, `GET /v1/map` | content manifest/config | Map catalog immutable theo `contentReleaseId`, không query hot path |
| `EnterMapAsync` / `GetMapPositionAsync` | select + WSS hello/snapshot | Admission bằng REST, authoritative position bằng checkpoint/snapshot |
| `MoveAsync`, `UpdatePositionAsync` | WSS `InputBatch.MoveInput` | Client gửi intent, không gửi vị trí authoritative; server ACK/reconcile 18 Hz |
| `ListItemsAsync` | bootstrap/snapshot projection | Item instance là UUID; template id pin content release |
| `ListSkillsAsync` | bootstrap/snapshot projection | Skill durable từ DB; cooldown biểu diễn bằng server tick |
| `LearnSkillAsync`, `LevelUpSkillAsync` | WSS durable command [CẦN XÁC NHẬN opcode] | `commandId` + economy transaction; không dùng POST path chứa DB ID |
| `CastSkillCheckAsync` | prediction local từ signed content | Chỉ UX; server không tin mana/life/distance do client khai |
| `CastSkillAsync` | WSS `CastSkillInput` | Server đọc state, tick, target và content đã pin |
| `CalcDamageAsync`, `StatusTickAsync`, `CheckPkAsync` | runtime nội bộ | Không public API; combat simulation server-authoritative |
| `/health` | platform readiness/liveness | Không thuộc game OpenAPI; deployment contract riêng |

Legacy envelope được adapter giải nén sang success model hoặc RFC 9457. Không
ánh xạ string `code="200"` sang business success nếu HTTP status/error body mâu
thuẫn. `clientIp` không nhận từ JSON; gateway lấy từ trusted proxy chain.

## Mock/network mapping

| Legacy | DevHarness test double |
| --- | --- |
| `MockGameBackend` in-memory mutable state | Fake adapter thực thi cùng OpenAPI/Protobuf contract và deterministic clock |
| `MockNetworkClient.Send<T>` JSON echo | In-process WSS transport encode/decode generated Protobuf; không echo command như state |
| Random 50-200 ms latency | Fault profile có seed: latency, loss, duplicate, reorder và disconnect |
| C# `OpCodes` 1001..11003 | Không mang sang wire v1; Protobuf `oneof` là discriminator normative |
| `MessageRouter` lookup runtime Type | Generated Protobuf switch exhaustive; unknown field giữ compatibility |

Golden test phải chứng minh: cùng input/content/seed/checkpoint tạo cùng checksum;
duplicate `client_seq`/`command_id` không nhân đôi side effect; missing baseline
gây resync; content mismatch bị từ chối trước simulation.

## Coexistence

1. Thêm adapter Unity `DevHarnessBackend` sau facade hiện tại; giữ
   `RestGameBackend` chỉ để regression.
2. Dùng UUID mapping theo `legacy_role_id` và đối soát stats, item, skill,
   position; tiền migrate bằng opening-balance ledger transaction.
3. Shadow-read character/bootstrap, sau đó canary WSS theo realm/account allowlist.
4. Tắt legacy writes trước, drain request, checkpoint, kiểm tra ledger/outbox rồi
   chuyển source of truth. Không dual-write economy từ client.
5. Xóa Mock/REST cũ chỉ khi telemetry không còn consumer và rollback window đã
   hết. Mốc thời gian, owner và legacy FastAPI retirement: [CẦN XÁC NHẬN].
