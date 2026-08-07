# 38 — i18n VN/EN (SurvivorText bundle + switch + fallback)

**What to build:** `SurvivorText` VN/EN bundle (StreamingAssets, pattern `TextResourceService`), key namespace `survivor.<screen>.<key>` (skill name/desc + UI label), runtime switch không restart, fallback `vi` → hiện key. Unity Localization (đã cài) chỉ là upgrade path, không dùng v1.

**Blocked by:** None — can start immediately.

**Status:** verified

- [x] Bundle VN/EN đủ key: skill name/desc + UI label; thiếu key → fallback vi, thiếu cả → hiện raw key
- [x] Switch runtime không restart, UI refresh ngay (event notify)
- [x] EditMode self-check xanh: lookup + fallback chain 3 tầng

**Verification (orchestrator):** EditMode 126/126 PASSED. Test fix: en bundle thêm entry rỗng cho survivor.only.vi + gọi đúng key (test cũ gọi survivor.empty.vi sai key).
