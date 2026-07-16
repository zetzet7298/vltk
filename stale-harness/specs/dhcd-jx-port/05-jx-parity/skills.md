# JX Skills Parity

| Trường | Giá trị |
|---|---|
| Mục đích | Audit toàn bộ skill tree và port base behavior/visual exact |
| Trạng thái | `provisional` |
| Owner / reviewer | JX skill owner / JX reviewer |
| Cập nhật | 2026-07-15 |

## Per-skill manifest

Mỗi skill cần `skill_id`, original name/path, row trong `skills.txt`, level/base formula, target mode, child skill, missile IDs, `SkillIcon`, `PreCastSpr`, `AnimFile*`, WAV, Hash_UID/pack/version, resolver/decode evidence và golden vector.

## Workflow

1. Đọc row từ canonical config, giữ encoding gốc.
2. Dùng `~/Projects/vltktool` resolve từng logical asset.
3. Decode SPR và kiểm tra action/direction/frame.
4. So sánh với Unity `PcSkill`/runtime; ghi gap, không sửa source JX.
5. Port smallest change; giữ PC ID/path/UID trong manifest.
6. Chạy C# và Go golden damage/status/child-event.

## Boundary với DHCD

JX quyết định skill identity/base behavior/visual. DHCD chỉ quyết định cách skill được đưa vào card/auto-cast nếu evidence đủ. Product design không được đổi JX formula mà không ADR.

## Coverage states

`verified` chỉ khi resource và behavior golden pass; `provisional` khi row có nhưng child/effect chưa resolve; `missing` khi candidate không tồn tại; `conflict` khi nhiều version chưa xác định winner.

## Acceptance

- [ ] Full catalog audit có row-level provenance và coverage state.
- [ ] Mỗi pilot skill pass C#/Go golden vector và visual/audio linkage.
- [ ] Missing/ambiguous resource fail closed và tạo blocker.
