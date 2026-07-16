# DevHarness contracts

Các tệp trong thư mục này là contract normative cho server runtime mới. Thứ tự
ưu tiên khi có mâu thuẫn: Protobuf/OpenAPI/JSON Schema/SQL > đặc tả Markdown >
client C# legacy. Client legacy chỉ là bằng chứng migration, không phải nguồn
chân lý của giao thức mới.

| Contract | Phạm vi |
| --- | --- |
| `openapi/game.v1.yaml` | REST auth, bootstrap và character |
| `proto/game/v1/game.proto` | WSS binary realtime `game.v1`, tick 18 Hz |
| `sql/game.v1.sql` | PostgreSQL logical schema và invariant tenant/economy |
| `content/manifest.v1.schema.json` | Manifest content/config/Lua có provenance |
| `errors.md` | Error taxonomy và RFC 9457 |
| `idempotency.md` | Quy tắc replay cho REST, command và economy |
| `versioning.md` | Compatibility REST, Protobuf, DB và content |
| `legacy-mapping.md` | Ánh xạ C# REST/Mock hiện hữu sang DevHarness |

Mọi UUID truyền qua REST là chuỗi canonical lowercase; trong Protobuf là
`string` cùng định dạng để log và đối soát nhất quán. Thời gian lưu trữ dùng
UTC `timestamptz`; simulation dùng `tick` nguyên, không dùng clock client làm
nguồn chân lý.
