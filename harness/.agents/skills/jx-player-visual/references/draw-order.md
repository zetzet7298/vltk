# Draw-order tables & SortingOffset

The PC client decides, per facing direction, the back-to-front paint order of a
character's parts. This is what makes hair sit behind the head when facing away but
in front when facing toward the camera, weapons go behind the body when the back is
turned, etc. It is the single most important table for a believable avatar.

Source: `Settings/NpcRes/男主角贴图顺序表.txt` (male) and `女主角贴图顺序表.txt`
(female). Both are `.ini`-style: `DirN=<part ids, back to front>`. **Dir1..Dir8 are
identical between male and female**, so one `SortingOffset` implementation serves both.

## Part ids used in the table

```
-1 = shadow      0 = head     1 = hair      4 = shoulder   5 = body
 6 = left hand   7 = right hand   8 = left weapon   9 = right weapon
12 = horse front  13 = horse middle  14 = horse rear
```

## The 8 directions (male/female, Dir1..Dir8)

```
Dir1 (S ): -1,14,13, 1, 4, 9, 7, 5, 6,12, 8, 0
Dir2 (SW): -1,14,13, 9, 7, 4, 1, 5,12, 6, 8, 0
Dir3 (W ): -1, 9, 7,12,13,14, 5, 4, 1, 0, 6, 8
Dir4 (NW): -1, 9, 7,12,13, 5,14, 4, 1, 0, 8, 6
Dir5 (N ): -1,12,13, 8, 6, 5,14, 4, 1, 0, 7, 9
Dir6 (NE): -1, 8, 6,12,13, 5,14, 4, 1, 0, 9, 7
Dir7 (E ): -1, 8, 6,12,13,14, 5, 4, 1, 0, 9, 7
Dir8 (SE): -1,14,13, 4, 1, 8, 6, 5,12, 0, 9, 7
```

(The PC tables actually go to Dir16 to cover mounted/extra poses; the on-foot avatar
only needs Dir1..Dir8. `动作贴图顺序表.INI` holds per-action overrides — only needed
for special multi-weapon poses, ignore until required.)

## How SortingOffset reads it

`MalePlayerSpriteCatalog.SortingOffset(kind, direction)` finds the part id's index in
that direction's row and returns `index * 2`. Earlier index = painted earlier = lower
order = further back. The `* 2` leaves odd slots free so map/other layers can interleave
if ever needed. Parts not in the row fall back to `100 + partId` (stable, on top).

The visual then sets each renderer's order to `PlayerBaseSortingOrder() + offset`, so
the entire avatar is lifted above the map ceiling (see SKILL.md Bug 1) while preserving
internal layering.

## Direction mapping (input vector -> dir index)

`DirectionFromMove` converts a move vector to the dir index via the angle:

```
0=S, 1=SW, 2=W, 3=NW, 4=N, 5=NE, 6=E, 7=SE
```

E=6, NE=5, N=4, NW=3, W=2, SW=1, S=0, SE=7. Idle (zero vector) returns -1 -> Idle action.
Keep this mapping in sync with the SPR direction layout; the run SPRs are authored in
this exact order, so changing one without the other rotates the whole avatar.
