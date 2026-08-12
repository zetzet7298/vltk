# Execution Plan: DHCD Feature Docs Suite

Date: 2026-07-15

## Status

Completed

## Outcome

Bộ docs mô tả **DHCD có tính năng gì** (góc sản phẩm/game-design), sống ở
`C:/Projects/dhcd/docs/features/`. Pure feature reference, không dính port
opinion. Phục vụ clone team hiểu trọn DHCD trước khi build survivor mode trong
vltk-mobile. 10 core system spec sâu + 8 meta catalog + INDEX + glossary +
2 ADR.

## Context

- Nguồn RE: `C:/Projects/dhcd/` (IL2CPP reconstructed corpus, evidence
  `docs/evidence/r-dhcd-*.md`, `docs/gameplay-evidence-map.md`).
- Nguồn gốc (WSL, read-only, primary truth): `//wsl.localhost/Ubuntu-24.04/var/www/dhcd/`
  — `服务端/extracted_server/` (server source + 652 config .txt/.bin),
  `客户端/` (client), `视频教程/视频教程.mp4` (playthrough), `localization_vi/`.
- RE escalation: `//wsl.localhost/Ubuntu-24.04/var/www/reverse-skill/` (skills
  router, `skills/MASTER-ROUTING.md`) — dùng khi cần RE nặng (decode config
  mã hóa FastXXTEA, native analysis, IL2CPP method malformed).
- Port spec hiện có: `docs/SURVIVOR_PLAN.md` — **STALE, mark superseded**.
- Quyết định scope: `C:/Projects/dhcd/docs/server-reverse-decision.md` —
  **OVERRIDE** (xem ADR #1).

## Scope

**In scope:**

- `dhcd/docs/features/` skeleton: `CONTEXT.md`, `INDEX.md`, `CONVENTIONS.md`,
  `adr/0001`, `adr/0002`, folders `core/` + `meta/`.
- 10 core system deep specs (core/01-10).
- 8 meta catalog docs (meta/00-overview + 7 con).
- Sample approve flow: `core/01-battle-lifecycle-modes.md` viết trước → owner
  approve → batch 17 còn lại.

**Out of scope:**

- Port verdict / vltk-mobile build opinion (để SURVIVOR_PLAN mới sau).
- Full RE của 652 config (chỉ catalog tồn tại cho meta, spec sâu cho core).
- Server protocol reverse (chỉ đọc config plaintext làm evidence value).
- Publish công khai (docs internal clone-purpose).

## Approach

Sequence (smallest coherent):

1. **Freeze decisions** (session này, 11 node đã lock — xem Decisions).
2. **Scaffold skeleton** Supervisor viết: CONTEXT.md seed + INDEX catalog +
   CONVENTIONS + ADR #1/#2 + folders. Không stub rỗng (lazy).
3. **Mark SURVIVOR_PLAN superseded** (append-only header).
4. **Delegate Lead (Paseo)** bounded autonomy:
   - Sample phase: 1 Peer viết `core/01-battle-lifecycle-modes.md` theo
     CONVENTIONS, chạy self-check probe + reconciliation matrix + video
     cross-check (core UX). Lead review tier standard. Supervisor relay →
     owner approve.
   - Batch phase (sau approve): Lead decompose 17 còn lại thành Peer tasks
     (cluster theo dependency: 02-03 player/skill, 04-05 wave/monster,
     06-07 combat/drops, 08-09 pause/UI, 10 onboarding; meta 8 song song).
     Review tier theo độ khó (core sâu = dual, meta catalog = standard).
5. **Per-doc gate**: self-check probe (A) + reconciliation matrix (B) mọi doc;
   +video cross-check (C) cho core UX (01, 03, 09). Meta = A+B.
6. **Accept**: Lead accept từng doc → Supervisor reconcile evidence → update
   INDEX progress. Owner approve final INDEX.

Update approach khi evidence đổi (ví dụ: video thiếu behavior, server config
contradict IL).

## Risks And Recovery

- **Rủi ro license/ownership server source.** Đây là source gốc private.
  Mitigation: docs internal, k publish, citation path-based (k embed raw data
  lớn). Recovery: nếu constraint đổi → revert ADR #1, fallback IL-only (nhiều
  claim `unresolved`).
- **Term pinyin歧义.** BaiZhan/BianShen/DanYao... dễ dịch sai. Mitigation:
  CONTEXT.md glossary map pinyin + Hán + nghĩa Vi, mỗi term 1 canonical.
  Peer phải tra CONTEXT trước khi dùng term.
- **652 config quá rộng, Peerover/under-spec.** Mitigation: CONVENTIONS rành
  mạch core=deep / meta=catalog; sample approve làm quality gate trước batch.
- **IL2CPP gap (method malformed).** Mitigation: RE escalation qua
  `/var/www/reverse-skill` (skills router). Nếu vẫn gap → tag `[? unresolved]`,
  ghi vào Known Gaps, không fabricate.
- **FastXXTEA config mã hóa (r-dhcd-001-build-key blocked).** Mitigation: dùng
  server `.txt` plaintext (đã extract) làm truth, k cần decode client bundle.

Recovery tổng: plan append-only, mỗi doc có evidence appendix audit-trail,
contradiction phát hiện → update doc + bump confidence tag.

## Progress

- [x] Grilling session — 11 decisions lock.
- [x] Scaffold `dhcd/docs/features/` skeleton.
- [x] Mark `SURVIVOR_PLAN.md` superseded.
- [x] Handoff Lead (Paseo): sample `core/01-battle-lifecycle-modes.md`.
- [x] Owner approve sample (blanket pre-auth — Supervisor self-approve, ACCEPT, quality vượt CONVENTIONS).
- [x] Batch 17 còn lại (Lead `6b4e934e`: 7 peer cluster C1-C5+M1/M2, DUAL review core 03/08/09 bắt 4 lỗi factual, 19 doc = 10 core + 9 meta incl 00-overview).
- [x] Owner approve final INDEX (self-approve per pre-auth — 19/19 ✅, 553V/77I/90U, git HEAD 6029f6a).
- [x] Move plan → `completed/`.

## Decisions

- **Q1 → A**: Pure feature catalog ở `dhcd/docs/features/`, không port opinion.
- **Q2 → A**: WSL server+client+video primary truth, IL2CPP cross-check.
  Override `server-reverse-decision.md`. → **ADR #1**.
- **Q3 → C**: Hybrid — INDEX flat catalog + per-system deep spec.
- **Q4 → C**: Core survivor deep (10) + meta catalog (8). → **ADR #2**.
- **Q5**: Taxonomy 10 core + 8 meta (danh sách trong INDEX.md).
- **Q6**: Vi prose + pinyin/EN identifier verbatim; root `CONTEXT.md` +
  per-system Terms section.
- **Q7**: Confidence 3 cấp `[✓ verified]` / `[~ inferred]` / `[? unresolved]`
  + inline ref `(src: ...)` + per-doc evidence appendix.
- **Q8 → C**: Delegate Lead+Peer (Paseo), sample approve trước, batch rest.
  Supervisor route+reconcile+relay, không implement.
- **Q9 → D**: Self-check probe + reconciliation matrix mọi doc; +video
  cross-check cho core UX (01 lifecycle, 03 skill, 09 UI).
- **Q10**: Layout `features/{CONTEXT,INDEX,CONVENTIONS}.md`, `adr/`, `core/01-10`,
  `meta/00+8`. Core numbered, meta unnumbered, kebab-case.
- **Q11**: ADR #1 (source hierarchy override) + #2 (scope); rest vào
  CONVENTIONS.
- **RE escalation**: Peer gặp gap RE nặng → `/var/www/reverse-skill`
  (skills/MASTER-ROUTING.md). Không tự decode/fabricate.
- **Owner blanket pre-approve (2026-07-15)**: Owner ủy quyền Supervisor
  tự relay-approve mọi gate (sample + final INDEX) và drive batch 17 →
  completion, KHÔNG hỏi. Escalate lại owner CHỈ khi: material risk
  (license/scope-creep vượt plan), contradiction k resolve được qua RE
  escalation, hoặc Lead ERROR/attention signal.

Promote lasting architecture decisions vào `dhcd/docs/features/adr/`.

## Validation

- **Focused proof**: mỗi doc có self-check probe (1 runnable check cho claim
  quan trọng nhất) — ponytail gate.
- **Integration proof**: reconciliation matrix (claim × {server, client, IL,
  video}) trong appendix mọi doc; `[✓ verified]` = ≥2 nguồn.
- **Reality check (core UX)**: 01/03/09 cross-check `视频教程.mp4` timestamp.
- **Audit**: INDEX progress + ADR trail + CONTEXT glossary complete.
- **Owner gate**: sample approve (sau 01) + final INDEX approve.

## Result

**DONE 2026-07-16.** Bộ DHCD feature docs hoàn chỉnh tại `C:/Projects/dhcd/docs/features/`:

- 19 doc: 10 core deep spec + meta/00-overview hub + 8 meta catalog.
- Claim stats: **553 verified / 77 inferred / 90 unresolved** (720 tag, 5956 dòng).
- Gate A (probe) + B (matrix ≥2 nguồn) mọi doc; Gate C (video frame-verified) core UX 01/03/09/10.
- DUAL review core 03/08/09 bắt 4 lỗi factual → fix.
- Git: dhcd init repo (Lead side-effect có lợi), HEAD `6029f6a`, clean.
- Agents: Lead `6b4e934e` self-archived, 7 peer đóng sạch (6 ACCEPT + 1 error tombstone, 0 gap). Lead `e28d37bf` (sample phase) archived.
- Notebook: 5 entry material/durable theo protocol.

**Known Gaps lớn nhất (risk clone survivor — flag cho implementation phase):**
1. **core/06 damage formula** (recon IL malformed) — CENTRAL cho combat, cần runtime probe/RE trước khi implement hit.
2. **core/05 NpcEntity body** (recon fail) — monster actor.
3. **core/03 randomCount source + reroll price**.
4. **core/08 pause sink identity**.
5. save runtime flow (database.tdr = schema only).

**Verdict mở rộng (Lead):** core 01/02/03/04/05/07/10 clone trực tiếp; core 06/08 cần RE thêm; meta = P3+ ngoài survivor scope.

**Follow-up:** (1) SURVIVOR_PLAN mới dựng sau khi feature docs hoàn (cũ đã superseded); (2) RE escalation core/06 damage formula khi đến Phase combat; (3) notebook entry 4+5 (scope-creep metadata) nên merge count.

**Limitations:** 90 unresolved (12.5%) — nội tại do IL malformed/FastXXTEA block, đã document Known Gaps. License: docs internal, citation path-based, k publish.
