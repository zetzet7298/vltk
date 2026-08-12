# Brief trao đổi với khách — 2 map Liên đấu

## Cách mô tả sản phẩm

Không nói “bán map VLTK gốc”. Nên nói:

> Bên mình cung cấp gói Unity map runtime/data cho 2 map Liên đấu đã port và chạy được: Hội trường liên đấu và Đấu trường liên đấu. Gói gồm geometry, sprite refs tối thiểu, catalog, server-region data tùy chọn, verifier và hướng dẫn tích hợp. Nếu dùng art/data gốc JX/VLTK trong sản phẩm thương mại, bên mua cần có quyền sử dụng IP gốc.

## Scope đề xuất

### Gói A — Data package

- 2 map data đã port.
- Catalog rút gọn cho map 396/397.
- SPR tối thiểu cho 2 map.
- Verifier package.
- Không gồm runtime renderer độc lập.

### Gói B — Unity integration

- Gói A.
- Tích hợp vào Unity project của khách.
- Scene demo load/chuyển 2 map.
- Camera/player bounds cơ bản.

### Gói C — Full runtime license

- Gói A + B.
- Cấp quyền dùng phần code renderer/parser/SPR decoder của mình theo hợp đồng riêng.
- Support/fix trong thời hạn.

## Điều cần hỏi khách trước khi báo giá

1. Họ cần **data only** hay cần **Unity demo chạy được**?
2. Unity version, render pipeline, target platform?
3. Họ đã có quyền dùng asset/source JX/VLTK chưa?
4. Cần visual-only hay cần cả collision/trap/server-region?
5. Cần source code runtime hay chỉ package tích hợp?
6. Cần support bao lâu?
7. Có được dùng tên VLTK/JX trong game của họ không? Nếu không có license, khuyên không dùng.

## Điều khoản nên có

- Bán license sử dụng, không chuyển ownership IP gốc.
- Không được resale/redistribute package standalone.
- Khách tự chịu trách nhiệm quyền IP gốc nếu dùng PC-derived assets.
- Giao hàng sau khi nhận cọc.
- Acceptance: map 396/397 load được, không lỗi runtime mới, verifier pass.
- Support giới hạn theo thời gian/scope.
