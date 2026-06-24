# Kế hoạch: Xây dựng trang HTML tĩnh hiển thị thông tin & hình ảnh đối tượng VLTK PC và khắc phục lỗi mã hóa tiếng Việt

## 1. Mục tiêu
- Tạo trang HTML tĩnh hoàn chỉnh tại `/var/www/vltk-mobile/html/` hiển thị 100% đối tượng trong game PC (5,922 Vật phẩm & Trang bị + 1,695 NPC, Boss & Quái + 12 Nhân vật tuyển chọn).
- Sửa triệt để lỗi mã hóa tên tiếng Việt (Mojibake như `ẵðẽọìể01` hoặc `ãỹÄĐệđ...`).
- Việt hóa toàn bộ các tên tiếng Trung chưa được dịch trong file settings bằng phương pháp dịch Hán Việt tự động.
- Sửa lỗi hình ảnh Nhân vật ở Mục 3 bị thiếu tay chân đầu bằng cách ghép (composite) các phần body, head, hands thành một ảnh nhân vật hoàn chỉnh.
- Không sử dụng khung Harness (no harness framework) theo đúng yêu cầu của người dùng.

---

## 2. Phân tích lỗi & Phương pháp giải quyết

### A. Lỗi mã hóa và Dịch thuật (NPCs & Trang bị)
- **Nguyên nhân chính**: File `npcs.txt` và `goldequip.txt` chứa hỗn hợp encoding:
  - **Tiếng Việt TCVN3**: Các dòng đã được VNG dịch. Cần decode qua `cp1252` rồi ánh xạ qua bảng TCVN3 sang Unicode.
  - **Tiếng Trung GBK/GB18030**: Các dòng chưa được dịch (như `金箱子01` - Rương vàng 01, `金军运粮士兵` - Binh sĩ vận lương Kim). Do decode chung cả file bằng TCVN3 nên các ký tự Trung Quốc bị biến thành Mojibake.
- **Giải pháp tách biệt**:
  - Đối với **NPCs**: Đối chiếu song song từng dòng giữa client `npcs.txt` và file gốc tiếng Trung `dmjx06/settings/npcs.txt`. Nếu raw bytes của tên khớp nhau, nghĩa là dòng này **chưa dịch**, ta sẽ lấy tên tiếng Trung và dịch sang Hán Việt. Nếu khác nhau, nghĩa là **đã dịch**, ta giải mã theo TCVN3.
  - Đối với **Trang bị hoàng kim (`goldequip.txt`)**: Dùng danh sách signature Mojibake để phát hiện dòng lỗi, decode lại bằng GB18030 rồi dịch Hán Việt.

### B. Lỗi Nhân vật thiếu tay, chân, đầu (Mục 3)
- **Nguyên nhân**: JX PC sử dụng hệ thống avatar phân lớp (layered SPR character system). Một nhân vật đứng yên gồm nhiều file SPR xếp chồng lên nhau:
  - `BD` (Body - thân/giáp)
  - `HD` (Head - đầu)
  - `HR` (Hair - tóc, nếu có)
  - `LH` (Left Hand - tay trái)
  - `RH` (Right Hand - tay phải)
  Hiện tại, script `generate_static_gallery.py` chỉ trích xuất duy nhất phần body `BD` nên nhân vật bị thiếu các bộ phận còn lại.
- **Giải pháp ghép ảnh (Composite)**:
  1. Phân tích tên file body (ví dụ `ma_bd_001_st01.spr`), trích xuất giới tính (`ma`/`fm`) và mã variant (`001`).
  2. Tự động xác định đường dẫn logic của các phần còn lại: `ma_hd_001_st01.spr`, `ma_lh_001_st01.spr`, `ma_rh_001_st01.spr`.
  3. Đọc dữ liệu frame 0 của từng phần, giải mã tọa độ offset (`offsetX`, `offsetY`) nằm ở byte 4-7 trong frame blob.
  4. Tính toán bounding box chung (`min_x`, `max_x`, `min_y`, `max_y`) bao phủ toàn bộ các phần.
  5. Xếp chồng các lớp ảnh lên canvas chung theo thứ tự vẽ (draw order) chuẩn của VLTK:
     - Tóc (`hr` - nếu có) -> Thân (`bd`) -> Tay trái (`lh`) -> Tay phải (`rh`) -> Đầu (`hd` - trước nhất).
  6. Lưu ảnh composite hoàn chỉnh dưới dạng PNG thay thế cho ảnh body cũ.

---

## 3. Các file thay đổi
- `/var/www/vltk-mobile/html/generate_static_gallery.py` (Script Python sinh HTML)
- `/var/www/vltk-mobile/html/index.html` (Được sinh tự động từ script)
- `~/Projects/vltktool/hanviet_dict.json` (Đã được tạo và cập nhật chữ `話` -> `thoại`)

---

## 4. Kế hoạch triển khai chi tiết

### Bước 1: Khai báo các Hàm Hỗ trợ dịch thuật & ghép ảnh trong `generate_static_gallery.py`
- Tải từ điển Hán Việt `~/Projects/vltktool/hanviet_dict.json`.
- Thiết lập `word_map` cho các từ ghép thông dụng để dịch tự nhiên hơn.
- Xây dựng hàm `translate_cjk_to_vietnamese(text)`.
- Xây dựng hàm `extract_composite_character(body_logical_path, out_abs_path)`:
  - Đọc tọa độ offset và kích thước của các phần `bd`, `hd`, `lh`, `rh`, `hr`.
  - Tạo canvas trong suốt lớn bằng kích thước bounding box bao phủ.
  - Vẽ đè các bộ phận theo Z-order chuẩn lên canvas.
  - Lưu thành ảnh PNG hoàn chỉnh.

### Bước 2: Refactor phần parse `npcs.txt` (Dịch NPC/Boss/Quái)
- Đọc song song 2 file:
  - Client: `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/npcs.txt`
  - Gốc Trung Quốc: `/var/www/vltksource_new/pak_unpacked/dmjx06/settings/npcs.txt`
- Với mỗi dòng:
  - So sánh raw bytes: nếu trùng nhau -> Dịch tiếng Trung sang Hán Việt. Nếu khác nhau -> Giải mã TCVN3.

### Bước 3: Refactor phần parse `goldequip.txt`
- Đọc từng dòng của `goldequip.txt`.
- Nếu phát hiện signature Mojibake (dùng hàm `is_mojibake_tcvn3`), giải mã GB18030 và dịch Hán Việt.

### Bước 4: Chạy Script và Tạo lại trang HTML tĩnh
- Chạy `python3 /var/www/vltk-mobile/html/generate_static_gallery.py`.
- Lệnh này sẽ sinh ra ảnh composite cho 12 nhân vật, sửa toàn bộ encoding của trang bị/NPC, và tạo lại file `index.html`.

---

## 5. Kịch bản Kiểm thử & Xác nhận (Verification)
1. **Kiểm tra Số lượng**:
   - Đảm bảo trang chứa đủ 7,629 cards (5,922 Vật phẩm & Trang bị + 1,695 NPC/Boss/Quái + 12 Nhân vật).
2. **Kiểm tra Encoding và Dịch thuật**:
   - Xác nhận `Rương Vàng 01` (Row 1810), `Xe Vận Lương Tống Kim` (Row 1349) và các trang bị hoàng kim hiển thị đúng tiếng Việt không có lỗi Mojibake.
3. **Kiểm tra Nhân vật**:
   - Mở HTML (hoặc ảnh được sinh ra) và xác nhận 12 nhân vật ở Mục 3 hiển thị đầy đủ bộ phận đầu, thân, tay chân, không còn bị khuyết hoặc lệch vị trí.
