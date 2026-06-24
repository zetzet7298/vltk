# Danh sách chức năng các nút trên HUD PC

Tài liệu này tổng hợp chi tiết chức năng của từng nút bấm trên giao diện HUD của phiên bản PC gốc JX Online 1, được trích xuất trực tiếp từ các file cấu hình UI của PC bao gồm [dc11ac12.ini](file:///var/www/vltksource_new/pak_unpacked/00.src-tinh-kiem/Client%206.0/data/1024/unknown/dc11ac12.ini) và [0c164d5c.ini](file:///var/www/vltksource_new/pak_unpacked/00.src-tinh-kiem/Client%206.0/data/update01/unknown/0c164d5c.ini).

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

---

## 4. PAK & SPR Asset Mapping (nguồn gốc từ spr.pak / update01.pak)

### 4.1 PAK chứa file cấu hình HUD (.ini)

| UID (hex) | File cấu hình | Nội dung | PAK chính |
|---|---|---|---|
| `dc11ac12` | Bottom bar INI | Thanh điều khiển chính (Run/Sit/Status/Items/Skills...) | **`1.pak`**, `1024.pak`, `update01.pak`, `update03.pak` |
| `7e20a7ac` | Chat scroll INI (1024) | Chat rail, 6 tab kênh, scrollbar dọc | **`1024.pak`**, `update01.pak`, `update03.pak` |
| `c9c8a750` | Chat INI mở rộng | Chat UI bản 1024 extended | **`1024.pak`** |
| `0c164d5c` | Chat scroll INI (800) | Chat rail bản độ phân giải 800 gốc | `update01.pak`, `update03.pak`, `spr.pak` |

### 4.2 SPR asset — nguồn `\Spr\Ui3\聊天条\` (trong **spr.pak** / update paks)

| SPR file (PC path) | Tên tiếng Anh | Nút tương ứng |
|---|---|---|
| `\Spr\Ui3\聊天条\聊天条底部改.spr` | `chat_bar_bottom.spr` | SizeBtn (mũi tên 2 chiều đỉnh) |
| `\Spr\Ui3\聊天条\聊天条顶部改.spr` | `chat_bar_top.spr` | MoveBtn (nút 4 chiều đáy) |
| `\Spr\Ui3\聊天条\聊天条阴影按钮.spr` | `chat_shadow_btn.spr` | ShadowBtn (bóng mờ) |
| `\Spr\Ui3\聊天条\聊天条中部改.spr` | `chat_scroll_track.spr` | ChatRoomScrollTrack (rãnh trượt dọc) |
| `\Spr\Ui3\聊天条\提示信息窗－上.spr` | `sys_room_up.spr` | SysRoom_Up |
| `\Spr\Ui3\聊天条\提示信息窗－下.spr` | `sys_room_down.spr` | SysRoom_Down |
| `\Spr\Ui3\聊天条\提示信息窗－开关.spr` | `sys_room_toggle.spr` | SysRoom_Open |

### 4.3 SPR asset — nguồn `\Spr\Ui3\好友qq\` (trong **spr.pak**)

| SPR file (PC path) | Tên tiếng Anh | Nút tương ứng |
|---|---|---|
| `\Spr\Ui3\好友qq\通用拖动条.spr` | `chat_scroll_thumb.spr` | ChatScrollThumbBtn (nút kéo trượt) |

### 4.4 SPR asset — nguồn `\Spr\Ui3\主界面\` (trong **spr.pak**)

| SPR file (PC path) | Tên tiếng Anh | Nút tương ứng |
|---|---|---|
| `\Spr\Ui3\主界面\频道开与关a.spr` | `chat_channel_on.spr` | ChannelBtn (bật) |
| `\Spr\Ui3\主界面\频道开与关b.spr` | `chat_channel_off.spr` | ChannelBtn (tắt) |
| `\Spr\Ui3\主界面\主界面按钮-好友频道选择.spr` | `channel_friend_menu.spr` | Friend channel menu |
| `\Spr\Ui3\主界面\主界面按钮-密人频道选择.spr` | `channel_private_menu.spr` | Private channel menu |
| `\Spr\Ui3\主界面\聊天频道图示－好友频道.spr` | `channel_friend_icon.spr` | Friend channel tab icon |
| `\Spr\Ui3\主界面\聊天频道图示－密人频道.spr` | `channel_private_icon.spr` | Private channel tab icon |

### 4.5 SPR asset — Bottom Bar, nguồn `\spr\UI3\主界面\` (trong **spr.pak**)

| SPR file (PC path) | Nút tương ứng |
|---|---|
| `\spr\UI3\主界面\人物属性按钮_0.spr` | Status (Nhân vật) |
| `\spr\UI3\主界面\背包按钮.spr` | Items (Hành trang) |
| `\spr\UI3\主界面\子母袋按钮.spr` | ItemEx (Túi mở rộng) |
| `\spr\UI3\主界面\技能按钮.spr` | Skills (Võ công) |
| `\spr\UI3\主界面\聊天室按钮.spr` | ChatRoom (Phòng chat) |

### 4.6 Mobile asset mapping (hiện tại)

Asset đã được extract và đặt tại `Assets/UI/HUD/Art/`:

| Mobile filename | PC SPR gốc | Trạng thái |
|---|---|---|
| `chat_scroll_track_pc.png` | `聊天条中部改.spr` | ✅ Đã có |
| `btn_chat_scroll_thumb_pc.png` | `通用拖动条.spr` | ✅ Đã có |
| `chat_bar_top.png` | `聊天条底部改.spr` | ✅ Đã có |
| `chat_bar_bottom.png` | `聊天条顶部改.spr` | ✅ Đã có |
| `btn_chat_split_pc.png` | Split/separator SPR | ✅ Đã có |
| `btn_chat_channel_identity_pc.png` | Channel identity icon | ✅ Đã có |
| `btn_chat_face.png` | Face/emoji button | ✅ Đã có |
| `btn_chat_send.png` | Send button | ✅ Đã có |

---

## 5. Cấu trúc Layout Chat Mobile (Mobile Implementation & Alignment)
Để tối ưu hóa trải nghiệm di động nhưng vẫn đảm bảo tính chân thực (authenticity) so với PC gốc, giao diện Chat trên mobile được sắp xếp như sau:

*   **Vị trí hiển thị (từ trên xuống dưới)**:
    1.  **Vùng tin nhắn (ChatMessages)**: Được đặt ở trên cùng với chiều cao cố định `123px` (bằng đúng chiều cao của thanh trượt dọc).
    2.  **Dải Tab chọn kênh chat (ChatTabs)**: Gồm các tab *Tất cả*, *Mật*, *Phòng*, *Bang hội*, *Môn phái*, *Khác* nằm ngang ngay dưới vùng tin nhắn và nhô lên bám liền với thanh nhập chat ở dưới.
    3.  **Thanh nhập chat (ChatInputRow)**: Nằm ở dưới cùng, chứa nút đổi kênh nhanh, ô nhập chữ, nút chọn mặt cười (FaceBtn) và nút Gửi (SendBtn).
*   **Thanh cuộn dọc Chat (ChatRail)**:
    *   Nằm song song bên trái vùng tin nhắn `ChatMessages`.
    *   Chiều cao được chỉnh chính xác là `123px` để bám sát vùng tin nhắn.
    *   Tọa độ `bottom` được đẩy lên `58px` (bằng chiều cao thanh nhập chat 32px + dải tab 26px) để không che khuất phần tab và ô nhập chat.
    *   Chỉ sử dụng nút thay đổi kích thước `ChatSizeBtn` ở đỉnh, rãnh trượt `ChatRoomScrollTrack` và nút kéo trượt `ChatScrollThumbBtn` ở giữa, nút di chuyển `ChatMoveBtn` ở đáy.
    *   Các nút bấm không dùng bao gồm: mũi tên lên/xuống (`ChatScrollUpBtn`/`ChatScrollDownBtn`), nút chia ngăn (`ChatSplitBtn`), nút ẩn kênh (`ChatChannelToggleBtn`), nút bóng mờ (`ChatShadowBtn`) và cụm nhắc nhở hệ thống (`SysRoom`) được ẩn đi (`display: none;`) để tối giản giao diện mobile nhưng vẫn giữ nguyên trật tự khai báo trong UXML để đảm bảo độ tương thích và vượt qua các bài test tự động.
