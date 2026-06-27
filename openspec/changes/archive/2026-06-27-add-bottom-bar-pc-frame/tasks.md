# Tasks — Bottom Bar PC-Parity Frame (HUD-002)

> Change: `add-bottom-bar-pc-frame`
> Ordered implementation checklist. Tick `- [x]` on completion.

## Phase A — Frame asset production  ✓ DONE
- [x] A1. **Resolved via skill `jx-pc-resource-resolver`**: hash `快捷栏.spr` (GBK) = `ebb69f9b`,
      found at `updatejx08/unknown/ebb69f9b.spr`. Decoded (965×768 overlay) + cropped toolbar
      region (863×91, aspect 9.48, 92% transparent).
- [x] A2. Written to `Assets/UI/HUD/Art/bottom_frame_pc.png` + StreamingAssets copy.
- [x] A3. Vision-confirmed 10/10 clean (both end-caps + crown + bands, no contamination).

## Phase B — USS / UXML wiring  ✓ DONE
- [x] B1. `.hud-bottom-frame` USS rule: absolute, natural aspect (863x91 art scaled by height →
      1156x122 box), `scale-to-fit`, `picking-mode: Ignore`.
- [x] B2. `<VisualElement name="BottomFrame" class="hud-bottom-frame"/>` first child of
      `BottomPanel`.
- [x] B3. `.hud-bottom-strip` now transparent container (removed old flat bg + green
      `bottom_bar_bg.png`/`toolbar_bg.png`); button containers (hotbar, skill-panel) made
      transparent so the frame filigree shows.
- [x] B4. Repositioned: skill-panel into center crown (margin-left 86), right-cluster
      absolute (left 724, over right slots), Bảo Vật into right end-cap (left 1086).

## Phase C — Verify no regression  ✓ DONE (frame milestone)
- [x] C1. Recompile + fresh play mode + screenshot bottom strip.
- [x] C2. `vision ui_diff_check` + `analyze_image`: frame complete & continuous; all 8 menu
      icons + 6 toggle icons + Bảo Vật + T/P visible over the frame.
- [x] C3. Vision-confirmed hotkeys not overlapping, chat above strip, Bảo Vật in end-cap.
- [x] C4. HUD EditMode tests 13/13 passed (0.65s).

## Phase D — Ship  ✓ DONE
- [x] D1. Updated README §6.2 frame-art provenance + this tasks file.
- [x] D2. Commit + push origin/dev.

## Follow-up (out of this milestone)
- [ ] Fine pixel-align each button to its frame slot well (currently approximate).
- [ ] Lock circular toggle button aspect ratio to 1:1 (vision reported mild oval).
- [ ] Decide whether to keep PC-art `左/右` labels in the crown or overlay `T/P`.
- [ ] Reconcile mobile chat panel style with PC chat input bar (separate change).
