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
            int currentMap = 1;
            int totalMaps = 0;
            string currentName = string.Empty;
            int posX = 0, posY = 0;
            if (svc != null)
            {
                totalMaps = svc.Count;
                var info = svc.GetMapInfo(currentMap);
                if (info != null) currentName = info.name ?? string.Empty;
            }
            return new MapPanelSnapshot
            {
                playerId = playerId,
                currentMapId = currentMap,
                currentMapName = currentName,
                currentPosX = posX,
                currentPosY = posY,
                totalMaps = totalMaps,
                unlockedMaps = 0,
                rows = System.Array.Empty<MapPanelRow>(),
            };
        }

        /// <summary>Dịch chuyển nhân vật tới map đích (stub — cần server handshake).</summary>
        public static bool TryTeleport(MapManager svc, int playerId, int targetMapId, int scrollId)
        {
            if (svc == null || playerId <= 0 || targetMapId <= 0) return false;
            return false;
        }

        /// <summary>Lấy các map lân cận hiện tại.</summary>
        public static IReadOnlyList<MapPanelRow> GetNearbyMaps(int currentMapId, int count)
        {
            int max = count <= 0 ? 8 : count;
            var rows = new List<MapPanelRow>();
            for (int i = 1; i <= max; i++)
            {
                rows.Add(new MapPanelRow(
                    mapId: currentMapId + i,
                    name: $"Map {currentMapId + i}",
                    type: (i % 5),
                    requiredLevel: 1 + (i * 5),
                    isUnlocked: i % 2 == 0,
                    isCurrent: i == 1,
                    distance: i * 100));
            }
            return rows;
        }

        /// <summary>Lọc theo loại bản đồ.</summary>
        public static IReadOnlyList<MapPanelRow> GetMapsByType(IReadOnlyList<MapPanelRow> source, int type)
        {
            if (source == null) return System.Array.Empty<MapPanelRow>();
            return source.Where(r => r.type == type).ToList();
        }

        /// <summary>Trả về đường dẫn icon cho map.</summary>
        public static string GetMapIconPath(int mapId)
        {
            if (mapId <= 0) return string.Empty;
            return $"UI/Maps/icon_{mapId:D4}.png";
        }
    }
}
