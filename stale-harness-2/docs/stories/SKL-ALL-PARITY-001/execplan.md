# Exec Plan

## Goal

Đưa toàn bộ player-facing skill 10 phái đến evidence-backed parity với canonical jx-pc, theo wave nhỏ, runnable và review độc lập.

## Risk

High risk: thay đổi combat public, existing behavior, source/config encoding, mobile packaging, weak proof hiện tại.

## Phases

1. [completed] Lập inventory canonical ↔ Unity ↔ tests cho toàn bộ player-facing skill của 10 phái: deterministic canonical-PC-first row-level union (PC progression + skillbook là membership evidence, Unity panel là observed display), exact-byte vltktool slice `PcAllFactionLearnedDisplaySkills.txt` + provenance, phân loại `shared` / `pc_learned_only` / `unity_display_only_unresolved`, summary counts và assert partitions/unions exactly; matrix schema `vltk.all-faction.membership-matrix/v1` pin canonical hashes và generated artifact hashes. Generator chỉ parse slice, không tự read/hash full encoded `skills.txt`, có `--check` fail-on-stale và independent per-faction recomputation.
2. [completed] `SKL-CB-PROOF-002`: pin canonical static oracle + hashed result artifact; residual runtime/platform gaps vẫn mở (display-scope only).
3. [completed] `SKL-TM-PROOF-001` / `SKL-TM-CATALOG-001`: reconcile PC learned-skill membership với Unity panel/root classification, pin membership-classification + 23-ID learned oracle + 32 relationship-target closure; runtime/platform residuals vẫn mở (learned-scope).
4. [completed] `SKL-KL-PROOF-001`: pin 24-ID canonical learned membership, 18-ID observed display và static oracle; generator exclude chỉ sau hash/schema/scope validation.
5. [completed] `SKL-S-PROOF-001`: pin 20-ID canonical learned membership, 17-ID observed display và exact 26-row vltktool slice; generator exclude chỉ sau hash/schema/scope validation.
6. [completed] `SKL-EM-PROOF-001`: pin 21-ID canonical learned membership, 15-ID observed display, exact 25-row union + 23-row relationship vltktool slices; `90` remains excluded as Côn Luân learned; generator exclude chỉ sau hash/schema/scope validation.
7. [completed] `SKL-TR-PROOF-001`: pin 20-ID canonical learned membership, 18-ID observed display, exact 25-row union + 15-row relationship vltktool slices; generator exclude chỉ sau hash/schema/scope validation.
8. [completed] `SKL-WD-PROOF-001`: pin 17-ID canonical learned membership, 16-ID observed display, exact 22-row union + 15-row relationship vltktool slices; generator exclude chỉ sau hash/schema/scope validation.
9. [completed] `SKL-WDU-PROOF-001`: pin 23-ID canonical learned membership, 16-ID observed display, exact 24-row union + 16-row relationship vltktool slices; generator exclude chỉ sau hash/schema/scope validation.
10. [completed] `SKL-TW-PROOF-001`: pin 23-ID canonical learned membership, 15-ID observed display, exact 23-row union + 16-row relationship vltktool slices; generator exclude chỉ sau hash/schema/scope validation.
11. [completed] `SKL-CY-PROOF-001`: pin 17-ID canonical learned membership, 13-ID observed display, exact 19-row union + 17-row relationship vltktool slices; `101,103` remain Unity-only unresolved; generator exclude only after hash/schema/scope validation.
12. Risk-first membership ranking now has no candidate: all 10 factions have verified exclusion scope. Next work is runtime/presentation/platform proof, not another membership story.
13. Trích exact source slice bằng `vltktool` bắt buộc trong từng wave con.
14. Sửa minimum factory/model/runtime/UI surface trong story riêng khi canonical proof chỉ ra mismatch.
15. Thêm test fail-before/pass-after và verifier wave.
16. Herdr reviewer kiểm tra correctness + test oracle.
17. Record Harness evidence; lặp wave theo runtime/presentation risk ranking.
18. [completed for the current foundation slice] `SKL-MOUNTED-CAST-001`: shared cast foundation cho male/female,
    foot/mounted, all weapon families và variants `1..30`, PC action clock và
    `KNpcRes` absolute frame driver. Pose/runtime proof không thay thế per-skill
    VFX/audio/buff golden. Shared lifecycle follow-up now fail-closes passive
    no-visual sub-effects and pins active/stationary/state-aura categories.
19. [in progress] Presentation inventory hiện sinh deterministic cho đủ 242 root rows,
    dùng `BaseSkill` để phân namespace 172 child links (`138` missile, `34` canonical
    child skill, `70` none), và full `--check` đã pass sau khi rebuild missile audit từ
    exact `slistcache.pak` payload. Năm false missing child `718,1083,1084,1087,1088`
    đã đóng. Toàn bộ 9.196 Unity field refs hiện fail-closed là `source_only` với
    `unity_ref_not_dereferenced`, không còn fabricated `verified`. Tiếp tục dereference
    thật từng shared owner và xử lý 17 state + 45 event gaps: CharAnim, speed/tick,
    start/fly/collide/vanish SPR, sound slots, attached buff/aura visuals và
    golden coverage; xếp hạng shared root gaps trước per-faction patches. PC
    authority is `skills.txt` + `missles.txt` + state-aura mapping + shared
    NpcRes tables; no inferred fallback rows.
20. Chạy Android smoke + live-PC framebuffer/audio golden trước khi đóng epic.

## Stop Conditions

- Source server/client mâu thuẫn chưa resolve.
- Product behavior cần lựa chọn ngoài PC evidence.
- Asset/hash/encoded config chưa resolve bằng `vltktool`.
- Validation phải yếu đi hoặc test oracle lấy từ implementation.
