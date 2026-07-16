# Domain: Thế giới, map và di chuyển

## Định danh và phạm vi

- Domain ID: `DOM-WMM`; DRI: Gameplay/World; reviewer: Backend/QA; sâu P1 map 53, P2 world catalog.
- Sở hữu map identity, spawn/revive, tọa độ, barrier, region/AOI, move/reconcile, channel/map transfer.

## Bằng chứng as-is

- `EVID-0046`: `KSubWorld.cpp:168-299` có đổi Map/Mps, khoảng cách và test barrier; `:495-588` load map.
- `EVID-0047`: `KSubWorld.cpp:856-990` chuyển region cho NPC/object/missile/player; `:1057` sync data.
- `EVID-0048`: `KPlayer.cpp:602-668` có walk/turn/select; `KNpcFindPath.cpp` tồn tại thuật toán tìm đường. Đây không chứng minh cùng hệ tọa độ ở mobile.
- `CON-0002` và `GAP-005`: Ba Lăng canonical là mapId 53; đường remap 79 là false parity phải loại khỏi production, còn extraction/runtime golden map 53 giữ `BLOCKED` tới khi pin manifest.

## Invariant và contract

- Server quyết định position/collision/AOI; client prediction luôn reconcile theo server tick/revision.
- Position dùng integer/fixed-point; công thức scale/rounding cụ thể BLOCKED `[CẦN XÁC NHẬN]`; owner Gameplay; gỡ block bằng source trace + PC runtime golden + reviewer.
- Command: `MoveIntent`, `StopMove`, `EnterWorld`, `TransferRequest`; event: `PositionDelta`, `MoveRejected`, `RegionChanged`, `TransferPrepared/Committed`, `WorldSnapshot`.
- Transfer giữ character/session và party affinity; không nhận command gameplay giữa prepare và destination snapshot.
- Catalog P2 phải có owner cho mọi map/region/barrier/spawn/portal đã phát hiện, kể cả map chưa mở.

## Nghiệm thu

- `TEST-WMM-001` P1: map 53 spawn, barrier, walk/reconcile deterministic; không xuyên vật cản ở RTT 100 ms.
- `TEST-WMM-002` P1: collision/coordinate map 53 phải khớp canonical extraction và PC runtime golden; không được alias/remap 53->79.
- `TEST-WMM-003` P2: property/E2E test chuyển region, giữ `channel_epoch` và reconnect giữa transfer.
