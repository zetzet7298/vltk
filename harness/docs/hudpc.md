# Danh sách chức năng các nút trên HUD PC

Tài liệu này tổng hợp chi tiết chức năng của từng nút bấm trên giao diện HUD của phiên bản PC gốc JX Online 1, được trích xuất trực tiếp từ các file cấu hình UI của PC bao gồm [dc11ac12.ini](file:///var/www/vltksource_new/vl_update_27/pak_unpacked/vl_update_27/Client%206.0/data/1024/unknown/dc11ac12.ini) và [0c164d5c.ini](file:///var/www/vltksource_new/vl_update_27/pak_unpacked/vl_update_27/Client%206.0/data/update01/unknown/0c164d5c.ini).

---

## 1. Thanh điều khiển chính (Main Bottom Bar — cấu hình bởi `dc11ac12.ini`)
Đây là thanh công cụ dưới cùng của màn hình chứa các phím tắt tính năng chính của nhân vật:

*   **Status (Trạng thái/Nhân vật)**: Mở bảng thuộc tính nhân vật (phím tắt mặc định **F3**) để xem các chỉ số sinh lực, nội lực, kháng tính ngũ hành, danh vọng và tăng điểm tiềm năng (Sức mạnh, Thân pháp, Ngoại công, Nội công).
*   **Items (Hành trang)**: Mở túi đồ cá nhân chứa trang bị, dược phẩm và các vật phẩm mang theo người (phím tắt mặc định **F4**).
*   **ItemEx (Túi mở rộng/Hành lý)**: Mở rương chứa đồ mở rộng (hành lý mang thêm) để tăng số lượng ô chứa vật phẩm.
*   **Skills (Võ công)**: Mở bảng quản lý kỹ năng võ công môn phái (phím tắt mặc định **F5**) để tăng điểm kỹ năng võ công và thiết lập chiêu thức chiến đấu.
*   **Task (Nhiệm vụ)**: Mở bảng nhật ký danh sách các nhiệm vụ đang thực hiện (phím tắt mặc định **F6**).
*   **Team (Tổ đội)**: Mở giao diện quản lý và thiết lập đội ngũ (phím tắt mặc định **F7**) để tạo đội, mời đội, xin gia nhập hoặc thiết lập chế độ chia điểm/đồ.
*   **Faction (Bang hội/Môn phái)**: Mở bảng thông tin bang hội/môn phái (phím tắt mặc định **F8**) để xem danh sách thành viên bang, cống hiến, lãnh địa.
*   **Run (Chạy/Đi bộ)**: Chuyển đổi trạng thái di chuyển giữa chạy nhanh (tiêu hao điểm thể lực - Stamina) và đi bộ chậm.
*   **Sit (Ngồi thiền)**: Cho nhân vật ngồi thiền để tăng tốc độ tự động phục hồi Sinh lực và Nội lực.
*   **Horse (Lên/Xuống ngựa)**: Phím tắt cưỡi ngựa nhanh (phím tắt mặc định **M**).
*   **Exchange (Giao dịch)**: Bật chế độ sẵn sàng giao dịch vật phẩm/tiền tệ với người chơi khác.
*   **PK (Chiến đấu/Đồ sát)**: Chuyển đổi trạng thái chiến đấu của nhân vật (chế độ Luyện công, Đồ sát, Tuyên chiến, Bang hội).
*   **Rec (Quay phim/Replay)**: Công cụ quay phim màn hình lưu lại video định dạng replay của hệ thống game.
*   **ChatRoom (Phòng chat)**: Mở giao diện tạo hoặc tham gia phòng tán gẫu riêng tư của nhóm người chơi.

---

## 2. Cụm thanh dọc Chat (Chat Scroll Rail — cấu hình bởi `0c164d5c.ini`)
Dải dọc nằm ở bên trái cụm chat để cuộn, di chuyển và tùy biến giao diện hiển thị tin nhắn:

*   **SizeBtn (Size — nút mũi tên 2 chiều ở đỉnh)**: Click để phóng to/thu nhỏ độ cao của khung hiển thị tin nhắn chat.
*   **ChatScrollUpBtn (Up — mũi tên lên)**: Cuộn tin nhắn chat lên phía trên dòng cũ hơn.
*   **ChatRoomScrollTrack (Thanh trượt dọc)**: Rãnh nền màu xanh rêu dọc để nút thumb di chuyển lên xuống.
*   **ChatScrollThumbBtn (Thumb — nút tròn xanh biển)**: Nút trượt để người chơi nhấp giữ và kéo cuộn nhanh nội dung tin nhắn.
*   **ChatScrollDownBtn (Down — mũi tên xuống)**: Cuộn tin nhắn chat xuống dưới dòng mới hơn.
*   **ChatSplitBtn (Split — nút chia ngăn)**: Click để phân chia màn hình chat thành các ngăn hiển thị riêng biệt.
*   **ChatChannelToggleBtn (Channel Switch)**: Click để bật/tắt dải phím chọn nhanh kênh chat ở trên thanh nhập.
*   **ChatShadowBtn (Shadow — nút bóng mờ)**: Bật/tắt lớp bóng mờ màu đen phía sau dòng chữ chat giúp người chơi dễ đọc chữ khi đi qua các vùng map sáng.
*   **ChatMoveBtn (Move — nút 4 chiều ở đáy)**: Nhấp giữ vào nút này để kéo thả và di chuyển toàn bộ cụm chat đi vị trí khác trên màn hình.

---

## 3. Vùng nhắc nhở hệ thống (System Reminder — cấu hình bởi `0c164d5c.ini`)
Vùng nằm ở phía dưới góc bên trái cụm rail chuyên hiển thị các nhắc nhở, cảnh báo của hệ thống (mặc định được cắt bớt trên phiên bản Mobile hiện tại):

*   **SysRoom_Open (Dấu chấm than đỏ)**: Nút bật hoặc tắt cửa sổ nhỏ chuyên hiển thị dòng nhắc hệ thống (như thông báo nạp thẻ, hệ thống bảo trì, cảnh báo...).
*   **SysRoom_Up (Mũi tên lên màu đen trắng)**: Cuộn ngược dòng nhắc nhở hệ thống cũ.
*   **SysRoom_Down (Mũi tên xuống màu đen trắng)**: Cuộn xuôi dòng nhắc nhở hệ thống mới.
