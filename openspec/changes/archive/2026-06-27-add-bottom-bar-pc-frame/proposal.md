# Proposal — Bottom Bar PC-Parity Frame (HUD-002)

> Change: `add-bottom-bar-pc-frame`
> Status: proposed
> Owner: el Gentleman (inline SDD; subagent runner unstable)
> Skill: `jx-pc-port-rule`, `jx-hud-port`

## Problem

The mobile bottom toolbar (`Assets/UI/HUD/GameHud.uxml` `BottomPanel` + `GameHud.uss`
`.hud-bottom-strip`) currently uses a **flat placeholder frame**: a dark rectangle with a
single stretched `bottom_bar_bg.png` texture and ad-hoc green borders. The PC reference
(`/var/www/vltk-mobile/pc-evidence/hud/bottom_bar.png`, 933×120, 1024×768 era) is an
**ornate antique-silver / pewter filigree housing** with Asian cloud scrollwork, a raised
center "crown" over the T/P skill slots, double filigree bands framing the right menu, and
a circular right end-cap housing the Bảo Vật button.

A `vision ui_diff_check` (PC-expected vs mobile-actual) scored the visual match at **~35%**,
with the containing frame flagged as the critical gap. The user explicitly called out "cái
khung chứa" (the containing frame) as "khác xa nhất" (the most divergent element).

> Note: a prior commit (`2bc2f3128`) already replaced the 14 button **icons** with real PC
> SPR art and fixed hotkey overlap. Button icons are therefore NOT in scope; only the
> **frame + positioning** remain.

## Vision / Outcome

The mobile bottom bar visually matches the PC filigree housing: ornate silver frame, raised
center crown over T/P, banded right menu, circular Bảo Vật end-cap — and the hotkey/skill/
menu/toggle elements are repositioned to match PC coordinates (scaled 4:3 → 16:9 via anchors,
no raw aspect-ratio distortion per README §5).

## Scope

**In scope:**
1. Locate and decode the real PC toolbar **filigree** SPR asset(s) to PNG (frame, crown,
   bands, end-cap).
2. Composite the filigree as a layered background in the mobile `.hud-bottom-strip`
   (USS), preserving button hit areas and existing click wiring.
3. Reposition hotkey slots 1–9, T/P skill slots, toggle row, menu row, and Bảo Vật to
   PC-proportional coordinates (anchor-based, aspect-ratio preserved).
4. Keep mobile-only chat panel/tabs/warning positioned cleanly above the strip (no overlap).

**Out of scope:**
- Button icon art (done in `2bc2f3128`).
- Functional behavior of buttons (click handlers unchanged).
- Minimap, topbar, character panels.
- Animation / state frames (using frame 0 = normal idle, consistent with prior work).

## Evidence / Source

- **PC INI**: `pak_unpacked/1024/unknown/dc11ac12.ini` — `[Main]` references
  `快捷栏(800).spr` (`800×90`, `Left=0,Top=400` in 1024×768). Button coords documented in
  README §6.2.
- **PC reference screenshot**: `pc-evidence/hud/bottom_bar.png` (933×120).
- **Mobile current screenshot**: `pc-evidence/hud/bb_crop.png` + clipboard capture.
- **Toolbar SPR candidates** (decoded to `/tmp/cand_*.png`):
  - `188b91a2.spr` / `fce2191e.spr` (973×104, `1024/unknown`) — ornate filigree w/ end-caps
    + raised crown profile (top candidates for the in-game toolbar housing).
  - `82615d95.spr` / `52cc8143.spr` (800×104) — plain flat bars (800-res variant, rejected).
  - **Open asset question**: confirm which 973×104 SPR is the in-game toolbar vs a
    character-select frame, OR whether the in-game bar is composited from multiple segment
    SPRs (left band / crown / right band / end-cap). Resolved in `sdd-explore`/`design`.

## Risks

- **R1 — Wrong SPR**: the ornate 973×104 candidates may be a character-select frame, not
  the in-game toolbar. Mitigation: pixel-compare decoded SPRs against `bottom_bar.png`
  before committing.
- **R2 — Aspect distortion**: naively stretching an 800×90 (4:3) frame across a 16:9 strip
  will distort scrollwork. Mitigation: anchor-based layout (README §5), keep frame art
  aspect-locked; use `scale-to-fit` / 9-slice where appropriate.
- **R3 — Click-area regression**: layering a background must not break `RegisterClick`
  wiring. Mitigation: filigree is `pickingMode: Ignore` background; buttons stay on top.
- **R4 — Review budget (400 lines)**: frame USS rewrite + reposition may approach budget.
  If forecast > 400 changed lines, pause for chained-PR decision (per preflight
  `auto-forecast`).

## Open Questions (for design phase)

1. Is the PC toolbar ONE SPR or multiple composited segments?
2. Should the mobile frame be a single `background-image` or layered per-segment for
   responsive scaling?
3. How to handle the chat panel that PC places differently (PC chat is a separate resizable
   window; mobile has a fixed bottom-left panel)?

## Non-Goals

- Replicating PC's resizable chat window.
- Porting the character-select frame (separate screen).
- Touch-specific affordances (sizing for fingers already handled; not changing).
