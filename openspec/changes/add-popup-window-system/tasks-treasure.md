# Tasks — Treasure Window (HUD-003 slice 3) — BtnTreasure

> Slice 3 of `add-popup-window-system`. Reuses PopupWindow/PopupManager base.
> Vietnamese UI. Source of truth: existing PC manifest service `TreasureMallPanelService`
> (`9e5f75d1` Kỳ Trân Các, `1463f852` Giỏ hàng, `b54fbe43` Rương báu).

## Research / source evidence
- [x] Loaded required port/resource skills: `jx-pc-port-rule`, `jx-hud-port`, `jx-pc-resource-resolver`.
- [x] External research requirement satisfied: Exa searched/fetched Unity UI Toolkit modal/inventory best practices; DeepWiki attempted public inventory repo but repo was not indexed.
- [x] Current Unity source mapped: `GameHudController.OnTreasureClick` was log-only; `TreasureMallPanelService`, `MallPanelService`, `TreasureHuntPanelService`, `MallService`, and `TreasureHuntService` already exist.

## Implementation
- [x] Add `TreasureContent` implementing `IPopupContent` + `IPopupLayoutHint`, title `Bảo Vật`.
- [x] Render 3 PC sections: `Kỳ Trân Các`, `Giỏ Hàng`, `Rương Báu`.
- [x] Render PC-derived control manifest rows with Vietnamese labels and source IDs.
- [x] Add `Treasure.uss` and link it from `GameHud.uxml`.
- [x] Wire `BtnTreasure` through `PopupManager.Instance.Show(new TreasureContent(...))`.

## Verification
- [x] Unity refresh/compile completed; no new C# compile errors observed.
- [x] `run_tests(mode="EditMode", category_names=["Popup"])` → 34/34 passed.

## Follow-up (NOT this slice)
- Real mall purchase/cart mutation.
- Real treasure chest betting/spin/reward flow.
- Exact PC art reconstruction for Kỳ Trân Các / Rương Báu internal buttons if/when gameplay is implemented.
