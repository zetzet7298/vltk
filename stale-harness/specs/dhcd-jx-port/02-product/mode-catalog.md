# Mode Catalog Và Arena Selection

| Trường | Giá trị |
|---|---|
| Mục đích | Tách mode target khỏi mode đã được reverse, và chọn arena bằng geometry evidence |
| Trạng thái | `design` |
| Owner / reviewer | Product owner / gameplay reviewer |
| Cập nhật | 2026-07-15 |

## Mode matrix

| Mode | Phase | Contract | Trạng thái evidence |
|---|---|---|---|
| Normal solo | P0 pilot | Một player, wave, card/reroll, reward, replay | Target product; `NormalLevelLogic.IsMultiPlayer` không chứng minh solo |
| Normal co-op | P1 | P1+ room/relay/verify, deterministic input | Chưa recovered; phải làm sau ADR wave ownership |
| Arena candidate | P0 prerequisite | Một map JX có Region_C/collision/bounds/minimap golden | `yanwuchang`, `jingjichang`, `shiliantang` mới là candidate names |
| PvP/royal | P3 | Chỉ mở khi `R-DHCD-008`, product ADR và server/security design đều đạt gate | Chưa recovered; feature flag off |
| Boss/escort/tower | P2 | Mode-specific evidence, AI, map và reward sau `R-DHCD-009` | Chưa recovered; feature flag off |

## Arena selection gate

Mỗi candidate phải có:

- absolute path của `Region_C` và các map data liên quan;
- map ID, bounds, tile/height/collision conversion;
- xác nhận load-order/pack winner;
- minimap và visual golden;
- spawn points, walkable mask, camera bounds;
- Unity runtime test trên thiết bị portrait.

Chỉ candidate đạt tất cả gate mới được gọi là `pilot arena`. HTML/minimap name hoặc script dungeon một mình không chứng minh collision/winner.

## Queue ưu tiên

Audit theo thứ tự bắt buộc `yanwuchang` -> `jingjichang` -> `shiliantang`. Chỉ được đổi queue bằng ADR đã approve, kèm evidence candidate trước bị thiếu/corrupt/không có Region_C winner; patch mới hơn tự nó không đủ để đổi thứ tự. Kết quả ghi ở [arena-candidate-audit](../05-jx-parity/arena-candidate-audit.md).

## Acceptance

- [ ] Mọi candidate có audit row và rejection/selection reason.
- [ ] Chỉ một arena có Region_C/collision golden được bật cho P0.
- [ ] Mode chưa đủ evidence giữ feature flag off và ledger status đúng.
