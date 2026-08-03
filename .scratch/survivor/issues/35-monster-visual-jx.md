# 35 — Monster visual JX (PcNpcVisual adapter)

**What to build:** Quái render bằng JX visual thật: `PcNpcVisual` + `NpcTemplateRegistry` qua `IActorVisual` adapter (read-only Sandbox), mapping MonsterDef → NPC res; chưa map → proxy màu (fail-closed). Hướng quay theo move, ẩn visual khi chết.

**Blocked by:** None — can start immediately.

**Status:** verified

- [x] Monster spawn → JX visual đúng template (≥5 loại quái khác nhau nhìn đúng)
- [x] Chưa map template → proxy màu, không crash
- [x] SetDirection theo hướng di chuyển; die → ẩn visual đúng lúc
- [x] PlayMode manual: quái thường + boss nhìn đúng loài

**Verification (orchestrator):** EditMode 84/84 PASSED. 7 resTypes (enemy005/023/036/051/083/205 + boss012) resolve staged SPR, fail-closed fallback proxy.
