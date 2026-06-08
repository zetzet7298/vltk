// -----------------------------------------------------------------------------
// VLTK Mobile — Tong map entrance catalog fields/properties.
// PC sources:
// - script/tong/map/entrance_trap.lua
// - script/tong/addtongnpc.lua
// - script/tong/workshop/tongcolltask.lua
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed partial class PcTrapActionCatalogEntry
    {
        public const int TongMapEntranceTaskLpCountId = 1745;
        public const int TongMapEntranceDefaultCellX = 1718;
        public const int TongMapEntranceDefaultCellY = 3313;
        public const int TongMapEntranceBorderCellX = 1712;
        public const int TongMapEntranceBorderCellY = 3330;
        public const int TongMapEntranceBorderMapCopyId = 591;

        public string tongProductRegion;
        public int tongMapType = 1;
        public int tongMapTongId;
        public int tongPlayerTongId;
        public int tongPlayerTongIdTaskId;
        public int tongMapBan;
        public int tongExpireState;
        public int tongNoExpireWarning = 1;
        public string tongExpireDate;
        public int tongTaskLpCountId = TongMapEntranceTaskLpCountId;
        public int tongCurrentMapCopyId;
        public int tongTemplateMapId;
        public int tongDefaultEnterCellX = TongMapEntranceDefaultCellX;
        public int tongDefaultEnterCellY = TongMapEntranceDefaultCellY;
        public int[] tongEnterMapCopyIds;
        public int[] tongEnterCellXs;
        public int[] tongEnterCellYs;

        public bool IsTongMapEntrance => string.Equals(actionKind, "TongMapEntrance", StringComparison.OrdinalIgnoreCase);

        public Vector2 TongMapEnterWorldPosition(int mapCopyId)
            => CellToWorld(TongMapEnterCellX(mapCopyId), TongMapEnterCellY(mapCopyId));

        private int TongMapEnterCellX(int mapCopyId)
            => TongMapEnterCellAt(mapCopyId, tongEnterCellXs, TongMapEntranceBorderCellX,
                tongDefaultEnterCellX > 0 ? tongDefaultEnterCellX : TongMapEntranceDefaultCellX);

        private int TongMapEnterCellY(int mapCopyId)
            => TongMapEnterCellAt(mapCopyId, tongEnterCellYs, TongMapEntranceBorderCellY,
                tongDefaultEnterCellY > 0 ? tongDefaultEnterCellY : TongMapEntranceDefaultCellY);

        private int TongMapEnterCellAt(int mapCopyId, int[] values, int borderValue, int defaultValue)
        {
            if (tongEnterMapCopyIds != null && values != null)
            {
                int count = Math.Min(tongEnterMapCopyIds.Length, values.Length);
                for (int i = 0; i < count; i++)
                    if (tongEnterMapCopyIds[i] == mapCopyId && values[i] > 0)
                        return values[i];
            }
            return mapCopyId == TongMapEntranceBorderMapCopyId ? borderValue : defaultValue;
        }
    }
}
