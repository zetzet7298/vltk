# 0009 Protocol Porting Strategy

Date: 2026-06-12

## Status

Accepted

## Context

Server PC giao tiếp với client qua **binary TCP** với hệ thống message riêng:
`script/protocol.lua` định nghĩa enum `ScriptProtocol.KE_SCRIPT_PROTOCOL`,
dispatch qua `requesttable.lua` (`RequestTable` → `DynamicExecute`), và tầng
gateway (bishop) dùng các cổng riêng (acc 5002, role 5001, client 5622,
gamesvr 5632). Client mobile bản port cần một hợp đồng giao tiếp với backend
FastAPI. Câu hỏi: giữ nguyên binary TCP parity hay expose REST/JSON?

## Decision

Tiếp cận **2 lớp, parity ở tầng behavior chứ không ở tầng byte**:

1. **Tầng behavior (bắt buộc parity 100%)**: mỗi message/handler của PC phải có
   một use-case tương đương ở backend với cùng input → cùng state change → cùng
   output. Đây là phần được kiểm chứng parity.
2. **Tầng transport (thích nghi cho mobile)**: backend expose REST/JSON dưới
   `/v1` cho từng use-case. Mỗi endpoint ghi rõ nó tương ứng message
   `emSCRIPT_PROTOCOL_*` nào của PC.

Lý do: client mobile là bản port (Unity, HTTP-friendly) chứ không phải binary
client PC nguyên bản; giữ byte-level TCP parity không mang lại giá trị mà còn
cản trở. Giá trị thật nằm ở parity hành vi gameplay.

Duy trì một **bảng ánh xạ protocol** (`docs/product/protocol-map.md`, tạo dần)
liệt kê: message PC → endpoint backend → story → trạng thái parity.

## Alternatives Considered

1. Giữ binary TCP nguyên bản: bị loại — phức tạp, không cần cho client port,
   khó test/quan sát.
2. Bỏ qua cấu trúc message PC, thiết kế API mới tự do: bị loại — vi phạm yêu cầu
   port 100% logic/behavior.

## Consequences

Positive:

- Backend hiện đại, dễ test/quan sát, hợp client mobile.
- Vẫn truy vết được từng endpoint về message PC gốc (provenance).

Tradeoffs:

- Cần bảng ánh xạ protocol và kỷ luật ghi nguồn cho mỗi endpoint.
- Nếu sau này cần client PC gốc kết nối, phải thêm adapter TCP (chưa nằm trong scope).

## Follow-Up

- Tạo `docs/product/protocol-map.md` và cập nhật theo từng domain.
