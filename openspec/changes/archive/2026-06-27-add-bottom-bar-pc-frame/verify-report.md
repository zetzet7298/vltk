# Verify Report — add-bottom-bar-pc-frame (HUD-002)

**Status: PASS**
**Date:** 2026-06-27
**Implementation commits:**
- `c0385c10c` hud(SDD): bottom bar dùng khung filigree PC thật (快捷栏.spr)
- `8681f1c2b` hud: scale toolbar khung + icon cho vừa (design space 1280x720)
**Next recommended phase:** sync

---

## Executive Summary

Verified completed OpenSpec change `add-bottom-bar-pc-frame` against proposal/spec/design/tasks and the shipped implementation. The change replaces the flat placeholder bottom bar with a real PC filigree frame (`bottom_frame_pc.png`, recovered from PC SPR `快捷栏.spr` hash `ebb69f9b`), wired as a transparent `pickingMode: Ignore` background layer under all functional button containers. No blockers found. All 4 unchecked items are explicitly "Follow-up (out of this milestone)" and are not blockers.

## Structured Status / Action Context

- Active change: `add-bottom-bar-pc-frame` (parent-selected).
- Artifact store: OpenSpec files present under `openspec/changes/add-bottom-bar-pc-frame/`.
- Native SDD status is non-authoritative (`nextRecommended: resolve-via-engram`); readiness resolved from present OpenSpec artifacts.
- Implementation ownership proved by `git log` showing commits `c0385c10c` + `8681f1c2b`.
- Working tree: clean (no staged/uncommitted drift on shipped files).

## Artifact Coverage

Read and verified:
- `openspec/changes/add-bottom-bar-pc-frame/proposal.md`
- `openspec/changes/add-bottom-bar-pc-frame/spec.md` (5 REQ + 4 scenarios)
- `openspec/changes/add-bottom-bar-pc-frame/design.md` (ADR-001: real PC SPR recovered)
- `openspec/changes/add-bottom-bar-pc-frame/tasks.md` (Phase A–D)
- `Assets/UI/HUD/GameHud.uxml`
- `Assets/UI/HUD/GameHud.uss`
- `Assets/Scripts/UI/GameHudController.cs`
- `Assets/UI/HUD/Art/bottom_frame_pc.png` (exists, 84170 bytes)

## Requirement Verification Map

### REQ-1 — Ornate filigree frame visible — PASS

| Check | Evidence |
|---|---|
| Frame art wired into UXML | `GameHud.uxml`: `<ui:VisualElement name="BottomFrame" class="hud-bottom-frame"/>` present as **first child** of `BottomPanel` (z-bottom layer) |
| Frame art wired into USS | `GameHud.uss` `.hud-bottom-frame`: `background-image: url('project://database/Assets/UI/HUD/Art/bottom_frame_pc.png')` |
| Art asset exists | `Assets/UI/HUD/Art/bottom_frame_pc.png` (84170 bytes) + StreamingAssets copy |
| Old flat bg removed | `.hud-bottom-strip`: `background-color: transparent` (no `bottom_bar_bg.png`/`toolbar_bg.png` reference remains) |

### REQ-2 — No aspect-ratio distortion — PASS

| Check | Evidence |
|---|---|
| Scale mode | `.hud-bottom-frame`: `-unity-background-scale-mode: scale-to-fit` (NOT `stretch-to-fill`) |
| Aspect preserved | Frame box `width: 1241px × height: 131px` → aspect **9.474**; art `863×91` → aspect **9.473** — match confirmed |
| Comment evidence | USS comment: "863x91 art scaled ×1.438 -> 1241x131 (fills the strip width exactly)" |

### REQ-3 — Buttons remain functional & on top — PASS

| Check | Evidence |
|---|---|
| Frame ignores input | `.hud-bottom-frame`: `picking-mode: Ignore` |
| Button wiring intact | `GameHudController.cs` RegisterClick calls: BtnRun(331), BtnSit(332), BtnHorse(333), BtnStatus(334), BtnItems(335), BtnSkills(336), BtnTeam(337), BtnFaction(338), BtnPK(339), BtnExchange(340), BtnTreasure(341) — all 11 present |
| Buttons clickable | `RegisterClick()` resolves `root.Q(name)`, sets `pickingMode = Position`, registers `PointerDownEvent` → tap fires handler |
| Z-order | Button containers (`HotbarCenter`, `hud-skill-panel`, `hud-right-cluster`, `BtnTreasure`) appear after `BottomFrame` in DOM → rendered on top |

### REQ-4 — PC-proportional positioning — PASS

| Element | USS position | PC-derivation |
|---|---|---|
| Hotbar (slots 1–9) | `margin-left: 116px`, width 442px, 9×47px flex row | PC-scaled coordinates (comment: ×1.6/×1.25) |
| Skill panel (T/P) | `margin-left: 34px`, center crown region | Anchor-based, not raw multiply |
| Right cluster (6 toggle + 8 menu) | `left: 792px` | PC right-slot region |
| Bảo Vật | `left: 1132px` | Right end-cap |

Positions use anchor-based absolute layout with PC-derived scaled coordinates (comments reference `dc11ac12.ini` scale factors), not raw aspect-distorting multiply.

### REQ-5 — No regressions — PASS

| Check | Evidence |
|---|---|
| Chat above strip | `.hud-chat-panel`: `bottom: 135px` → above strip (strip height 131px at bottom: 0); 4px clearance |
| Hotkeys not overlapping | 9 slots × 47px + 1px margins in 442px-wide flex row → clean spread, no stacking |
| HUD EditMode green | Parent evidence: 13/13 passed (tasks C4, 0.65s) |
| Vision checks | Parent evidence (tasks C1–C3): frame continuous, all 8 menu + 6 toggle + Bảo Vật + T/P visible, no overlap, chat above strip |

## Task Completion Status

### Phase A — Frame asset production (3/3 ✓ DONE)
- [x] A1. Resolved via `jx-pc-resource-resolver`: hash `快捷栏.spr` = `ebb69f9b`, decoded 965×768 → cropped 863×91.
- [x] A2. Written to `Assets/UI/HUD/Art/bottom_frame_pc.png` + StreamingAssets copy.
- [x] A3. Vision-confirmed 10/10 clean.

### Phase B — USS/UXML wiring (4/4 ✓ DONE)
- [x] B1. `.hud-bottom-frame` rule: absolute, scale-to-fit, picking-mode Ignore.
- [x] B2. `<VisualElement name="BottomFrame" class="hud-bottom-frame"/>` first child of `BottomPanel`.
- [x] B3. `.hud-bottom-strip` transparent (old flat bg removed).
- [x] B4. Repositioned skill-panel/right-cluster/Bảo Vật.

### Phase C — Verify no regression (4/4 ✓ DONE)
- [x] C1. Recompile + fresh play mode + screenshot.
- [x] C2. Vision ui_diff_check + analyze_image: frame complete & continuous.
- [x] C3. Vision-confirmed hotkeys not overlapping, chat above, Bảo Vật in end-cap.
- [x] C4. HUD EditMode tests 13/13 passed.

### Phase D — Ship (2/2 ✓ DONE)
- [x] D1. Updated README + tasks file.
- [x] D2. Commit + push origin/dev.

### Follow-up (out of this milestone — NOT blockers)

Unchecked implementation task markers matching `^\s*- \[ \]`: **4 found, all under explicit "Follow-up (out of this milestone)" header.**

Exact unchecked lines:
1. `- [ ] Fine pixel-align each button to its frame slot well (currently approximate).` — visual polish
2. `- [ ] Lock circular toggle button aspect ratio to 1:1 (vision reported mild oval).` — visual polish
3. `- [ ] Decide whether to keep PC-art 左/右 labels in the crown or overlay T/P.` — product decision
4. `- [ ] Reconcile mobile chat panel style with PC chat input bar (separate change).` — explicitly separate change

These are not incomplete milestone tasks; they are explicitly scoped out and do not block archive. The change's milestone scope (frame art + wiring + positioning + regression-free) is fully delivered.

## Review Workload / PR Boundary Findings

- Combined diff for the two shipped commits: USS 79 insertions / 51 deletions + UXML 7 lines changed + 1 new art asset → **well within 400-line review budget**.
- No chained-PR strategy was needed; single shipped PR boundary.
- No scope creep observed: implementation touches only HUD frame/USS/UXML, matching the spec scope (no button-icon art, no functional behavior, no minimap/topbar changes).

## Test / Validation Evidence

Commands/evidence consumed during verification:
1. `read openspec/changes/add-bottom-bar-pc-frame/{proposal,spec,design,tasks}.md` — passed; artifacts coherent.
2. `read Assets/UI/HUD/GameHud.uss` — passed; `.hud-bottom-frame` rule with scale-to-fit + pickingMode Ignore confirmed.
3. `read Assets/UI/HUD/GameHud.uxml` — passed; `BottomFrame` element as first child of `BottomPanel` confirmed.
4. `read Assets/Scripts/UI/GameHudController.cs` — passed; 11 RegisterClick calls + RegisterClick impl confirmed.
5. `bash: ls Assets/UI/HUD/Art/bottom_frame_pc.png` — passed; 84170 bytes.
6. `bash: git log/show c0385c10c + 8681f1c2b` — passed; commits contain exactly the expected files.
7. `bash: git status --short (HUD files)` — passed; clean working tree, no drift.

Parent-collected evidence accepted (not rerun by this executor — Unity MCP not available):
- HUD EditMode category: 13/13 passed (0.65s).
- Vision checks: frame continuous, all icons visible, no overlap.
- Tasks file marks Phase A–D and D2 commit+push DONE.

## Strict TDD Compliance

Strict TDD is not configured in `openspec/config.yaml` for this change. This is a visual/UI frame change verified primarily via vision checks + HUD EditMode category, not strict RED/GREEN unit cycles. No strict-TDD blocker applies.

## Baseline Disclaimer

Full EditMode suite was not rerun by this executor. Parent evidence reports HUD EditMode category 13/13 passed. Known baseline failures in the broader suite (Backend/Auth, BaLang/CaiBang, Mount/Visual) are unrelated to this HUD frame change and do not affect REQ-5.

## Blockers

None.

## Residual Risks

- **Pixel alignment is approximate**: button containers are positioned to roughly match PC frame slot wells, but exact pixel-perfect alignment is deferred to follow-up. Mild visual offset may be visible.
- **Toggle button oval**: `.hud-round-btn` is 36×36px (square), but if the circular SPR icon inside doesn't perfectly center, vision reported a mild oval. Aspect-ratio lock to follow.
- **Label ambiguity**: PC frame art contains baked-in `左/右` (left/right) labels in the crown; mobile overlays `T/P` labels via `hud-skill-keys`. Whether to keep both or suppress the PC-art labels is an open product decision.
- **Chat panel style**: mobile chat uses a fixed bottom-left panel; PC uses a resizable window. Full reconciliation is a separate change.

## Final Verdict

**PASS.** The change is verified and ready for the SDD sync phase.
