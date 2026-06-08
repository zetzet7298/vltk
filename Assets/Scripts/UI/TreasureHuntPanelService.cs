// -----------------------------------------------------------------------------
// VLTK Mobile — Treasure Hunt Panel Service (Săn Kho Báu)
// Dựng snapshot cho UI săn kho báu từ TreasureHuntService.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct TreasureHuntPanelRow
    {
        public readonly int treasureId;
        public readonly int mapId;
        public readonly string mapName;
        public readonly float posX;
        public readonly float posY;
        public readonly string itemName;
        public readonly int itemCount;
        public readonly float distance;
        public readonly bool canDig;
        public readonly float detectionRange;

        public TreasureHuntPanelRow(int treasureId, int mapId, string mapName, float posX, float posY, string itemName, int itemCount, float distance, bool canDig, float detectionRange)
        {
            this.treasureId = treasureId;
            this.mapId = mapId;
            this.mapName = mapName;
            this.posX = posX;
            this.posY = posY;
            this.itemName = itemName;
            this.itemCount = itemCount;
            this.distance = distance;
            this.canDig = canDig;
            this.detectionRange = detectionRange;
        }
    }

    public sealed class TreasureHuntPanelSnapshot
    {
        public int playerId;
        public int currentMapId;
        public float posX;
        public float posY;
        public int nearbyTreasures;
        public int totalTreasures;
        public IReadOnlyList<TreasureHuntPanelRow> rows;
    }

    public static class TreasureHuntPanelService
    {
        public const string LabelTreasure = "Săn Kho Báu";
        public const string LabelNearby = "Kho báu gần";
        public const string LabelDig = "Đào";
        public const string LabelDetect = "Phát hiện";
        public const string LabelDistance = "Khoảng cách";

        public static TreasureHuntPanelSnapshot BuildSnapshot(TreasureHuntService svc, int playerId, int currentMapId, float posX, float posY)
        {
            if (svc == null)
                return new TreasureHuntPanelSnapshot { playerId = playerId, currentMapId = currentMapId, posX = posX, posY = posY, rows = Array.Empty<TreasureHuntPanelRow>() };

            var rows = GetNearby(svc, currentMapId, posX, posY);
            return new TreasureHuntPanelSnapshot
            {
                playerId = playerId,
                currentMapId = currentMapId,
                posX = posX,
                posY = posY,
                nearbyTreasures = rows.Count,
                totalTreasures = svc.Count,
                rows = rows
            };
        }

        public static IReadOnlyList<TreasureHuntPanelRow> GetNearby(TreasureHuntService svc, int mapId, float x, float y)
        {
            if (svc == null)
                return Array.Empty<TreasureHuntPanelRow>();
            var entries = svc.GetByMap(mapId);
            var rows = new List<TreasureHuntPanelRow>(entries.Count);
            foreach (var e in entries)
            {
                float dx = e.posX - x;
                float dy = e.posY - y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                float range = e.detectionRange > 0 ? e.detectionRange : 0;
                if (range <= 0 || dist <= range)
                    rows.Add(ToRow(e, dist));
            }
            return rows;
        }

        public static bool TryDig(TreasureHuntService svc, int playerId, int treasureId)
        {
            if (svc == null || playerId <= 0 || treasureId <= 0)
                return false;
            return svc.CanDig(treasureId, int.MaxValue);
        }

        private static TreasureHuntPanelRow ToRow(PcTreasureHuntEntry e, float distance)
        {
            return new TreasureHuntPanelRow(e.treasureId, e.mapId, $"Bản đồ #{e.mapId}", e.posX, e.posY,
                $"Vật phẩm #{e.itemId}", e.itemCount, distance, true, e.detectionRange);
        }
    }
}
