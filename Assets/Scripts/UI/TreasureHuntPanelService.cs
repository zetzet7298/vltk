// -----------------------------------------------------------------------------
// VLTK Mobile — Treasure Hunt Panel Service (Săn Kho Báu)
// Dựng snapshot cho UI săn kho báu. Kết hợp TreasureHuntService + vị trí map.
// Vietnamese: "Săn Kho Báu", "Kho báu gần", "Đào", "Phát hiện", "Khoảng cách".
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
            var snap = new TreasureHuntPanelSnapshot
            {
                playerId = playerId,
                currentMapId = currentMapId,
                posX = posX,
                posY = posY,
                totalTreasures = svc?.Count ?? 0,
                rows = Array.Empty<TreasureHuntPanelRow>(),
            };
            if (svc == null) return snap;
            var rows = new List<TreasureHuntPanelRow>();
            int nearby = 0;
            foreach (var entry in EnumerateAll(svc))
            {
                float dx = entry.posX - posX;
                float dy = entry.posY - posY;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (entry.mapId == currentMapId && dist <= entry.detectionRange) nearby++;
                bool canDig = svc.CanDig(entry.treasureId, 50);
                rows.Add(new TreasureHuntPanelRow(entry.treasureId, entry.mapId, entry.mapName, entry.posX, entry.posY, entry.itemName, entry.itemCount, dist, canDig, entry.detectionRange));
            }
            snap.nearbyTreasures = nearby;
            snap.rows = rows;
            return snap;
        }

        public static IReadOnlyList<TreasureHuntPanelRow> GetNearby(TreasureHuntService svc, int mapId, float x, float y)
        {
            if (svc == null) return Array.Empty<TreasureHuntPanelRow>();
            var list = new List<TreasureHuntPanelRow>();
            foreach (var entry in EnumerateAll(svc))
            {
                if (entry.mapId != mapId) continue;
                float dx = entry.posX - x;
                float dy = entry.posY - y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (dist > entry.detectionRange) continue;
                list.Add(new TreasureHuntPanelRow(entry.treasureId, entry.mapId, entry.mapName, entry.posX, entry.posY, entry.itemName, entry.itemCount, dist, svc.CanDig(entry.treasureId, 50), entry.detectionRange));
            }
            return list;
        }

        public static bool TryDig(TreasureHuntService svc, int playerId, int treasureId)
        {
            if (svc == null || playerId <= 0 || treasureId <= 0) return false;
            return svc.TryDig(treasureId, playerId);
        }

        private static IEnumerable<TreasureEntry> EnumerateAll(TreasureHuntService svc)
        {
            var field = typeof(TreasureHuntService).GetField("_reg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(svc) is TreasureRegistry reg)
            {
                return reg.All;
            }
            return Array.Empty<TreasureEntry>();
        }
    }

    public class TreasureEntry
    {
        public int treasureId;
        public int mapId;
        public string mapName;
        public float posX;
        public float posY;
        public string itemName;
        public int itemCount;
        public float detectionRange;
    }

    public class TreasureRegistry
    {
        public IEnumerable<TreasureEntry> All => Array.Empty<TreasureEntry>();
    }
}
