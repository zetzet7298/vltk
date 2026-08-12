# Design

## Domain Model

Bộ specs dùng ba lớp truy vết:

1. `PC source evidence`: file/symbol/include/config hiện tồn tại và SHA-256.
2. `Target contract`: Unity DTO/interface, proto và server-runtime contract.
3. `Backend state`: application/API/game-loop wiring cùng executable tests.

Coverage unit nhỏ nhất là một source artifact được phân loại server-relevant,
client-only, third-party/tooling hoặc cần xác minh. Requirement behavior tham
chiếu một hay nhiều coverage unit và không được đánh dấu hoàn tất chỉ vì có
constant, docstring hoặc metadata cùng tên.

## Application Flow

Quy trình specs: inventory nguồn → phân loại domain/variant → trích behavior và
dependency → đối chiếu target contract → đối chiếu runtime/test backend → ghi
gap/priority/proof → chạy validator → audit revision drift và coverage orphan.

## Interface Contract

Specs không tự phát minh endpoint. Mọi REST/WSS/DTO/envelope được ghi từ route
backend thực sự auto-discover, OpenAPI hiện hành, `IGameBackend`/DTO Unity hoặc
proto target. Mâu thuẫn giữa các tầng được giữ thành gap/decision; không chọn theo
comment cũ.

## Data Model

CNPM pha 3 mô tả schema logic và schema PostgreSQL hiện hành/tương lai, ownership,
key/index/constraint, encoding/index base/time unit, transaction/outbox,
backup/restore/deletion. Chưa có bằng chứng thì dùng `[CẦN XÁC NHẬN]`, không bịa
default.

## UI / Platform Impact

Không có tài liệu thiết kế giao diện. Unity chỉ được xem xét ở boundary contract,
prediction/reconciliation và dữ liệu server-authoritative. Asset/SPR/PAK chỉ vào
phạm vi khi quyết định behavior/content server và phải tuân thủ provenance
`vltktool` nếu cần trích xuất.

## Observability

Manifest và validator phải báo: source count theo scope/domain/status, orphan
source, orphan requirement, provenance thiếu/hash drift, target/backend mapping
thiếu, gap không có priority/acceptance và placeholder chưa giải quyết.

## Alternatives Considered

1. Viết một spec tổng quát theo module backend: loại vì không chứng minh coverage PC.
2. Dùng specs cũ đang bị xóa trong mobile: loại vì không phải current-state authority và có thể mô tả stack Go cũ.
3. Dùng inventory máy đọc được kết hợp tài liệu domain: chọn vì vừa audit được toàn bộ file vừa giữ behavior dễ đọc.

