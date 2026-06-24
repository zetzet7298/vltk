# The sorting model — how objects layer correctly

This documents the complete sorting system that produces correct isometric draw order in
the Unity renderer. The original JX engine uses a spatial binary tree (KIpoTree); the Unity
port achieves equivalent results with three mechanisms working together.

## The original engine: KIpoTree spatial binary tree

Source: `KIpoTree.cpp`, `KIpotBranch.cpp`, `KIpotLeaf.cpp` (recovered from the out-of-scope
`jxwin-kinnox` engine tree — these `.cpp` files are NOT under
`/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem`; the in-scope client ships only `represent3.dll`).
The behavior below is validated against the working Unity port, so trust it; to re-derive
from binary, escalate to `reverse-engineering` against `represent3.dll`.

### Tree structure
- Each branch has a split line (`m_oHeadPoint → m_oEndPoint`) and two child slots.
- Objects are inserted via `AddLeafLine` (line objects) or `AddLeafPoint` (point objects).
- `SM_Relation_PointLine` determines whether an object is UP or DOWN relative to the split.
- The tree recursively subdivides space.

### Rendering (PaintObjectLayer)
```cpp
void KIpotBranch::PaintObjectLayer(RECT* pRepresentArea)
{
    for (int i = 0; i < 2; i++)
    {
        if (IS_BRANCH(i))
            m_pSubBranch[i]->PaintObjectLayer(pRepresentArea);
        else if (m_pLeafs[i])
            KIpotLeaf_PaintObjectLayer(m_pLeafs[i], pRepresentArea);

        if (i == 0)
        {
            if (m_pFirstObject)
            {
                PaintABranchObject(m_pFirstObject, pRepresentArea);
                for (int j = 0; j < m_nNumObjects; j++)
                    PaintABranchObject(m_pObjectList[j], pRepresentArea);
            }
        }
    }
}
```

Traversal order: child[0] (UP/far) → branch objects → child[1] (DOWN/near). This is
standard in-order traversal of a BSP tree, producing correct back-to-front ordering for
isometric scenes.

### Three render passes
The engine calls `Paint` three times from `KScenePlaceC::Draw`:
1. `IPOT_RL_COVER_GROUND` — ground cover/decals (grass, roads)
2. `IPOT_RL_OBJECT` — built-in objects (houses, trees, gates)
3. `IPOT_RL_INFRONTOF_ALL` — foreground effects

## The Unity port: three-mechanism sorting

### Mechanism 1: Layer separation via sortingOrder

| Layer | sortingOrder | Contents |
|-------|-------------|----------|
| Ground | -1000 | Terrain tiles (always beneath everything) |
| Cover | 0 | Flat ground decals: grass, roads, stone patterns |
| Builtin | 1000 + counter | Structures: houses, trees, gates, walls |
| Player | 5000 | Player character parts (always above map art) |

These values are intentionally far apart so there is NO chance of layer bleed. Cover objects
(sortingOrder=0) can NEVER draw above any builtin object (sortingOrder≥1000), regardless of
their Y positions. This eliminates the "grass on rooftop" bug entirely.

### Mechanism 2: File-order counter for builtins

Each builtin object gets a unique, monotonically-increasing `sortingOrder` starting at 1000:

```csharp
private int _builtinSortCounter;

// In RenderBuiltinObjects, for each object:
sr.sortingOrder = BuiltinSortingOrder + (_builtinSortCounter++);
```

The counter resets to 0 when the map is cleared. Regions are iterated col-by-row
(back-to-front in the isometric view), and within each region the objects are stored in the
same order as the KIpoTree's in-order traversal. This means the counter value exactly
reproduces the original engine's spatial-tree draw order.

**Why not Y-sort?** Pure Y-sorting breaks multi-piece structures. Consider the 牌坊 gate:
4 pillars + 4 crossbeam segments + 4 top pieces. The near pillar and far pillar have similar
Y values, but the crossbeam between them must draw BEHIND the near pillar and IN FRONT OF
the far pillar. Y-sorting cannot express this — the beam has one Y value but needs two
different depth relationships. The file-order counter preserves the authored ordering from
the KIpoTree, which handles this correctly.

### Mechanism 3: CustomAxis world-Y sort as tiebreaker

```csharp
// In SandboxManager.FrameCameraOnMap():
cam.transparencySortMode = CustomAxis;
cam.transparencySortAxis = new Vector3(0f, 1f, 0f);
```

This tells Unity to sort sprites at the same `sortingOrder` by their world Y position:
higher Y = drawn later = appears in front. This handles:
- Cover-vs-cover ordering (grass patches at different Y positions)
- Ground tile ordering within the same sortingOrder

CustomAxis does NOT replace the file-order counter — it only resolves ties within the same
sortingOrder value. The counter is the primary ordering mechanism for builtins.

## How the three mechanisms work together

For two builtin objects A and B:
- If `A.sortingOrder < B.sortingOrder`: A draws behind B (counter determines order)
- If `A.sortingOrder == B.sortingOrder`: higher world-Y draws in front (CustomAxis tiebreak)

For cover vs builtin:
- Cover (sortingOrder=0) always draws behind builtin (sortingOrder≥1000). Period.
- No amount of Y-position difference can override this.

For player vs map:
- Player (sortingOrder=5000) always draws above all map art (max builtin ≈ 3645).
- Player-behind-building occlusion is a separate feature not yet implemented.

## The int16 overflow trap

Unity's `sortingOrder` is stored internally as **int16** (range -32768..32767). Any value
outside this range wraps/truncates silently. The old approach `sortingOrder = screenY * 2`
produced values up to ~100000, which overflowed. `Mathf.Clamp(±32000)` "fixed" the overflow
but pinned 3580 objects at identical values, destroying all ordering information.

The current approach avoids this entirely:
- Minimum sortingOrder: -1000 (ground)
- Maximum sortingOrder: 1000 + 2645 (builtin) = 3645, or 5000 + 16 (player) = 5016
- All values comfortably within int16 range.

## Camera configuration

The `SandboxPlayerController.FollowCamera` method configures the camera every frame:
```csharp
cam.orthographic = true;
cam.orthographicSize = Mathf.Max(1f, followOrthoSize); // default 480
cam.transparencySortMode = CustomAxis; // set once in SandboxManager
```

`CameraRigService` is a pure C# class (no MonoBehaviour) that applies focus/zoom to the
Unity Camera each frame. It reasserts position every frame, so manual camera moves in play
mode will be overridden on the next frame.
