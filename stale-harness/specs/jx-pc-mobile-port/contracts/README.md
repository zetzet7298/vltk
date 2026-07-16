# VLTK production normative contracts

Thứ tự ưu tiên khi mâu thuẫn: OpenAPI/Protobuf/JSON Schema/SQL > Markdown > C#
legacy. REST chỉ dùng cho auth/bootstrap/character; WSS Protobuf `game.v1` dùng
cho realtime 18 Hz. PostgreSQL mục tiêu là phiên bản 16.

| File | Phạm vi |
| --- | --- |
| `openapi/game.v1.yaml` | REST auth/bootstrap/character |
| `proto/game/v1/game.proto` | WSS binary realtime |
| `sql/game.v1.sql` | Schema/invariant PostgreSQL 16 |
| `content/manifest.v1.schema.json` | Content/config/Lua provenance |
| `errors.md`, `idempotency.md`, `versioning.md` | Semantics dùng chung |
| `realtime-semantics.md`, `protobuf-fuzz.md` | ACK/resume/delta và fuzz gate WSS |
| `legacy-mapping.md` | Cô lập C# REST/Mock trong DevHarness và map production adapter |

UUID là lowercase canonical; timestamp là UTC; simulation sắp thứ tự bằng
server tick, không bằng clock client.

`last_processed_client_seq`/`ack_server_seq` chỉ là transport ACK. Business
success cần `CommandResult.outcome=COMMITTED`; economy còn cần transaction
`POSTED` sau commit PostgreSQL.
