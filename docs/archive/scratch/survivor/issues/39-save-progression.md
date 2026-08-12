# 39 — Save/progression (round-trip + migration + corrupt-recovery)

**What to build:** Progress (unlock/best/meta-upgrade) + settings RIÊNG, v1 = PlayerPrefs + JsonUtility (shape `BaseClientData` + `PcSaveSlotService` reference), slot/versioning/migration, corrupt-recovery (reset + giữ backup). Mid-run save KHÔNG thuộc ticket này (defer).

**Blocked by:** None — can start immediately.

**Status:** verified

- [x] Save/load round-trip progress + settings không mất dữ liệu
- [x] Version cũ → migrate lên version mới; file hỏng → reset + backup giữ lại
- [x] EditMode self-check xanh: round-trip, migration, corrupt-recovery

**Verification (orchestrator):** EditMode 126/126 PASSED. TryParse wrapper catch JsonUtility ArgumentException.
