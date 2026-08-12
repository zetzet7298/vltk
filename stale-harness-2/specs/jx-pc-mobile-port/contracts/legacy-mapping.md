# Legacy C# REST/Mock sang DevHarness

| Legacy | DevHarness v1 | Quy tắc |
| --- | --- | --- |
| `LoginAsync`, `/account/login` | `POST /auth/login` | `accName` -> `accountName`, thêm realm, trả token thật |
| `ListRolesAsync` | `GET /characters` | Account từ bearer; int ID -> UUID, giữ `legacyRoleId` migration |
| `GetPlayerStateAsync` | `GET /characters/{id}` | Gộp role/stats/position; money sang ledger/wallet |
| `ListMapsAsync` | signed content manifest | Catalog immutable theo release |
| `EnterMap/GetPosition` | select + WSS hello/snapshot | Position server authoritative |
| `Move/UpdatePosition` | WSS `MoveInput` | Gửi intent, reconcile 18 Hz |
| `ListItems/ListSkills` | bootstrap/snapshot projection | Template pin content release |
| `Learn/LevelUp` | durable WSS command | `command_id`, idempotency, economy atomic |
| `CastSkillCheck` | prediction local | Không là authority |
| `CastSkill/CalcDamage/StatusTick/PkCheck` | WSS/runtime nội bộ | Client không gửi resource/damage authoritative |
| `MockGameBackend` | deterministic fake adapter | Cùng generated contracts/clock seed |
| `MockNetworkClient` JSON echo | in-process Protobuf transport | Fault profile seed, không echo command thành state |
| C# opcode 1001..11003 | Protobuf `oneof` | Không mang opcode legacy sang wire v1 |

Coexistence: thêm Unity adapter sau facade, backfill UUID mapping và opening
balance ledger, shadow-read, canary theo realm, tắt legacy write rồi cutover.
Không dual-write economy từ client. Retirement owner/timeline phải nằm trong
migration plan.
