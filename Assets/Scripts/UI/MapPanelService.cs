// -----------------------------------------------------------------------------
// VLTK Mobile — Map Panel Service (Bản đồ thế giới)
// UI service: dựng snapshot các bản đồ lân cận, mở khóa, dịch chuyển.
// PC reference: maplist.ini, maptraffic.ini, scroll.txt (cuộn dịch chuyển).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>Một dòng trong panel bản đồ.</summary>
    public readonly struct MapPanelRow
    {
        public readonly int mapId;
        public readonly string name;
        public readonly int type; // 0=thành, 1=vùng, 2=đồng, 3=hang, 4=chiến trường, 5=bang
        public readonly int requiredLevel;
        public readonly bool isUnlocked;
        public readonly bool isCurrent;
        public readonly int distance;

        public MapPanelRow(int mapId, string name, int type, int requiredLevel, bool isUnlocked, bool isCurrent, int distance)
        {
            this.mapId = mapId;
            this.name = name ?? string.Empty;
            this.type = type;
            this.requiredLevel = requiredLevel;
            this.isUnlocked = isUnlocked;
            this.isCurrent = isCurrent;
            this.distance = distance;
        }
    }

    /// <summary>Snapshot toàn bộ panel bản đồ.</summary>
    public sealed class MapPanelSnapshot
    {
        public int playerId;
        public int currentMapId;
        public string currentMapName;
        public int currentPosX;
        public int currentPosY;
        public int totalMaps;
        public int unlockedMaps;
        public IReadOnlyList<MapPanelRow> rows;
    }

    /// <summary>Dịch vụ UI: panel bản đồ thế giới.</summary>
    public static class MapPanelService
    {
        public const string Title = "Bản Đồ";
        public const string LabelCurrent = "Vị trí hiện tại";
        public const string LabelTeleport = "Dịch chuyển";
        public const string LabelUnlocked = "Mở khóa";
        public const string LabelLocked = "Chưa mở";
        public const string LabelRequiredLevel = "Yêu cầu cấp";

        /// <summary>Dựng snapshot bản đồ cho player.</summary>
        public static MapPanelSnapshot BuildSnapshot(MapManager svc, int playerId)
        {
            return new MapPanelSnapshot { rows = System.Array.Empty<MapPanelRow>() };
        }

        public static bool TryTeleport(MapManager svc, int playerId, int targetMapId, int scrollId)
        {
            return false;
        }

        public static IReadOnlyList<MapPanelRow> GetNearbyMaps(int currentMapId, int count)
        {
            return System.Array.Empty<MapPanelRow>();
        }

        public static IReadOnlyList<MapPanelRow> GetMapsByType(IReadOnlyList<MapPanelRow> source, int type)
        {
            if (source == null) return System.Array.Empty<MapPanelRow>();
            var result = new List<MapPanelRow>();
            foreach (var row in source)
                if (row.type == type) result.Add(row);
            return result;
        }

        public static string GetMapIconPath(int mapId)
        {
            // PC map icon convention: icon_<4-digit zero-padded mapId>.
            return $"map/icon_{mapId:D4}";
        }

    }
}
