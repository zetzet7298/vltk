# Sandbox Runtime

## Fresh-session faction contract

Sau mỗi Stop/Play (một Sandbox runtime mới), player mặc định là **Đường Môn**.
Boot phải đồng bộ `PlayerProgression`, `GameplayLoop` combat faction, mana formula và
tên player thành Đường Môn. Việc chuyển phái trong runtime vẫn thay thế state này cho
đến khi session kết thúc; không có persistence profile giữa các Unity session trong
scope này.

## Skill panel to combat deck

Trong popup **Kỹ năng võ công**, khi chọn một skill chủ động đã học, phần chi tiết hiện
5 nút `Ô 1`–`Ô 5`. Mỗi nút gán skill vào đúng ô của combat deck đang active. Passive và
skill chưa học không có quyền gán; skill chưa học vẫn hiện dãy nút bị khóa cùng lý do
để người chơi biết phải nâng lên cấp 1. Hotbar là nơi kiểm tra cuối cùng trước khi thay
đổi.
