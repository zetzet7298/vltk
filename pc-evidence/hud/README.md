# Bảng Tra Cứu Tên File SPR Gốc Cho Giao Diện HUD (PC Client 6.0)

Tài liệu này lưu trữ danh sách các tập tin ảnh giao diện HUD dưới dạng `.spr` thực tế được giải nén từ game PC gốc Võ Lâm Truyền Kỳ 1 tại `/var/www/vltksource_new/`.

Do hệ thống đóng gói PAK sử dụng mã băm (hash UID) một chiều cho đường dẫn, các file này được giải nén thành tên file hệ lục phân (hex) trong các thư mục `unknown` của từng thư mục giải nén PAK tương ứng.

---

## 1. Thanh trạng thái phía trên (Top Status Bar)
*Nằm tại các thư mục giải nén: `update01/unknown/`, `spr/unknown/` hoặc `dmjx01/unknown/`*

| Tên chức năng (Tiếng Việt) | Đường dẫn file gốc trên PC | Tên file SPR trên Disk PC | Mô tả chi tiết |
| :--- | :--- | :--- | :--- |
| **Khung viền chính** | `\Spr\Ui3\主界面\新血条面板.spr` | **`973816f3.spr`** | Khung chứa máu, mana, thể lực và điểm kinh nghiệm |
| **Thanh Sinh Lực (HP)** | `\Spr\Ui3\主界面\生命条.spr` | **`74b299b9.spr`** | Thanh hiển thị máu đỏ |
| **Thanh Nội Lực (Mana)** | `\Spr\Ui3\主界面\内力条.spr` | **`b72be14b.spr`** | Thanh hiển thị mana xanh dương |
| **Thanh Thể Lực (Stamina)** | `\Spr\Ui3\主界面\体力条.spr` | **`83e13762.spr`** | Thanh hiển thị thể lực xanh lá cây |
| **Thanh Kinh Nghiệm (EXP)** | `\Spr\Ui3\主界面\经验条.spr` | **`f5d017dd.spr`** | Thanh hiển thị kinh nghiệm màu vàng dưới cùng |

---

## 2. Thanh phím tắt phía dưới (Bottom Shortcut Bar / Toolbar)
*Nằm tại các thư mục giải nén: `dmjx01/unknown/` hoặc `updatejx08/unknown/`*

| Tên chức năng (Tiếng Việt) | Đường dẫn file gốc trên PC | Tên file SPR trên Disk PC | Mô tả chi tiết / Phím tắt |
| :--- | :--- | :--- | :--- |
| **Phòng Chat** | `\spr\UI3\主界面\聊天室按钮.spr` | **`de6475b9.spr`** | Nút mở danh sách phòng chat |
| **Nhân Vật (F1)** | `\spr\UI3\主界面\人物属性按钮_0.spr` | **`cf92ecbe.spr`** | Nút xem thuộc tính và trang bị nhân vật |
| **Hành Trang (F2)** | `\spr\UI3\主界面\背包按钮.spr` | **`175edefc.spr`** | Nút mở hành lý chứa trang bị và vật phẩm |
| **Túi Phụ** | `\spr\UI3\主界面\子母袋按钮.spr` | **`c732baf9.spr`** | Nút mở rương chứa đồ mở rộng |
| **Võ Công (F3)** | `\spr\UI3\主界面\技能按钮.spr` | **`2317ae46.spr`** | Nút mở bảng kỹ năng / chiêu thức môn phái |
| **Nhiệm Vụ (F4/F8)** | `\spr\UI3\主界面\任务按钮.spr` | **`a3717b5e.spr`** | Nút mở bảng theo dõi nhiệm vụ |
| **Tổ Đội (F6)** | `\spr\UI3\主界面\队伍按钮.spr` | **`b3455277.spr`** | Nút quản lý nhóm / tổ đội hiện tại |
| **Bang Hội (F7)** | `\spr\UI3\主界面\帮会按钮.spr` | **`234770bb.spr`** | Nút mở bảng bang hội / môn phái |
| **Chạy bộ** | `\spr\UI3\主界面\跑步按钮.spr` | **`41d364a1.spr`** | Nút chuyển đổi đi bộ và chạy bộ |
| **Đả tọa** | `\spr\UI3\主界面\打坐按钮.spr` | **`82a5aa21.spr`** | Nút ngồi thiền phục hồi sinh lực / nội lực |
| **Cưỡi ngựa** | `\spr\UI3\主界面\骑马按钮.spr` | **`fc8a4f16.spr`** | Nút lên ngựa / xuống ngựa nhanh (phím M) |
| **Giao dịch** | `\spr\UI3\主界面\交易按钮.spr` | **`cc903517.spr`** | Nút đóng mở tính năng giao dịch với người chơi khác |
| **Trạng thái PK** | `\spr\UI3\主界面\PK按钮.spr` | **`42e22aac.spr`** | Nút chuyển đổi trạng thái đồ sát / luyện công |
| **Quay phim** | `\spr\UI3\主界面\摄像机按钮.spr` | **`9aca89f7.spr`** | Nút kích hoạt chế độ quay phim màn hình |

---

## 3. Bản đồ nhỏ (Minimap Radar)
*Nằm tại các thư mục giải nén: `update01/unknown/` hoặc `spr/unknown/`*

| Tên chức năng (Tiếng Việt) | Đường dẫn file gốc trên PC | Tên file SPR trên Disk PC | Mô tả chi tiết |
| :--- | :--- | :--- | :--- |
| **Nút Switch** | `\Spr\Ui3\小地图\小地图－切换按钮0.spr` | **`14f1acc9.spr`** | Nút bật tắt hiển thị / phóng to thu nhỏ minimap |
| **Bản đồ Sơn động** | `\Spr\Ui3\小地图\小地图－洞窟.spr` | **`2e66ad6f.spr`** | Nút hiển thị sơ đồ sơn động / địa đạo |
| **Bản đồ Thế giới** | `\Spr\Ui3\小地图\小地图－世界大地图按钮.spr` | **`c33f656f.spr`** | Nút mở bản đồ thế giới lớn |
| **Nút Cắm cờ** | `\Spr\Ui3\小地图\小地图－旗帜按钮.spr` | **`c9371d0d.spr`** | Nút bật chế độ cắm cờ đánh dấu địa điểm |
| **Icon Cờ đỏ** | `\Spr\Ui3\小地图\地图小旗帜.spr` | **`206e74a3.spr`** | Icon lá cờ đỏ hiển thị trực tiếp trên radar bản đồ |

---

## 4. Cửa sổ chat và Kênh Chat (Chat Window & Channels)
*Nằm tại các thư mục giải nén: `update01/unknown/`, `dmjx01/unknown/` hoặc `update03/unknown/`*

### 4.1. Khung viền và nút điều khiển
*   **Nút bật/tắt toàn bộ kênh chat (mở)**: `\Spr\Ui3\主界面\频道开与关a.spr` -> **`3b255f40.spr`**
*   **Nút bật/tắt toàn bộ kênh chat (tắt)**: `\Spr\Ui3\主界面\频道开与关b.spr` -> **`34fc44d5.spr`**
*   **Thanh đáy khung chat**: `\Spr\Ui3\聊天条\聊天条底部改.spr` -> **`bdf9af98.spr`**
*   **Thanh đỉnh khung chat**: `\Spr\Ui3\聊天条\聊天条顶部改.spr` -> **`8fa68495.spr`**
*   **Thanh giữa (Nền cuộn tin nhắn)**: `\Spr\Ui3\聊天条\聊天条中部改.spr` -> **`3483ec02.spr`**
*   **Bóng nút cửa sổ chat**: `\Spr\Ui3\聊天条\聊天条阴影按钮.spr` -> **`bcca4952.spr`**
*   **Nút kéo thanh trượt tin nhắn**: `\Spr\Ui3\好友qq\通用拖动条.spr` -> **`23fe2a10.spr`**
*   **Biểu tượng kênh chat "Tự nói" (Thường)**: `\Spr\Ui3\主界面\聊天频道图示－自己说.spr` -> **`50304af7.spr`**

### 4.2. Biểu tượng và nút chọn các kênh chat chuyên biệt

| Tên kênh chat | File chọn kênh (`MenuImage`) | File nhãn kênh (`TextImage`) |
| :--- | :--- | :--- |
| **Kênh Nói thầm / Mật** | **`3be3a09f.spr`** (`主界面按钮-密人频道选择`) | **`69fbc7e6.spr`** (`聊天频道图示－密人频道`) |
| **Kênh Bạn bè (MSN)** | **`7addeacc.spr`** (`主界面按钮-好友频道选择`) | **`2c66b90e.spr`** (`聊天频道图示－好友频道`) |
| **Kênh Thế giới** | **`59b0db0b.spr`** (`主界面按钮-世界频道选择`) | **`50d91112.spr`** (`聊天频道图示－世界频道`) |
| **Kênh Tổ đội** | **`8ff6d47a.spr`** (`主界面按钮-队伍频道选择`) | **`a9d1f2f2.spr`** (`聊天频道图示－队伍频道`) |
| **Kênh Môn phái** | **`4074febd.spr`** (`主界面按钮-门派频道选择`) | **`69f46c8c.spr`** (`聊天频道图示－门派频道`) |
| **Kênh Lân cận** | **`314af2aa.spr`** (`主界面按钮-附近频道选择`) | **`f434779f.spr`** (`聊天频道图示－附近频道`) |
| **Kênh Thành thị** | **`a8671666.spr`** (`主界面按钮-城市频道选择`) | **`b6d58e29.spr`** (`聊天频道图示－城市频道`) |
| **Kênh GM / Hệ thống** | **`b2a6f8a3.spr`** (`主界面按钮-GM频道选择`) | **`e277c438.spr`** (`聊天频道图示－GM频道`) |
| **Kênh Bang hội** | **`401cf1d6.spr`** (`帮会聊天频道选择`) | **`8340787f.spr`** (`聊天频道图示－帮派频道`) |
| **Kênh Liên minh** | **`9d6df5e0.spr`** (`主界面按钮-联盟频道选择`) | **`64f8476e.spr`** (`聊天频道图示－联盟频道`) |
| **Kênh Tống chiến trường** | **`58166d73.spr`** (`主界面按钮-宋方频道选择`) | **`8f8c13b9.spr`** (`聊天频道图示－宋方频道`) |
| **Kênh Kim chiến trường** | **`bcc87eec.spr`** (`主界面按钮-金方频道选择`) | **`efb03ac7.spr`** (`聊天频道图示－金方频道`) |

---

## 5. Tỉ lệ màn hình & Độ phân giải gốc trên PC (Screen Resolution & Aspect Ratio)

Game PC gốc (`vltksource_new`) được thiết kế chạy trên màn hình CRT cũ với tỉ lệ khung hình chuẩn là **4:3**. Game hỗ trợ hai độ phân giải chính được định nghĩa trong tệp cấu hình `config.ini` của Client:
*   **`800x600`** (Tỉ lệ 4:3) - `Resolution=0`
*   **`1024x768`** (Tỉ lệ 4:3) - `Resolution=1` (Đây là chế độ hiển thị mặc định của game)

Tất cả các tài nguyên ảnh giao diện `.spr` và hệ thống bố cục (layout) trong file INI đều được thiết kế dựa trên các kích thước gốc 4:3 này. Khi port sang nền tảng di động (tỉ lệ màn hình rộng hiện nay như 16:9, 18:9, 19.5:9), **không được** nhân tỷ lệ thô trực tiếp cho ảnh (gây méo). Thay vào đó, áp dụng neo góc màn hình (Anchor) theo cụm HUD (topbar neo Top-Center, minimap neo Top-Right, chat neo Bottom-Left, toolbar neo Bottom-Center), giữ nguyên tỉ lệ ảnh (Aspect Ratio) của từng `.spr`, và co giãn theo tỉ lệ màn hình thực tế của thiết bị.

> **Ghi chú sửa nguồn**: Cơ chế nội bộ `curSalx = frameSize.height / frameSize.width` từng bị ghi nhầm — nguồn thực là bản port C++ `jx-cocos` (`~/Projects/jx-cocos`), **không phải PC gốc**. Đã gỡ bỏ. Từ đây nguồn duy nhất là `/var/www/vltksource_new` (file INI trong `pak_unpacked/1024` + `config.ini`).

---

## 6. Cơ chế Quản lý & Nạp Giao diện (HUD Layout & INI Processing)

Game PC gốc xử lý việc dàn trang (layout) các thành phần giao diện động bằng cách nạp trực tiếp các cấu hình tọa độ và tài nguyên ảnh từ các tệp tin cấu hình dạng **INI**. Các tệp này được tổ chức theo độ phân giải màn hình tương ứng (ví dụ: thư mục `800` cho 800x600 và `1024` cho 1024x768).

*   **Tọa độ pixel tuyệt đối**: Các thẻ trong file INI khai báo cụ thể tọa độ góc trên bên trái (`Left`, `Top`), kích thước chiều rộng và chiều cao (`Width`, `Height`).
*   **Tham chiếu trực tiếp đường dẫn SPR**: Trong file INI, thuộc tính `Image=` trỏ trực tiếp đến file `.spr` gốc trong PAK (ví dụ: `Image=\Spr\Ui3\主界面\新血条面板.spr`).
*   **Liên kết lớp Engine (Class Binding)**: Mỗi phần tử chức năng được định danh qua thuộc tính `ClassType` để Engine C++ tự động liên kết với logic dữ liệu nhân vật như `Player_Life` (Máu), `Player_Mana` (Mana), `Player_Stamina` (Thể lực), `Player_Exp` (Kinh nghiệm), `Player_Level` (Cấp độ).

### 6.1. Toạ độ thật của Topbar (verify từ `ffb7d31b.ini` / `8da7027d.ini` trong `pak_unpacked/1024`)

Bảng `新血条面板` là **một dải ngang** `552×17` (đặt tại `Left=120, Top=0` trong khung 1024×768), **không có avatar / không có tên nhân vật** (ảnh chân dung nằm ở bảng F1 riêng). Bố cục là một hàng ngang duy nhất, toạ độ tính từ góc trên-trái của panel:

| Phần tử | `ClassType` | SPR fill | `Left` | `Top` | `Width` | `Height` (fill) | Màu chữ |
| :--- | :--- | :--- | ---: | ---: | ---: | ---: | :--- |
| Cấp độ (Level) | `Player_Level` | — (chỉ text) | 35 | 2 | 20 | 12 | `55,231,63` (xanh) |
| Thể lực (Stamina) | `Player_Stamina` | `体力条.spr` | 58 | 3 | 104 | 9 | `255,255,255` |
| Sinh lực (HP) | `Player_Life` | `生命条.spr` | 168 | 3 | 104 | 9 | `255,255,255` |
| Nội lực (MP) | `Player_Mana` | `内力条.spr` | 278 | 3 | 104 | 9 | `255,255,255` |
| Kinh nghiệm (EXP) | `Player_Exp` | `经验条.spr` | 388 | 3 | 104 | 9 | `255,255,255` |
| Hạng thế giới (WorldSort) | `Player_WorldSort` | — (chỉ text) | 522 | 2 | 28 | 12 | `55,231,63` (xanh) |

*   Text mỗi thanh đặt ngay dưới thanh fill (`Top=12` so với thanh), `Font=12`, căn giữa (`HAlign=1`), định dạng `hiện tại/tối đa`.
*   Tooltip (`Tip`) đã là tiếng Việt sẵn trong INI: `Sinh lực`, `Nội lực`, `Thể lực`, `kinh nghiệm`.
*   **Thứ tự PC chuẩn (trái→phải)**: `Cấp → Thể lực → Sinh lực → Nội lực → Kinh nghiệm → Hạng`. Đây là mốc bắt buộc khi port topbar mobile.

### 6.2. Toạ độ thật của Bottom Toolbar (verify từ `dc11ac12.ini` trong `pak_unpacked/1024`)

Bottom bar là **một ảnh nền SPR duy nhất** `快捷栏(800).spr` (`800×90`, `Left=0,Top=400` trong khung 1024×768, 3 dòng bị comment trong `[Main]`). Các nút được phủ lên bằng toạ độ INI tuyệt đối. **Không có avatar/name ở đây** (chân dung nằm ở bảng F1 riêng).

**Dòng toggle (hàng trên, 31×31 tròn — PC `Top=675`):**

| Nút | ClassType | SPR fill | `Left` | `Top` | `W×H` | Chức năng |
| :--- | :--- | :--- | ---: | ---: | :--- | :--- |
| Đả tọa | `Player_Sit` | `打坐按钮.spr` (`82a5aa21`) | 656 | 675 | 31×31 | Ngồi thiền |
| Chạy bộ | `Player_Run` | `跑步按钮.spr` (`41d364a1`) | 687 | 675 | 31×31 | Đi/chạy |
| Cưỡi ngựa | `Player_Horse` | `骑马按钮.spr` (`fc8a4f16`) | 719 | 675 | 31×31 | Lên/xuống ngựa (M) |
| Giao dịch | `Player_Exchange` | `交易按钮.spr` (`cc903517`) | 750 | 675 | 31×31 | Mở giao dịch |
| Quay phim | `Player_Recorder` | `摄像机按钮.spr` (`9aca89f7`) | 783 | 675 | 31×31 | Quay phim |
| PK | `Player_PK` | `PK按钮.spr` (`42e22aac`) | 815 | 675 | 31×31 | Chế độ PK |

**Dòng menu (hàng dưới, 28×28 vuông — PC `Top=728`):**

| Nút | ClassType | SPR fill | `Left` | `Top` | `W×H` | Chức năng |
| :--- | :--- | :--- | ---: | ---: | :--- | :--- |
| Nhân vật (F1) | `Player_Status` | `人物属性按钮_0.spr` (`cf92ecbe`) | 580 | 728 | 28×28 | Thuộc tính/trang bị |
| Hành trang (F2) | `Player_Items` | `背包按钮.spr` (`175edefc`) | 611 | 728 | 28×28 | Hành lý |
| Túi phụ | `Player_ItemEx` | `子母袋按钮.spr` (`c732baf9`) | 642 | 728 | 28×28 | Rương mở rộng |
| Võ công (F3) | `Player_Skills` | `技能按钮.spr` (`2317ae46`) | 673 | 728 | 28×28 | Bảng chiêu thức |
| Nhiệm vụ (F4) | `Player_Task` | `任务按钮.spr` (`a3717b5e`) | 704 | 728 | 28×28 | Theo dõi nhiệm vụ |
| Tổ đội (F6) | `Player_Team` | `队伍按钮.spr` (`b3455277`) | 766 | 728 | 28×28 | Quản lý nhóm |
| Bang hội (F7) | `Player_Faction` | `帮会按钮.spr` (`234770bb`) | 797 | 728 | 28×28 | Bang hội/môn phái |
| Phòng chat | `Player_ChatRoom` | `聊天室按钮.spr` (`de6475b9`) | 828 | 728 | 28×28 | Danh sách phòng chat |

> **Ghi chú port**: Trên mobile (16:9) các toggle (31px) + menu (28px) được gom thành **2 hàng** ở cụm phải (toggle trên, menu dưới). Hotkey 1–9 + skill T/P ở giữa/giữa-trái, Bảo Vật góc phải. SPR thật đã được decode (frame 0 = normal idle) bằng `extract_item_spr.py` và lưu tại `Assets/StreamingAssets/UI/HUD/Art/btn_*.png` + `Assets/UI/HUD/Art/PcButtons/`.

### 6.3. SPR khung Toolbar (快捷栏.spr) — khôi phục bằng hash resolver

Khung filigree (vòng tròn hai đầu + vương miện giữa + dải scrollwork) là **một SPR overlay trong suốt duy nhất**:

| Tên gốc PC | Hash UID | Đường dẫn disk | Kích thước |
| :--- | :--- | :--- | :--- |
| `快捷栏.spr` | `ebb69f9b` | `vl_update_27/pak_unpacked/updatejx08/unknown/ebb69f9b.spr` | 965×768 overlay (toolbar ở đáy, ~y628-715) |

*   **Cách khôi phục**: file INI `dc11ac12.ini` `[Main]` tham chiếu `快捷栏(800).spr` (bị comment). Dùng skill `jx-pc-resource-resolver`: normalize lowercase `\spr\ui3\主界面\快捷栏.spr` → encode GBK → JX Pack Hash UID algorithm → `ebb69f9b`. Decode bằng `extract_item_spr.py` → overlay 965×768 → crop vùng toolbar (content bbox) → `Assets/UI/HUD/Art/bottom_frame_pc.png` (863×91, aspect 9.48, 92% trong suốt). Vision confirm 10/10 sạch (cả hai end-cap + vương miện + dải scrollwork, không contamination).
*   **Lưu ý**: bảng `_labels.json` (73270 entries) chỉ map SPR **item**, không có SPR UI `主界面` → phải dùng hash algorithm, không tra cứu tên trực tiếp được. Screenshot `bottom_bar.png` KHÔNG dùng được (bị contaminant: game-world bleed, tooltip "Bạn Hữu"/"Bảo Vật", buff icons).

---

## 7. Cơ chế Cập nhật Dữ liệu & Đồ họa UI/UX (Data Update & Bar Scaling)

*   **Co giãn tỷ lệ theo trục X (Horizontal Scaling)**: Đối với các thanh trạng thái như Sinh lực, Nội lực, Thể lực và EXP, Engine tính toán tỷ lệ phần trăm hiện tại:
    $$\text{Tỷ lệ} = \frac{\text{Giá trị hiện tại}}{\text{Giá trị tối đa}}$$
    Sau đó, Engine thực hiện thay đổi tỉ lệ hiển thị (Scale X) của sprite tương ứng (`生命条.spr`, `内力条.spr`, `体力条.spr`, `经验条.spr`) từ trái qua phải tương ứng từ $0.0$ đến $1.0$.
*   **Hiển thị text đè (Text Overlay)**: Text hiển thị được vẽ đè lên trên thanh trạng thái, căn giữa màn hình (`HAlign=1`), kích thước phông chữ chuẩn `Font=12`, màu trắng (`Color=255,255,255`). Định dạng hiển thị là dạng chuỗi `Dữ liệu hiện tại/Tối đa` (ví dụ: `2500/2500`).
*   **Màu sắc văn bản đặc biệt**: Phần Cấp độ (`Level`) và Xếp hạng thế giới (`WorldSort`) sử dụng màu xanh lá cây sáng (`Color=55,231,63`) để tăng độ tương phản hiển thị trên nền gỗ sẫm của thanh trạng thái.

---

## 8. Cơ chế Bản đồ nhỏ (Minimap Radar & Coordinates)

*   **Khuôn hình giới hạn (Clipping Mask)**: Minimap sử dụng một Mask dạng Clipping Node với kích thước cố định là `128x128` pixel. Bản đồ nền địa hình gốc (được tải động dựa theo map ID hiện tại từ `maxMapPicPath.jpg`) sẽ được đặt bên trong và di chuyển nghịch hướng với tọa độ nhân vật chính.
*   **Công thức dịch chuyển bản đồ nền**:
    $$nRoleDisX = nRoleMpsX - maxMapRc.left \times 512$$
    $$nRoleDisY = nRoleMpsY - maxMapRc.top \times 1024$$
    Tọa độ này xác định vị trí tương đối của nhân vật trên tệp ảnh MAP lớn để căn chỉnh chính giữa tâm của ô cắt hiển thị Minimap.
*   **Vẽ điểm đối tượng (Radar Dot Rendering)**: Các đối tượng như đồng đội (chấm xanh lá), kẻ thù/quái (chấm đỏ), NPC (chấm vàng) được tính toán khoảng cách và vẽ trực tiếp đè lên khung cắt Clipping Node dưới dạng các chấm màu.
*   **Hiển thị tọa độ**: Tọa độ thực tế của nhân vật trong thế giới game được chia cho **8** để hiển thị lên nhãn tọa độ Minimap dạng `X/Y` (ví dụ: Tọa độ thực tế `nX=1600, nY=3200` sẽ hiển thị là `200/400`).

---

## 9. Cơ chế Kênh Chat & Tùy biến Khung chat (Chat Window & Cooldowns)

*   **Tự do điều chỉnh kích thước (Resizable)**: Khung chat cho phép người chơi co kéo chiều rộng và chiều cao bằng cách kéo các nút neo điều chỉnh kích thước (`SplitBtn`, `MoveImg`). Kích thước dòng hiển thị tin nhắn dao động từ 20 dòng và lưu trữ lịch sử tối đa lên đến 120 dòng tin nhắn (`MaxMsgCount=120`).
*   **Bật tắt bóng nền (Shadow Toggle)**: Một nút nhấn Checkbox (`ShadowBtn` liên kết tới `聊天条阴影按钮.spr`) cho phép người chơi bật/tắt hiển thị bóng đen phía sau tin nhắn để dễ nhìn chữ hơn trong các điều kiện môi trường sáng/tối khác nhau.
*   **Phân chia thông báo Hệ thống (SysRoom)**: Các thông tin hệ thống, thông báo nhiệm vụ, hệ thống bang hội được tách riêng biệt ra một khung phụ ở dưới cùng của khung chat (`SysRoom`), có nút cuộn riêng (`SysRoom_Up`, `SysRoom_Down`) giúp hạn chế tình trạng trôi tin nhắn đối thoại của người chơi.
*   **Thời gian chờ gửi tin (Send Cooldown / Cooldowns)**: Để chống spam tin nhắn phá hoại, mỗi kênh chat được áp đặt một thời gian chờ tối thiểu giữa 2 lần gửi (`SendMsgInterval` tính bằng mili-giây) và số lượng tin nhắn tối đa gửi cùng lúc (`SendMsgNum`):
    *   **Kênh Nói thầm / Mật**: 2,000 ms (Chờ 2 giây)
    *   **Kênh Tổ đội**: 800 ms (Chờ 0.8 giây)
    *   **Kênh Thế giới**: 60,000 ms (Chờ 60 giây / 1 phút)
    *   **Kênh Môn phái**: 10,000 ms (Chờ 10 giây)
    *   **Kênh Lân cận**: 2,000 ms (Chờ 2 giây)
    *   **Kênh Thành thị**: 20,000 ms (Chờ 20 giây)
    *   **Kênh Bang hội**: 10,000 ms (Chờ 10 giây)
