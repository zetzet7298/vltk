# Exec Plan

## Goal

Đưa toàn bộ player-facing skill 10 phái đến evidence-backed parity với canonical jx-source, theo wave nhỏ, runnable và review độc lập.

## Risk

High risk: thay đổi combat public, existing behavior, source/config encoding, mobile packaging, weak proof hiện tại.

## Phases

1. [completed] Lập inventory canonical ↔ Unity ↔ tests cho toàn bộ player-facing skill của 10 phái: deterministic canonical-PC-first row-level union (PC progression + skillbook là membership evidence, Unity panel là observed display), exact-byte vltktool slice `PcAllFactionLearnedDisplaySkills.txt` + provenance, phân loại `shared` / `pc_learned_only` / `unity_display_only_unresolved`, summary counts và assert partitions/unions exactly; matrix schema `vltk.all-faction.membership-matrix/v1` pin canonical hashes và generated artifact hashes. Generator chỉ parse slice, không tự read/hash full encoded `skills.txt`, có `--check` fail-on-stale và independent per-faction recomputation.
2. [completed] `SKL-CB-PROOF-002`: pin canonical static oracle + hashed result artifact; residual runtime/platform gaps vẫn mở (display-scope only).
3. [completed] `SKL-TM-PROOF-001` / `SKL-TM-CATALOG-001`: reconcile PC learned-skill membership với Unity panel/root classification, pin membership-classification + 23-ID learned oracle + 32 relationship-target closure; runtime/platform residuals vẫn mở (learned-scope).
4. Tiếp theo theo risk-first ranking của inventory matrix: verify repo-local proof artifacts trước khi exclude completed Cái Bang/Đường Môn; winner sau verified exclusions là `SKL-KL-PROOF-001` (Côn Luân, gap 16).
5. Trích exact source slice bằng `vltktool` bắt buộc trong từng wave con.
6. Sửa minimum factory/model/runtime/UI surface trong story riêng khi canonical proof chỉ ra mismatch.
7. Thêm test fail-before/pass-after và verifier wave.
8. Herdr reviewer kiểm tra correctness + test oracle.
9. Record Harness evidence; lặp wave theo ranking.
10. Chạy Android smoke + PC runtime golden trước khi đóng epic.

## Stop Conditions

- Source server/client mâu thuẫn chưa resolve.
- Product behavior cần lựa chọn ngoài PC evidence.
- Asset/hash/encoded config chưa resolve bằng `vltktool`.
- Validation phải yếu đi hoặc test oracle lấy từ implementation.
