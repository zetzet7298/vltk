# Thuật ngữ miền

| Thuật ngữ | Định nghĩa chuẩn |
| --- | --- |
| PC canonical | Phiên bản runtime tiếng Việt active được chứng minh bởi manifest/package/source matching |
| SPR | Tài nguyên sprite PC gốc, gồm frame, anchor/pivot, timing và metadata |
| Parity | Logic/behavior đúng tuyệt đối theo oracle; visual skill đạt gate định lượng và review |
| HUD freeze | Không thay geometry vị trí/kích thước/cụm nút; được sửa binding, hitbox, state và Safe Area |
| Panel | Màn hình/popup mở từ HUD; implementation hiện tại stale và được làm mới cho mobile |
| Realm | Miền người chơi/persistence logic; mọi row gameplay có `realm_id` |
| Channel | Instance động của một map, có epoch và một owner simulation |
| AOI | Tập entity server gửi cho một người chơi theo vùng quan tâm |
| Intent | Ý định input client; server mới quyết định cast/move/interact hợp lệ |
| Semantic ACK | Xác nhận nghiệp vụ thành công sau validation/commit, khác transport ACK |
| Content bundle | Bundle immutable do compiler offline tạo, pin version/hash/provenance |
| DevHarness | Mock/catalog/training/fallback chỉ dùng dev/test, không được production fallback |
| Visual debt | Dùng đúng asset nhưng scale/crop/anchor chưa đạt golden, chưa được coi parity |
