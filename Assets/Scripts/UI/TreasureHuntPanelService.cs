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
            return new TreasureHuntPanelSnapshot { rows = System.Array.Empty<TreasureHuntPanelRow>() };
        }

        public static IReadOnlyList<TreasureHuntPanelRow> GetNearby(TreasureHuntService svc, int mapId, float x, float y)
        {
            return System.Array.Empty<TreasureHuntPanelRow>();
        }

        public static bool TryDig(TreasureHuntService svc, int playerId, int treasureId)
        {
            return false;
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
