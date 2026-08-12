# Design

## Domain Model

`SkillDefinition` và level-derived combat fields phải phản ánh PC skill row + `gaibang.lua`/source logic. Stable identity là skill ID và level. Child, missile, collide và state skill giữ ID riêng, không gộp theo tên hiển thị.

## Application Flow

1. Resolve canonical PC source/config bằng đường dẫn và tool bắt buộc.
2. Map từng player skill Cái Bang sang source function/row và child relations.
3. Parse level curve bằng semantics PC hiện có; không thêm tuning table song song nếu parser/service chung đã đủ.
4. Runtime cast dùng definition đã resolve; shared root cause sửa tại factory/parser/runtime chung thay vì vá từng test.
5. Test so expected từ PC evidence, không copy expected từ Unity implementation.

## Interface Contract

Không đổi public API. User-visible combat behavior của skill Cái Bang phải khớp PC evidence.

## Data Model

Không persistence migration. `CombatCastReport` giữ impact skill ID + level theo projectile instance để nested `ByMissle` events không dùng nhầm parent definition.

Canonical static row slice được giữ dưới `StreamingAssets` làm audit artifact và exact duplicate `.bytes` dưới `Resources` cho synchronous Android-safe runtime load.

## UI / Platform Impact

Unity mobile gameplay. HUD projectile callback tiếp tục resolve nested collide-event missiles. Default Cái Bang deck dùng năm damage skill player canonical; migration chỉ thay deck rỗng hoặc hai generated legacy shapes, không ghi đè deck tùy chỉnh.

## Observability

Story validation ghi mapping skill → source evidence → test. Giá trị chưa chứng minh ghi blocker.

## Alternatives Considered

1. Vá từng skill theo cảm nhận runtime: loại bỏ, không có provenance.
2. Giữ tuning hard-code hiện tại và sửa test: loại bỏ nếu canonical parser/service có thể cung cấp cùng dữ liệu.
3. Sửa shared parser/factory/runtime một lần: ưu tiên khi root cause dùng chung.
