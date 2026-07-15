# Deck Và Timeline

| Trường | Giá trị |
|---|---|
| Mục đích | Mô hình hóa card/upgrade mà không gắn design proposal thành DHCD fact |
| Trạng thái | `design` |
| Owner / reviewer | Gameplay owner / product owner |
| Cập nhật | 2026-07-15 |

## Evidence boundary

DHCD có `LevelRandomSkillCtrl` và flow choice/reroll theo evidence map, nhưng card count, weight, cost, cap và global pause chưa recovered đủ. Declaration có per-player state/waiting-list; thứ tự serialize runtime chưa được chứng minh. Không ghi các con số hoặc ordering đó như parity.

## To-be schema

```yaml
Card:
  id: stable-card-id
  source_skill_id: jx-skill-id-or-null
  kind: skill|buff|support
  scope: run
  base_weight: data
  weight_curve_by_owned_copy: versioned-data
  copy_curve_by_owned_copy: versioned-data
  max_copies: data
  cost: data
  prerequisites: data
  stacking: replace|add|multiply|unique
  permanent_upgrade_id: optional-reference
  visual_asset_id: verified-jx-asset
```

`Card` chỉ có hiệu lực trong run và không ghi permanent progression. Buff/support permanent được nâng ngoài run bằng transaction server riêng; card projection của buff/support chỉ eligible khi permanent cap tương ứng còn chỗ và vẫn chỉ tạo hiệu lực run-local. Mọi card trong match phải trỏ tới visual JX đã verified.

`base_weight`, `weight_curve_by_owned_copy`, `copy_curve_by_owned_copy`, `max_copies`, `cost` và cap phải là data versioned. Effective weight và số copy/offer còn lại phải giảm hoặc giữ nguyên khi số copy đã sở hữu tăng. Exact curve/number bị chặn cho tới khi `R-DHCD-001` chạy xong và corpus được cập nhật; nếu reverse vẫn inconclusive, rule mới chỉ được ship sau ADR product đã approve dựa trên reverse evidence. Không hard-code default, không dùng ADR thay bước reverse và không gắn nhãn DHCD parity.

## Timeline

- `Mode + difficulty + seed` chọn timeline version.
- Tier event có `eligible_card_query`, `offer_count`, `reroll_policy`, `expiry`.
- P0 product design tạm thời: choice modal được serialize theo player; queue event nếu player đang ở modal. Đây không phải recovered DHCD ordering và phải quay lại sau `R-DHCD-002`.
- Trong co-op, pause/choice ownership phải có ADR-006 trước implementation.

## Acceptance

- Seed + deck + timeline version tái tạo cùng offer order.
- Không offer card vượt cap/prerequisite; buff/support permanent đã đạt cap không xuất hiện.
- Effective weight và số copy/offer còn lại không tăng khi owned-copy tăng, và cùng data version cho cùng offer order.
- Run card không mutate permanent skill/buff/support progression.
- Reroll idempotent, cost và reward proposal nằm trong replay.
- Modal queue không mất event khi reconnect.

## Open reverse gates

Reverse `LevelRandomSkillCtrl` và `BattleLearnSkillCtrl` cho event order, request/response và per-player state; không suy ra global timeScale từ nhánh malformed.
